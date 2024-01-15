using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public interface ITsmSeedSymbolProvider
{
    Task UpdateAuctionEndTimeAsync(LogEventContext context, string symbol, long auctionEndTime);

    Task HandleBidPlacedAsync(LogEventContext context, Contracts.Auction.BidPlaced eventValue,
        SymbolBidInfoIndex bidInfo, long auctionEndTime);
}

public class TsmSeedSymbolProvider : ITsmSeedSymbolProvider, ISingletonDependency
{
    private readonly ILogger<TsmSeedSymbolProvider> _logger;
    private readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> _tsmSeedSymbolIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<SymbolBidInfoIndex, LogEventInfo> _symbolBidInfoIndexRepository;
    private readonly IObjectMapper _objectMapper;

    public TsmSeedSymbolProvider(
        IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> tsmSeedSymbolIndexRepository,
        IAElfIndexerClientEntityRepository<SymbolBidInfoIndex, LogEventInfo> symbolBidInfoIndexRepository,
        IObjectMapper objectMapper,
        ILogger<TsmSeedSymbolProvider> logger)
    {
        _tsmSeedSymbolIndexRepository = tsmSeedSymbolIndexRepository;
        _symbolBidInfoIndexRepository = symbolBidInfoIndexRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task UpdateAuctionEndTimeAsync(LogEventContext context, string symbol, long auctionEndTime)
    {
        var tsmSeed = await GetTsmSeedAsync(context.ChainId, symbol);
        if (tsmSeed == null)
        {
            _logger.LogInformation("UpdateAuctionEndTimeAsync tsmSeed is null, chainId:{chainId} symbol:{symbol}",
                context.ChainId, symbol);
            return;
        }

        var seedSymbolIndex =
            await _tsmSeedSymbolIndexRepository.GetFromBlockStateSetAsync(tsmSeed.Id, context.ChainId);
        if (seedSymbolIndex == null)
        {
            return;
        }

        _objectMapper.Map(context, seedSymbolIndex);
        seedSymbolIndex.AuctionEndTime = auctionEndTime;
        _logger.LogInformation(
            "UpdateAuctionEndTimeAsync tsmSeedSymbolId {tsmSeedSymbolId} auctionEndTime:{auctionEndTime}",
            tsmSeed.Id, auctionEndTime);
        await _tsmSeedSymbolIndexRepository.AddOrUpdateAsync(seedSymbolIndex);
    }

    public async Task HandleBidPlacedAsync(LogEventContext context, Contracts.Auction.BidPlaced eventValue,
        SymbolBidInfoIndex bidInfo, long auctionEndTime)
    {
        var tsmSeed = await GetTsmSeedAsync(context.ChainId, bidInfo.Symbol);
        if (tsmSeed == null)
        {
            _logger.LogInformation("HandleBidPlacedAsync tsmSeed is null, chainId:{chainId} symbol:{symbol}",
                context.ChainId, bidInfo.Symbol);
            return;
        }

        var seedSymbolIndex =
            await _tsmSeedSymbolIndexRepository.GetFromBlockStateSetAsync(tsmSeed.Id, context.ChainId);
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
        _logger.LogInformation(
            "HandleBidPlacedAsync tsmSeedSymbolId {tsmSeedSymbolId} bidsCount:{bidsCount} biddersCount:{biddersCount} topBidPrice:{topBidPrice}",
            tsmSeed.Id, seedSymbolIndex.BidsCount, seedSymbolIndex.BiddersCount,
            JsonConvert.SerializeObject(seedSymbolIndex.TopBidPrice));
        await _tsmSeedSymbolIndexRepository.AddOrUpdateAsync(seedSymbolIndex);
    }


    private async Task<HashSet<string>> GetAllBiddersAsync(string auctionId)
    {
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

    private async Task<TsmSeedSymbolIndex> GetTsmSeedAsync(string chainId, string seedSymbol)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.ChainId)
                .Value(chainId)),

            q => q.Term(i => i.Field(f => f.SeedSymbol)
                .Value(seedSymbol))
        };

        QueryContainer Filter(QueryContainerDescriptor<TsmSeedSymbolIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var result = await _tsmSeedSymbolIndexRepository.GetListAsync(Filter);
        return result.Item2.IsNullOrEmpty() ? null : result.Item2.FirstOrDefault();
    }
}