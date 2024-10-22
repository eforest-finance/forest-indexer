using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class SoldLogEventProcessor : LogEventProcessorBase<Sold>

{
    private readonly ILogger<SoldLogEventProcessor> _logger;
    private readonly IObjectMapper _objectMapper;


    public SoldLogEventProcessor(
        ILogger<SoldLogEventProcessor> logger,
        IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetNFTForestContractAddress(chainId);
    }

    public static decimal ToPrice(long amount, int decimals)
    {
        return amount / (decimal)Math.Pow(10, decimals);
    }

    public async override Task ProcessAsync(Sold eventValue, LogEventContext context)
    {
        // It's possible to execute multiple identical 'sold' events in a single transaction.
        var soldIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.NftSymbol, context.Transaction.TransactionId, Guid.NewGuid());
        _logger.LogDebug("[Sold] START: soldIndexId={soldIndexId}, Event={Event}", soldIndexId,
            JsonConvert.SerializeObject(eventValue));

        var nftInfoIndexId = "";

        if (SymbolHelper.CheckSymbolIsSeedSymbol(eventValue.NftSymbol))
        {
            nftInfoIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.NftSymbol);
        }
        else if (SymbolHelper.CheckSymbolIsNoMainChainNFT(eventValue.NftSymbol, context.ChainId))
        {
            nftInfoIndexId = IdGenerateHelper.GetNFTInfoId(context.ChainId, eventValue.NftSymbol);
        }

        if (nftInfoIndexId.IsNullOrEmpty())
        {
            _logger.LogError("eventValue.NftSymbol is not nft return,symbol={A}", eventValue.NftSymbol);
            return;
        }


        // NFT token Index
        var nftTokenIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.NftSymbol);
        var nftTokenIndex = await GetEntityAsync<TokenInfoIndex>(nftTokenIndexId);
        if (nftTokenIndex == null)
        {
            _logger.LogDebug(
                "[Sold] FAIL: nftInfo not found soldIndex not found soldIndexId={soldIndexId}, tokenIndexId={tokenIndexId}",
                soldIndexId, nftTokenIndexId);
            return;
        }

        _objectMapper.Map(context, nftTokenIndex);
        await SaveEntityAsync(nftTokenIndex);

        // query purchaseToken
        var purchaseTokenIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.PurchaseSymbol);
        var purchaseTokenIndex = await GetEntityAsync<TokenInfoIndex>(purchaseTokenIndexId);

        if (purchaseTokenIndex == null)
        {
            _logger.LogDebug(
                "[Sold] FAIL: purchaseToken not found soldIndexId={soldIndexId}, purchaseTokenIndexId={purchaseTokenIndexId}",
                soldIndexId, purchaseTokenIndexId);
            return;
        }

        var totalPrice = ToPrice(eventValue.PurchaseAmount, purchaseTokenIndex.Decimals);
        var totalCount = (int)TokenHelper.GetIntegerDivision(eventValue.NftQuantity, nftTokenIndex.Decimals);
        var singlePrice = CalSinglePrice(totalPrice,
            totalCount);
        
        nftTokenIndex.Prices = singlePrice;

        // sold Index
        var soldIndex = await GetEntityAsync<SoldIndex>(soldIndexId);
        if (soldIndex != null)
        {
            _logger.LogDebug("[Sold] FAIL: soldIndex exists soldIndexId={soldIndexId}", soldIndexId);
            return;
        }

        soldIndex = _objectMapper.Map<Sold, SoldIndex>(eventValue);
        soldIndex.Id = soldIndexId;
        soldIndex.DealTime = context.Block.BlockTime;
        soldIndex.PurchaseTokenId = purchaseTokenIndex.Id;
        soldIndex.NftInfoId = nftTokenIndexId;
        soldIndex.CollectionSymbol = SymbolHelper.GetNFTCollectionSymbol(eventValue.NftSymbol);
        _objectMapper.Map(context, soldIndex);
        _logger.LogDebug("[Sold] SAVE: soldIndex, soldIndex={Id}", nftTokenIndexId);
        await SaveEntityAsync(soldIndex);
        // NFT market
        var nftMarketIndex = _objectMapper.Map<LogEventContext, NFTMarketInfoIndex>(context);
        nftMarketIndex.Id = soldIndexId;
        nftMarketIndex.PurchaseSymbol = eventValue.PurchaseSymbol;
        nftMarketIndex.Price = singlePrice;
        nftMarketIndex.Quantity = (int)eventValue.NftQuantity;
        nftMarketIndex.Timestamp = context.Block.BlockTime;
        nftMarketIndex.NFTInfoId = nftTokenIndexId;
        _logger.LogDebug("[Sold] STEP: save nftMarketIndex, tokenIndexId={Id}, nftMarketIndex={nftMarketIndex}",
            nftTokenIndexId, JsonConvert.SerializeObject(nftMarketIndex));
        await SaveEntityAsync(nftMarketIndex);


        // NFT activity
        var nftActivityIndexId =
            IdGenerateHelper.GetId(context.ChainId, eventValue.NftSymbol, "SOLD", soldIndexId);
        var activitySaved = await AddNFTActivityAsync(context, new NFTActivityIndex
            {
                Id = nftActivityIndexId,
                Type = NFTActivityType.Sale,
                From = FullAddressHelper.ToFullAddress(eventValue.NftFrom.ToBase58(), context.ChainId),
                To = FullAddressHelper.ToFullAddress(eventValue.NftTo.ToBase58(), context.ChainId),
                Amount = TokenHelper.GetIntegerDivision(eventValue.NftQuantity, nftTokenIndex.Decimals),
                Price = singlePrice,
                PriceTokenInfo = purchaseTokenIndex,
                TransactionHash = context.Transaction.TransactionId,
                Timestamp = context.Block.BlockTime,
                NftInfoId = nftInfoIndexId
            });
        if (!activitySaved)
        {
            _logger.LogDebug("[Sold] SAVE activity FAILED, nftActivityIndexId={nftActivityIndexId}", nftActivityIndexId);
            return;
        }
        
        await CalculateMarketData(nftInfoIndexId, totalCount, totalPrice, context);
        
        if (SymbolHelper.CheckSymbolIsSeedSymbol(eventValue.NftSymbol))
        {
            var nftInfo = await GetEntityAsync<SeedSymbolIndex>(nftInfoIndexId);
            if (nftInfo != null)
            {
                nftInfo.LatestDealToken = purchaseTokenIndex;

                _logger.LogDebug(
                    "[Sold] STEP: update nftInfo (seed), tokenIndexId={Id}, LatestDealPrice={LatestDealPrice}, LatestDealTime={LatestDealTime}",
                    nftTokenIndexId, singlePrice, soldIndex.DealTime);
                _objectMapper.Map(context, nftInfo);
                await SaveEntityAsync(nftInfo);
            }
        }else if (SymbolHelper.CheckSymbolIsNoMainChainNFT(eventValue.NftSymbol, context.ChainId))
        {
            var nftInfo = await GetEntityAsync<NFTInfoIndex>(nftInfoIndexId);
            if (nftInfo != null)
            {
                nftInfo.LatestDealPrice = singlePrice;
                nftInfo.LatestDealTime = soldIndex.DealTime;
                nftInfo.LatestDealToken = purchaseTokenIndex;

                _logger.LogDebug(
                    "[Sold] STEP: update nftInfo, tokenIndexId={Id}, LatestDealPrice={LatestDealPrice}, LatestDealTime={LatestDealTime}",
                    nftTokenIndexId, nftInfo.LatestDealPrice, nftInfo.LatestDealTime);
                _objectMapper.Map(context, nftInfo);
                await SaveEntityAsync(nftInfo);
            }
        }
    }
    
    private async Task<bool> AddNFTActivityAsync(LogEventContext context, NFTActivityIndex nftActivityIndex)
    {
        // NFT activity
        var nftActivityIndexExists = await GetEntityAsync<NFTActivityIndex>(nftActivityIndex.Id);
        if (nftActivityIndexExists != null)
        {
            Logger.LogDebug("[AddNFTActivityAsync] FAIL: activity EXISTS, nftActivityIndexId={Id}", nftActivityIndex.Id);
            return false;
        }

        var from = nftActivityIndex.From;
        var to = nftActivityIndex.To;
        _objectMapper.Map(context, nftActivityIndex);
        nftActivityIndex.From = FullAddressHelper.ToFullAddress(from, context.ChainId);
        nftActivityIndex.To = FullAddressHelper.ToFullAddress(to, context.ChainId);

        Logger.LogDebug("[AddNFTActivityAsync] SAVE: activity SAVE, nftActivityIndexId={Id}", nftActivityIndex.Id);
        await SaveEntityAsync(nftActivityIndex);

        Logger.LogDebug("[AddNFTActivityAsync] SAVE: activity FINISH, nftActivityIndexId={Id}", nftActivityIndex.Id);
        return true;
    }

    private async Task CalculateMarketData(string nftInfoId, int quantity, decimal totalPrice, LogEventContext context)
    {
        await CalculateMarketDataForDay(nftInfoId, quantity, totalPrice, context);
        await CalculateMarketDataForWeek(nftInfoId, quantity, totalPrice, context);
    }

    private async Task CalculateMarketDataForDay(string nftInfoId, int quantity, decimal totalPrice, LogEventContext context)
    {
        var todayBegin = DateTimeHelper.GainBeginDateTime(context.Block.BlockTime);
        var todayBeginId = IdGenerateHelper.GetMarketDataTodayIndexId(nftInfoId
            , DateTimeHelper.ToUnixTimeMilliseconds(todayBegin));
        var marketDataForDay = await GetEntityAsync<NFTMarketDayIndex>(todayBeginId);
        var singlePrice = CalSinglePrice(totalPrice, quantity);
        if (marketDataForDay == null)
        {
            marketDataForDay = new NFTMarketDayIndex
            {
                Id = todayBeginId,
                AveragePrice = singlePrice,
                MinPrice = singlePrice,
                MaxPrice = singlePrice,
                MarketNumber = quantity,
                DayBegin = todayBegin,
                UpdateDate = context.Block.BlockTime,
                NFTInfoId = nftInfoId
            };
        }
        else
        {
            marketDataForDay.UpdateDate = context.Block.BlockTime;
            marketDataForDay.AveragePrice = CalculateNewAveragePrice(marketDataForDay.AveragePrice,
                marketDataForDay.MarketNumber, totalPrice, quantity);
            marketDataForDay.MinPrice = CalculateMinPrice(marketDataForDay.MinPrice, singlePrice);
            marketDataForDay.MaxPrice = CalculateMaxPrice(marketDataForDay.MaxPrice, singlePrice);
            marketDataForDay.MarketNumber += quantity;
        }

        _objectMapper.Map(context, marketDataForDay);
        await SaveEntityAsync(marketDataForDay);
    }

    private async Task CalculateMarketDataForWeek(string nftInfoId, int quantity, decimal totalPrice, LogEventContext context)
    {
        var weekMondayBegin = DateTimeHelper.GainMondayDateTime(context.Block.BlockTime);
        var weekMondayBeginId = IdGenerateHelper.GetMarketDataWeekIndexId(nftInfoId
            , DateTimeHelper.ToUnixTimeMilliseconds(weekMondayBegin));
        var marketDataForWeek = await GetEntityAsync<NFTMarketWeekIndex>(weekMondayBeginId);
        var singlePrice = CalSinglePrice(totalPrice, quantity);
        if (marketDataForWeek == null)
        {
            marketDataForWeek = new NFTMarketWeekIndex
            {
                Id = weekMondayBeginId,
                AveragePrice = singlePrice,
                MinPrice = singlePrice,
                MaxPrice = singlePrice,
                MarketNumber = quantity,
                DayBegin = weekMondayBegin,
                UpdateDate = context.Block.BlockTime,
                NFTInfoId = nftInfoId
            };
        }
        else
        {
            marketDataForWeek.UpdateDate = context.Block.BlockTime;
            marketDataForWeek.AveragePrice = CalculateNewAveragePrice(marketDataForWeek.AveragePrice,
                marketDataForWeek.MarketNumber, totalPrice, quantity);
            marketDataForWeek.MinPrice = CalculateMinPrice(marketDataForWeek.MinPrice, singlePrice);
            marketDataForWeek.MaxPrice = CalculateMaxPrice(marketDataForWeek.MaxPrice, singlePrice);
            marketDataForWeek.MarketNumber += quantity;
        }

        _objectMapper.Map(context, marketDataForWeek);
        await SaveEntityAsync(marketDataForWeek);
    }

    private decimal CalSinglePrice(decimal totalPrice, int count)
    {
        return Math.Round(totalPrice / Math.Max(1, count), 8);
    }
    
    private decimal CalculateNewAveragePrice(decimal averagePrice
        , int marketNumber
        , decimal addonTotalPrice, int addonCount)
    {
        return Math.Round((averagePrice * marketNumber + addonTotalPrice) / (marketNumber + addonCount), 2);
    }

    private decimal CalculateMinPrice(decimal originiMinPrice
        , decimal newPrice)
    {
        return Math.Min(originiMinPrice, newPrice);
    }
    
    private decimal CalculateMaxPrice(decimal originiMaxPrice
        , decimal newPrice)
    {
        return Math.Max(originiMaxPrice, newPrice);
    }
}