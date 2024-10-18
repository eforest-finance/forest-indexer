using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors.Provider;
using Forest.Indexer.Plugin.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;
//todo V2 code:doing
public class ListedNFTChangedLogEventProcessor : LogEventProcessorBase<ListedNFTChanged>
{
    private readonly IObjectMapper _objectMapper;
    private readonly INFTInfoProvider _nftInfoProvider;
    private readonly ICollectionChangeProvider _collectionChangeProvider;
    private readonly INFTListingChangeProvider _listingChangeProvider;


    public ListedNFTChangedLogEventProcessor(
        INFTInfoProvider nftInfoProvider,
        ICollectionChangeProvider collectionChangeProvider,
        INFTListingChangeProvider listingChangeProvider,
        IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
        _nftInfoProvider = nftInfoProvider;
        _collectionChangeProvider = collectionChangeProvider;
        _listingChangeProvider = listingChangeProvider;
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
                throw new UserFriendlyException("nftInfo NOT FOUND");
            
            
            var purchaseTokenId = IdGenerateHelper.GetId(context.ChainId, eventValue.Price.Symbol);
            var tokenIndex = await GetEntityAsync<TokenInfoIndex>(purchaseTokenId);
            if (tokenIndex == null)
                throw new UserFriendlyException($"Purchase token {context.ChainId}-{eventValue.Price.Symbol} NOT FOUND");
                                
            listedNFTIndex.Prices = eventValue.Price.Amount / (decimal)Math.Pow(10, tokenIndex.Decimals);
            listedNFTIndex.PurchaseToken = tokenIndex;
            listedNFTIndex.Quantity = eventValue.Quantity;
            listedNFTIndex.RealQuantity = Math.Min(eventValue.Quantity, listedNFTIndex.RealQuantity);

            // copy block data
            _objectMapper.Map(context, listedNFTIndex);

            await UpdateListedInfoCommonAsync(context.ChainId, eventValue.Symbol, context, listedNFTIndex,"");

            Logger.LogDebug("[ListedNFTChanged] SAVE:, ChainId={ChainId}, symbol={Symbol}, Quantity={Quantity}, Id={Id}",
                context.ChainId, eventValue.Symbol, eventValue.Quantity, listedNftIndexId);

            await SaveEntityAsync(listedNFTIndex);

            await _collectionChangeProvider.SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);
            await _listingChangeProvider.SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);

            Logger.LogDebug("[ListedNFTChanged] FINISH: Id={Id}", listedNftIndexId);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "ListedNFTChanged error, listedNFTIndexId={Id}", listedNftIndexId);
            throw;
        }
    }
    private async Task<UpdateListedInfoResponse> UpdateListedInfoCommonAsync(string chainId, string symbol,
        LogEventContext context,
        NFTListingInfoIndex listingInfoNftInfoIndex, string excludeListingId)
    {
        Logger.LogDebug("UpdateListedInfoCommonAsync1"+chainId+" "+symbol+" "+excludeListingId+" "+JsonConvert.SerializeObject(context));
        Logger.LogDebug("UpdateListedInfoCommonAsync2"+chainId+" "+symbol+" "+excludeListingId+" "+JsonConvert.SerializeObject(listingInfoNftInfoIndex));
        UpdateListedInfoResponse response = null;
        if (SymbolHelper.CheckSymbolIsSeedSymbol(symbol))
        {
            Logger.LogDebug("UpdateListedInfoCommonAsync3"+chainId+" "+symbol+" "+excludeListingId);
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
            Logger.LogDebug("UpdateListedInfoCommonAsync4"+chainId+" "+symbol+" "+excludeListingId);
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
        var minNftListing = await GetMinListingNftAsync(seedSymbolIndex.Id, deleteListingId, listingInfoNftInfoIndex);
        seedSymbolIndex.OfMinNftListingInfo(minNftListing);

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
        var nftListingInfos = await _listingInfoProvider.GetEffectiveNftListingInfos(nftInfoId, excludeListingIds);
        if (current != null && !current.Id.IsNullOrWhiteSpace())        
        {
            Logger.LogDebug(
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

        Logger.LogDebug(
            "GetMinNftListingAsync nftInfoId:{nftInfoId} minNftListing id:{id} minListingPrice:{minListingPrice}",
            nftInfoId, minNftListing?.Id, minNftListing?.Prices);
        return minNftListing;
    }

     private async Task<NFTInfoIndex> UpdateListedInfoForCommonNFTAsync(string chainId, string symbol,
        LogEventContext context,
        NFTListingInfoIndex listingInfoNftInfoIndex, string deleteListingId)
    {
        Logger.LogDebug("UpdateListedInfoCommonAsync-5"+chainId+" "+symbol+" "+deleteListingId+" "+JsonConvert.SerializeObject(listingInfoNftInfoIndex));
        Logger.LogDebug("UpdateListedInfoCommonAsync-6"+chainId+" "+symbol+" "+deleteListingId+" "+JsonConvert.SerializeObject(context));

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
        var minNftListing = await GetMinListingNftAsync(nftInfo.Id, deleteListingId, listingInfoNftInfoIndex);
        nftInfo.OfMinNftListingInfo(minNftListing);
        
        _objectMapper.Map(context, nftInfo);
        await SaveEntityAsync(nftInfo);

        await UpdateUserBanlanceBynftInfoIdAsync(nftInfo, context, 0L);
        return nftInfo;
    }
     public async Task UpdateUserBanlanceBynftInfoIdAsync(NFTInfoIndex nftInfoIndex, LogEventContext context,
         long beginBlockHeight)
     {
         if (nftInfoIndex == null || context == null || nftInfoIndex.Id.IsNullOrWhiteSpace() || beginBlockHeight < 0) return;

         var result = await QueryAndUpdateUserBanlanceBynftInfoId(nftInfoIndex, beginBlockHeight, context.Block.BlockHeight);
         if (result != null && result.Item1 > 0 && result.Item2 != null)
         {
             beginBlockHeight = result.Item2.Last().BlockHeight;
             foreach (var userBalanceIndex in result.Item2)
             {
                 userBalanceIndex.ListingPrice = nftInfoIndex.ListingPrice;
                 userBalanceIndex.ListingTime = nftInfoIndex.LatestListingTime;
                 _objectMapper.Map(context, userBalanceIndex);
                 await SaveEntityAsync(userBalanceIndex);

             }

             await UpdateUserBanlanceBynftInfoIdAsync(nftInfoIndex, context, beginBlockHeight);
         }
     }
     private async Task<Tuple<long,List<UserBalanceIndex>>> QueryAndUpdateUserBanlanceBynftInfoId(NFTInfoIndex nftInfoIndex, long blockHeight,long temMaxBlockHeight)
     {
         var mustQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>();
         mustQuery.Add(q => q.Range(i => i.Field(index => index.BlockHeight).GreaterThan(blockHeight)));
         mustQuery.Add(q => q.Range(i => i.Field(index => index.BlockHeight).LessThan(temMaxBlockHeight)));
         mustQuery.Add(q => q.Term(i => i.Field(index => index.NFTInfoId).Value(nftInfoIndex.Id)));

         QueryContainer FilterForUserBalance(QueryContainerDescriptor<UserBalanceIndex> f)
             => f.Bool(b => b.Must(mustQuery));

         var resultUserBalanceIndex = await _userBalanceIndexRepository.GetListAsync(FilterForUserBalance,
             sortType: SortOrder.Ascending,
             sortExp: o => o.BlockHeight, skip: 0, limit: 100);
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
         var mustQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();

         mustQuery.Add(q => q.TermRange(i
             => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime)).GreaterThan(DateTime.UtcNow.ToString("O"))));
         mustQuery.Add(q=>q.Term(i=>i.Field(index=>index.NftInfoId).Value(nftInfoId)));
        
         var mustNotQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();
         if (!noListingOwner.IsNullOrEmpty())
         {
             mustNotQuery.Add(q=>q.Term(i=>i.Field(index=>index.Owner).Value(noListingOwner)));

         }

         if (!noListingId.IsNullOrEmpty())
         {
             mustNotQuery.Add(q=>q.Term(i=>i.Field(index=>index.Id).Value(noListingId)));
         }
       
         QueryContainer Filter(QueryContainerDescriptor<NFTListingInfoIndex> f) => f.Bool(b => b.Must(mustQuery)
             .MustNot(mustNotQuery));
         var result = await _listedNFTIndexRepository.GetListAsync(Filter, sortExp: k => k.BlockHeight,
             sortType: SortOrder.Descending, skip: 0, limit: 1);
         return result?.Item2?.FirstOrDefault();
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