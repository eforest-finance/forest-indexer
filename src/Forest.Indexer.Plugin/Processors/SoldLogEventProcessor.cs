using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class SoldLogEventProcessor : AElfLogEventProcessorBase<Sold, LogEventInfo>

{
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly ILogger<AElfLogEventProcessorBase<Sold, LogEventInfo>> _logger;
    private readonly IAElfIndexerClientEntityRepository<SoldIndex, LogEventInfo> _soldIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo> _nftActivityIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> _tokenIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> _nftInfoIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<NFTMarketInfoIndex, LogEventInfo> _nftMarketIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<NFTMarketDayIndex, LogEventInfo> _nftMarketDayIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<NFTMarketWeekIndex, LogEventInfo> _nftMarketWeekIndexRepository;
    private readonly INFTInfoProvider _nftInfoProvider;
    private readonly IObjectMapper _objectMapper;


    public SoldLogEventProcessor(
        ILogger<AElfLogEventProcessorBase<Sold, LogEventInfo>> logger,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        IAElfIndexerClientEntityRepository<SoldIndex, LogEventInfo> soldIndexRepository,
        IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo> nftActivityIndexRepository,
        IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> tokenIndexRepository,
        IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> nftInfoIndexRepository,
        IAElfIndexerClientEntityRepository<NFTMarketInfoIndex, LogEventInfo> nftMarketIndexRepository,
        IAElfIndexerClientEntityRepository<NFTMarketDayIndex, LogEventInfo> nftMarketDayIndexRepository,
        IAElfIndexerClientEntityRepository<NFTMarketWeekIndex, LogEventInfo> nftMarketWeekIndexRepository, 
        INFTInfoProvider nftInfoProvider) :
        base(logger)
    {
        _soldIndexRepository = soldIndexRepository;
        _nftActivityIndexRepository = nftActivityIndexRepository;
        _objectMapper = objectMapper;
        _tokenIndexRepository = tokenIndexRepository;
        _nftInfoIndexRepository = nftInfoIndexRepository;
        _nftMarketIndexRepository = nftMarketIndexRepository;
        _logger = logger;
        _contractInfoOptions = contractInfoOptions.Value;
        _nftMarketDayIndexRepository = nftMarketDayIndexRepository;
        _nftMarketWeekIndexRepository = nftMarketWeekIndexRepository;
        _nftInfoProvider = nftInfoProvider;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).NFTMarketContractAddress;
    }

    public static decimal ToPrice(long amount, int decimals)
    {
        return amount / (decimal)Math.Pow(10, decimals);
    }

    protected override async Task HandleEventAsync(Sold eventValue, LogEventContext context)
    {
        // It's possible to execute multiple identical 'sold' events in a single transaction.
        var soldIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.NftSymbol, context.TransactionId, Guid.NewGuid());
        _logger.Debug("[Sold] START: soldIndexId={soldIndexId}, Event={Event}", soldIndexId,
            JsonConvert.SerializeObject(eventValue));


        // NFT token Index
        var nftTokenIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.NftSymbol);
        var nftTokenIndex = await _tokenIndexRepository.GetFromBlockStateSetAsync(nftTokenIndexId, context.ChainId);
        if (nftTokenIndex == null)
        {
            _logger.Debug(
                "[Sold] FAIL: nftInfo not found soldIndex not found soldIndexId={soldIndexId}, tokenIndexId={tokenIndexId}",
                soldIndexId, nftTokenIndexId);
            return;
        }

        _objectMapper.Map(context, nftTokenIndex);
        await _tokenIndexRepository.AddOrUpdateAsync(nftTokenIndex);

        // query purchaseToken
        var purchaseTokenIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.PurchaseSymbol);
        var purchaseTokenIndex =
            await _tokenIndexRepository.GetFromBlockStateSetAsync(purchaseTokenIndexId, context.ChainId);
        if (purchaseTokenIndex == null)
        {
            _logger.Debug(
                "[Sold] FAIL: purchaseToken not found soldIndexId={soldIndexId}, purchaseTokenIndexId={purchaseTokenIndexId}",
                soldIndexId, purchaseTokenIndexId);
            return;
        }
        var totalPrice = ToPrice(eventValue.PurchaseAmount, purchaseTokenIndex.Decimals);
        var singlePrice = CalSinglePrice(totalPrice, (int)eventValue.NftQuantity);
        
        nftTokenIndex.Prices = singlePrice;

        // sold Index
        var soldIndex = await _soldIndexRepository.GetFromBlockStateSetAsync(soldIndexId, context.ChainId);
        if (soldIndex != null)
        {
            _logger.Debug("[Sold] FAIL: soldIndex exists soldIndexId={soldIndexId}", soldIndexId);
            return;
        }

        soldIndex = _objectMapper.Map<Sold, SoldIndex>(eventValue);
        soldIndex.Id = soldIndexId;
        soldIndex.DealTime = context.BlockTime;
        soldIndex.PurchaseTokenId = purchaseTokenIndex.Id;
        soldIndex.NftInfoId = nftTokenIndexId;
        soldIndex.CollectionSymbol = SymbolHelper.GetNFTCollectionSymbol(eventValue.NftSymbol);
        _objectMapper.Map(context, soldIndex);
        _logger.Debug("[Sold] SAVE: soldIndex, soldIndex={Id}", nftTokenIndexId);
        await _soldIndexRepository.AddOrUpdateAsync(soldIndex);
        // NFT market
        var nftMarketIndex = _objectMapper.Map<LogEventContext, NFTMarketInfoIndex>(context);
        nftMarketIndex.Id = soldIndexId;
        nftMarketIndex.PurchaseSymbol = eventValue.PurchaseSymbol;
        nftMarketIndex.Price = singlePrice;
        nftMarketIndex.Quantity = (int)eventValue.NftQuantity;
        nftMarketIndex.Timestamp = context.BlockTime;
        nftMarketIndex.NFTInfoId = nftTokenIndexId;
        _logger.Debug("[Sold] STEP: save nftMarketIndex, tokenIndexId={Id}, nftMarketIndex={nftMarketIndex}",
            nftTokenIndexId, JsonConvert.SerializeObject(nftMarketIndex));
        await _nftMarketIndexRepository.AddOrUpdateAsync(nftMarketIndex);

        // NFTInfo
        var nftInfoIndexId = IdGenerateHelper.GetNFTInfoId(context.ChainId, eventValue.NftSymbol);
        var nftInfo = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftInfoIndexId, context.ChainId);
        if (nftInfo != null)
        {
            var tokenInfoId = IdGenerateHelper.GetTokenInfoId(context.ChainId, eventValue.NftSymbol);
            var tokenInfo =
                await _tokenIndexRepository.GetFromBlockStateSetAsync(tokenInfoId, context.ChainId);
            nftInfo.LatestDealPrice = singlePrice;
            nftInfo.LatestDealTime = soldIndex.DealTime;
            nftInfo.LatestDealToken = tokenInfo;

            _logger.Debug(
                "[Sold] STEP: update fntInfo, tokenIndexId={Id}, LatestDealPrice={LatestDealPrice}, LatestDealTime={LatestDealTime}",
                nftTokenIndexId, nftInfo.LatestDealPrice, nftInfo.LatestDealTime);
            _objectMapper.Map(context, nftInfo);
            await _nftInfoIndexRepository.AddOrUpdateAsync(nftInfo);
        }

        // NFT activity
        var nftActivityIndexId =
            IdGenerateHelper.GetId(context.ChainId, eventValue.NftSymbol, "SOLD", soldIndexId);
        var activitySaved = await _nftInfoProvider.AddNFTActivityAsync(context, new NFTActivityIndex
            {
                Id = nftActivityIndexId,
                Type = NFTActivityType.Sale,
                From = eventValue.NftFrom.ToBase58(),
                To = eventValue.NftTo.ToBase58(),
                Amount = eventValue.NftQuantity,
                Price = singlePrice,
                PriceTokenInfo = purchaseTokenIndex,
                TransactionHash = context.TransactionId,
                Timestamp = context.BlockTime,
                NftInfoId = nftInfo.Id
            });
        if (!activitySaved)
        {
            _logger.Debug("[Sold] SAVE activity FAILED, nftActivityIndexId={nftActivityIndexId}", nftActivityIndexId);
            return;
        }
        
        await CalculateMarketData(nftMarketIndex.NFTInfoId, (int)eventValue.NftQuantity, totalPrice, context);
    }

    private async Task CalculateMarketData(string nftInfoId, int quantity, decimal totalPrice, LogEventContext context)
    {
        await CalculateMarketDataForDay(nftInfoId, quantity, totalPrice, context);
        await CalculateMarketDataForWeek(nftInfoId, quantity, totalPrice, context);
    }

    private async Task CalculateMarketDataForDay(string nftInfoId, int quantity, decimal totalPrice, LogEventContext context)
    {
        var todayBegin = DateTimeHelper.GainBeginDateTime(context.BlockTime);
        var todayBeginId = IdGenerateHelper.GetMarketDataTodayIndexId(nftInfoId
            , DateTimeHelper.ToUnixTimeMilliseconds(todayBegin));
        var marketDataForDay = await _nftMarketDayIndexRepository.GetFromBlockStateSetAsync(todayBeginId, context.ChainId);
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
                UpdateDate = context.BlockTime,
                NFTInfoId = nftInfoId
            };
        }
        else
        {
            marketDataForDay.UpdateDate = context.BlockTime;
            marketDataForDay.AveragePrice = CalculateNewAveragePrice(marketDataForDay.AveragePrice,
                marketDataForDay.MarketNumber, totalPrice, quantity);
            marketDataForDay.MinPrice = CalculateMinPrice(marketDataForDay.MinPrice, singlePrice);
            marketDataForDay.MaxPrice = CalculateMaxPrice(marketDataForDay.MaxPrice, singlePrice);
            marketDataForDay.MarketNumber += quantity;
        }

        _objectMapper.Map(context, marketDataForDay);
        await _nftMarketDayIndexRepository.AddOrUpdateAsync(marketDataForDay);
    }

    private async Task CalculateMarketDataForWeek(string nftInfoId, int quantity, decimal totalPrice, LogEventContext context)
    {
        var weekMondayBegin = DateTimeHelper.GainMondayDateTime(context.BlockTime);
        var weekMondayBeginId = IdGenerateHelper.GetMarketDataWeekIndexId(nftInfoId
            , DateTimeHelper.ToUnixTimeMilliseconds(weekMondayBegin));
        var marketDataForWeek = await _nftMarketWeekIndexRepository.GetAsync(weekMondayBeginId);
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
                UpdateDate = context.BlockTime,
                NFTInfoId = nftInfoId
            };
        }
        else
        {
            marketDataForWeek.UpdateDate = context.BlockTime;
            marketDataForWeek.AveragePrice = CalculateNewAveragePrice(marketDataForWeek.AveragePrice,
                marketDataForWeek.MarketNumber, totalPrice, quantity);
            marketDataForWeek.MinPrice = CalculateMinPrice(marketDataForWeek.MinPrice, singlePrice);
            marketDataForWeek.MaxPrice = CalculateMaxPrice(marketDataForWeek.MaxPrice, singlePrice);
            marketDataForWeek.MarketNumber += quantity;
        }

        _objectMapper.Map(context, marketDataForWeek);
        await _nftMarketWeekIndexRepository.AddOrUpdateAsync(marketDataForWeek);
    }

    private decimal CalSinglePrice(decimal totalPrice, int count)
    {
        return Math.Round(totalPrice / Math.Max(1, count), 2);
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