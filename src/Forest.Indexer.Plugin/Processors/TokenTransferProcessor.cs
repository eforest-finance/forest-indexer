using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using AElf.Contracts.MultiToken;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class TokenTransferProcessor : LogEventProcessorBase<Transferred>
{
    private readonly IObjectMapper _objectMapper;
    private readonly IReadOnlyRepository<OfferInfoIndex> _nftOfferIndexRepository;
    private readonly IReadOnlyRepository<NFTListingInfoIndex> _listedNFTIndexRepository;

    public TokenTransferProcessor(IObjectMapper objectMapper,
        IReadOnlyRepository<OfferInfoIndex> nftOfferIndexRepository,
        IReadOnlyRepository<NFTListingInfoIndex> listedNFTIndexRepository)
    {
        _objectMapper = objectMapper;
        _nftOfferIndexRepository = nftOfferIndexRepository;
        _listedNFTIndexRepository = listedNFTIndexRepository;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetTokenContractAddress(chainId);
    }

    public async override Task ProcessAsync(Transferred eventValue, LogEventContext context)
    {
        Logger.LogDebug("TokenTransferProcessor-1 {A}",JsonConvert.SerializeObject
            (eventValue));
        Logger.LogDebug("TokenTransferProcessor-2 {A}",JsonConvert.SerializeObject(context));
        if (eventValue == null) return;
        if (context == null) return;
        await UpdateUserFromBalanceAsync(eventValue, context);
        await UpdateUserToBalanceAsync(eventValue, context);
        if(SymbolHelper.CheckSymbolIsELF(eventValue.Symbol)) return;
        await SaveCollectionChangeIndexAsync(context, eventValue.Symbol);
        if (SymbolHelper.CheckSymbolIsSeedCollection(eventValue.Symbol)) return;
        if (SymbolHelper.CheckSymbolIsSeedSymbol(eventValue.Symbol))
        {
            Logger.LogDebug("TokenTransferProcessor-3 {A}",JsonConvert.SerializeObject
                (eventValue));
            await HandleForSeedSymbolTransferAsync(eventValue, context);
            return;
        }

        if (SymbolHelper.CheckSymbolIsNoMainChainNFT(eventValue.Symbol, context.ChainId))
        {
            await HandleForNFTTransferAsync(eventValue, context);
        }
    }
    
    private async Task HandleForSeedSymbolTransferAsync(Transferred eventValue, LogEventContext context)
    {
        await DoHandleForSeedSymbolTransferAsync(eventValue, context);
    }

    private async Task DoHandleForSeedSymbolTransferAsync(Transferred eventValue, LogEventContext context)
    {
        Logger.LogDebug("TokenTransferProcessor-4 {A}",JsonConvert.SerializeObject
            (eventValue));
        Logger.LogDebug("TokenTransferProcessor-5 {A}",JsonConvert.SerializeObject
            (context));
        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        var seedSymbol =
            await GetEntityAsync<SeedSymbolIndex>(seedSymbolId);

        if (seedSymbol == null) return;
        if (seedSymbol.IsDeleted) return;
        Logger.LogDebug("TokenTransferProcessor-8 {A}",JsonConvert.SerializeObject
            (seedSymbol));

        _objectMapper.Map(context, seedSymbol);
        await SaveEntityAsync(seedSymbol);
        await SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
        await SaveNftActivityIndexAsync(eventValue, context, seedSymbol.Id, seedSymbol.Decimals);
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
    private async Task SaveCollectionChangeIndexAsync(LogEventContext context, string symbol)
    {
        var collectionChangeIndex = new CollectionChangeIndex();
        var nftCollectionSymbol = SymbolHelper.GetNFTCollectionSymbol(symbol);
        if (nftCollectionSymbol == null)
        {
            return;
        }

        collectionChangeIndex.Symbol = nftCollectionSymbol;
        collectionChangeIndex.Id = IdGenerateHelper.GetNFTCollectionId(context.ChainId, nftCollectionSymbol);
        collectionChangeIndex.UpdateTime = context.Block.BlockTime;
        _objectMapper.Map(context, collectionChangeIndex);
        await SaveEntityAsync(collectionChangeIndex);
    }
    
    private async Task HandleForNFTTransferAsync(Transferred eventValue, LogEventContext context)
    {
        var nftInfoIndexId = IdGenerateHelper.GetNFTInfoId(context.ChainId, eventValue.Symbol);
        var nftInfoIndex = await GetEntityAsync<NFTInfoIndex>(nftInfoIndexId);
        if (nftInfoIndex == null) return;
        
        _objectMapper.Map(context, nftInfoIndex);
        await SaveEntityAsync(nftInfoIndex);
        await SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
        await SaveNftActivityIndexAsync(eventValue, context, nftInfoIndex.Id, nftInfoIndex.Decimals);
    }

    private async Task SaveNftActivityIndexAsync(Transferred eventValue, LogEventContext context, string bizId,
        int decimals)
    {
        var nftActivityIndexId = IdGenerateHelper.GetNftActivityId(context.ChainId, eventValue.Symbol,
            eventValue.From.ToBase58(),
            eventValue.To.ToBase58(), context.Transaction.TransactionId);
        var checkNftActivityIndex = await GetEntityAsync<NFTActivityIndex>(nftActivityIndexId);
        if (checkNftActivityIndex != null) return;
        
        NFTActivityIndex nftActivityIndex = new NFTActivityIndex
        {
            Id = nftActivityIndexId,
            Type = NFTActivityType.Transfer,
            Amount = TokenHelper.GetIntegerDivision(eventValue.Amount,decimals),
            TransactionHash = context.Transaction.TransactionId,
            Timestamp = context.Block.BlockTime,
            NftInfoId = bizId
        };
        _objectMapper.Map(context, nftActivityIndex);
        nftActivityIndex.From =
            FullAddressHelper.ToFullAddress(eventValue.From.ToBase58(), context.ChainId);
         nftActivityIndex.To =
             FullAddressHelper.ToFullAddress(eventValue.To.ToBase58(), context.ChainId);
         await SaveEntityAsync(nftActivityIndex);
    }

    private async Task UpdateUserFromBalanceAsync(Transferred eventValue, LogEventContext context)
    {
        var needRecordBalance =
            await NeedRecordBalance(eventValue.Symbol, eventValue.From.ToBase58(), context.ChainId);
        if (!needRecordBalance)
        {
            return;
        }

        var fromUserBalance = await SaveUserBalanceAsync(eventValue.Symbol,
            eventValue.From.ToBase58(),
            -eventValue.Amount, context);
        await UpdateOfferRealQualityAsync(eventValue.Symbol, fromUserBalance,
            eventValue.From.ToBase58(),
            context); 
        await   UpdateListingInfoRealQualityAsync(eventValue.Symbol, fromUserBalance,
            eventValue.From.ToBase58(), context);
    }
    public async Task<bool> NeedRecordBalance(string symbol, string offerFrom, string chainId)
    {
        if (!SymbolHelper.CheckSymbolIsELF(symbol))
        {
            return true;
        }

        if (ForestIndexerConstants.NeedRecordBalanceOptionsAddressList.Contains(offerFrom))
        {
            return true;
        } 

        var num = 0;
        var offerNumId = IdGenerateHelper.GetOfferNumId(chainId, offerFrom);
        var nftOfferNumIndex =
            await GetEntityAsync<UserNFTOfferNumIndex>(offerNumId);
        if (nftOfferNumIndex == null)
        {
            num = 0;
        }
        else
        {
            num = nftOfferNumIndex.OfferNum;
        }
        
        if (num > 0)
        {
            return true;
        }

        return false;
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

    private async Task UpdateUserToBalanceAsync(Transferred eventValue, LogEventContext context)
    {
        var needRecordBalance =
            await NeedRecordBalance(eventValue.Symbol, eventValue.To.ToBase58(), context.ChainId);
        if (!needRecordBalance)
        {
            return;
        }
        var nftInfoIndexId = IdGenerateHelper.GetNFTInfoId(context.ChainId, eventValue.Symbol);
        var userBalanceToId =
            IdGenerateHelper.GetUserBalanceId(eventValue.To.ToBase58(), context.ChainId, nftInfoIndexId);
        var userBalanceTo = await QueryUserBalanceByIdAsync(userBalanceToId, context.ChainId);
        if (userBalanceTo == null)
        {
            var lastNFTListingInfoDic =
                await QueryLatestNFTListingInfoByNFTIdsAsync(new List<string> { nftInfoIndexId },
                    "");

            var lastNFTListingInfo = lastNFTListingInfoDic != null && lastNFTListingInfoDic.ContainsKey(nftInfoIndexId)
                ? lastNFTListingInfoDic[nftInfoIndexId]
                : new NFTListingInfoIndex();
            userBalanceTo = new UserBalanceIndex
            {
                Id = userBalanceToId,
                ChainId = context.ChainId,
                NFTInfoId = nftInfoIndexId,
                Symbol = eventValue.Symbol,
                Address = eventValue.To.ToBase58(),
                Amount = eventValue.Amount,
                ChangeTime = context.Block.BlockTime,
                ListingPrice = lastNFTListingInfo.Prices,
                ListingTime = lastNFTListingInfo.StartTime
            };
        }
        else
        {
            userBalanceTo.Amount += eventValue.Amount;
            userBalanceTo.ChangeTime = context.Block.BlockTime;
        }

        _objectMapper.Map(context, userBalanceTo);
        await SaveEntityAsync(userBalanceTo);
        await UpdateOfferRealQualityAsync(eventValue.Symbol, userBalanceTo.Amount,
            eventValue.To.ToBase58(), context);
        await UpdateListingInfoRealQualityAsync(eventValue.Symbol, userBalanceTo.Amount, eventValue.To.ToBase58(), context);
        await SaveNFTOfferChangeIndexAsync(context, eventValue.Symbol, EventType.Other);
    }
    private async Task<Dictionary<string, NFTListingInfoIndex>> QueryLatestNFTListingInfoByNFTIdsAsync(
        List<string> nftInfoIds, string noListingId)
    {
        if (nftInfoIds == null) return new Dictionary<string, NFTListingInfoIndex>();
        var queryLatestList = new List<Task<NFTListingInfoIndex>>();
        foreach (string nftInfoId in nftInfoIds)
        {
            queryLatestList.Add(QueryLatestWhiteListByNFTIdAsync(nftInfoId, noListingId));
        }

        var latestList = await Task.WhenAll(queryLatestList);
        return await TransferToDicAsync(latestList);
    }
    private async Task<Dictionary<string, NFTListingInfoIndex>> TransferToDicAsync(
        NFTListingInfoIndex[] nftListingInfoIndices)
    {
        if (nftListingInfoIndices == null || nftListingInfoIndices.Length == 0)
            return new Dictionary<string, NFTListingInfoIndex>();

        nftListingInfoIndices = nftListingInfoIndices.Where(x => x != null).ToArray();

        return nftListingInfoIndices == null || nftListingInfoIndices.Length == 0
            ? new Dictionary<string, NFTListingInfoIndex>()
            : nftListingInfoIndices.ToDictionary(item => item.NftInfoId);
    }

    private async Task<NFTListingInfoIndex> QueryLatestWhiteListByNFTIdAsync(string nftInfoId, string noListingId)
    {
        var queryable = await _listedNFTIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(index =>
            index.ExpireTime > DateTime.UtcNow);
        queryable = queryable.Where(index => index.NftInfoId == nftInfoId);

        if (!noListingId.IsNullOrEmpty())
        {
            queryable = queryable.Where(index => index.Id != noListingId);
        }

        var result = queryable.Skip(0).Take(1).OrderByDescending(k => k.BlockHeight).ToList();
        return result?.FirstOrDefault();
    }
    private async Task UpdateListingInfoRealQualityAsync(string symbol, long balance, string ownerAddress, LogEventContext context)
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
        queryable = queryable.Where(x => x.ExpireTime>DateTime.UtcNow);
        queryable = queryable.Where(x=>x.NftInfoId==nftId);
        queryable = queryable.Where(x=>x.Owner==ownerAddress);

        int skip = 0;
        var nftListings = new List<NFTListingInfoIndex>();
        int queryCount = 0;
        while (queryCount < ForestIndexerConstants.MaxQueryCount)
        {

            var result = queryable.Skip(skip).Take(ForestIndexerConstants.MaxQuerySize).OrderByDescending(x=>x.BlockHeight).ToList();
            if (result.IsNullOrEmpty())
            {
                break;
            }
            if(result.Count < ForestIndexerConstants.MaxQuerySize)
            {
                nftListings.AddRange(result);
                break;
            }
            skip += ForestIndexerConstants.MaxQuerySize;
            queryCount++;
        }

        var writeCount = 0;
        //update RealQuantity
        foreach (var nftListingInfoIndex in nftListings)
        {
            writeCount++;
            if (writeCount >= ForestIndexerConstants.MaxWriteDBRecord)
            {
                Logger.LogInformation("CrossChainReceivedProcessor.UpdateListingInfoRealQualityAsync recordCount:{A} ,limit:{B}, user:{C},symbol:{D}, balance:{E}",
                    nftListings.Count,ForestIndexerConstants.MaxWriteDBRecord, ownerAddress, symbol, balance);
                break;
            }
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
        int limit = 80;
        
        {
            var queryable = await _nftOfferIndexRepository.GetQueryableAsync();
            var utcNow = DateTime.UtcNow;
            
            queryable = queryable.Where(i => i.ExpireTime > utcNow);
            queryable = queryable.Where(i => i.PurchaseToken.Symbol == symbol);
            queryable = queryable.Where(i => i.ChainId == context.ChainId);
            queryable = queryable.Where(i => i.OfferFrom == offerFrom);

            var result = queryable.OrderByDescending(i => i.Price)
                .Skip(skip)
                .Take(limit)
                .ToList();

            if (result.IsNullOrEmpty())
            {
                return;
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
                    var symbolTokenInfo =
                        await GetEntityAsync<TokenInfoIndex>(symbolTokenIndexId);
                    
                    var canBuyNum = Convert.ToInt64(Math.Floor(Convert.ToDecimal(balance) /
                                                               (offerInfoIndex.Price *
                                                                (decimal)Math.Pow(10,
                                                                    tokenIndex.Decimals))));
                    canBuyNum = (long)(canBuyNum * (decimal)Math.Pow(10, symbolTokenInfo.Decimals));
                    Logger.LogInformation(
                        "UpdateOfferRealQualityAsync  offerInfoIndex.BizSymbol {BizSymbol} canBuyNum {CanBuyNum} Quantity {Quantity} RealQuantity {RealQuantity}",
                        offerInfoIndex.BizSymbol, canBuyNum, offerInfoIndex.Quantity, offerInfoIndex.RealQuantity);
                    
                    var realQuantity = Math.Min(offerInfoIndex.Quantity,
                        canBuyNum);
                    if (realQuantity != offerInfoIndex.RealQuantity)
                    {
                        offerInfoIndex.RealQuantity = realQuantity;
                        _objectMapper.Map(context, offerInfoIndex);
                        var research = GetEntityAsync<OfferInfoIndex>(offerInfoIndex.Id);
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
        } 
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
    
    private async Task<UserBalanceIndex> QueryUserBalanceByIdAsync(string userBalanceId, string chainId)
    {
        if (userBalanceId.IsNullOrWhiteSpace() ||
            chainId.IsNullOrWhiteSpace())
        {
            return null;
        }
        return await GetEntityAsync<UserBalanceIndex>(userBalanceId);
    }
}