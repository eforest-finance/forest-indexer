using AElf.Contracts.MultiToken;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Processors.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class TokenBurnedLogEventProcessor : AElfLogEventProcessorBase<Burned, LogEventInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> _nftIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<SeedSymbolMarketTokenIndex, LogEventInfo>
        _symbolMarketTokenIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo>
        _tsmSeedSymbolIndexRepository;

    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly INFTInfoProvider _nftInfoProvider;
    private readonly ICollectionChangeProvider _collectionChangeProvider;
    private readonly INFTOfferProvider _nftOfferProvider;
    private readonly INFTListingInfoProvider _nftListingInfoProvider;
    private readonly INFTOfferChangeProvider _nftOfferChangeProvider;

    private readonly ILogger<AElfLogEventProcessorBase<Burned, LogEventInfo>> _logger;
    private readonly INFTListingChangeProvider _listingChangeProvider;
    private readonly IAElfClientServiceProvider _aElfClientServiceProvider;

    public TokenBurnedLogEventProcessor(ILogger<AElfLogEventProcessorBase<Burned, LogEventInfo>> logger
        , IObjectMapper objectMapper
        , IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> nftIndexRepository
        , IUserBalanceProvider userBalanceProvider
        , IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> seedSymbolIndexRepository
        , IAElfIndexerClientEntityRepository<SeedSymbolMarketTokenIndex, LogEventInfo>
            symbolMarketTokenIndexRepository
        , IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo>
            tsmSeedSymbolIndexRepository
        , INFTInfoProvider nftInfoProvider
        , ICollectionChangeProvider collectionChangeProvider
        , INFTOfferProvider nftOfferProvider
        , INFTListingInfoProvider nftListingInfoProvider
        , INFTOfferChangeProvider nftOfferChangeProvider
        , INFTListingChangeProvider listingChangeProvider
        ,IAElfClientServiceProvider aElfClientServiceProvider
        , IOptionsSnapshot<ContractInfoOptions> contractInfoOptions) : base(logger)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _nftIndexRepository = nftIndexRepository;
        _userBalanceProvider = userBalanceProvider;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _symbolMarketTokenIndexRepository = symbolMarketTokenIndexRepository;
        _tsmSeedSymbolIndexRepository = tsmSeedSymbolIndexRepository;
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
        return _contractInfoOptions.ContractInfos?.FirstOrDefault(c => c?.ChainId == chainId)?.TokenContractAddress;
    }

    protected override async Task HandleEventAsync(Burned eventValue, LogEventContext context)
    {
        _logger.Debug("TokenBurnedLogEventProcessor-1"+JsonConvert.SerializeObject(eventValue));
        _logger.Debug("TokenBurnedLogEventProcessor-2"+JsonConvert.SerializeObject(context));
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
        var seedSymbolIndex =
            await _tsmSeedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolIndexId,
                context.ChainId);
        if (seedSymbolIndex == null) return;

        var symbolMarketTokenIndexId = IdGenerateHelper.GetSymbolMarketTokenId(context.ChainId, eventValue.Symbol);
        var symbolMarketTokenIndex =
            await _symbolMarketTokenIndexRepository.GetFromBlockStateSetAsync(symbolMarketTokenIndexId,
                context.ChainId);
        if (symbolMarketTokenIndex == null) return;
        if (!symbolMarketTokenIndex.IsBurnable) return;
        if (symbolMarketTokenIndex.TotalSupply <= 0) return;

        symbolMarketTokenIndex.Supply -= eventValue.Amount;
        _objectMapper.Map(context, symbolMarketTokenIndex);
        await _symbolMarketTokenIndexRepository.AddOrUpdateAsync(symbolMarketTokenIndex);
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
        var seedSymbol = await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolId, context.ChainId);

        if (seedSymbol == null) return;

        var checkSeedIsUsedResult = await CheckSeedIsUsed(seedSymbol.SeedOwnedSymbol, context.ChainId);
        //burned tsm seed symbol index
        var tsmSeedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, seedSymbol.SeedOwnedSymbol);
        var tsmSeedSymbolIndex =
            await _tsmSeedSymbolIndexRepository.GetFromBlockStateSetAsync(tsmSeedSymbolIndexId, context.ChainId);
        _logger.LogDebug(
            "[TokenBurned] blockHeight: {BlockHeight} tsmSeedSymbolIndexId: {tsmSeedSymbolIndexId}  tsmSeedSymbolIndex: {tsmSeedSymbolIndex}",
            context.BlockHeight, tsmSeedSymbolIndexId, JsonConvert.SerializeObject(tsmSeedSymbolIndex));
        if (tsmSeedSymbolIndex != null)
        {
            _objectMapper.Map(context, tsmSeedSymbolIndex);
            tsmSeedSymbolIndex.IsBurned = true;
            if (checkSeedIsUsedResult)
            {
                tsmSeedSymbolIndex.Status = SeedStatus.REGISTERED;
            }
            _logger.Debug("DoHandleForSeedSymbolBurnedAsync tsmSeedSymbolIndex:{tsmSeedSymbolIndex}", JsonConvert.SerializeObject(tsmSeedSymbolIndex));
            await _tsmSeedSymbolIndexRepository.AddOrUpdateAsync(tsmSeedSymbolIndex);
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
        await _seedSymbolIndexRepository.AddOrUpdateAsync(seedSymbol);
        await _listingChangeProvider.SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
        await SaveActivityAsync(eventValue, context, seedSymbol.Id, seedSymbol.Decimals);
    }

    private async Task<bool> CheckSeedIsUsed(string seedOwnedSymbol,string chainId)
    {
        var address = _contractInfoOptions.ContractInfos
            .First(c => c.ChainId == chainId)
            .TokenContractAddress;

        
        var tokenInfo =
            await _aElfClientServiceProvider.GetTokenInfoAsync(chainId, address, seedOwnedSymbol);
        return tokenInfo != null && !tokenInfo.Symbol.IsNullOrEmpty();
    }

    private async Task HandleForNFTBurnedAsync(Burned eventValue, LogEventContext context)
    {
        var nftIndexId = IdGenerateHelper.GetNFTInfoId(context.ChainId, eventValue.Symbol);
        var nftIndex = await _nftIndexRepository.GetFromBlockStateSetAsync(nftIndexId, context.ChainId);

        if (nftIndex == null) return;
        if (!nftIndex.IsBurnable) return;
        if (nftIndex.TotalSupply <= 0) return;

        //add calc minNftListing
        var minNftListing = await _nftInfoProvider.GetMinListingNftAsync(nftIndexId);
        nftIndex.OfMinNftListingInfo(minNftListing);
        nftIndex.Supply -= eventValue.Amount;
        _objectMapper.Map(context, nftIndex);
        await _nftIndexRepository.AddOrUpdateAsync(nftIndex);
        await _collectionChangeProvider.SaveCollectionChangeIndexAsync(context, eventValue.Symbol);
        await _listingChangeProvider.SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
        await SaveActivityAsync(eventValue, context, nftIndex.Id, nftIndex.Decimals);
    }

    private async Task SaveActivityAsync(Burned eventValue, LogEventContext context, string bizId, int decimals)
    {
        var nftActivityIndexId =
            IdGenerateHelper.GetId(context.ChainId, eventValue.Symbol, NFTActivityType.Burn.ToString(),
                context.TransactionId);
        var nftActivityIndex = new NFTActivityIndex
        {
            Id = nftActivityIndexId,
            Type = NFTActivityType.Burn,
            From = FullAddressHelper.ToFullAddress(eventValue.Burner.ToBase58(), context.ChainId),
            Amount = TokenHelper.GetIntegerDivision(eventValue.Amount,decimals),
            TransactionHash = context.TransactionId,
            Timestamp = context.BlockTime,
            NftInfoId = bizId
        };
        await _nftInfoProvider.AddNFTActivityAsync(context, nftActivityIndex);
    }

}