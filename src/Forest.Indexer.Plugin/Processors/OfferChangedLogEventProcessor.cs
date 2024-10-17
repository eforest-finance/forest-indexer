using AeFinder.Sdk.Processor;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Microsoft.Extensions.Logging;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class OfferChangedLogEventProcessor : LogEventProcessorBase<OfferChanged>
{
    private readonly ILogger<OfferChangedLogEventProcessor> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly INFTInfoProvider _infoProvider;
    private readonly INFTOfferProvider _offerProvider;
    private readonly INFTOfferChangeProvider _nftOfferChangeProvider;
    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly ICollectionChangeProvider _collectionChangeProvider;

    public OfferChangedLogEventProcessor(
        ILogger<OfferChangedLogEventProcessor> logger, 
        IObjectMapper objectMapper,
        INFTInfoProvider infoProvider,
        INFTOfferProvider offerProvider,
        INFTOfferChangeProvider nftOfferChangeProvider,
        ICollectionProvider collectionProvider,
        ICollectionChangeProvider collectionChangeProvider,
        IUserBalanceProvider userBalanceProvider
        )
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

    public async override Task ProcessAsync(OfferChanged eventValue, LogEventContext context)
    {
        var offerIndexId = IdGenerateHelper.GetOfferId(context.ChainId, eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
            eventValue.OfferTo.ToBase58(), eventValue.ExpireTime.Seconds,eventValue.Price.Amount);
        var offerIndex = await GetEntityAsync<OfferInfoIndex>(offerIndexId);
        if (offerIndex == null) return;

        var tokenIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.Price.Symbol);
        var tokenIndex = await GetEntityAsync<TokenInfoIndex>(tokenIndexId);
        offerIndex.Price = eventValue.Price.Amount / (decimal)Math.Pow(10, tokenIndex.Decimals);
        offerIndex.Quantity = eventValue.Quantity;
        var userBalanceId =
            IdGenerateHelper.GetUserBalanceId(eventValue.OfferFrom.ToBase58(), context.ChainId, tokenIndexId);
        var userBalance = await _userBalanceProvider.QueryUserBalanceByIdAsync(userBalanceId, context.ChainId);
        var balance = userBalance == null ? 0 : userBalance.Amount;

        offerIndex.RealQuantity = Math.Min(eventValue.Quantity, balance / eventValue.Price.Amount);
        _objectMapper.Map(context, offerIndex);
        await SaveEntityAsync(offerIndex);
        await _collectionChangeProvider.SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);
        await _nftOfferChangeProvider.SaveNFTOfferChangeIndexAsync(context, eventValue.Symbol, EventType.Modify);
    }
}