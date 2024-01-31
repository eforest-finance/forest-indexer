using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class OfferRemovedLogEventProcessor : OfferLogEventProcessorBase<OfferRemoved>
{
    private readonly IAElfIndexerClientEntityRepository<OfferInfoIndex, LogEventInfo> _nftOfferIndexRepository;
    private readonly ILogger<AElfLogEventProcessorBase<OfferRemoved, LogEventInfo>> _logger;

    public OfferRemovedLogEventProcessor(ILogger<OfferRemovedLogEventProcessor> logger, IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo> nftActivityIndexRepository,
        IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> nftInfoIndexRepository,
        IAElfIndexerClientEntityRepository<OfferInfoIndex, LogEventInfo> nftOfferIndexRepository,
        IAElfIndexerClientEntityRepository<ProxyAccountIndex, LogEventInfo> proxyAccountIndexRepository,
        INFTInfoProvider infoProvider,
        INFTOfferProvider offerProvider,
        ICollectionProvider collectionProvider,
        ICollectionChangeProvider collectionChangeProvider,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        INFTOfferChangeProvider nftOfferChangeProvider) : base(logger, objectMapper,
        nftActivityIndexRepository, nftInfoIndexRepository, proxyAccountIndexRepository, infoProvider, offerProvider,collectionProvider,
        collectionChangeProvider,
        contractInfoOptions,
        nftOfferChangeProvider)
    {
        _logger = logger;
        _nftOfferIndexRepository = nftOfferIndexRepository;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).NFTMarketContractAddress;
    }

    protected override async Task HandleEventAsync(OfferRemoved eventValue, LogEventContext context)
    {
        _logger.Debug("OfferRemovedLogEventProcessor-1 {context}",JsonConvert.SerializeObject(context));
        _logger.Debug("OfferRemovedLogEventProcessor-2 {eventValue}",JsonConvert.SerializeObject(eventValue));

        
        var mustQuery = new List<Func<QueryContainerDescriptor<OfferInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.ChainId).Value(context.ChainId)));
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.BizSymbol).Value(eventValue.Symbol)));
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.OfferFrom).Value(eventValue.OfferFrom.ToBase58())));
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.OfferTo).Value(eventValue.OfferTo.ToBase58())));
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.ExpireTime).Value(eventValue.ExpireTime.ToDateTime())));  

        QueryContainer ListingFilter(QueryContainerDescriptor<OfferInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));
       
        var offerIndexList = await _nftOfferIndexRepository.GetListAsync(ListingFilter);
        if (offerIndexList.Item1 == 0) return;
        foreach (var index in offerIndexList.Item2)
        {
            var offerIndex = await _nftOfferIndexRepository.GetFromBlockStateSetAsync(index.Id, context.ChainId);
            if (offerIndex == null) return;
            var offerIndexId = offerIndex.Id;
            var nftInfoId = offerIndex.BizInfoId;
            _objectMapper.Map(context, offerIndex);
            await _nftOfferIndexRepository.DeleteAsync(offerIndex);

            var latestNFTOfferDic =
                await _offerProvider.QueryLatestNFTOfferByNFTIdsAsync(new List<string> { offerIndex.BizInfoId },
                    offerIndexId);

            var latestNFTOffer = latestNFTOfferDic != null && latestNFTOfferDic.ContainsKey(offerIndex.BizInfoId)
                ? latestNFTOfferDic[offerIndex.BizInfoId]
                : new OfferInfoIndex()
                {
                    BizInfoId = nftInfoId
                };

            await _infoProvider.UpdateOfferCommonAsync(context.ChainId, eventValue.Symbol, context,
                latestNFTOffer,
                offerIndexId);
        }

        await _offerProvider.UpdateOfferNumAsync(eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
            -offerIndexList.Item2.Count, context);
        await _collectionChangeProvider.SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);
        await _nftOfferChangeProvider.SaveNFTOfferChangeIndexAsync(context, eventValue.Symbol, EventType.Remove);
    }
}