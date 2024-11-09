using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using AElf.Contracts.MultiToken;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class TokenIssueLogEventProcessor : LogEventProcessorBase<Issued>
{
    private readonly IObjectMapper _objectMapper;
    private readonly IReadOnlyRepository<NFTListingInfoIndex> _listedNFTIndexRepository;
    private readonly IReadOnlyRepository<OfferInfoIndex> _nftOfferIndexRepository;

    public TokenIssueLogEventProcessor(IObjectMapper objectMapper,
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

    public async override Task ProcessAsync(Issued eventValue, LogEventContext context)
    {
        Logger.LogDebug("TokenIssueLogEventProcessor-1 {A}",JsonConvert.SerializeObject(eventValue));
        // Logger.LogDebug("TokenIssueLogEventProcessor-2 {B}",JsonConvert.SerializeObject(context));
        if (eventValue == null || context == null) return;
        await SaveCollectionChangeIndexAsync(context, eventValue.Symbol);
        var userBalance = await SaveUserBalanceAsync(eventValue.Symbol, eventValue.To.ToBase58(),
            eventValue.Amount, context);
        await UpdateOfferRealQualityAsync(eventValue.Symbol, userBalance, eventValue.To.ToBase58(), context);
        await UpdateListingInfoRealQualityAsync(eventValue.Symbol, userBalance, eventValue.To.ToBase58(), context);
        await SaveNFTOfferChangeIndexAsync(context, eventValue.Symbol, EventType.Other);

        if (SymbolHelper.CheckSymbolIsELF(eventValue.Symbol)) return;
        if (SymbolHelper.CheckSymbolIsSeedCollection(eventValue.Symbol)) return;

        if (SymbolHelper.CheckSymbolIsSeedSymbol(eventValue.Symbol))
        {
            await HandleForSeedSymbolIssueAsync(eventValue, context);
            return;
        }

        if (SymbolHelper.CheckSymbolIsNoMainChainNFT(eventValue.Symbol, context.ChainId))
        {
            await HandleForNFTIssueAsync(eventValue, context);
            return;
        }
        await HandleForSymbolMarketTokenAsync(eventValue, context);
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
                    // Logger.LogInformation(
                    //     "UpdateOfferRealQualityAsync  offerInfoIndex.BizSymbol {BizSymbol} canBuyNum {CanBuyNum} Quantity {Quantity} RealQuantity {RealQuantity}",
                    //     offerInfoIndex.BizSymbol, canBuyNum, offerInfoIndex.Quantity, offerInfoIndex.RealQuantity);
                    //
                    var realQuantity = Math.Min(offerInfoIndex.Quantity,
                        canBuyNum);
                    if (realQuantity != offerInfoIndex.RealQuantity)
                    {
                        offerInfoIndex.RealQuantity = realQuantity;
                        _objectMapper.Map(context, offerInfoIndex);
                        var research = GetEntityAsync<OfferInfoIndex>(offerInfoIndex.Id);
                        if (research == null)
                        {
                            // Logger.LogInformation(
                            //     "UpdateOfferRealQualityAsync offerInfoIndex.Id is not exist,not update {OfferInfoIndexId}",
                            //     offerInfoIndex.Id);
                            continue;
                        }
                        await SaveEntityAsync(offerInfoIndex);
                    }
                }
            }
        } 
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
        int limit = 80;
        var nftListings = new List<NFTListingInfoIndex>();
        {

            var result = queryable.OrderByDescending(x=>x.BlockHeight).Skip(skip).Take(limit).ToList();
            nftListings.AddRange(result);
        }

        var writeCount = 0;
        //update RealQuantity
        foreach (var nftListingInfoIndex in nftListings)
        {
            writeCount++;
            if (writeCount >= ForestIndexerConstants.MaxWriteDBRecord)
            {
                // Logger.LogInformation("CrossChainReceivedProcessor.UpdateListingInfoRealQualityAsync recordCount:{A} ,limit:{B}, user:{C},symbol:{D}, balance:{E}",
                //     nftListings.Count,ForestIndexerConstants.MaxWriteDBRecord, ownerAddress, symbol, balance);
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
        // Logger.LogInformation("SaveUserBalanceAsync Address {Address} symbol {Symbol} balance {Balance}", address,
        //     symbol, userBalanceIndex.Amount);
        await SaveEntityAsync(userBalanceIndex);
        return userBalanceIndex.Amount;
    }

    private async Task HandleForSymbolMarketTokenAsync(Issued eventValue, LogEventContext context)
    {
        Logger.LogDebug("TokenIssueLogEventProcessor-3-HandleForNoMainChainSeedTokenAsync");
        if (eventValue == null || context == null) return;
        
        var symbolMarketTokenIndexId = IdGenerateHelper.GetSymbolMarketTokenId(context.ChainId, eventValue.Symbol);
        var symbolMarketTokenIndex = await GetEntityAsync<SeedSymbolMarketTokenIndex>(symbolMarketTokenIndexId);

        if (symbolMarketTokenIndex == null)
        {
            Logger.LogDebug("TokenIssueLogEventProcessor-4-HandleForNoMainChainSeedTokenAsync result is null {A}",
                symbolMarketTokenIndexId);
            return;
        }
        symbolMarketTokenIndex.Supply += eventValue.Amount;
        symbolMarketTokenIndex.Issued += eventValue.Amount;
        if (symbolMarketTokenIndex.IssueToSet.IsNullOrEmpty())
        {
            symbolMarketTokenIndex.IssueToSet = new HashSet<string>()
            {
                eventValue.To.ToBase58()
            };
        }
        else if(!symbolMarketTokenIndex.IssueToSet.Contains(eventValue.To.ToBase58()))
        {
            symbolMarketTokenIndex.IssueToSet.Add(eventValue.To.ToBase58());
        }
        
        symbolMarketTokenIndex =
            await FillProxyAccountInfoForSymbolMarketTokenIndexIssuerAsync(symbolMarketTokenIndex,
                context.ChainId);
        // Logger.LogDebug("TokenIssueLogEventProcessor-31-HandleForNoMainChainSeedTokenAsync {A}",JsonConvert.SerializeObject(symbolMarketTokenIndex));
        _objectMapper.Map(context, symbolMarketTokenIndex);
        // Logger.LogDebug("TokenIssueLogEventProcessor-32-HandleForNoMainChainSeedTokenAsync {B}",JsonConvert.SerializeObject(symbolMarketTokenIndex));
        await SaveEntityAsync(symbolMarketTokenIndex);
        Logger.LogDebug("TokenIssueLogEventProcessor-33-HandleForNoMainChainSeedTokenAsync");
        await SaveActivityAsync(eventValue, context, symbolMarketTokenIndex.Id, symbolMarketTokenIndex.Decimals);
    }

    public async Task<SeedSymbolMarketTokenIndex> FillProxyAccountInfoForSymbolMarketTokenIndexIssuerAsync(SeedSymbolMarketTokenIndex seedSymbolMarketTokenIndex, string chainId)
    {
        if (seedSymbolMarketTokenIndex == null || chainId.IsNullOrEmpty() || seedSymbolMarketTokenIndex.Issuer.IsNullOrEmpty()) return seedSymbolMarketTokenIndex;
        var proxyAccountId = IdGenerateHelper.GetProxyAccountIndexId(seedSymbolMarketTokenIndex.Issuer);
        var proxyAccount =
            await GetEntityAsync<ProxyAccountIndex>(proxyAccountId);
        return FillSymbolMarketTokenIndexIssuer(seedSymbolMarketTokenIndex, proxyAccount);
    }
    
    
    private SeedSymbolMarketTokenIndex FillSymbolMarketTokenIndexIssuer(SeedSymbolMarketTokenIndex seedSymbolMarketTokenIndex,
        ProxyAccountIndex proxyAccountIndex)
    {
        if (seedSymbolMarketTokenIndex == null) return seedSymbolMarketTokenIndex;

        if (proxyAccountIndex != null)
            seedSymbolMarketTokenIndex.IssueManagerSet = proxyAccountIndex.ManagersSet;
        else
            seedSymbolMarketTokenIndex.IssueManagerSet = new HashSet<string> { seedSymbolMarketTokenIndex.Issuer };

        seedSymbolMarketTokenIndex.RandomIssueManager = seedSymbolMarketTokenIndex.IssueManagerSet?.FirstOrDefault("");

        return seedSymbolMarketTokenIndex;
    }
    
    private async Task HandleForSeedSymbolIssueAsync(Issued eventValue, LogEventContext context)
    {
        await DoHandleForSeedSymbolIssueAsync(eventValue, context);
    }

    private async Task DoHandleForSeedSymbolIssueAsync(Issued eventValue, LogEventContext context)
    {
        var seedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        var seedSymbolIndex = await GetEntityAsync<SeedSymbolIndex>(seedSymbolIndexId);
        if (seedSymbolIndex == null) return;
        seedSymbolIndex.IssuerTo = eventValue.To.ToBase58();
        seedSymbolIndex.Supply += eventValue.Amount;
        seedSymbolIndex.Issued += eventValue.Amount;
        
        _objectMapper.Map(context, seedSymbolIndex);
        await SaveEntityAsync(seedSymbolIndex);
        await SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
        await SaveActivityAsync(eventValue, context, seedSymbolIndex.Id, seedSymbolIndex.Decimals);
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

    private async Task HandleForNFTIssueAsync(Issued eventValue, LogEventContext context)
    {
        var nftInfoIndexId = IdGenerateHelper.GetNFTInfoId(context.ChainId, eventValue.Symbol);
        var nftInfoIndex = await GetEntityAsync<NFTInfoIndex>(nftInfoIndexId);
        
        if (nftInfoIndex == null) return;

        nftInfoIndex.Supply += eventValue.Amount;
        nftInfoIndex.Issued += eventValue.Amount;
        _objectMapper.Map(context, nftInfoIndex);

        nftInfoIndex =
            await FillProxyAccountInfoForNFTInfoIndexAsync(nftInfoIndex, context.ChainId);
        
        await SaveEntityAsync(nftInfoIndex);
        await SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);

        await SaveActivityAsync(eventValue, context, nftInfoIndex.Id, nftInfoIndex.Decimals);
    }
    public async Task<NFTInfoIndex> FillProxyAccountInfoForNFTInfoIndexAsync(NFTInfoIndex nftInfoIndex, string chainId)
    {
        if (nftInfoIndex == null || chainId.IsNullOrEmpty() || nftInfoIndex.Issuer.IsNullOrEmpty()) return nftInfoIndex;
        var proxyAccountId = IdGenerateHelper.GetProxyAccountIndexId(nftInfoIndex.Issuer);
        var proxyAccount =
            await GetEntityAsync<ProxyAccountIndex>(proxyAccountId);
        return FillNFTInfoIndex(nftInfoIndex, proxyAccount);
    }
    private NFTInfoIndex FillNFTInfoIndex(NFTInfoIndex nftInfoIndex,
        ProxyAccountIndex proxyAccountIndex)
    {
        if (nftInfoIndex == null) return nftInfoIndex;

        if (proxyAccountIndex != null)
            nftInfoIndex.IssueManagerSet = proxyAccountIndex.ManagersSet;
        else
            nftInfoIndex.IssueManagerSet = new HashSet<string> { nftInfoIndex.Issuer };

        nftInfoIndex.RandomIssueManager = nftInfoIndex.IssueManagerSet?.FirstOrDefault("");

        return nftInfoIndex;
    }
    
    private async Task SaveActivityAsync(Issued eventValue, LogEventContext context, string bizId, int decimals)
    {
        var nftActivityIndexId =
            IdGenerateHelper.GetId(context.ChainId, eventValue.Symbol, NFTActivityType.Issue.ToString(), context.Transaction.TransactionId);
        var activity = new NFTActivityIndex
        {
            Id = nftActivityIndexId,
            To = FullAddressHelper.ToFullAddress(eventValue.To.ToBase58(), context.ChainId),
            Amount = TokenHelper.GetIntegerDivision(eventValue.Amount, decimals),
            TransactionHash = context.Transaction.TransactionId,
            Timestamp = context.Block.BlockTime,
            NftInfoId = bizId
        };
        activity.OfType(NFTActivityType.Issue);
        var activitySaved = await AddNFTActivityAsync(context, activity);
    }
    public async Task<bool> AddNFTActivityAsync(LogEventContext context, NFTActivityIndex nftActivityIndex)
    {
        // NFT activity

        var from = nftActivityIndex.From;
        var to = nftActivityIndex.To;
        _objectMapper.Map(context, nftActivityIndex);
        nftActivityIndex.From = FullAddressHelper.ToFullAddress(from, context.ChainId);
        nftActivityIndex.To = FullAddressHelper.ToFullAddress(to, context.ChainId);

        // Logger.LogDebug("[AddNFTActivityAsync] SAVE: activity SAVE, nftActivityIndexId={Id}", nftActivityIndex.Id);
        await SaveEntityAsync(nftActivityIndex);

        // Logger.LogDebug("[AddNFTActivityAsync] SAVE: activity FINISH, nftActivityIndexId={Id}", nftActivityIndex.Id);
        return true;
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

    
}