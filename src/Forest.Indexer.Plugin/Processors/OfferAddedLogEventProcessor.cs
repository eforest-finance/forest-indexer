using AeFinder.Sdk.Processor;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class OfferAddedLogEventProcessor : LogEventProcessorBase<OfferAdded>
{
    private readonly ILogger<OfferAddedLogEventProcessor> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly INFTInfoProvider _infoProvider;
    private readonly INFTOfferProvider _offerProvider;
    private readonly INFTOfferChangeProvider _nftOfferChangeProvider;
    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly ICollectionChangeProvider _collectionChangeProvider;

    public OfferAddedLogEventProcessor(ILogger<OfferAddedLogEventProcessor> logger, 
        IObjectMapper objectMapper,
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

    public async override Task ProcessAsync(OfferAdded eventValue, LogEventContext context)
    {
        _logger.LogDebug("OfferAddedLogEventProcessor-1 {context}",JsonConvert.SerializeObject(context));
        _logger.LogDebug("OfferAddedLogEventProcessor-2 {eventValue}",JsonConvert.SerializeObject(eventValue));
        var offerIndexId = IdGenerateHelper.GetOfferId(context.ChainId, eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
            eventValue.OfferTo.ToBase58(), eventValue.ExpireTime.Seconds,eventValue.Price.Amount);
        var offerIndex = await GetEntityAsync<OfferInfoIndex>(offerIndexId);
        if (offerIndex != null) return;

        offerIndex = _objectMapper.Map<OfferAdded, OfferInfoIndex>(eventValue);
        offerIndex.Id = offerIndexId;
        var tokenIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.Price.Symbol);
        var tokenIndex = await GetEntityAsync<TokenInfoIndex>(tokenIndexId);
        offerIndex.Price = eventValue.Price.Amount / (decimal)Math.Pow(10, tokenIndex.Decimals);
        offerIndex.PurchaseToken = tokenIndex;
        offerIndex.CreateTime = context.Block.BlockTime;

        if (SymbolHelper.CheckSymbolIsSeedSymbol(eventValue.Symbol))
        {
            offerIndex.BizInfoId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        }
        else if (SymbolHelper.CheckSymbolIsNFT(eventValue.Symbol))
        {
            offerIndex.BizInfoId = IdGenerateHelper.GetNFTInfoId(context.ChainId, eventValue.Symbol);
        }
        offerIndex.BizSymbol = eventValue.Symbol;
        offerIndex.RealQuantity = eventValue.Quantity;
        _objectMapper.Map(context, offerIndex);
        await SaveEntityAsync(offerIndex);
        await _infoProvider.UpdateOfferCommonAsync(context.ChainId, eventValue.Symbol, context, offerIndex, "");
        /*await AddNFTActivityRecordAsync(eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
            eventValue.OfferTo.ToBase58(), eventValue.Quantity, offerIndex.Price, NFTActivityType.MakeOffer,
            context,
            tokenIndex,
            offerIndex.ExpireTime);*/  //todo v2
        await _collectionChangeProvider.SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);
        await _offerProvider.UpdateOfferNumAsync(eventValue.Symbol, eventValue.OfferFrom.ToBase58(), 1, context);
        await _userBalanceProvider.ReCoverUserBalanceAsync(eventValue.OriginBalanceSymbol, eventValue.OfferFrom.ToBase58(), eventValue.OriginBalance, context);
        await _nftOfferChangeProvider.SaveNFTOfferChangeIndexAsync(context, eventValue.Symbol, EventType.Add);
    }
}