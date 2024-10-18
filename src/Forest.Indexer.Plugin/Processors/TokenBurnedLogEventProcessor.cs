using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using AElf.Contracts.MultiToken;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class TokenBurnedLogEventProcessor : LogEventProcessorBase<Burned>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<TokenBurnedLogEventProcessor> _logger;
    private readonly IAElfClientServiceProvider _aElfClientServiceProvider;
    private readonly INFTOfferProvider _nftOfferProvider;

    public TokenBurnedLogEventProcessor(ILogger<TokenBurnedLogEventProcessor> logger
        , IObjectMapper objectMapper
        ,IAElfClientServiceProvider aElfClientServiceProvider,
        INFTOfferProvider nftOfferProvider)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _aElfClientServiceProvider = aElfClientServiceProvider;
        _nftOfferProvider = nftOfferProvider;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetTokenContractAddress(chainId);
    }

    public async override Task ProcessAsync(Burned eventValue, LogEventContext context)
    {
        _logger.LogDebug("TokenBurnedLogEventProcessor-1"+JsonConvert.SerializeObject(eventValue));
        _logger.LogDebug("TokenBurnedLogEventProcessor-2"+JsonConvert.SerializeObject(context));
        if (eventValue == null) return;
        if (context == null) return;
        var needRecordBalance =
            await _nftOfferProvider.NeedRecordBalance(eventValue.Symbol, eventValue.Burner.ToBase58(),
                context.ChainId);
        if (!needRecordBalance)
        {
            return;
        }
        var userBalance = await SaveUserBalanceAsync(eventValue.Symbol,
            eventValue.Burner.ToBase58(),
            -eventValue.Amount, context);
        await _nftOfferProvider.UpdateOfferRealQualityAsync(eventValue.Symbol, userBalance, eventValue.Burner.ToBase58(), context);
        
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
        var seedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        var seedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(seedSymbolIndexId);
        
        if (seedSymbolIndex == null) return;

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
        _logger.LogInformation("SaveUserBalanceAsync Address {Address} symbol {Symbol} balance {Balance}", address,
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
        var tsmSeedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, seedSymbol.SeedOwnedSymbol);
        var tsmSeedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(tsmSeedSymbolIndexId);
        
        _logger.LogDebug(
            "[TokenBurned] blockHeight: {BlockHeight} tsmSeedSymbolIndexId: {tsmSeedSymbolIndexId}  tsmSeedSymbolIndex: {tsmSeedSymbolIndex}",
            context.Block.BlockHeight, tsmSeedSymbolIndexId, JsonConvert.SerializeObject(tsmSeedSymbolIndex));
        if (tsmSeedSymbolIndex != null)
        {
            _objectMapper.Map(context, tsmSeedSymbolIndex);
            tsmSeedSymbolIndex.IsBurned = true;
            if (checkSeedIsUsedResult)
            {
                tsmSeedSymbolIndex.Status = SeedStatus.REGISTERED;
            }
            _logger.LogDebug("DoHandleForSeedSymbolBurnedAsync tsmSeedSymbolIndex:{tsmSeedSymbolIndex}", JsonConvert.SerializeObject(tsmSeedSymbolIndex));
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
            Type = NFTActivityType.Burn,
            From = FullAddressHelper.ToFullAddress(eventValue.Burner.ToBase58(), context.ChainId),
            Amount = TokenHelper.GetIntegerDivision(eventValue.Amount,decimals),
            TransactionHash = context.Transaction.TransactionId,
            Timestamp = context.Block.BlockTime,
            NftInfoId = bizId
        };
        await AddNFTActivityAsync(context, nftActivityIndex);
    }
    
    public async Task<bool> AddNFTActivityAsync(LogEventContext context, NFTActivityIndex nftActivityIndex)
    {
        // NFT activity
        var nftActivityIndexExists = await GetEntityAsync<NFTActivityIndex>(nftActivityIndex.Id);
        if (nftActivityIndexExists != null)
        {
            Logger.LogDebug("[AddNFTActivityAsync] FAIL: activity EXISTS, nftActivityIndexId={Id}", nftActivityIndex.Id);
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