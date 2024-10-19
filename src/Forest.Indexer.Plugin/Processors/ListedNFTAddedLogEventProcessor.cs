using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using AElf.CSharp.Core.Extension;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class ListedNFTAddedLogEventProcessor : LogEventProcessorBase<ListedNFTAdded>
{
    private readonly IObjectMapper _objectMapper;
    private readonly IReadOnlyRepository<NFTListingInfoIndex> _listedNFTIndexRepository;
    private readonly IReadOnlyRepository<UserBalanceIndex> _userBalanceIndexRepository;
    private static int WriteCount = 0;


    public ListedNFTAddedLogEventProcessor(
        IObjectMapper objectMapper, 
        IReadOnlyRepository<NFTListingInfoIndex> listedNFTIndexRepository,
        IReadOnlyRepository<UserBalanceIndex> userBalanceIndexRepository)
    {
        _objectMapper = objectMapper;
        _listedNFTIndexRepository = listedNFTIndexRepository;
        _userBalanceIndexRepository = userBalanceIndexRepository;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetTokenContractAddress(chainId);
    }

    public override async Task ProcessAsync(ListedNFTAdded eventValue, LogEventContext context)
    {
        var purchaseTokenId = IdGenerateHelper.GetId(context.ChainId, eventValue.Price.Symbol);

        var listedNftIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.Symbol, eventValue.Owner.ToBase58(),
            eventValue.Duration.StartTime.Seconds);
        Logger.LogDebug(
            "[ListedNFTAdded] START: ChainId={ChainId}, symbol={Symbol}, Quantity={Quantity}, Id={Id}, Owner={owner}",
            context.ChainId, eventValue.Symbol, eventValue.Quantity, listedNftIndexId, eventValue.Owner);

        try
        {
            var listingNftInfoIndex = await GetEntityAsync<NFTListingInfoIndex>(listedNftIndexId);
            if (listingNftInfoIndex != null)
                throw new UserFriendlyException("listingInfo EXISTS");

            var tokenIndex = await GetEntityAsync<TokenInfoIndex>(purchaseTokenId);
            if (tokenIndex == null)
                throw new UserFriendlyException($"purchase token {context.ChainId}-{purchaseTokenId} NOT FOUND");

            listingNftInfoIndex = _objectMapper.Map<ListedNFTAdded, NFTListingInfoIndex>(eventValue);
            listingNftInfoIndex.Id = listedNftIndexId;
            listingNftInfoIndex.Prices = eventValue.Price.Amount / (decimal)Math.Pow(10, tokenIndex.Decimals);
            listingNftInfoIndex.RealQuantity = eventValue.Quantity;
            listingNftInfoIndex.PurchaseToken = tokenIndex;
            listingNftInfoIndex.StartTime = eventValue.Duration.StartTime.ToDateTime();
            listingNftInfoIndex.PublicTime = eventValue.Duration.PublicTime.ToDateTime();
            listingNftInfoIndex.DurationHours = eventValue.Duration.DurationHours;
            listingNftInfoIndex.ExpireTime =
                eventValue.Duration.StartTime.AddHours(eventValue.Duration.DurationHours)
                    .AddMinutes(eventValue.Duration.DurationMinutes).ToDateTime();
            listingNftInfoIndex.CollectionSymbol = SymbolHelper.GetNFTCollectionSymbol(eventValue.Symbol);

            // copy block data
            _objectMapper.Map(context, listingNftInfoIndex);

            var updateListedInfoResponse = await UpdateListedInfoCommonAsync(context.ChainId,
                eventValue.Symbol, context, listingNftInfoIndex, "");
            if (updateListedInfoResponse == null) return;
            listingNftInfoIndex.NftInfoId = updateListedInfoResponse.NftInfoId;

            Logger.LogDebug("[ListedNFTAdded] SAVE: ChainId={ChainId}, symbol={Symbol}, Quantity={Quantity}, Id={Id}",
                context.ChainId, eventValue.Symbol, eventValue.Quantity, listedNftIndexId);

            await SaveEntityAsync(listingNftInfoIndex);

            Logger.LogDebug("[ListedNFTAdded] FINISH: ChainId={ChainId}, symbol={Symbol}, Quantity={Quantity}, Id={Id}",
                context.ChainId, eventValue.Symbol, eventValue.Quantity, listedNftIndexId);

            await SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);
            await SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);

            // NFT activity
            var nftActivityIndexId =
                IdGenerateHelper.GetId(context.ChainId, eventValue.Symbol, "LISTED", context.Transaction.TransactionId);

            var decimals = await QueryDecimal(context.ChainId, eventValue.Symbol);

            var activitySaved = await AddNFTActivityAsync(context, new NFTActivityIndex
            {
                Id = nftActivityIndexId,
                Type = NFTActivityType.ListWithFixedPrice,
                From = FullAddressHelper.ToFullAddress(eventValue.Owner.ToBase58(), context.ChainId),
                Amount = TokenHelper.GetIntegerDivision(updateListedInfoResponse.ListingQuantity, decimals),
                Price = updateListedInfoResponse.ListingPrice,
                PriceTokenInfo = tokenIndex,
                TransactionHash = context.Transaction.TransactionId,
                Timestamp = context.Block.BlockTime,
                NftInfoId = updateListedInfoResponse.NftInfoId
            });
            if (!activitySaved) throw new UserFriendlyException("Activity SAVE FAILED");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "[ListedNFTAdded] ERROR: listedNFTIndexId={Id}", listedNftIndexId);
            throw;
        }
    }
    private async Task<int> QueryDecimal(string chainId,string symbol)
    {
        var decimals = 0;
        if (SymbolHelper.CheckSymbolIsSeedSymbol(symbol))
        {
            var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, symbol);
            var seedSymbol = await GetEntityAsync<SeedSymbolIndex>(seedSymbolId);
            decimals = seedSymbol.Decimals;
        }
        else if (SymbolHelper.CheckSymbolIsNoMainChainNFT(symbol, chainId))
        {
            var nftIndexId = IdGenerateHelper.GetNFTInfoId(chainId, symbol);
            var nftIndex = await GetEntityAsync<NFTInfoIndex>(nftIndexId);
            decimals = nftIndex.Decimals;
        }

        return decimals;
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

    private async Task<bool> AddNFTActivityAsync(LogEventContext context, NFTActivityIndex nftActivityIndex)
    {
        // NFT activity
        var nftActivityIndexExists = await GetEntityAsync<NFTActivityIndex>(nftActivityIndex.Id);
        if (nftActivityIndexExists != null)
        {
            Logger.LogDebug("[AddNFTActivityAsync] FAIL: activity EXISTS, nftActivityIndexId={Id}",
                nftActivityIndex.Id);
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

    private async Task<UpdateListedInfoResponse> UpdateListedInfoCommonAsync(string chainId, string symbol,
        LogEventContext context,
        NFTListingInfoIndex listingInfoNftInfoIndex, string excludeListingId)
    {
        Logger.LogDebug("UpdateListedInfoCommonAsync1" + chainId + " " + symbol + " " + excludeListingId + " " +
                        JsonConvert.SerializeObject(context));
        Logger.LogDebug("UpdateListedInfoCommonAsync2" + chainId + " " + symbol + " " + excludeListingId + " " +
                        JsonConvert.SerializeObject(listingInfoNftInfoIndex));
        UpdateListedInfoResponse response = null;
        if (SymbolHelper.CheckSymbolIsSeedSymbol(symbol))
        {
            Logger.LogDebug("UpdateListedInfoCommonAsync3" + chainId + " " + symbol + " " + excludeListingId);
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
            Logger.LogDebug("UpdateListedInfoCommonAsync4" + chainId + " " + symbol + " " + excludeListingId);
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
    private async Task<NFTListingInfoIndex> GetMinListingNftAsync(string nftInfoId, string excludeListingId, NFTListingInfoIndex current)
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
                DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime) >= DateTime.UtcNow.ToUtcMilliSeconds())
            .OrderBy(info => info.Prices)
            .ThenByDescending(info => DateTimeHelper.ToUnixTimeMilliseconds(info.ExpireTime))
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
    private async Task<List<NFTListingInfoIndex>> GetEffectiveNftListingInfos(string nftInfoId, HashSet<string> excludeListingIds)
    {
        var queryable = await _listedNFTIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(index=>DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime)>long.Parse(DateTime.UtcNow.ToString("O")));
        queryable = queryable.Where(index => index.NftInfoId == nftInfoId);
        
        if (!excludeListingIds.IsNullOrEmpty())
        {
            queryable = queryable.Where(index=>!excludeListingIds.Contains(index.Id));
        }

        var result = queryable.Skip(0).OrderBy(k => k.Prices).ToList();
        return result??new List<NFTListingInfoIndex>();
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
        queryable = queryable.Where(index=>DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime)>long.Parse(DateTime.UtcNow.ToString("O")));
        queryable = queryable.Where(index=>index.NftInfoId == nftInfoId);

        if (!noListingOwner.IsNullOrEmpty())
        {
            queryable = queryable.Where(index=>index.Owner != noListingOwner);
        }

        if (!noListingId.IsNullOrEmpty())
        {
            queryable = queryable.Where(index=>index.Id != noListingId);
        }

        var result = queryable.Skip(0).Take(1).OrderByDescending(k => k.BlockHeight);
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
    private async Task<NFTInfoIndex> UpdateListedInfoForCommonNFTAsync(string chainId, string symbol,
        LogEventContext context,
        NFTListingInfoIndex listingInfoNftInfoIndex, string deleteListingId)
    {
        Logger.LogDebug("UpdateListedInfoCommonAsync-5" + chainId + " " + symbol + " " + deleteListingId + " " +
                        JsonConvert.SerializeObject(listingInfoNftInfoIndex));
        Logger.LogDebug("UpdateListedInfoCommonAsync-6" + chainId + " " + symbol + " " + deleteListingId + " " +
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
        var minNftListing = await GetMinListingNftAsync(nftInfo.Id, deleteListingId, listingInfoNftInfoIndex);
        nftInfo.OfMinNftListingInfo(minNftListing);

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
                WriteCount++;
                if (WriteCount >= ForestIndexerConstants.MaxWriteDBRecord)
                {
                    Logger.LogInformation("ListedNFTAddedLogEventProcessor.UpdateUserBanlanceBynftInfoIdAsync recordCount:{A} ,limit:{B}, nftInfoIndex:{C},BlockHeight:{D}",
                        result.Count,ForestIndexerConstants.MaxWriteDBRecord, nftInfoIndex, context.Block.BlockHeight);
                    break;
                }
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

        var resultUserBalanceIndex = queryable.Skip(0).Take(100).OrderByDescending(o => o.BlockHeight).ToList();
        return resultUserBalanceIndex;
    }
}