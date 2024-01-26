using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class OfferRemovedLogEventProcessor : OfferLogEventProcessorBase<OfferRemoved>
{
    private readonly IAElfIndexerClientEntityRepository<OfferInfoIndex, LogEventInfo> _nftOfferIndexRepository;
    private readonly ILogger<OfferRemovedLogEventProcessor> _logger;

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

        var offerIndexId = IdGenerateHelper.GetOfferId(context.ChainId, eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
            eventValue.OfferTo.ToBase58(), eventValue.ExpireTime.Seconds,eventValue.Price.Amount);
        var offerIndex = await _nftOfferIndexRepository.GetFromBlockStateSetAsync(offerIndexId, context.ChainId);
        if (offerIndex == null) return;
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
        await _collectionChangeProvider.SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);
        await _nftOfferChangeProvider.SaveNFTOfferChangeIndexAsync(context, eventValue.Symbol, EventType.Remove);
    }
}