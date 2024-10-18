using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using AElf.Contracts.MultiToken;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;
//todo V2, code:done needTest: update user list/offers has many record
namespace Forest.Indexer.Plugin.Processors;

public class CrossChainReceivedProcessor : LogEventProcessorBase<CrossChainReceived>
{
    private readonly IObjectMapper _objectMapper;
    private readonly IReadOnlyRepository<NFTListingInfoIndex> _listedNFTIndexRepository;
    private readonly IReadOnlyRepository<OfferInfoIndex> _nftOfferIndexRepository;

    private readonly int MaxQuerySize = 10000;
    private readonly int MaxQueryCount = 5;

    public CrossChainReceivedProcessor(
        IObjectMapper objectMapper,
        IReadOnlyRepository<NFTListingInfoIndex> listedNFTIndexRepository,
        IReadOnlyRepository<OfferInfoIndex> nftOfferIndexRepository)
    {
        _objectMapper = objectMapper;
        _listedNFTIndexRepository = listedNFTIndexRepository;
        _nftOfferIndexRepository = nftOfferIndexRepository;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetTokenContractAddress(chainId);
    }

    public override async Task ProcessAsync(CrossChainReceived eventValue, LogEventContext context)
    {
        Logger.LogDebug("CrossChainReceived-1-eventValue"+JsonConvert.SerializeObject(eventValue));
        Logger.LogDebug("CrossChainReceived-2-context"+JsonConvert.SerializeObject(context));
        if (eventValue == null || context == null) return;
        var needRecordBalance =
            await NeedRecordBalance(eventValue.Symbol, eventValue.To.ToBase58(), context.ChainId);
        if (!needRecordBalance)
        {
            return;
        }
        var userBalance = await SaveUserBalanceAsync(eventValue.Symbol, eventValue.To.ToBase58(),
            eventValue.Amount,
            context);
        await UpdateOfferRealQualityAsync(eventValue.Symbol, userBalance, eventValue.To.ToBase58(), context);
        await UpdateListingInfoRealQualityAsync(eventValue.Symbol, userBalance, eventValue.To.ToBase58(), context);
        await SaveNFTOfferChangeIndexAsync(context, eventValue.Symbol, EventType.Other);

        if (SymbolHelper.CheckSymbolIsSeedSymbol(eventValue.Symbol))
        {
            await HandleEventForSeedAsync(eventValue, context);
        }else if (SymbolHelper.CheckSymbolIsNoMainChainNFT(eventValue.Symbol, context.ChainId))
        {
            await HandleEventForNFTAsync(eventValue, context);
        }
    }
    
    private async Task UpdateOfferRealQualityAsync(string symbol, long balance, string offerFrom,
        LogEventContext context)
    {
        if (context.ChainId.Equals(ForestIndexerConstants.MainChain))
        {
            return;
        }
        if (!SymbolHelper.CheckSymbolIsELF(symbol))
        {
            return;
        }
        int skip = 0;
        int queryCount;
        int limit = 1000;
        do
        {
            var queryable = await _nftOfferIndexRepository.GetQueryableAsync();
            queryable = queryable.Where(index=>DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime) > long.Parse(DateTime.UtcNow.ToString("O")));
            queryable = queryable.Where(index => index.PurchaseToken.Symbol == symbol);
            queryable = queryable.Where(index =>index.ChainId == context.ChainId);
            queryable = queryable.Where(index => index.OfferFrom == offerFrom);
            var result = queryable.Skip(skip).Take(limit).OrderByDescending(x => x.Price).ToList();
    
            if (result.IsNullOrEmpty())
            {
                break;
            }

            var tokenIndexId = IdGenerateHelper.GetId(context.ChainId, symbol);
            var tokenIndex = await GetEntityAsync<TokenInfoIndex>(tokenIndexId);
            if (tokenIndex == null)
            {
                return;
            }

            //update RealQuantity
            foreach (var offerInfoIndex in result)
            {
                if (symbol.Equals(offerInfoIndex!.PurchaseToken.Symbol))
                {
                    var symbolTokenIndexId = IdGenerateHelper.GetId(context.ChainId, offerInfoIndex.BizSymbol);
                    var symbolTokenInfo = await GetEntityAsync<TokenInfoIndex>(symbolTokenIndexId);

                    var canBuyNum = Convert.ToInt64(Math.Floor(Convert.ToDecimal(balance) /
                                                               (offerInfoIndex.Price *
                                                                (decimal)Math.Pow(10,
                                                                    tokenIndex.Decimals))));
                    canBuyNum = (long)(canBuyNum * (decimal)Math.Pow(10, symbolTokenInfo.Decimals));
                    Logger.LogInformation(
                        "UpdateOfferRealQualityAsync  offerInfoIndex.BizSymbol {BizSymbol} canBuyNum {CanBuyNum} Quantity {Quantity} RealQuantity {RealQuantity}",
                        offerInfoIndex.BizSymbol, canBuyNum, offerInfoIndex.Quantity, offerInfoIndex.RealQuantity);
                    var realQuantity = Math.Min((long)offerInfoIndex.Quantity, canBuyNum);
                    if (realQuantity != offerInfoIndex.RealQuantity)
                    {
                        offerInfoIndex.RealQuantity = realQuantity;
                        _objectMapper.Map(context, offerInfoIndex);
                        var research = await GetEntityAsync<OfferInfoIndex>(offerInfoIndex.Id);
                        if (research == null)
                        {
                            Logger.LogInformation(
                                "UpdateOfferRealQualityAsync offerInfoIndex.Id is not exist,not update {OfferInfoIndexId}",
                                offerInfoIndex.Id);
                            continue;
                        }
                        await SaveEntityAsync(offerInfoIndex);
                    }
                }
            }

            queryCount = result.Count;
            skip += limit;
        } while (queryCount == limit);
    }
    private async Task<bool> NeedRecordBalance(string symbol, string offerFrom, string chainId)
    {
        if (!SymbolHelper.CheckSymbolIsELF(symbol))
        {
            return true;
        }

        if (NeedRecordBalanceOptions.AddressList.Contains(offerFrom))
        {
            return true;
        }

        var num = await GetOfferNumAsync(offerFrom, chainId);
        if (num > 0)
        {
            return true;
        }

        return false;
    }
    private async Task<int> GetOfferNumAsync(string offerFrom, string chainId)
    {
        var offerNumId = IdGenerateHelper.GetOfferNumId(chainId, offerFrom);
        var nftOfferNumIndex = await GetEntityAsync<UserNFTOfferNumIndex>(offerNumId);
        if (nftOfferNumIndex == null)
        {
            return 0;
        }

        return nftOfferNumIndex.OfferNum;
    }
    private async Task SaveNFTOfferChangeIndexAsync(LogEventContext context, string symbol, EventType eventType)
    {
        if (context.ChainId.Equals(ForestIndexerConstants.MainChain))
        {
            return;
        }

        if (symbol.Equals(ForestIndexerConstants.TokenSimpleElf))
        {
            return;
        }

        var nftOfferChangeIndex = new NFTOfferChangeIndex
        {
            Id = IdGenerateHelper.GetId(context.ChainId, symbol, Guid.NewGuid()),
            NftId = IdGenerateHelper.GetNFTInfoId(context.ChainId, symbol),
            EventType = eventType,
            CreateTime = context.Block.BlockTime
        };
        
        _objectMapper.Map(context, nftOfferChangeIndex);
        await SaveEntityAsync(nftOfferChangeIndex);

    }
    private async Task HandleEventForSeedAsync(CrossChainReceived eventValue, LogEventContext context)
    {
        //Get the seed owned symbol from seed symbol index
        
        var seedSymbolIndexIdToChainId =
            IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        var seedSymbolIndexToChain = await GetEntityAsync<SeedSymbolIndex>(seedSymbolIndexIdToChainId);
       
        Logger.LogDebug("CrossChainReceived-3-seedSymbolIndexIdToChainId"+seedSymbolIndexIdToChainId);
        Logger.LogDebug("CrossChainReceived-4-seedSymbolIndexToChain"+JsonConvert.SerializeObject(seedSymbolIndexToChain));
        if(seedSymbolIndexToChain == null) return;
        
        seedSymbolIndexToChain.IsDeleteFlag = false;
        seedSymbolIndexToChain.ChainId = context.ChainId;
        seedSymbolIndexToChain.IssuerTo = eventValue.To.ToBase58();
        _objectMapper.Map(context, seedSymbolIndexToChain);
        seedSymbolIndexToChain.Supply = eventValue.Amount;
        //add calc minNftListing
        var minNftListing = await GetMinListingNftAsync(seedSymbolIndexIdToChainId);
        seedSymbolIndexToChain.OfMinNftListingInfo(minNftListing);
        await SaveEntityAsync(seedSymbolIndexToChain);

        //Set the tsm seed symbol index info to the to chain
        var tsmSeedSymbolIndexIdToChainId =
            IdGenerateHelper.GetSeedSymbolId(context.ChainId, seedSymbolIndexToChain.SeedOwnedSymbol);
        var tsmSeedSymbolIndexToChain = await GetEntityAsync<TsmSeedSymbolIndex>(tsmSeedSymbolIndexIdToChainId);
        
        Logger.LogDebug("CrossChainReceived-5-tsmSeedSymbolIndexIdToChainId"+tsmSeedSymbolIndexIdToChainId);
        Logger.LogDebug("CrossChainReceived-6-tsmSeedSymbolIndexToChain"+JsonConvert.SerializeObject(tsmSeedSymbolIndexToChain));
        if(tsmSeedSymbolIndexToChain == null) return;
        tsmSeedSymbolIndexToChain.IsBurned = false;
        tsmSeedSymbolIndexToChain.ChainId = context.ChainId;
        tsmSeedSymbolIndexToChain.Owner = eventValue.To.ToBase58();
        _objectMapper.Map(context, tsmSeedSymbolIndexToChain);
        await SaveEntityAsync(tsmSeedSymbolIndexToChain);
        await SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
    }
    private async Task<NFTListingInfoIndex> GetMinListingNftAsync(string nftInfoId)
    {
        //After the listing and the transaction is recorded, listing will be deleted first, but the transfer can query it.
        //So add check data in memory 
        return await GetMinListingNftAsync(nftInfoId, null, null, async info =>
        {
            var listingInfo = await GetEntityAsync<NFTListingInfoIndex>(info.Id);
            return listingInfo != null;
        });
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
         queryable = queryable.Where(q=>DateTimeHelper.ToUnixTimeMilliseconds(q.ExpireTime) > long.Parse(DateTime.UtcNow.ToString("0")));
         queryable = queryable.Where(q => q.NftInfoId == nftInfoId);
         if (!excludeListingIds.IsNullOrEmpty())
         {
             queryable = queryable.Where(q => !excludeListingIds.Contains(q.Id));
         }

         var dataList = queryable.Skip(0).OrderBy(x=>x.Prices).ToList();
         if (dataList.IsNullOrEmpty())
         {
             return new List<NFTListingInfoIndex>();
         }

         return dataList;
     }
    private async Task HandleEventForNFTAsync(CrossChainReceived eventValue, LogEventContext context)
    {
        var nftInfoId =
            IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        var nftInfoIndex = await GetEntityAsync<NFTInfoIndex>(nftInfoId);
        
        Logger.LogDebug("CrossChainReceived-5-nftInfoId"+nftInfoId);
        Logger.LogDebug("CrossChainReceived-6-nftInfo"+JsonConvert.SerializeObject(nftInfoIndex));
        if(nftInfoIndex == null) return;
        var minNftListing = await GetMinListingNftAsync(nftInfoIndex.Id);
        nftInfoIndex.OfMinNftListingInfo(minNftListing);
        _objectMapper.Map(context, nftInfoIndex);
        nftInfoIndex.Supply += eventValue.Amount;
        await SaveEntityAsync(nftInfoIndex);

        await SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
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
    //todo V2 ,code:done  need test user have many list scene
     public async Task UpdateListingInfoRealQualityAsync(string symbol, long balance, string ownerAddress, LogEventContext context)
    {
        if (context.ChainId.Equals(ForestIndexerConstants.MainChain))
        {
            return;
        }
        if (SymbolHelper.CheckSymbolIsELF(symbol))
        {
            return;
        }
        var nftId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, symbol);
        var queryable = await _listedNFTIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(x => DateTimeHelper.ToUnixTimeMilliseconds(x.ExpireTime)>long.Parse(DateTime.UtcNow.ToString("O")));
        queryable = queryable.Where(x=>x.NftInfoId==nftId);
        queryable = queryable.Where(x=>x.Owner==ownerAddress);

        int skip = 0;
        var nftListings = new List<NFTListingInfoIndex>();
        int queryCount = 0;
        while (queryCount < MaxQueryCount)
        {

            var result = queryable.Skip(skip).Take(MaxQuerySize).ToList();
            if (result.IsNullOrEmpty())
            {
                break;
            }
            if(result.Count < MaxQuerySize)
            {
                nftListings.AddRange(result);
                break;
            }
            skip += MaxQuerySize;
            queryCount++;
        }

        //update RealQuantity
        foreach (var nftListingInfoIndex in nftListings)
        {
            var realNftListingInfoIndex = await GetEntityAsync<NFTListingInfoIndex>(nftListingInfoIndex.Id);
            if (realNftListingInfoIndex == null) continue;
            var realQuantity = Math.Min(realNftListingInfoIndex.Quantity, balance);
            if (realQuantity != realNftListingInfoIndex.RealQuantity)
            {
                realNftListingInfoIndex.RealQuantity = realQuantity;
                _objectMapper.Map(context, realNftListingInfoIndex);
                await SaveEntityAsync(realNftListingInfoIndex);
            }
        }
    }
     public async Task<long> SaveUserBalanceAsync(String symbol, String address, long amount, LogEventContext context)
     {
         var nftInfoIndexId = IdGenerateHelper.GetNFTInfoId(context.ChainId, symbol);
         var userBalanceId = IdGenerateHelper.GetUserBalanceId(address, context.ChainId, nftInfoIndexId);
         var userBalanceIndex = await GetEntityAsync<UserBalanceIndex>(userBalanceId);
         
         if (userBalanceIndex == null)
         {
             userBalanceIndex = new UserBalanceIndex()
             {
                 Id = userBalanceId,
                 ChainId = context.ChainId,
                 NFTInfoId = nftInfoIndexId,
                 Address = address,
                 Amount = amount,
                 Symbol = symbol,
                 ChangeTime = context.Block.BlockTime
             };
         }
         else
         {
             userBalanceIndex.Amount += amount;
             userBalanceIndex.ChangeTime = context.Block.BlockTime;
         }

         _objectMapper.Map(context, userBalanceIndex);
         Logger.LogInformation("SaveUserBalanceAsync Address {Address} symbol {Symbol} balance {Balance}", address,
             symbol, userBalanceIndex.Amount);
         await SaveEntityAsync(userBalanceIndex);
         return userBalanceIndex.Amount;
     }
}