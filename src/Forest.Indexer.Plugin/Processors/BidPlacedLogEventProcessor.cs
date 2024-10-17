using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class BidPlacedLogEventProcessor : LogEventProcessorBase<Forest.Contracts.Auction.BidPlaced>
{
    private readonly IObjectMapper _objectMapper;
    private readonly IAElfClientServiceProvider _aElfClientServiceProvider;

    public BidPlacedLogEventProcessor(
        IObjectMapper objectMapper,
        IAElfClientServiceProvider aElfClientServiceProvider
        )
    {
        _objectMapper = objectMapper;
        _aElfClientServiceProvider = aElfClientServiceProvider;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetAuctionContractAddress(chainId);
    }

    public override async Task ProcessAsync(Forest.Contracts.Auction.BidPlaced eventValue, LogEventContext context)
    {
        Logger.LogDebug("BidPlaced eventValue AuctionId {AuctionId}", eventValue.AuctionId.ToHex());
        var startTime = DateTime.UtcNow;
        Logger.LogInformation("BidPlaced eventValue  get date start time:{time} amount:{amount} BlockHeight:{BlockHeight}", startTime.ToString(), eventValue.Price.Amount,
            context.Block.BlockHeight);


        if (eventValue == null) return;

        var auctionInfoIndex = await GetEntityAsync<SymbolAuctionInfoIndex>(eventValue.AuctionId.ToHex());
        var enDTime = DateTime.UtcNow;
        Logger.LogInformation("BidPlaced eventValue  Symbol: {Symbol} amount:{amount} get symbol end time:{time}", auctionInfoIndex.Symbol, eventValue.Price.Amount,
            enDTime.ToString());
        Logger.LogInformation("BidPlaced eventValue  Symbol: {Symbol} amount:{amount} get GetFromBlockStateSetAsync cost  time:{time}ms", auctionInfoIndex.Symbol,
            eventValue.Price.Amount, (enDTime - startTime).TotalMilliseconds);

        var indexerStart = DateTime.UtcNow;
        Logger.LogInformation("BidPlaced eventValue Symbol: {Symbol} amount:{amount} start time:{time} BlockHeight:{BlockHeight}", auctionInfoIndex.Symbol, eventValue.Price.Amount,
            indexerStart.ToString(),context.Block.BlockHeight);
        if (auctionInfoIndex != null)
        {
            auctionInfoIndex.FinishPrice = new TokenPriceInfo
            {
                Amount = eventValue.Price.Amount,
                Symbol = eventValue.Price.Symbol
            };
            auctionInfoIndex.TransactionHash = context.Transaction.TransactionId;
            auctionInfoIndex.FinishBidder = eventValue.Bidder.ToBase58();
            _objectMapper.Map(context, auctionInfoIndex);
            await SaveEntityAsync(auctionInfoIndex);

            var symbolBidInfoIndex = new SymbolBidInfoIndex
            {
                Id = context.Transaction.TransactionId,
                Symbol = auctionInfoIndex.Symbol,
                Bidder = eventValue.Bidder.ToBase58(),
                PriceAmount = eventValue.Price.Amount,
                PriceSymbol = eventValue.Price.Symbol,
                BidTime = eventValue.BidTime.Seconds,
                AuctionId = eventValue.AuctionId.ToHex(),
                TransactionHash = context.Transaction.TransactionId
            };
            _objectMapper.Map(context, symbolBidInfoIndex);
            await SaveEntityAsync(symbolBidInfoIndex);

            var indexerEnd = DateTime.UtcNow;
            Logger.LogInformation("BidPlaced eventValue Symbol: {Symbol} amount:{amount} end time:{time} ms BlockHeight:{BlockHeight}", auctionInfoIndex.Symbol,
                eventValue.Price.Amount,
                indexerEnd.ToString(),context.Block.BlockHeight);

            Logger.LogInformation("BidPlaced eventValue Symbol: {Symbol} amount:{amount} update cost time:{time} ms BlockHeight:{BlockHeight} ", auctionInfoIndex.Symbol, eventValue.Price.Amount,
                (indexerEnd - indexerStart).TotalMilliseconds, context.Block.BlockHeight);
            
            await HandleBidPlacedAsync(context, eventValue, symbolBidInfoIndex, auctionInfoIndex.EndTime);
            await SaveCollectionPriceChangeIndexAsync(context, auctionInfoIndex.CollectionSymbol);
        }

        await SetSeedSymbolIndexPriceByAuctionInfoAsync(eventValue.AuctionId.ToHex(), DateTimeHelper.FromUnixTimeSeconds(eventValue.BidTime.Seconds), context);
    }
    private async Task SetSeedSymbolIndexPriceByAuctionInfoAsync(string auctionId, DateTime dateTime, LogEventContext context)
    {
        Logger.LogDebug("SetSeedSymbolIndexPriceByAuctionInfoAsync 1 {chainId} {auctionId}",context.ChainId,auctionId);
        var auctionInfoIndex = await GetEntityAsync<SymbolAuctionInfoIndex>(auctionId);
        if (auctionInfoIndex == null)
        {
            Logger.LogDebug("SetSeedSymbolIndexPriceByAuctionInfoAsync 1-stop {chainId} {symbol}",context.ChainId,auctionInfoIndex.Symbol);
            return;
        }
        Logger.LogDebug("SetSeedSymbolIndexPriceByAuctionInfoAsync 2 {chainId} {symbol}",context.ChainId,auctionInfoIndex.Symbol);
        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, auctionInfoIndex.Symbol);
        var seedSymbolIndex = await GetEntityAsync<SeedSymbolIndex>(seedSymbolId);
        if (seedSymbolIndex == null)
        {
            Logger.LogDebug("SetSeedSymbolIndexPriceByAuctionInfoAsync 2-stop {chainId} {symbol}",context.ChainId,auctionInfoIndex.Symbol);
            return;
        }
        Logger.LogDebug("SetSeedSymbolIndexPriceByAuctionInfoAsync 3 {chainId} {symbol}",context.ChainId,auctionInfoIndex.Symbol);

        if (auctionInfoIndex.FinishPrice != null && auctionInfoIndex.FinishPrice.Amount >= 0)
        {
            seedSymbolIndex.AuctionPriceSymbol = auctionInfoIndex.FinishPrice.Symbol;
            seedSymbolIndex.AuctionPrice = DecimalUntil.ConvertToElf(auctionInfoIndex.FinishPrice.Amount);
        }
        else
        {
            seedSymbolIndex.AuctionPriceSymbol = auctionInfoIndex.StartPrice.Symbol;
            seedSymbolIndex.AuctionPrice = DecimalUntil.ConvertToElf(auctionInfoIndex.StartPrice.Amount);
            
        }
        seedSymbolIndex.MaxAuctionPrice = seedSymbolIndex.AuctionPrice;
        seedSymbolIndex.HasAuctionFlag = true;

        seedSymbolIndex.AuctionDateTime = dateTime;
        seedSymbolIndex.BeginAuctionPrice = DecimalUntil.ConvertToElf(auctionInfoIndex.StartPrice.Amount);
        Logger.LogDebug("SetSeedSymbolIndexPriceByAuctionInfoAsync 4 {chainId} {symbol}",context.ChainId,auctionInfoIndex.Symbol);
        _objectMapper.Map(context, seedSymbolIndex);
        Logger.LogDebug("SetSeedSymbolIndexPriceByAuctionInfoAsync 4 {chainId} {symbol} {seedSymbolIndex}",context.ChainId,auctionInfoIndex.Symbol,JsonConvert.SerializeObject(seedSymbolIndex));
        await SaveEntityAsync(seedSymbolIndex);
    }
    private async Task SaveCollectionPriceChangeIndexAsync(LogEventContext context, string symbol)
    {
        var collectionPriceChangeIndex = new CollectionPriceChangeIndex();
        var nftCollectionSymbol = SymbolHelper.GetNFTCollectionSymbol(symbol);
        if (nftCollectionSymbol == null)
        {
            return;
        }

        collectionPriceChangeIndex.Symbol = nftCollectionSymbol;
        collectionPriceChangeIndex.Id = IdGenerateHelper.GetNFTCollectionId(context.ChainId, nftCollectionSymbol);
        collectionPriceChangeIndex.UpdateTime = context.Block.BlockTime;
        _objectMapper.Map(context, collectionPriceChangeIndex);
        await SaveEntityAsync(collectionPriceChangeIndex);

    }
    public async Task HandleBidPlacedAsync(LogEventContext context, Contracts.Auction.BidPlaced eventValue,
        SymbolBidInfoIndex bidInfo, long auctionEndTime)
    {
        var tsmSeed = await GetTsmSeedAsync(context.ChainId, bidInfo.Symbol);
        if (tsmSeed == null)
        {
            Logger.LogInformation("HandleBidPlacedAsync tsmSeed is null, chainId:{chainId} symbol:{symbol}",
                context.ChainId, bidInfo.Symbol);
            return;
        }

        var seedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(tsmSeed.Id);
        if (seedSymbolIndex == null)
        {
            return;
        }

        _objectMapper.Map(context, seedSymbolIndex);
        seedSymbolIndex.AuctionStatus = (int)SeedAuctionStatus.Bidding;
        seedSymbolIndex.BidsCount += 1;
        seedSymbolIndex.TopBidPrice = new TokenPriceInfo
        {
            Amount = eventValue.Price.Amount,
            Symbol = eventValue.Price.Symbol
        };
        if (auctionEndTime > 0)
        {
            seedSymbolIndex.AuctionEndTime = auctionEndTime;
        }
        //calc BiddersCount
        var bidderSet = await GetAllBiddersAsync(bidInfo.AuctionId);
        //add current bidInfo.Bidder
        bidderSet.Add(bidInfo.Bidder);
        seedSymbolIndex.BiddersCount = bidderSet.Count;
        Logger.LogInformation(
            "HandleBidPlacedAsync tsmSeedSymbolId {tsmSeedSymbolId} bidsCount:{bidsCount} biddersCount:{biddersCount} topBidPrice:{topBidPrice}",
            tsmSeed.Id, seedSymbolIndex.BidsCount, seedSymbolIndex.BiddersCount,
            JsonConvert.SerializeObject(seedSymbolIndex.TopBidPrice));
        await SaveEntityAsync(seedSymbolIndex);
    }
    private async Task<TsmSeedSymbolIndex> GetTsmSeedAsync(string chainId, string seedSymbol)
    {
        //todo V2 getTsmSeedInfo from Contract //code done, need test by self
        var tokenContractAddress = ContractInfoHelper.GetTokenContractAddress(chainId);
        var tokenInfo =
            await _aElfClientServiceProvider.GetTokenInfoAsync(chainId, tokenContractAddress, seedSymbol);
        if (tokenInfo == null)
        {
            return null;
        }
        var seedOwnedSymbol = EnumDescriptionHelper.GetExtraInfoValue(tokenInfo.ExternalInfo, TokenCreatedExternalInfoEnum.SeedOwnedSymbol);
        if (seedOwnedSymbol == null)
        {
            return null;
        }

        var seedSymbolIndexId = IdGenerateHelper.GetTsmSeedSymbolId(chainId, seedOwnedSymbol);
        return new TsmSeedSymbolIndex()
        {
            Id = seedSymbolIndexId
        };
    }
    private async Task<HashSet<string>> GetAllBiddersAsync(string auctionId)
    {
        //todo V2 GetAllBiddersAsync //code: doing, plan:1、Contract add View method record bidders.2、aefinder not record bidders,server build info with bidders

        var mustQuery = new List<Func<QueryContainerDescriptor<SymbolBidInfoIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.AuctionId)
                .Value(auctionId))
        };

        QueryContainer Filter(QueryContainerDescriptor<SymbolBidInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var skipCount = 0;
        var bidderSet = new HashSet<string>();
        List<SymbolBidInfoIndex> dataList;
        do
        {
            var result = await _symbolBidInfoIndexRepository.GetListAsync(Filter, skip: skipCount);
            dataList = result.Item2;
            if (dataList.IsNullOrEmpty())
            {
                break;
            }

            foreach (var bidder in dataList.Select(i => i.Bidder))
            {
                bidderSet.Add(bidder);
            }

            skipCount += dataList.Count;
        } while (!dataList.IsNullOrEmpty());

        return bidderSet;
    }
}