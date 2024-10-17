using AeFinder.Sdk.Processor;
using Forest.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class OfferRemovedLogEventProcessor : LogEventProcessorBase<OfferRemoved>
{
    private readonly ILogger<OfferRemovedLogEventProcessor> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly INFTInfoProvider _infoProvider;
    private readonly INFTOfferProvider _offerProvider;
    private readonly INFTOfferChangeProvider _nftOfferChangeProvider;
    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly ICollectionChangeProvider _collectionChangeProvider;

    public OfferRemovedLogEventProcessor(ILogger<OfferRemovedLogEventProcessor> logger, IObjectMapper objectMapper,
        INFTInfoProvider infoProvider,
        INFTOfferProvider offerProvider,
        INFTOfferChangeProvider nftOfferChangeProvider,
        ICollectionProvider collectionProvider,
        ICollectionChangeProvider collectionChangeProvider,
        IUserBalanceProvider userBalanceProvider)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _infoProvider = infoProvider;
        _offerProvider = offerProvider;
        _nftOfferChangeProvider = nftOfferChangeProvider;
        _collectionProvider = collectionProvider;
        _collectionChangeProvider = collectionChangeProvider;
        _userBalanceProvider = userBalanceProvider;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetNFTForestContractAddress(chainId);
    }

    public async override Task ProcessAsync(OfferRemoved eventValue, LogEventContext context)
    {
        _logger.LogDebug("OfferRemovedLogEventProcessor-1 {context}",JsonConvert.SerializeObject(context));
        _logger.LogDebug("OfferRemovedLogEventProcessor-2 {eventValue}",JsonConvert.SerializeObject(eventValue));

        var offerIndexId = IdGenerateHelper.GetOfferId(context.ChainId, eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
            eventValue.OfferTo.ToBase58(), eventValue.ExpireTime.Seconds,eventValue.Price.Amount);
        var offerIndex = await GetEntityAsync<OfferInfoIndex>(offerIndexId);

        if (offerIndex == null) return;
        var nftInfoId = offerIndex.BizInfoId;
        _objectMapper.Map(context, offerIndex);
        await DeleteEntityAsync<OfferInfoIndex>(offerIndexId);

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
        await _offerProvider.UpdateOfferNumAsync(eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
            -1, context);

        await _collectionChangeProvider.SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);
        await _nftOfferChangeProvider.SaveNFTOfferChangeIndexAsync(context, eventValue.Symbol, EventType.Remove);
    }
}