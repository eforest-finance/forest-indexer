using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using AElf.Contracts.MultiToken;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class TokenBurnedLogEventProcessor : LogEventProcessorBase<Burned>
{
    private readonly IObjectMapper _objectMapper;
    private readonly IAElfClientServiceProvider _aElfClientServiceProvider;
    private readonly IReadOnlyRepository<OfferInfoIndex> _nftOfferIndexRepository;
    private readonly IReadOnlyRepository<TsmSeedSymbolIndex> _tsmSeedSymbolIndexRepository;

    public TokenBurnedLogEventProcessor(IObjectMapper objectMapper
        ,IAElfClientServiceProvider aElfClientServiceProvider,
        IReadOnlyRepository<OfferInfoIndex> nftOfferIndexRepository,
        IReadOnlyRepository<TsmSeedSymbolIndex> tsmSeedSymbolIndexRepository)
    {
        _objectMapper = objectMapper;
        _aElfClientServiceProvider = aElfClientServiceProvider;
        _nftOfferIndexRepository = nftOfferIndexRepository;
        _tsmSeedSymbolIndexRepository = tsmSeedSymbolIndexRepository;

    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetTokenContractAddress(chainId);
    }

    public async override Task ProcessAsync(Burned eventValue, LogEventContext context)
    {
        Logger.LogDebug("TokenBurnedLogEventProcessor-1 {A}",JsonConvert.SerializeObject(eventValue));
        Logger.LogDebug("TokenBurnedLogEventProcessor-2 {B}",JsonConvert.SerializeObject(context));
        if (eventValue == null) return;
        if (context == null) return;
        var needRecordBalance =
            await NeedRecordBalance(eventValue.Symbol, eventValue.Burner.ToBase58(),
                context.ChainId);
        if (!needRecordBalance)
        {
            return;
        }
        var userBalance = await SaveUserBalanceAsync(eventValue.Symbol,
            eventValue.Burner.ToBase58(),
            -eventValue.Amount, context);
        // await UpdateOfferRealQualityAsync(eventValue.Symbol, userBalance, eventValue.Burner.ToBase58(), context); todo v2
        
        await SaveNFTOfferChangeIndexAsync(context, eventValue.Symbol, EventType.Other);

        if (SymbolHelper.CheckSymbolIsELF(eventValue.Symbol)) return;
        await SaveCollectionChangeIndexAsync(context, eventValue.Symbol);
        
        if (SymbolHelper.CheckSymbolIsSeedCollection(eventValue.Symbol)) return;

        if (SymbolHelper.CheckSymbolIsSeedSymbol(eventValue.Symbol))
        {
            await HandleForSeedSymbolBurnedAsync(eventValue, context);
            return;
        }

        if (SymbolHelper.CheckSymbolIsNoMainChainNFT(eventValue.Symbol, context.ChainId))
        {
            await HandleForNFTBurnedAsync(eventValue, context);
            return;
        }
        
        await HandleForSeedTokenAsync(eventValue, context);
        
    }
    private async Task<bool> NeedRecordBalance(string symbol, string offerFrom, string chainId)
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
        return;//todo v2 tem
        
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

    private async Task HandleForSeedTokenAsync(Burned eventValue, LogEventContext context)
    {
        if (eventValue == null || context == null) return;
        // var seedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        // var seedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(seedSymbolIndexId);
        //
        // if (seedSymbolIndex == null) return;
        
        var tsmSeedSymbolIndex = await GetTsmSeedAsync(context.ChainId, eventValue.Symbol);
        if (tsmSeedSymbolIndex == null)
        {
            Logger.LogError("HandleForSeedTokenAsync tsmSeedSymbolIndex is null chainId={A} symbol={B}", context.ChainId, eventValue.Symbol);
           return;
        }

        var symbolMarketTokenIndexId = IdGenerateHelper.GetSymbolMarketTokenId(context.ChainId, eventValue.Symbol);
        var symbolMarketTokenIndex = await GetEntityAsync<SeedSymbolMarketTokenIndex>(symbolMarketTokenIndexId);
        if (symbolMarketTokenIndex == null) return;
        if (!symbolMarketTokenIndex.IsBurnable) return;
        if (symbolMarketTokenIndex.TotalSupply <= 0) return;

        symbolMarketTokenIndex.Supply -= eventValue.Amount;
        _objectMapper.Map(context, symbolMarketTokenIndex);
        await SaveEntityAsync(symbolMarketTokenIndex);
        await SaveActivityAsync(eventValue, context, symbolMarketTokenIndex.Id, symbolMarketTokenIndex.Decimals);
    }
    
    private async Task<TsmSeedSymbolIndex> GetTsmSeedAsync(string chainId, string seedSymbol)
    {
        var queryable = await _tsmSeedSymbolIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(x=>x.ChainId == chainId && x.SeedSymbol == seedSymbol);
        List<TsmSeedSymbolIndex> list = queryable.OrderByDescending(i => i.ExpireTime).Skip(0).Take(1).ToList();
        return list.IsNullOrEmpty() ? null : list.FirstOrDefault();
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

    private async Task HandleForSeedSymbolBurnedAsync(Burned eventValue, LogEventContext context)
    {
        await DoHandleForSeedSymbolBurnedAsync(eventValue, context);
        return;
    }

    private async Task DoHandleForSeedSymbolBurnedAsync(Burned eventValue, LogEventContext context)
    {
        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        var seedSymbol = await GetEntityAsync<SeedSymbolIndex>(seedSymbolId);

        if (seedSymbol == null) return;

        var checkSeedIsUsedResult = await CheckSeedIsUsed(seedSymbol.SeedOwnedSymbol, context.ChainId);
        //burned tsm seed symbol index
        //var tsmSeedSymbolIndex = await GetTsmSeedAsync(context.ChainId, seedSymbol.SeedOwnedSymbol);
        
        var newId = IdGenerateHelper.GetNewTsmSeedSymbolId(context.ChainId, eventValue.Symbol,
            seedSymbol.SeedOwnedSymbol);
        var oldId = IdGenerateHelper.GetOldTsmSeedSymbolId(context.ChainId, seedSymbol.SeedOwnedSymbol);
        var nftSeedSymbolIndexId = newId;
                
        var tsmSeedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(nftSeedSymbolIndexId);
        if (tsmSeedSymbolIndex == null)
        {
            Logger.LogDebug("TokenBurnedLogEventProcessor new nftSeedSymbolIndex is null id={A}", nftSeedSymbolIndexId);
            nftSeedSymbolIndexId = oldId;
            tsmSeedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(nftSeedSymbolIndexId);
            if (tsmSeedSymbolIndex == null)
            {
                Logger.LogDebug("TokenBurnedLogEventProcessor old nftSeedSymbolIndex is null id={A}", nftSeedSymbolIndexId);
            }
        }
        
        if (tsmSeedSymbolIndex == null)
        {
            Logger.LogError("TokenBurnedLogEventProcessor HandleForSeedTokenAsync tsmSeedSymbolIndex is null chainId={A} symbol={B}", context.ChainId, eventValue.Symbol);
        }

        if (tsmSeedSymbolIndex != null)
        {
            Logger.LogDebug(
                "TokenBurnedLogEventProcessor blockHeight: {BlockHeight} tsmSeedSymbolIndexId: {tsmSeedSymbolIndexId}  tsmSeedSymbolIndex: {tsmSeedSymbolIndex}",
                context.Block.BlockHeight, tsmSeedSymbolIndex.Id, JsonConvert.SerializeObject(tsmSeedSymbolIndex));
            
            _objectMapper.Map(context, tsmSeedSymbolIndex);
            tsmSeedSymbolIndex.IsBurned = true;
            if (checkSeedIsUsedResult)
            {
                tsmSeedSymbolIndex.Status = SeedStatus.REGISTERED;
            }
            Logger.LogDebug("TokenBurnedLogEventProcessor DoHandleForSeedSymbolBurnedAsync tsmSeedSymbolIndex:{tsmSeedSymbolIndex}", JsonConvert.SerializeObject(tsmSeedSymbolIndex));
            await SaveEntityAsync(tsmSeedSymbolIndex);
        }
        
        
        //burned seed symbol index
        if (!seedSymbol.IsBurnable) return;

        _objectMapper.Map(context, seedSymbol);
        seedSymbol.IsDeleteFlag = true;
        seedSymbol.Supply -= 1;
        if (checkSeedIsUsedResult)
        {
            seedSymbol.SeedStatus = SeedStatus.REGISTERED;
        }
        await SaveEntityAsync(seedSymbol);
        await SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
        await SaveActivityAsync(eventValue, context, seedSymbol.Id, seedSymbol.Decimals);
    }

    private async Task<bool> CheckSeedIsUsed(string seedOwnedSymbol,string chainId)
    {
        var address = ContractInfoHelper.GetTokenContractAddress(chainId);

        
        var tokenInfo =
            await _aElfClientServiceProvider.GetTokenInfoAsync(chainId, address, seedOwnedSymbol);
        return tokenInfo != null && !tokenInfo.Symbol.IsNullOrEmpty();
    }

    private async Task HandleForNFTBurnedAsync(Burned eventValue, LogEventContext context)
    {
        var nftIndexId = IdGenerateHelper.GetNFTInfoId(context.ChainId, eventValue.Symbol);
        var nftIndex = await GetEntityAsync<NFTInfoIndex>(nftIndexId);

        if (nftIndex == null) return;
        if (!nftIndex.IsBurnable) return;
        if (nftIndex.TotalSupply <= 0) return;
        
        nftIndex.Supply -= eventValue.Amount;
        _objectMapper.Map(context, nftIndex);
        await SaveEntityAsync(nftIndex);
        await SaveCollectionChangeIndexAsync(context, eventValue.Symbol);
        await SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
        await SaveActivityAsync(eventValue, context, nftIndex.Id, nftIndex.Decimals);
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
    
    private async Task SaveActivityAsync(Burned eventValue, LogEventContext context, string bizId, int decimals)
    {
        var nftActivityIndexId =
            IdGenerateHelper.GetId(context.ChainId, eventValue.Symbol, NFTActivityType.Burn.ToString(),
                context.Transaction.TransactionId);
        var nftActivityIndex = new NFTActivityIndex
        {
            Id = nftActivityIndexId,
            From = FullAddressHelper.ToFullAddress(eventValue.Burner.ToBase58(), context.ChainId),
            Amount = TokenHelper.GetIntegerDivision(eventValue.Amount,decimals),
            TransactionHash = context.Transaction.TransactionId,
            Timestamp = context.Block.BlockTime,
            NftInfoId = bizId
        };
        nftActivityIndex.OfType(NFTActivityType.Burn);
        await AddNFTActivityAsync(context, nftActivityIndex);
    }
    
    public async Task<bool> AddNFTActivityAsync(LogEventContext context, NFTActivityIndex nftActivityIndex)
    {
        // NFT activity

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
    
    public async Task SaveNFTListingChangeIndexAsync(LogEventContext context, string symbol)
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

}