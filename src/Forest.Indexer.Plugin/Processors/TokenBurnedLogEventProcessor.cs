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
    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly INFTInfoProvider _nftInfoProvider;
    private readonly ICollectionChangeProvider _collectionChangeProvider;
    private readonly INFTOfferProvider _nftOfferProvider;
    private readonly INFTListingInfoProvider _nftListingInfoProvider;
    private readonly INFTOfferChangeProvider _nftOfferChangeProvider;

    private readonly ILogger<TokenBurnedLogEventProcessor> _logger;
    private readonly INFTListingChangeProvider _listingChangeProvider;
    private readonly IAElfClientServiceProvider _aElfClientServiceProvider;

    public TokenBurnedLogEventProcessor(ILogger<TokenBurnedLogEventProcessor> logger
        , IObjectMapper objectMapper
        , IUserBalanceProvider userBalanceProvider
        , INFTInfoProvider nftInfoProvider
        , ICollectionChangeProvider collectionChangeProvider
        , INFTOfferProvider nftOfferProvider
        , INFTListingInfoProvider nftListingInfoProvider
        , INFTOfferChangeProvider nftOfferChangeProvider
        , INFTListingChangeProvider listingChangeProvider
        ,IAElfClientServiceProvider aElfClientServiceProvider)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _userBalanceProvider = userBalanceProvider;
        _nftInfoProvider = nftInfoProvider;
        _collectionChangeProvider = collectionChangeProvider;
        _nftOfferProvider = nftOfferProvider;
        _nftListingInfoProvider = nftListingInfoProvider;
        _nftOfferChangeProvider = nftOfferChangeProvider;
        _listingChangeProvider = listingChangeProvider;
        _aElfClientServiceProvider = aElfClientServiceProvider;
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
        var userBalance = await _userBalanceProvider.SaveUserBalanceAsync(eventValue.Symbol,
            eventValue.Burner.ToBase58(),
            -eventValue.Amount, context);
        await _nftOfferProvider.UpdateOfferRealQualityAsync(eventValue.Symbol, userBalance, eventValue.Burner.ToBase58(), context);
        await _nftListingInfoProvider.UpdateListingInfoRealQualityAsync(eventValue.Symbol, userBalance, eventValue.Burner.ToBase58(), context);
        await _nftOfferChangeProvider.SaveNFTOfferChangeIndexAsync(context, eventValue.Symbol, EventType.Other);

        if (SymbolHelper.CheckSymbolIsELF(eventValue.Symbol)) return;
        await _collectionChangeProvider.SaveCollectionChangeIndexAsync(context, eventValue.Symbol);
        
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

        //add calc minNftListing
        var minNftListing = await _nftInfoProvider.GetMinListingNftAsync(seedSymbolId);
        seedSymbol.OfMinNftListingInfo(minNftListing);

        _objectMapper.Map(context, seedSymbol);
        seedSymbol.IsDeleteFlag = true;
        seedSymbol.Supply -= 1;
        if (checkSeedIsUsedResult)
        {
            seedSymbol.SeedStatus = SeedStatus.REGISTERED;
        }
        await SaveEntityAsync(seedSymbol);
        await _listingChangeProvider.SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
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

        //add calc minNftListing
        var minNftListing = await _nftInfoProvider.GetMinListingNftAsync(nftIndexId);
        nftIndex.OfMinNftListingInfo(minNftListing);
        nftIndex.Supply -= eventValue.Amount;
        _objectMapper.Map(context, nftIndex);
        await SaveEntityAsync(nftIndex);
        await _collectionChangeProvider.SaveCollectionChangeIndexAsync(context, eventValue.Symbol);
        await _listingChangeProvider.SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
        await SaveActivityAsync(eventValue, context, nftIndex.Id, nftIndex.Decimals);
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
        await _nftInfoProvider.AddNFTActivityAsync(context, nftActivityIndex);
    }

}