using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;

namespace Forest.Indexer.Plugin.Processors.Provider;

public interface ICollectionProvider
{
    public Task<decimal> CalcCollectionFloorPriceAsync(string chainId, string symbol, decimal oriFloorPrice);

    public Task<decimal> CalcCollectionFloorPriceWithTimestampAsync(string chainId, string symbol, long beginUtcStampSecond,
        long endUtcStampSecond);

    public Task<Dictionary<long, decimal>> CalcNFTCollectionTradeAsync(string chainId, string collectionId,
        long beginUtcStampSecond,
        long endUtcStampSecond);
}

public class CollectionProvider : ICollectionProvider, ISingletonDependency
{
    private readonly IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo>
        _symbolAuctionInfoIndexRepository;
    
    private readonly IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo>
        _nftActivityIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo>
        _nftListingInfoIndexRepository;
    
    private readonly ILogger<ICollectionProvider> _logger;
    private readonly GeneralityOptions _generalityOptions;

    public CollectionProvider(
        IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo>
            symbolAuctionInfoIndexRepository,
        IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo>
            nftListingInfoIndexRepository,
        IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo>
            nftActivityIndexRepository,
        ILogger<ICollectionProvider> logger,
        IOptionsSnapshot<GeneralityOptions> generalityOptions
        )
    {
        _symbolAuctionInfoIndexRepository = symbolAuctionInfoIndexRepository;
        _nftListingInfoIndexRepository = nftListingInfoIndexRepository;
        _nftActivityIndexRepository = nftActivityIndexRepository;
        _logger = logger;
        _generalityOptions = generalityOptions.Value;

    }
    
    public async Task<decimal> CalcCollectionFloorPriceAsync(string chainId, string symbol, decimal oriFloorPrice)
    {
        //Calculate current FloorPrice
        //symbol is collection symbol.
        var symbolAuctionInfoIndex = await QueryMinPriceForSymbolAuctionInfoIndexAsync(chainId, symbol, 0, 0);
        decimal? auctionMinPrice =  symbolAuctionInfoIndex?.FinishPrice.Amount;
        if (auctionMinPrice !=null && auctionMinPrice.Value > 0)
        {
            auctionMinPrice = DecimalUntil.ConvertToElf(auctionMinPrice.Value);
        }
        var nftListingInfoIndex = await QueryMinPriceForNFTListingInfoIndexAsync(chainId, symbol);
        decimal? listingMinPrice =  nftListingInfoIndex?.Prices;
        if (auctionMinPrice == null && listingMinPrice == null)
        {
            return -1m;
        } 
        return Math.Min(auctionMinPrice ?? decimal.MaxValue, listingMinPrice ?? decimal.MaxValue);
    }

    public async Task<decimal> CalcCollectionFloorPriceWithTimestampAsync(string chainId, string symbol,
        long beginUtcStampSecond, long endUtcStampSecond)
    {
        //Calculate current FloorPrice
        //symbol is collection symbol.
        var symbolAuctionInfoIndex =
            await QueryMinPriceForSymbolAuctionInfoIndexAsync(chainId, symbol, beginUtcStampSecond, endUtcStampSecond);
        decimal? auctionMinPrice = symbolAuctionInfoIndex?.FinishPrice.Amount;
        if (auctionMinPrice != null && auctionMinPrice.Value > 0)
        {
            auctionMinPrice = DecimalUntil.ConvertToElf(auctionMinPrice.Value);
        }

        var nftListingInfoIndex =
            await QueryMinPriceWithTimestampForNFTListingInfoIndexAsync(chainId, symbol, beginUtcStampSecond, endUtcStampSecond);
        decimal? listingMinPrice = nftListingInfoIndex?.Prices;
        if (auctionMinPrice == null && listingMinPrice == null)
        {
            return -1m;
        }

        return Math.Min(auctionMinPrice ?? decimal.MaxValue, listingMinPrice ?? decimal.MaxValue);
    }

    public async Task<Dictionary<long, decimal>> CalcNFTCollectionTradeAsync(string chainId, string collectionId,
        long beginUtcStampSecond, long endUtcStampSecond)
    {
        var skipCount = 0;
        decimal volumeTotal = 0;
        long salesTotal = 0;
        while (true)
        {
            var result = await CalcNFTCollectionTradeSingleAsync(chainId, collectionId, skipCount, beginUtcStampSecond,
                endUtcStampSecond);
            if (result == null || result.Item2.IsNullOrEmpty() || result.Item2.Count == 0)
            {
                break;
            }

            volumeTotal += result.Item2.Sum(item => item.Price * item.Amount);
            salesTotal += result.Item2.Count;
            skipCount += result.Item2.Count;
        }

        return new Dictionary<long, decimal>()
        {
            { salesTotal, volumeTotal }
        };
    }

    private async Task<Tuple<long, List<NFTActivityIndex>>> CalcNFTCollectionTradeSingleAsync(string chainId,
        string collectionId,
        int skipCount,
        long beginUtcStampSecond, long endUtcStampSecond)
    {
        
       
        var collectionSymbolPre = TokenHelper.GetCollectionIdPre(collectionId);

        _logger.Debug(
            "CalcNFTCollectionTradeSingleAsync chainId={A} collectionId={B} skipCount={C} beginUtcStampSecond={D} endUtcStampSecond={E} beginTime={F} endTime={G}",
            chainId, collectionId, skipCount, beginUtcStampSecond, endUtcStampSecond, DateTimeOffset
                .FromUnixTimeSeconds(beginUtcStampSecond).ToLocalTime().DateTime.ToString("O"), DateTimeOffset
                .FromUnixTimeSeconds(endUtcStampSecond).ToLocalTime().DateTime.ToString("O"));
        
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTActivityIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Type).Terms(NFTActivityType.Sale, NFTActivityType.PlaceBid)));
        mustQuery.Add(q => q.DateRange(i
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.Timestamp)).GreaterThan(DateTimeOffset
                .FromUnixTimeSeconds(beginUtcStampSecond).ToLocalTime().DateTime.ToString("O"))));
        mustQuery.Add(q => q.DateRange(i
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.Timestamp)).LessThan(DateTimeOffset
                .FromUnixTimeSeconds(endUtcStampSecond).ToLocalTime().DateTime.ToString("O"))));
        mustQuery.Add(q => q
            .Script(sc => sc
                .Script(script =>
                    script.Source($"{"doc['nftInfoId'].value.contains('"+collectionSymbolPre+"')"}")
                )
            )
        );
        
        QueryContainer Filter(QueryContainerDescriptor<NFTActivityIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await _nftActivityIndexRepository.GetListAsync(Filter, sortExp: k => k.Id,
            sortType: SortOrder.Ascending,skip: skipCount);
        return result;
    }

    private async Task<SymbolAuctionInfoIndex> QueryMinPriceForSymbolAuctionInfoIndexAsync(string chainId,
        string symbol,long beginStampSecond,long endStampSecond)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SymbolAuctionInfoIndex>, QueryContainer>>();
        
        var mustNotQuery = new List<Func<QueryContainerDescriptor<SymbolAuctionInfoIndex>, QueryContainer>>();
        
        mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionSymbol).Value(symbol)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));

        if (beginStampSecond > 0)
        {
            mustNotQuery.Add(q => q.LongRange(
                i => i.Field(f => f.MaxEndTime).LessThan(beginStampSecond)));
        }

        if (endStampSecond > 0)
        {
            mustNotQuery.Add(q => q.LongRange(
                i => i.Field(f => f.StartTime).GreaterThan(endStampSecond)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<SymbolAuctionInfoIndex> f)
            => f.Bool(b => b.Must(mustQuery).MustNot(mustNotQuery));

        var result = await _symbolAuctionInfoIndexRepository.GetListAsync(Filter, sortExp: k => k.FinishPrice.Amount,
            sortType: SortOrder.Ascending, limit: 1);

        
        return result?.Item2?.FirstOrDefault();
    }

    private async Task<NFTListingInfoIndex> QueryMinPriceWithTimestampForNFTListingInfoIndexAsync(string chainId, string symbol,
        long beginStampSecond, long endStampSecond)
    {
        
        var optionSGRCollection = _generalityOptions.SGRCollection;
        var decimals = 0;
        if (optionSGRCollection.IsNullOrEmpty())
        {
            optionSGRCollection = ForestIndexerConstants.SGRCollection;
        }

        if (optionSGRCollection.Equals(symbol))
        {
            decimals = ForestIndexerConstants.SGRDecimal;
        }
        _logger.LogInformation(" QueryMinPriceWithTimestampForNFTListingInfoIndexAsync Get SGRCollection:{SGR}",optionSGRCollection);
        var minQuantity = (int)(1 * Math.Pow(10, decimals));
        
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();
        var mustNotQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();
        
        mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionSymbol).Value(symbol)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
        //Add conditions within effective time
        
        //add RealQuantity > 0
        mustQuery.Add(q => q.TermRange(i
            => i.Field(index => index.RealQuantity).GreaterThanOrEquals(minQuantity.ToString())));
        QueryContainer Filter(QueryContainerDescriptor<NFTListingInfoIndex> f)
            => f.Bool(b => b.Must(mustQuery).MustNot(mustNotQuery));

        mustNotQuery.Add(q => q.DateRange(i 
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime)).LessThan(DateTimeOffset.FromUnixTimeSeconds(beginStampSecond).ToLocalTime().DateTime.ToString("O"))));
        mustNotQuery.Add(q => q.DateRange(i 
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.StartTime)).GreaterThan(DateTimeOffset.FromUnixTimeSeconds(endStampSecond).ToLocalTime().DateTime.ToString("O"))));
        
        var result = await _nftListingInfoIndexRepository.GetListAsync(Filter, sortExp: k => k.Prices,
            sortType: SortOrder.Ascending, limit: 1);

        return result?.Item2?.FirstOrDefault();
    }
    private async Task<NFTListingInfoIndex> QueryMinPriceForNFTListingInfoIndexAsync(string chainId, string symbol)
    {
        var optionSGRCollection = _generalityOptions.SGRCollection;
        var decimals = 0;
        if (optionSGRCollection.IsNullOrEmpty())
        {
            optionSGRCollection = ForestIndexerConstants.SGRCollection;
        }

        if (optionSGRCollection.Equals(symbol))
        {
            decimals = ForestIndexerConstants.SGRDecimal;
        }
        _logger.LogInformation("Get SGRCollection:{SGR}",optionSGRCollection);
        var minQuantity = (int)(1 * Math.Pow(10, decimals));
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionSymbol).Value(symbol)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
        //Add conditions within effective time
        mustQuery.Add(q => q.TermRange(i 
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime)).GreaterThan(DateTime.UtcNow.ToString("O"))));
        //add RealQuantity > 0
        mustQuery.Add(q => q.TermRange(i
            => i.Field(index => index.RealQuantity).GreaterThanOrEquals(minQuantity.ToString())));
        QueryContainer Filter(QueryContainerDescriptor<NFTListingInfoIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await _nftListingInfoIndexRepository.GetListAsync(Filter, sortExp: k => k.Prices,
            sortType: SortOrder.Ascending, limit: 1);

        return result?.Item2?.FirstOrDefault();
    }
}