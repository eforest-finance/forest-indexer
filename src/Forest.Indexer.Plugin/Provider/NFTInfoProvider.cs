using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using Forest.Indexer.Plugin.Entities;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public interface INFTInfoProvider
{
    //public Task<NFTListingInfoIndex> GetMinListingNftAsync(string nftInfoId);

   // public Task<NFTListingInfoIndex> GetMinListingNftAsync(string nftInfoId, string excludeListingId, NFTListingInfoIndex current);
    
    public Task<OfferInfoIndex> GetMaxOfferInfoAsync(string nftInfoId);
    
   // public Task<OfferInfoIndex> GetMaxOfferInfoAsync(string nftInfoId, string excludeOfferId, OfferInfoIndex current);

    //public Task<int> QueryDecimal(string chainId, string symbol);
}

public class NFTInfoProvider : INFTInfoProvider, ISingletonDependency
{
    private readonly IReadOnlyRepository<NFTInfoIndex> _nftInfoIndexRepository;
    private readonly IReadOnlyRepository<SeedSymbolIndex> _seedSymbolIndexRepository;
    private readonly IReadOnlyRepository<TokenInfoIndex> _tokenInfoIndexRepository;
    private readonly IReadOnlyRepository<NFTActivityIndex> _nftActivityIndexRepository;
    private readonly IReadOnlyRepository<NFTListingInfoIndex> _listedNftIndexRepository;
    
    private static readonly IAeFinderLogger Logger;
    private readonly IObjectMapper _objectMapper;
    private readonly INFTOfferProvider _nftOfferInfoProvider;

    public NFTInfoProvider(
        IReadOnlyRepository<NFTInfoIndex> nftInfoIndexRepository,
        IReadOnlyRepository<TokenInfoIndex> tokenInfoIndexRepository,
        IReadOnlyRepository<NFTActivityIndex> nftActivityIndexRepository,
        IReadOnlyRepository<NFTListingInfoIndex> listedNftIndexRepository,
        IReadOnlyRepository<SeedSymbolIndex> seedSymbolIndexRepository,
        INFTOfferProvider nftOfferInfoProvider,
        IObjectMapper objectMapper)
    {
        _nftInfoIndexRepository = nftInfoIndexRepository;
        _tokenInfoIndexRepository = tokenInfoIndexRepository;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _nftOfferInfoProvider = nftOfferInfoProvider;
        _objectMapper = objectMapper;
        _listedNftIndexRepository = listedNftIndexRepository;
        _nftActivityIndexRepository = nftActivityIndexRepository;
    }
    
    /*public async Task<NFTListingInfoIndex> GetMinListingNftAsync(string nftInfoId)
    {
        //After the listing and the transaction is recorded, listing will be deleted first, but the transfer can query it.
        //So add check data in memory 
        return await GetMinListingNftAsync(nftInfoId, null, null, async info =>
        {
            var listingInfo = await _listedNftIndexRepository.GetFromBlockStateSetAsync(info.Id, info.ChainId);
            return listingInfo != null;
        });
    }*/
    
    /*public async Task<NFTListingInfoIndex> GetMinListingNftAsync(string nftInfoId, string excludeListingId, NFTListingInfoIndex current)
    {
        return await GetMinListingNftAsync(nftInfoId, excludeListingId, current, info => Task.FromResult(true));
    }*/
    
    /*private async Task<NFTListingInfoIndex> GetMinListingNftAsync(string nftInfoId, string excludeListingId, NFTListingInfoIndex current, 
        Func<NFTListingInfoIndex, Task<bool>> additionalConditionAsync)
    {
        var excludeListingIds = new HashSet<string>();
        if (!excludeListingId.IsNullOrWhiteSpace())
        {
            excludeListingIds.Add(excludeListingId);
        }

        if (current != null && !current.Id.IsNullOrWhiteSpace())        
        {
            excludeListingIds.Add(current.Id);
        }

        //Get Effective NftListingInfos
        var nftListingInfos = await _listingInfoProvider.GetEffectiveNftListingInfos(nftInfoId, excludeListingIds);
        if (current != null && !current.Id.IsNullOrWhiteSpace())        
        {
            _logger.LogDebug(
                "GetMinNftListingAsync nftInfoId:{nftInfoId} current id:{id} price:{price}", nftInfoId, current.Id, current.Prices);
            nftListingInfos.Add(current);
        }

        //order by price asc, expireTime desc
        nftListingInfos = nftListingInfos.Where(index =>
                DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime) >= DateTime.UtcNow.ToUtcMilliSeconds())
            .OrderBy(info => info.Prices)
            .ThenByDescending(info => DateTimeHelper.ToUnixTimeMilliseconds(info.ExpireTime))
            .ToList();

        NFTListingInfoIndex minNftListing = null;
        //find first listingInfo match: userBalance > 0 and additionalCondition
        foreach (var info in nftListingInfos)
        {
            var userBalanceId = IdGenerateHelper.GetUserBalanceId(info.Owner, info.ChainId, nftInfoId);
            var userBalance = await _userBalanceProvider.QueryUserBalanceByIdAsync(userBalanceId, info.ChainId);

            if (userBalance?.Amount > 0 && await additionalConditionAsync(info))
            {
                minNftListing = info;
                break;
            }
        }

        _logger.LogDebug(
            "GetMinNftListingAsync nftInfoId:{nftInfoId} minNftListing id:{id} minListingPrice:{minListingPrice}",
            nftInfoId, minNftListing?.Id, minNftListing?.Prices);
        return minNftListing;
    }*/

    public async Task<OfferInfoIndex> GetMaxOfferInfoAsync(string nftInfoId)
    {
        return await GetMaxOfferInfoAsync(nftInfoId, null, null);
    }

    public async Task<OfferInfoIndex> GetMaxOfferInfoAsync(string nftInfoId, string excludeOfferId, OfferInfoIndex current)
    {
        var offerInfos = await _nftOfferInfoProvider.GetEffectiveNftOfferInfosAsync(nftInfoId, excludeOfferId);
        if (current != null)
        {
            Logger.LogDebug(
                "GetMaxOfferInfoAsync nftInfoId:{nftInfoId} current id:{id} price:{price}", nftInfoId, current.Id, current.Price);
            offerInfos.Add(current);
        }
        //order by price desc, expireTime desc
        offerInfos = offerInfos.Where(index =>
                DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime) >= DateTime.UtcNow.ToUtcMilliSeconds())
            .OrderByDescending(info => info.Price)
            .ThenByDescending(info => DateTimeHelper.ToUnixTimeMilliseconds(info.ExpireTime))
            .ToList();
        var maxOfferInfo = offerInfos.FirstOrDefault();
        Logger.LogDebug(
            "GetMaxOfferInfoAsync nftInfoId:{nftInfoId} maxOfferInfo id:{id} maxOfferPrice:{maxOfferPrice}", nftInfoId,
            maxOfferInfo?.Id, maxOfferInfo?.Price);
        return maxOfferInfo;
    }

    /*
    public async Task<int> QueryDecimal(string chainId,string symbol)
    {
        var decimals = 0;
        if (SymbolHelper.CheckSymbolIsSeedSymbol(symbol))
        {
            var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, symbol);
            var seedSymbol = await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolId, chainId);
            decimals = seedSymbol.Decimals;
        }
        else if (SymbolHelper.CheckSymbolIsNoMainChainNFT(symbol, chainId))
        {
            var nftIndexId = IdGenerateHelper.GetNFTInfoId(chainId, symbol);
            var nftIndex = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftIndexId, chainId);
            decimals = nftIndex.Decimals;
        }

        return decimals;
    }
    */

    /*private async Task<bool> CheckOtherListExistAsync(string bizId, string noListingOwner, string excludeListingId)
    {
        if (noListingOwner.IsNullOrEmpty()) return false;
        var result = await
            _listingInfoProvider.QueryOtherAddressNFTListingInfoByNFTIdsAsync(new List<string> { bizId },
                noListingOwner, excludeListingId);
        return result != null && result.ContainsKey(bizId);
    }

    private async Task<decimal> QueryMinPriceExcludeSpecialListingIdAsync(string bizId, string excludeListingId)
    {
        var result = await
            _listingInfoProvider.QueryMinPriceExcludeSpecialListingIdAsync(bizId, excludeListingId);
        return result?.Prices ?? 0;
    }*/
    
    private async Task<decimal> QueryMaxPriceExcludeSpecialOfferIdAsync(string bizId, string excludeOfferId)
    {
        var result = await
            _nftOfferInfoProvider.QueryMaxPriceExcludeSpecialOfferIdAsync(bizId, excludeOfferId);
        return result?.Price ?? 0;
    }
}