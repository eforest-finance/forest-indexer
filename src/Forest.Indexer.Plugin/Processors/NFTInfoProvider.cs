using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public interface INFTInfoProvider
{
    public Task<UpdateOfferResponse> UpdateOfferCommonAsync(string chainId, string symbol,
        LogEventContext context,
        OfferInfoIndex offerInfoIndex, string excludeOfferId);

    public Task<UpdateListedInfoResponse> UpdateListedInfoCommonAsync(string chainId, string symbol,
        LogEventContext context,
        NFTListingInfoIndex listingInfoNftInfoIndex, string excludeListingId);

    public Task<bool> AddNFTActivityAsync(LogEventContext context, NFTActivityIndex nftActivityIndex);
    
    public Task<NFTListingInfoIndex> GetMinListingNftAsync(string nftInfoId);

    public Task<NFTListingInfoIndex> GetMinListingNftAsync(string nftInfoId, string excludeListingId, NFTListingInfoIndex current);
    
    public Task<OfferInfoIndex> GetMaxOfferInfoAsync(string nftInfoId);
    
    public Task<OfferInfoIndex> GetMaxOfferInfoAsync(string nftInfoId, string excludeOfferId, OfferInfoIndex current);
}

public class NFTInfoProvider : INFTInfoProvider, ISingletonDependency
{
    private readonly IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> _nftInfoIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> _tokenInfoIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo> _nftActivityIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo> _listedNftIndexRepository;

    private readonly ILogger<NFTInfoProvider> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly INFTListingInfoProvider _listingInfoProvider;
    private readonly INFTOfferProvider _nftOfferInfoProvider;

    public NFTInfoProvider(
        IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> nftInfoIndexRepository,
        IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> tokenInfoIndexRepository,
        IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo> nftActivityIndexRepository,
        IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo> listedNftIndexRepository,
        IUserBalanceProvider userBalanceProvider,
        INFTListingInfoProvider listingInfoProvider,
        IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> seedSymbolIndexRepository,
        INFTOfferProvider nftOfferInfoProvider,
        IObjectMapper objectMapper, ILogger<NFTInfoProvider> logger)
    {
        _nftInfoIndexRepository = nftInfoIndexRepository;
        _tokenInfoIndexRepository = tokenInfoIndexRepository;
        _userBalanceProvider = userBalanceProvider;
        _listingInfoProvider = listingInfoProvider;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _nftOfferInfoProvider = nftOfferInfoProvider;
        _objectMapper = objectMapper;
        _logger = logger;
        _listedNftIndexRepository = listedNftIndexRepository;
        _nftActivityIndexRepository = nftActivityIndexRepository;
    }

    public async Task<UpdateOfferResponse> UpdateOfferCommonAsync(string chainId, string symbol,
        LogEventContext context,
        OfferInfoIndex offerInfoIndex, string excludeOfferId)
    {
        UpdateOfferResponse response = null;
        if (SymbolHelper.CheckSymbolIsSeedSymbol(symbol))
        {
            var nftInfoIndex = await UpdateOfferForSeedAsync(chainId, symbol, context,
                offerInfoIndex, excludeOfferId);
            if (nftInfoIndex == null) return response;
            response = new UpdateOfferResponse
            {
                NftInfoId = nftInfoIndex.Id,
            };
        }
        else if (SymbolHelper.CheckSymbolIsNoMainChainNFT(symbol, chainId))
        {
            var nftInfoIndex = await UpdateOfferForCommonNFTAsync(chainId, symbol, context,
                offerInfoIndex, excludeOfferId);
            if (nftInfoIndex == null) return response;
            response = new UpdateOfferResponse
            {
                NftInfoId = nftInfoIndex.Id,
            };
        }

        return response;
    }

    private async Task<NFTInfoIndex> UpdateOfferForCommonNFTAsync(string chainId, string symbol,
        LogEventContext context,
        OfferInfoIndex offerInfoIndex, string excludeOfferId)
    {
        _logger.Debug("UpdateOfferForCommonNFTAsync-1+symbol:{symbol}  nftOfferIndex:{nftOfferIndex}",symbol,JsonConvert.SerializeObject(offerInfoIndex));
        if (symbol.IsNullOrWhiteSpace() || chainId.IsNullOrWhiteSpace() || offerInfoIndex == null) return null;

        var nftInfo = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(
            IdGenerateHelper.GetNFTInfoId(chainId, symbol), chainId);
        if (nftInfo == null) return null;

        nftInfo.OfferPrice = offerInfoIndex.Price;
        nftInfo.OfferQuantity = offerInfoIndex.Quantity;
        nftInfo.OfferExpireTime = offerInfoIndex.ExpireTime;
        nftInfo.LatestOfferTime = context.BlockTime;

        if (offerInfoIndex.PurchaseToken != null &&
            !offerInfoIndex.PurchaseToken.Symbol.IsNullOrWhiteSpace())
        {
            var tokenInfoId = IdGenerateHelper.GetTokenInfoId(chainId, offerInfoIndex.PurchaseToken.Symbol);
            var tokenInfo = await _tokenInfoIndexRepository.GetFromBlockStateSetAsync(tokenInfoId, chainId);

            nftInfo.OfferToken = tokenInfo;
        }
        else
        {
            nftInfo.OfferToken = null;
        }
        //query history listing + current and compare.
        var maxOfferInfo = await GetMaxOfferInfoAsync(nftInfo.Id, excludeOfferId, offerInfoIndex);
        nftInfo.OfMaxOfferInfo(maxOfferInfo);

        _objectMapper.Map(context, nftInfo);
        await _nftInfoIndexRepository.AddOrUpdateAsync(nftInfo);

        return nftInfo;
    }

    private async Task<SeedSymbolIndex> UpdateOfferForSeedAsync(string chainId, string symbol, LogEventContext context,
        OfferInfoIndex offerInfoIndex, string excludeOfferId)
    {
        if (symbol.IsNullOrWhiteSpace() || chainId.IsNullOrWhiteSpace() || offerInfoIndex == null) return null;

        var seedSymbolIndex = await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(
            IdGenerateHelper.GetSeedSymbolId(chainId, symbol), chainId);
        if (seedSymbolIndex == null) return null;

        seedSymbolIndex.OfferPrice = offerInfoIndex.Price;
        seedSymbolIndex.OfferQuantity = offerInfoIndex.Quantity;
        seedSymbolIndex.OfferExpireTime = offerInfoIndex.ExpireTime;
        seedSymbolIndex.LatestOfferTime = context.BlockTime;

        if (offerInfoIndex.PurchaseToken != null &&
            !offerInfoIndex.PurchaseToken.Symbol.IsNullOrWhiteSpace())
        {
            var tokenInfoId = IdGenerateHelper.GetTokenInfoId(chainId, offerInfoIndex.PurchaseToken.Symbol);
            var tokenInfo = await _tokenInfoIndexRepository.GetFromBlockStateSetAsync(tokenInfoId, chainId);

            seedSymbolIndex.OfferToken = tokenInfo;
        }
        else
        {
            seedSymbolIndex.OfferToken = null;
        }
        
        //query history listing + current and compare.
        var maxOfferInfo = await GetMaxOfferInfoAsync(seedSymbolIndex.Id, excludeOfferId, offerInfoIndex);
        seedSymbolIndex.OfMaxOfferInfo(maxOfferInfo);
        
        _objectMapper.Map(context, seedSymbolIndex);
        await _seedSymbolIndexRepository.AddOrUpdateAsync(seedSymbolIndex);

        return seedSymbolIndex;
    }

    public async Task<UpdateListedInfoResponse> UpdateListedInfoCommonAsync(string chainId, string symbol,
        LogEventContext context,
        NFTListingInfoIndex listingInfoNftInfoIndex, string excludeListingId)
    {
        _logger.Debug("UpdateListedInfoCommonAsync1"+chainId+" "+symbol+" "+excludeListingId+" "+JsonConvert.SerializeObject(context));
        _logger.Debug("UpdateListedInfoCommonAsync2"+chainId+" "+symbol+" "+excludeListingId+" "+JsonConvert.SerializeObject(listingInfoNftInfoIndex));
        UpdateListedInfoResponse response = null;
        if (SymbolHelper.CheckSymbolIsSeedSymbol(symbol))
        {
            _logger.Debug("UpdateListedInfoCommonAsync3"+chainId+" "+symbol+" "+excludeListingId);
            var nftInfoIndex = await UpdateListedInfoForSeedAsync(chainId, symbol, context,
                listingInfoNftInfoIndex, excludeListingId);
            if (nftInfoIndex == null) return response;
            response = new UpdateListedInfoResponse
            {
                NftInfoId = nftInfoIndex.Id,
                ListingQuantity = nftInfoIndex.ListingQuantity,
                ListingPrice = nftInfoIndex.ListingPrice
            };
        }
        else if (SymbolHelper.CheckSymbolIsNoMainChainNFT(symbol, chainId))
        {
            _logger.Debug("UpdateListedInfoCommonAsync4"+chainId+" "+symbol+" "+excludeListingId);
            var nftInfoIndex = await UpdateListedInfoForCommonNFTAsync(chainId, symbol, context,
                listingInfoNftInfoIndex, excludeListingId);
            if (nftInfoIndex == null) return response;
            response = new UpdateListedInfoResponse
            {
                NftInfoId = nftInfoIndex.Id,
                ListingQuantity = nftInfoIndex.ListingQuantity,
                ListingPrice = nftInfoIndex.ListingPrice
            };
        }

        return response;
    }

    private async Task<NFTInfoIndex> UpdateListedInfoForCommonNFTAsync(string chainId, string symbol,
        LogEventContext context,
        NFTListingInfoIndex listingInfoNftInfoIndex, string deleteListingId)
    {
        _logger.Debug("UpdateListedInfoCommonAsync-5"+chainId+" "+symbol+" "+deleteListingId+" "+JsonConvert.SerializeObject(listingInfoNftInfoIndex));
        _logger.Debug("UpdateListedInfoCommonAsync-6"+chainId+" "+symbol+" "+deleteListingId+" "+JsonConvert.SerializeObject(context));

        if (symbol.IsNullOrWhiteSpace() || chainId.IsNullOrWhiteSpace() || listingInfoNftInfoIndex == null) return null;

        var nftInfo = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(
            IdGenerateHelper.GetNFTInfoId(chainId, symbol), chainId);
        
        if (nftInfo == null) return null;

        nftInfo.ListingAddress = listingInfoNftInfoIndex.Owner;
        nftInfo.ListingId = listingInfoNftInfoIndex.Id;
        nftInfo.LatestListingTime = listingInfoNftInfoIndex.PublicTime;
        nftInfo.ListingPrice = listingInfoNftInfoIndex.Prices;
        nftInfo.ListingQuantity = listingInfoNftInfoIndex.Quantity;
        nftInfo.ListingEndTime = listingInfoNftInfoIndex.ExpireTime;

        nftInfo.LatestListingTime = context.BlockTime;

        if (listingInfoNftInfoIndex.PurchaseToken != null &&
            !listingInfoNftInfoIndex.PurchaseToken.Symbol.IsNullOrWhiteSpace())
        { 
            var tokenInfoId = IdGenerateHelper.GetTokenInfoId(chainId, listingInfoNftInfoIndex.PurchaseToken.Symbol);
            var tokenInfo = await _tokenInfoIndexRepository.GetFromBlockStateSetAsync(tokenInfoId, chainId);

            nftInfo.ListingToken = tokenInfo;
        }
        else
        {
            nftInfo.ListingToken = null;
        }
        
        nftInfo.OtherOwnerListingFlag =
            await CheckOtherListExistAsync(nftInfo.Id, nftInfo.ListingAddress, deleteListingId);

        //query history listing + current and compare.
        var minNftListing = await GetMinListingNftAsync(nftInfo.Id, deleteListingId, listingInfoNftInfoIndex);
        nftInfo.OfMinNftListingInfo(minNftListing);
        
        _objectMapper.Map(context, nftInfo);
        await _nftInfoIndexRepository.AddOrUpdateAsync(nftInfo);

        await _userBalanceProvider.UpdateUserBanlanceBynftInfoIdAsync(nftInfo, context, 0L);
        return nftInfo;
    }

    private async Task<SeedSymbolIndex> UpdateListedInfoForSeedAsync(string chainId, string symbol,
        LogEventContext context,
        NFTListingInfoIndex listingInfoNftInfoIndex, string deleteListingId)
    {
        if (symbol.IsNullOrWhiteSpace() || chainId.IsNullOrWhiteSpace() || listingInfoNftInfoIndex == null) return null;

        var seedSymbolIndex = await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(
            IdGenerateHelper.GetSeedSymbolId(chainId, symbol), chainId);
        if (seedSymbolIndex == null) return null;

        seedSymbolIndex.ListingAddress = listingInfoNftInfoIndex.Owner;
        seedSymbolIndex.ListingId = listingInfoNftInfoIndex.Id;
        seedSymbolIndex.LatestListingTime = listingInfoNftInfoIndex.PublicTime;
        seedSymbolIndex.ListingPrice = listingInfoNftInfoIndex.Prices;
        seedSymbolIndex.ListingQuantity = listingInfoNftInfoIndex.Quantity;
        seedSymbolIndex.ListingEndTime = listingInfoNftInfoIndex.ExpireTime;

        seedSymbolIndex.LatestListingTime = context.BlockTime;

        if (listingInfoNftInfoIndex.PurchaseToken != null &&
            !listingInfoNftInfoIndex.PurchaseToken.Symbol.IsNullOrWhiteSpace())
        {
            var tokenInfoId = IdGenerateHelper.GetTokenInfoId(chainId, listingInfoNftInfoIndex.PurchaseToken.Symbol);
            var tokenInfo = await _tokenInfoIndexRepository.GetFromBlockStateSetAsync(tokenInfoId, chainId);

            seedSymbolIndex.ListingToken = tokenInfo;
        }
        else
        {
            seedSymbolIndex.ListingToken = null;
        }

        seedSymbolIndex.OtherOwnerListingFlag =
            await CheckOtherListExistAsync(seedSymbolIndex.Id, seedSymbolIndex.ListingAddress, deleteListingId);
        
        //query history listing + current and compare.
        var minNftListing = await GetMinListingNftAsync(seedSymbolIndex.Id, deleteListingId, listingInfoNftInfoIndex);
        seedSymbolIndex.OfMinNftListingInfo(minNftListing);

        _objectMapper.Map(context, seedSymbolIndex);
        await _seedSymbolIndexRepository.AddOrUpdateAsync(seedSymbolIndex);

        return seedSymbolIndex;
    }

    public async Task<bool> AddNFTActivityAsync(LogEventContext context, NFTActivityIndex nftActivityIndex)
    {
        // NFT activity
        var nftActivityIndexExists =
            await _nftActivityIndexRepository.GetFromBlockStateSetAsync(nftActivityIndex.Id, context.ChainId);
        if (nftActivityIndexExists != null)
        {
            _logger.Debug("[AddNFTActivityAsync] FAIL: activity EXISTS, nftActivityIndexId={Id}", nftActivityIndex.Id);
            return false;
        }

        var from = nftActivityIndex.From;
        var to = nftActivityIndex.To;
        _objectMapper.Map(context, nftActivityIndex);
        nftActivityIndex.From = from;
        nftActivityIndex.To = to;

        _logger.Debug("[AddNFTActivityAsync] SAVE: activity SAVE, nftActivityIndexId={Id}", nftActivityIndex.Id);
        await _nftActivityIndexRepository.AddOrUpdateAsync(nftActivityIndex);

        _logger.Debug("[AddNFTActivityAsync] SAVE: activity FINISH, nftActivityIndexId={Id}", nftActivityIndex.Id);
        return true;
    }

    public async Task<NFTListingInfoIndex> GetMinListingNftAsync(string nftInfoId)
    {
        //After the listing and the transaction is recorded, listing will be deleted first, but the transfer can query it.
        //So add check data in memory 
        return await GetMinListingNftAsync(nftInfoId, null, null, async info =>
        {
            var listingInfo = await _listedNftIndexRepository.GetFromBlockStateSetAsync(info.Id, info.ChainId);
            return listingInfo != null;
        });
    }
    
    public async Task<NFTListingInfoIndex> GetMinListingNftAsync(string nftInfoId, string excludeListingId, NFTListingInfoIndex current)
    {
        return await GetMinListingNftAsync(nftInfoId, excludeListingId, current, info => Task.FromResult(true));
    }
    
    private async Task<NFTListingInfoIndex> GetMinListingNftAsync(string nftInfoId, string excludeListingId, NFTListingInfoIndex current, 
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
            _logger.LogInformation(
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

        _logger.LogInformation(
            "GetMinNftListingAsync nftInfoId:{nftInfoId} minNftListing id:{id} minListingPrice:{minListingPrice}",
            nftInfoId, minNftListing?.Id, minNftListing?.Prices);
        return minNftListing;
    }

    public async Task<OfferInfoIndex> GetMaxOfferInfoAsync(string nftInfoId)
    {
        return await GetMaxOfferInfoAsync(nftInfoId, null, null);
    }

    public async Task<OfferInfoIndex> GetMaxOfferInfoAsync(string nftInfoId, string excludeOfferId, OfferInfoIndex current)
    {
        var offerInfos = await _nftOfferInfoProvider.GetEffectiveNftOfferInfosAsync(nftInfoId, excludeOfferId);
        if (current != null)
        {
            _logger.LogInformation(
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
        _logger.LogInformation(
            "GetMaxOfferInfoAsync nftInfoId:{nftInfoId} maxOfferInfo id:{id} maxOfferPrice:{maxOfferPrice}", nftInfoId,
            maxOfferInfo?.Id, maxOfferInfo?.Price);
        return maxOfferInfo;
    }

    private async Task<bool> CheckOtherListExistAsync(string bizId, string noListingOwner, string excludeListingId)
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
    }
    
    private async Task<decimal> QueryMaxPriceExcludeSpecialOfferIdAsync(string bizId, string excludeOfferId)
    {
        var result = await
            _nftOfferInfoProvider.QueryMaxPriceExcludeSpecialOfferIdAsync(bizId, excludeOfferId);
        return result?.Price ?? 0;
    }
}