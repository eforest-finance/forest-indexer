using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;
public class ListedNFTChangedLogEventProcessor : LogEventProcessorBase<ListedNFTChanged>
{
    private readonly IObjectMapper _objectMapper;
    private readonly IReadOnlyRepository<NFTListingInfoIndex> _listedNFTIndexRepository;
    private readonly IReadOnlyRepository<UserBalanceIndex> _userBalanceIndexRepository;


    public ListedNFTChangedLogEventProcessor(
        IReadOnlyRepository<NFTListingInfoIndex> listedNFTIndexRepository,
        IReadOnlyRepository<UserBalanceIndex> userBalanceIndexRepository,
        IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
        _listedNFTIndexRepository = listedNFTIndexRepository;
        _userBalanceIndexRepository = userBalanceIndexRepository;

    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetNFTForestContractAddress(chainId);
    }

    public override async Task ProcessAsync(ListedNFTChanged eventValue, LogEventContext context)
    {
        var listedNftIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.Symbol, eventValue.Owner.ToBase58(),
            eventValue.Duration.StartTime.Seconds);
        Logger.LogDebug(
            "[ListedNFTChanged] START: ChainId={ChainId}, symbol={Symbol}, Quantity={Quantity}, Id={Id}, Owner={Owner}",
            context.ChainId, eventValue.Symbol, eventValue.Quantity, listedNftIndexId, eventValue.Owner);
        try
        {
            var listedNFTIndex = await GetEntityAsync<NFTListingInfoIndex>(listedNftIndexId);
            if (listedNFTIndex == null)
            {
                Logger.LogError("nftInfo NOT FOUND");
                return;
            }

            var purchaseTokenId = IdGenerateHelper.GetId(context.ChainId, eventValue.Price.Symbol);
            var tokenIndex = await GetEntityAsync<TokenInfoIndex>(purchaseTokenId);
            if (tokenIndex == null)
            {
                Logger.LogError("Purchase token {A}-{B} NOT FOUND", context.ChainId, eventValue.Price.Symbol);
                return;
            }
                                
            listedNFTIndex.Prices = eventValue.Price.Amount / (decimal)Math.Pow(10, tokenIndex.Decimals);
            listedNFTIndex.PurchaseToken = tokenIndex;
            listedNFTIndex.Quantity = eventValue.Quantity;
            listedNFTIndex.RealQuantity = Math.Min(eventValue.Quantity, listedNFTIndex.RealQuantity);

            // copy block data
            _objectMapper.Map(context, listedNFTIndex);

            // await UpdateListedInfoCommonAsync(context.ChainId, eventValue.Symbol, context, listedNFTIndex,""); todo v2

            Logger.LogDebug("[ListedNFTChanged] SAVE:, ChainId={ChainId}, symbol={Symbol}, Quantity={Quantity}, Id={Id}",
                context.ChainId, eventValue.Symbol, eventValue.Quantity, listedNftIndexId);

            await SaveEntityAsync(listedNFTIndex);

            await SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);
            await SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);

            Logger.LogDebug("[ListedNFTChanged] FINISH: Id={Id}", listedNftIndexId);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "ListedNFTChanged error, listedNFTIndexId={Id}", listedNftIndexId);
            throw;
        }
    }
    private async Task SaveNFTListingChangeIndexAsync(LogEventContext context, string symbol)
    {
        if (context.ChainId.Equals(ForestIndexerConstants.MainChain))
        {
            return;
        }

        if (symbol.Equals(ForestIndexerConstants.TokenSimpleElf))
        {
            return;
        }

        var nftListingChangeIndex = new NFTListingChangeIndex
        {
            Symbol = symbol,
            Id = IdGenerateHelper.GetId(context.ChainId, symbol),
            UpdateTime = context.Block.BlockTime
        };
        _objectMapper.Map(context, nftListingChangeIndex);
        await SaveEntityAsync(nftListingChangeIndex);
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
    private async Task<UpdateListedInfoResponse> UpdateListedInfoCommonAsync(string chainId, string symbol,
        LogEventContext context,
        NFTListingInfoIndex listingInfoNftInfoIndex, string excludeListingId)
    {
        Logger.LogDebug("UpdateListedInfoCommonAsync1 {A} {B} {C} {D}",chainId,symbol,excludeListingId,JsonConvert.SerializeObject(context));
        Logger.LogDebug("UpdateListedInfoCommonAsync2 {A} {B} {C} {D}",chainId,symbol,excludeListingId,JsonConvert.SerializeObject(listingInfoNftInfoIndex));
        UpdateListedInfoResponse response = null;
        if (SymbolHelper.CheckSymbolIsSeedSymbol(symbol))
        {
            // Logger.LogDebug("UpdateListedInfoCommonAsync3"+chainId+" "+symbol+" "+excludeListingId);
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
    private async Task<SeedSymbolIndex> UpdateListedInfoForSeedAsync(string chainId, string symbol,
        LogEventContext context,
        NFTListingInfoIndex listingInfoNftInfoIndex, string deleteListingId)
    {
        if (symbol.IsNullOrWhiteSpace() || chainId.IsNullOrWhiteSpace() || listingInfoNftInfoIndex == null) return null;

        var seedSymbolIndex = await GetEntityAsync<SeedSymbolIndex>(IdGenerateHelper.GetSeedSymbolId(chainId, symbol));
        if (seedSymbolIndex == null) return null;

        seedSymbolIndex.ListingAddress = listingInfoNftInfoIndex.Owner;
        seedSymbolIndex.ListingId = listingInfoNftInfoIndex.Id;
        seedSymbolIndex.LatestListingTime = listingInfoNftInfoIndex.PublicTime;
        seedSymbolIndex.ListingPrice = listingInfoNftInfoIndex.Prices;
        seedSymbolIndex.ListingQuantity = listingInfoNftInfoIndex.Quantity;
        seedSymbolIndex.ListingEndTime = listingInfoNftInfoIndex.ExpireTime;

        seedSymbolIndex.LatestListingTime = context.Block.BlockTime;

        if (listingInfoNftInfoIndex.PurchaseToken != null &&
            !listingInfoNftInfoIndex.PurchaseToken.Symbol.IsNullOrWhiteSpace())
        {
            var tokenInfoId = IdGenerateHelper.GetTokenInfoId(chainId, listingInfoNftInfoIndex.PurchaseToken.Symbol);
            var tokenInfo = await GetEntityAsync<TokenInfoIndex>(tokenInfoId);

            seedSymbolIndex.ListingToken = tokenInfo;
        }
        else
        {
            seedSymbolIndex.ListingToken = null;
        }

        seedSymbolIndex.OtherOwnerListingFlag =
            await CheckOtherListExistAsync(seedSymbolIndex.Id, seedSymbolIndex.ListingAddress, deleteListingId);
        
        //query history listing + current and compare.
        // var minNftListing = await GetMinListingNftAsync(seedSymbolIndex.Id, deleteListingId, listingInfoNftInfoIndex);
        // seedSymbolIndex.OfMinNftListingInfo(minNftListing);

        _objectMapper.Map(context, seedSymbolIndex);
        await SaveEntityAsync(seedSymbolIndex);

        return seedSymbolIndex;
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
        var nftListingInfos = await GetEffectiveNftListingInfos(nftInfoId, excludeListingIds);
        if (current != null && !current.Id.IsNullOrWhiteSpace())        
        {
            Logger.LogDebug(
                "GetMinNftListingAsync nftInfoId:{nftInfoId} current id:{id} price:{price}", nftInfoId, current.Id, current.Prices);
            nftListingInfos.Add(current);
        }

        //order by price asc, expireTime desc
        nftListingInfos = nftListingInfos.Where(index =>
                index.ExpireTime >= DateTime.UtcNow)
            .OrderBy(info => info.Prices)
            .ThenByDescending(info => info.ExpireTime)
            .ToList();

        NFTListingInfoIndex minNftListing = null;
        //find first listingInfo match: userBalance > 0 and additionalCondition
        foreach (var info in nftListingInfos)
        {
            var userBalanceId = IdGenerateHelper.GetUserBalanceId(info.Owner, info.ChainId, nftInfoId);
            var userBalance = await QueryUserBalanceByIdAsync(userBalanceId, info.ChainId);

            if (userBalance?.Amount > 0 && await additionalConditionAsync(info))
            {
                minNftListing = info;
                break;
            }
        }

        Logger.LogDebug(
            "GetMinNftListingAsync nftInfoId:{nftInfoId} minNftListing id:{id} minListingPrice:{minListingPrice}",
            nftInfoId, minNftListing?.Id, minNftListing?.Prices);
        return minNftListing;
    }
     private async Task<UserBalanceIndex> QueryUserBalanceByIdAsync(string userBalanceId, string chainId)
     {
         if (userBalanceId.IsNullOrWhiteSpace() ||
             chainId.IsNullOrWhiteSpace())
         {
             return null;
         }
         return await GetEntityAsync<UserBalanceIndex>(userBalanceId);
     }
     private async Task<List<NFTListingInfoIndex>> GetEffectiveNftListingInfos(string nftInfoId, HashSet<string> excludeListingIds)
     {
         var queryable = await _listedNFTIndexRepository.GetQueryableAsync();
         queryable = queryable.Where(index=>index.ExpireTime>DateTime.UtcNow);
         queryable = queryable.Where(index => index.NftInfoId == nftInfoId);
        
         if (!excludeListingIds.IsNullOrEmpty())
         {
             queryable = queryable.Where(index=>!excludeListingIds.Contains(index.Id));
         }

         var result = queryable.OrderBy(k => k.Prices).Skip(0).Take(1).ToList();
         return result??new List<NFTListingInfoIndex>();
     }
     private async Task<NFTInfoIndex> UpdateListedInfoForCommonNFTAsync(string chainId, string symbol,
        LogEventContext context,
        NFTListingInfoIndex listingInfoNftInfoIndex, string deleteListingId)
    {
        Logger.LogDebug("UpdateListedInfoCommonAsync-5 {A} {B} {C} {D}", chainId, symbol, deleteListingId,
            JsonConvert.SerializeObject(listingInfoNftInfoIndex));
        Logger.LogDebug("UpdateListedInfoCommonAsync-6 {A} {B} {C} {D}", chainId, symbol, deleteListingId,
            JsonConvert.SerializeObject(context));

        if (symbol.IsNullOrWhiteSpace() || chainId.IsNullOrWhiteSpace() || listingInfoNftInfoIndex == null) return null;

        var nftInfo = await GetEntityAsync<NFTInfoIndex>(IdGenerateHelper.GetNFTInfoId(chainId, symbol));
        
        if (nftInfo == null) return null;

        nftInfo.ListingAddress = listingInfoNftInfoIndex.Owner;
        nftInfo.ListingId = listingInfoNftInfoIndex.Id;
        nftInfo.LatestListingTime = listingInfoNftInfoIndex.PublicTime;
        nftInfo.ListingPrice = listingInfoNftInfoIndex.Prices;
        nftInfo.ListingQuantity = listingInfoNftInfoIndex.Quantity;
        nftInfo.ListingEndTime = listingInfoNftInfoIndex.ExpireTime;

        nftInfo.LatestListingTime = context.Block.BlockTime;

        if (listingInfoNftInfoIndex.PurchaseToken != null &&
            !listingInfoNftInfoIndex.PurchaseToken.Symbol.IsNullOrWhiteSpace())
        { 
            var tokenInfoId = IdGenerateHelper.GetTokenInfoId(chainId, listingInfoNftInfoIndex.PurchaseToken.Symbol);
            var tokenInfo = await GetEntityAsync<TokenInfoIndex>(tokenInfoId);
            nftInfo.ListingToken = tokenInfo;
        }
        else
        {
            nftInfo.ListingToken = null;
        }
        
        nftInfo.OtherOwnerListingFlag =
            await CheckOtherListExistAsync(nftInfo.Id, nftInfo.ListingAddress, deleteListingId);

        //query history listing + current and compare.
        // var minNftListing = await GetMinListingNftAsync(nftInfo.Id, deleteListingId, listingInfoNftInfoIndex);
        // nftInfo.OfMinNftListingInfo(minNftListing);
        
        _objectMapper.Map(context, nftInfo);
        await SaveEntityAsync(nftInfo);

        await UpdateUserBanlanceBynftInfoIdAsync(nftInfo, context, 0L);
        return nftInfo;
    }
     private async Task UpdateUserBanlanceBynftInfoIdAsync(NFTInfoIndex nftInfoIndex, LogEventContext context,
         long beginBlockHeight)
     {
         if (nftInfoIndex == null || context == null || nftInfoIndex.Id.IsNullOrWhiteSpace() || beginBlockHeight < 0) return;

         var result = await QueryAndUpdateUserBanlanceBynftInfoId(nftInfoIndex, beginBlockHeight, context.Block.BlockHeight);
         if (result != null && result.Count > 0)
         {
             beginBlockHeight = result.Last().BlockHeight;
             foreach (var userBalanceIndex in result)
             {
                 userBalanceIndex.ListingPrice = nftInfoIndex.ListingPrice;
                 userBalanceIndex.ListingTime = nftInfoIndex.LatestListingTime;
                 _objectMapper.Map(context, userBalanceIndex);
                 await SaveEntityAsync(userBalanceIndex);

             }

             await UpdateUserBanlanceBynftInfoIdAsync(nftInfoIndex, context, beginBlockHeight);
         }
     }
     private async Task<List<UserBalanceIndex>> QueryAndUpdateUserBanlanceBynftInfoId(NFTInfoIndex nftInfoIndex, long blockHeight,long temMaxBlockHeight)
     {
         var queryable = await _userBalanceIndexRepository.GetQueryableAsync();
         queryable = queryable.Where(x=>x.BlockHeight > blockHeight && x.BlockHeight < temMaxBlockHeight);
         queryable = queryable.Where(x=>x.NFTInfoId == nftInfoIndex.Id );

         var resultUserBalanceIndex = queryable.OrderBy(o => o.BlockHeight).Skip(0).Take(100).ToList();
         return resultUserBalanceIndex;
     }
     private async Task<bool> CheckOtherListExistAsync(string bizId, string noListingOwner, string excludeListingId)
     {
         if (noListingOwner.IsNullOrEmpty()) return false;
         var result = await QueryOtherAddressNFTListingInfoByNFTIdsAsync(new List<string> { bizId },
                 noListingOwner, excludeListingId);
         return result != null && result.ContainsKey(bizId);
     }
     
     public async Task<Dictionary<string, NFTListingInfoIndex>> QueryOtherAddressNFTListingInfoByNFTIdsAsync(
         List<string> nftInfoIds, string noListingOwner, string noListingId)
     {
         if (nftInfoIds == null) return new Dictionary<string, NFTListingInfoIndex>();

         var queryOtherList = new List<Task<NFTListingInfoIndex>>();
         foreach (string nftInfoId in nftInfoIds)
         {
             queryOtherList.Add(QueryOtherWhiteListExistAsync(nftInfoId, noListingOwner, noListingId));
         }

         var otherList = await Task.WhenAll(queryOtherList);
         return await TransferToDicAsync(otherList);
     }
     private async Task<NFTListingInfoIndex> QueryOtherWhiteListExistAsync(string nftInfoId,string noListingOwner ,string noListingId)
     {
         var queryable = await _listedNFTIndexRepository.GetQueryableAsync();
         queryable = queryable.Where(index=>index.ExpireTime>DateTime.UtcNow);
         queryable = queryable.Where(index=>index.NftInfoId == nftInfoId);

         if (!noListingOwner.IsNullOrEmpty())
         {
             queryable = queryable.Where(index=>index.Owner != noListingOwner);
         }

         if (!noListingId.IsNullOrEmpty())
         {
             queryable = queryable.Where(index=>index.Id != noListingId);
         }

         var result = queryable.OrderByDescending(k => k.BlockHeight).Skip(0).Take(1).ToList();
         return result?.FirstOrDefault();
     }
     private async Task<Dictionary<string, NFTListingInfoIndex>> TransferToDicAsync(
         NFTListingInfoIndex[] nftListingInfoIndices)
     {
         if (nftListingInfoIndices == null || nftListingInfoIndices.Length == 0)
             return new Dictionary<string, NFTListingInfoIndex>();

         nftListingInfoIndices = nftListingInfoIndices.Where(x => x != null).ToArray();

         return nftListingInfoIndices == null || nftListingInfoIndices.Length == 0
             ? new Dictionary<string, NFTListingInfoIndex>()
             :
             nftListingInfoIndices.ToDictionary(item => item.NftInfoId);
     }
}