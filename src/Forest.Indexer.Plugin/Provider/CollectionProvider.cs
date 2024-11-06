using System.Linq.Dynamic.Core;
using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using Forest.Indexer.Plugin.Entities;
using Microsoft.IdentityModel.Tokens;
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
    private readonly IReadOnlyRepository<SymbolAuctionInfoIndex> _symbolAuctionInfoIndexRepository;
    
    private readonly IReadOnlyRepository<NFTActivityIndex> _nftActivityIndexRepository;

    private readonly IReadOnlyRepository<NFTListingInfoIndex> _nftListingInfoIndexRepository;
    
    private static readonly IAeFinderLogger Logger;

    public CollectionProvider(
        IReadOnlyRepository<SymbolAuctionInfoIndex>
            symbolAuctionInfoIndexRepository,
        IReadOnlyRepository<NFTListingInfoIndex>
            nftListingInfoIndexRepository,
        IReadOnlyRepository<NFTActivityIndex>
            nftActivityIndexRepository
        )
    {
        _symbolAuctionInfoIndexRepository = symbolAuctionInfoIndexRepository;
        _nftListingInfoIndexRepository = nftListingInfoIndexRepository;
        _nftActivityIndexRepository = nftActivityIndexRepository;
    }
    
    public async Task<decimal> CalcCollectionFloorPriceAsync(string chainId, string symbol, decimal oriFloorPrice)
    {
        //Calculate current FloorPrice
        //symbol is collection symbol.
        var symbolAuctionInfoIndex = await QueryMinPriceForSymbolAuctionInfoIndexAsync(chainId, symbol, 0, 0);
        
        decimal? auctionMinPrice =  symbolAuctionInfoIndex?.FinishPrice?.Amount;
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

        // Logger.LogDebug(
        //     "CalcNFTCollectionTradeSingleAsync chainId={A} collectionId={B} skipCount={C} beginUtcStampSecond={D} endUtcStampSecond={E} beginTime={F} endTime={G}",
        //     chainId, collectionId, skipCount, beginUtcStampSecond, endUtcStampSecond, DateTimeOffset
        //         .FromUnixTimeSeconds(beginUtcStampSecond).ToLocalTime().DateTime.ToString("O"), DateTimeOffset
        //         .FromUnixTimeSeconds(endUtcStampSecond).ToLocalTime().DateTime.ToString("O"));
        var queryable = await _nftActivityIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(f => f.ChainId == chainId);
        var intTypeList = new List<int> { (int)NFTActivityType.Sale,(int)NFTActivityType.PlaceBid};
        queryable = queryable.Where(f => intTypeList.Contains(f.IntType));
        queryable = queryable.Where(f => f.Timestamp > DateTimeHelper.FromUnixTimeMilliseconds(beginUtcStampSecond));
        queryable = queryable.Where(f => f.Timestamp < DateTimeHelper.FromUnixTimeMilliseconds(endUtcStampSecond));
        queryable = queryable.Where(f=>f.NftInfoId.Contains(collectionSymbolPre));

        var result = queryable.OrderBy(k => k.Id).Skip(skipCount).Take(ForestIndexerConstants.DefaultMaxCountNumber).ToList();
        return new Tuple<long, List<NFTActivityIndex>>(result.Count,result);
    }

    private async Task<SymbolAuctionInfoIndex> QueryMinPriceForSymbolAuctionInfoIndexAsync(string chainId,
        string symbol,long beginStampSecond,long endStampSecond)
    {
        var queryable = await _symbolAuctionInfoIndexRepository.GetQueryableAsync();
        if (!symbol.IsNullOrEmpty())
        {
            queryable = queryable.Where(f => f.CollectionSymbol == symbol);
        }

        if (!chainId.IsNullOrEmpty())
        {
            queryable = queryable.Where(f => f.ChainId == chainId);
        }
        
        if (beginStampSecond > 0)
        {
            queryable = queryable.Where(f => f.MaxEndTime >= beginStampSecond);
        }

        if (endStampSecond > 0)
        {
            queryable = queryable.Where(f => f.StartTime <= endStampSecond);
        }

        var result = queryable.OrderBy(k => k.FinishPrice.Amount).Skip(0).Take(1).ToList();
        return result?.FirstOrDefault();
    }

    private async Task<NFTListingInfoIndex> QueryMinPriceWithTimestampForNFTListingInfoIndexAsync(string chainId, string symbol,
        long beginStampSecond, long endStampSecond)
    {
        var queryable = await _nftListingInfoIndexRepository.GetQueryableAsync();

        var optionSGRCollection = "'";
        var decimals = 0;
        if (optionSGRCollection.IsNullOrEmpty())
        {
            optionSGRCollection = ForestIndexerConstants.SGRCollection;
        }

        if (optionSGRCollection.Equals(symbol))
        {
            decimals = ForestIndexerConstants.SGRDecimal;
        }
        // Logger.LogInformation(" QueryMinPriceWithTimestampForNFTListingInfoIndexAsync Get SGRCollection:{SGR}",optionSGRCollection);
        var minQuantity = (int)(1 * Math.Pow(10, decimals));

        if (!symbol.IsNullOrEmpty())
        {
            queryable = queryable.Where(f => f.CollectionSymbol == symbol);
        }

        if (!chainId.IsNullOrEmpty())
        {
            queryable = queryable.Where(f => f.ChainId == chainId);
        }
        
        //Add conditions within effective time
        
        //add RealQuantity > 0
        queryable = queryable.Where(f => f.RealQuantity >= minQuantity);

        if (beginStampSecond > 0)
        {
            queryable = queryable.Where(index => index.ExpireTime >= DateTimeHelper.FromUnixTimeMilliseconds(beginStampSecond));
        }

        if (endStampSecond > 0)
        {
            queryable = queryable.Where(index => index.StartTime <= DateTimeHelper.FromUnixTimeMilliseconds(endStampSecond));
        }
        
        var result = queryable.OrderBy(k => k.Prices).Take(1).ToList();
        return result?.FirstOrDefault();
    }
    private async Task<NFTListingInfoIndex> QueryMinPriceForNFTListingInfoIndexAsync(string chainId, string symbol)
    {
        var queryable = await _nftListingInfoIndexRepository.GetQueryableAsync();

        var optionSGRCollection = "";
        var decimals = 0;
        if (optionSGRCollection.IsNullOrEmpty())
        {
            optionSGRCollection = ForestIndexerConstants.SGRCollection;
        }

        if (optionSGRCollection.Equals(symbol))
        {
            decimals = ForestIndexerConstants.SGRDecimal;
        }
        // Logger.LogInformation("Get SGRCollection:{SGR}",optionSGRCollection);
        var minQuantity = (int)(1 * Math.Pow(10, decimals));
        queryable = queryable.Where(f => f.CollectionSymbol == symbol);
        queryable = queryable.Where(f => f.ChainId == chainId);
        //Add conditions within effective time
        queryable = queryable.Where(f => f.ExpireTime > DateTime.UtcNow);

        //add RealQuantity > 0
        queryable = queryable.Where(index => index.RealQuantity >= minQuantity);

        var result = queryable.OrderBy(k => k.Prices).Skip(0).Take(1).ToList();

        return result?.FirstOrDefault();
    }
}