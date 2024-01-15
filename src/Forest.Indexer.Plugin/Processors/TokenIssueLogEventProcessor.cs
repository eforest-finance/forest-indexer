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

public class TokenIssueLogEventProcessor : AElfLogEventProcessorBase<Issued, LogEventInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> _nftInfoIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo>
        _tsmSeedSymbolIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<SeedSymbolMarketTokenIndex, LogEventInfo>
        _symbolMarketTokenIndexRepository;

    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly IProxyAccountProvider _proxyAccountProvider;
    private readonly INFTInfoProvider _nftInfoProvider;
    private readonly ICollectionChangeProvider _collectionChangeProvider;
    private readonly INFTOfferProvider _nftOfferProvider;
    private readonly INFTListingInfoProvider _nftListingInfoProvider;

    private readonly ILogger<AElfLogEventProcessorBase<Issued, LogEventInfo>> _logger;


    public TokenIssueLogEventProcessor(ILogger<AElfLogEventProcessorBase<Issued, LogEventInfo>> logger
        , IObjectMapper objectMapper
        , IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> nftInfoIndexRepository
        , IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> seedSymbolIndexRepository
        , IAElfIndexerClientEntityRepository<SeedSymbolMarketTokenIndex, LogEventInfo>
            symbolMarketTokenIndexRepository
        , IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo>
            tsmSeedSymbolIndexRepository
        , IUserBalanceProvider userBalanceProvider
        , IProxyAccountProvider proxyAccountProvider
        , INFTInfoProvider nftInfoProvider
        , ICollectionChangeProvider collectionChangeProvider
        , INFTOfferProvider nftOfferProvider
        , NFTListingInfoProvider nftListingInfoProvider
        , IOptionsSnapshot<ContractInfoOptions> contractInfoOptions) : base(logger)
    {
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _nftInfoIndexRepository = nftInfoIndexRepository;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _symbolMarketTokenIndexRepository = symbolMarketTokenIndexRepository;
        _tsmSeedSymbolIndexRepository = tsmSeedSymbolIndexRepository;
        _userBalanceProvider = userBalanceProvider;
        _proxyAccountProvider = proxyAccountProvider;
        _nftInfoProvider = nftInfoProvider;
        _collectionChangeProvider = collectionChangeProvider;
        _nftOfferProvider = nftOfferProvider;
        _nftListingInfoProvider = nftListingInfoProvider;
        _logger = logger;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos?.FirstOrDefault(c => c?.ChainId == chainId)?.TokenContractAddress;
    }

    protected override async Task HandleEventAsync(Issued eventValue, LogEventContext context)
    {
        _logger.Debug("TokenIssueLogEventProcessor-1"+JsonConvert.SerializeObject(eventValue));
        _logger.Debug("TokenIssueLogEventProcessor-2"+JsonConvert.SerializeObject(context));
        if (eventValue == null || context == null) return;
        await _collectionChangeProvider.SaveCollectionChangeIndexAsync(context, eventValue.Symbol);
        var userBalance = await _userBalanceProvider.SaveUserBalanceAsync(eventValue.Symbol, eventValue.To.ToBase58(),
            eventValue.Amount, context);
        await _nftOfferProvider.UpdateOfferRealQualityAsync(eventValue.Symbol, userBalance, eventValue.To.ToBase58(),
            context);
        await _nftListingInfoProvider.UpdateListingInfoRealQualityAsync(eventValue.Symbol, userBalance, eventValue.To.ToBase58(), context);

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

    private async Task HandleForSymbolMarketTokenAsync(Issued eventValue, LogEventContext context)
    {
        _logger.Debug("TokenIssueLogEventProcessor-3-HandleForNoMainChainSeedTokenAsync");
        if (eventValue == null || context == null) return;
        var seedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(ForestIndexerConstants.MainChain, eventValue.Symbol);
        var seedSymbolIndex =
            await _tsmSeedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolIndexId,
                ForestIndexerConstants.MainChain);
        if (seedSymbolIndex == null) return;
        
        var symbolMarketTokenIndexId = IdGenerateHelper.GetSymbolMarketTokenId(context.ChainId, eventValue.Symbol);
        var symbolMarketTokenIndex =
            await _symbolMarketTokenIndexRepository.GetFromBlockStateSetAsync(symbolMarketTokenIndexId,
                context.ChainId);
        if (symbolMarketTokenIndex == null) return;
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
            await _proxyAccountProvider.FillProxyAccountInfoForSymbolMarketTokenIndexIssuerAsync(symbolMarketTokenIndex,
                context.ChainId);
        _logger.Debug("TokenIssueLogEventProcessor-31-HandleForNoMainChainSeedTokenAsync"+JsonConvert.SerializeObject(symbolMarketTokenIndex));
        _objectMapper.Map(context, symbolMarketTokenIndex);
        _logger.Debug("TokenIssueLogEventProcessor-32-HandleForNoMainChainSeedTokenAsync"+JsonConvert.SerializeObject(symbolMarketTokenIndex));
        await _symbolMarketTokenIndexRepository.AddOrUpdateAsync(symbolMarketTokenIndex);
        _logger.Debug("TokenIssueLogEventProcessor-33-HandleForNoMainChainSeedTokenAsync");
        await SaveActivityAsync(eventValue, context, symbolMarketTokenIndex.Id);
    }

    private async Task HandleForSeedSymbolIssueAsync(Issued eventValue, LogEventContext context)
    {
        await DoHandleForSeedSymbolIssueAsync(eventValue, context);
    }

    private async Task DoHandleForSeedSymbolIssueAsync(Issued eventValue, LogEventContext context)
    {
        var seedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        var seedSymbolIndex =
            await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolIndexId, context.ChainId);
        if (seedSymbolIndex == null) return;
        seedSymbolIndex.IssuerTo = eventValue.To.ToBase58();
        seedSymbolIndex.Supply += eventValue.Amount;
        seedSymbolIndex.Issued += eventValue.Amount;
        //add calc minNftListing
        var minNftListing = await _nftInfoProvider.GetMinListingNftAsync(seedSymbolIndexId);
        seedSymbolIndex.OfMinNftListingInfo(minNftListing);
        _objectMapper.Map(context, seedSymbolIndex);
        await _seedSymbolIndexRepository.AddOrUpdateAsync(seedSymbolIndex);
        await SaveActivityAsync(eventValue, context, seedSymbolIndex.Id);
    }

    private async Task HandleForNFTIssueAsync(Issued eventValue, LogEventContext context)
    {
        var nftInfoIndexId = IdGenerateHelper.GetNFTInfoId(context.ChainId, eventValue.Symbol);
        var nftInfoIndex = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftInfoIndexId, context.ChainId);
        if (nftInfoIndex == null) return;

        nftInfoIndex.Supply += eventValue.Amount;
        nftInfoIndex.Issued += eventValue.Amount;
        //add calc minNftListing
        var minNftListing = await _nftInfoProvider.GetMinListingNftAsync(nftInfoIndexId);
        nftInfoIndex.OfMinNftListingInfo(minNftListing);
        _objectMapper.Map(context, nftInfoIndex);

        nftInfoIndex =
            await _proxyAccountProvider.FillProxyAccountInfoForNFTInfoIndexAsync(nftInfoIndex, context.ChainId);

        await _nftInfoIndexRepository.AddOrUpdateAsync(nftInfoIndex);

        await SaveActivityAsync(eventValue, context, nftInfoIndex.Id);
    }

    private async Task SaveActivityAsync(Issued eventValue, LogEventContext context, string bizId)
    {
        var nftActivityIndexId =
            IdGenerateHelper.GetId(context.ChainId, eventValue.Symbol, NFTActivityType.Issue.ToString(), context.TransactionId);
        var activitySaved = await _nftInfoProvider.AddNFTActivityAsync(context, new NFTActivityIndex
        {
            Id = nftActivityIndexId,
            Type = NFTActivityType.Issue,
            To = eventValue.To.ToBase58(),
            Amount = eventValue.Amount,
            TransactionHash = context.TransactionId,
            Timestamp = context.BlockTime,
            NftInfoId = bizId
        });
    }

    
}