using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class OfferCanceledLogEventProcessor : OfferLogEventProcessorBase<OfferCanceled>
{
    private readonly IAElfIndexerClientEntityRepository<OfferInfoIndex, LogEventInfo> _nftOfferIndexRepository;
    private readonly ILogger<AElfLogEventProcessorBase<OfferCanceled, LogEventInfo>> _logger;

    public OfferCanceledLogEventProcessor(ILogger<OfferCanceledLogEventProcessor> logger, IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo> nftActivityIndexRepository,
        IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> nftInfoIndexRepository,
        IAElfIndexerClientEntityRepository<OfferInfoIndex, LogEventInfo> nftOfferIndexRepository,
        IAElfIndexerClientEntityRepository<ProxyAccountIndex, LogEventInfo> proxyAccountIndexRepository,
        INFTInfoProvider infoProvider,
        INFTOfferProvider offerProvider,
        ICollectionProvider collectionProvider,
        ICollectionChangeProvider collectionChangeProvider,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions) : base(logger, objectMapper,
        nftActivityIndexRepository, nftInfoIndexRepository, proxyAccountIndexRepository, infoProvider, offerProvider,collectionProvider,
        collectionChangeProvider,
        contractInfoOptions)
    {
        _logger = logger;
        _nftOfferIndexRepository = nftOfferIndexRepository;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).NFTMarketContractAddress;
    }

    protected override async Task HandleEventAsync(OfferCanceled eventValue, LogEventContext context)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<OfferInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.ChainId).Value(context.ChainId)));
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.BizSymbol).Value(eventValue.Symbol)));
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.OfferFrom).Value(eventValue.OfferFrom.ToBase58())));

        QueryContainer ListingFilter(QueryContainerDescriptor<OfferInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var offerIndex = await _nftOfferIndexRepository.GetListAsync(ListingFilter);
        if (offerIndex.Item1 == 0) return;
        // The current number of items in the IndexList is only 1 
        foreach (var index in eventValue.IndexList.Value)
        {
            try
            {
                var cancelOfferIndex = offerIndex.Item2[index];
                if (cancelOfferIndex == null) return;
                var nftInfoId = cancelOfferIndex.BizInfoId;
                await AddNFTActivityRecordAsync(eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
                    null, cancelOfferIndex.Quantity, cancelOfferIndex.Price,
                    NFTActivityType.CancelOffer, context, cancelOfferIndex.PurchaseToken);
                await _nftOfferIndexRepository.DeleteAsync(cancelOfferIndex);
                var latestNFTOfferDic =
                    await _offerProvider.QueryLatestNFTOfferByNFTIdsAsync(new List<string> { cancelOfferIndex.BizInfoId },
                        cancelOfferIndex.Id);

                var latestNFTOffer = latestNFTOfferDic != null && latestNFTOfferDic.ContainsKey(cancelOfferIndex.BizInfoId)
                    ? latestNFTOfferDic[cancelOfferIndex.BizInfoId]
                    : new OfferInfoIndex(){
                        BizInfoId = nftInfoId
                    };

                await _infoProvider.UpdateOfferCommonAsync(context.ChainId, eventValue.Symbol, context,
                    latestNFTOffer,
                    cancelOfferIndex.Id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[OfferCanceled] ERROR: Symbol={Symbol},index = {Index},size = {Size}",
                    eventValue.Symbol, index, offerIndex.Item1);
                throw;
            }
        }
        await _collectionChangeProvider.SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);
    }
}