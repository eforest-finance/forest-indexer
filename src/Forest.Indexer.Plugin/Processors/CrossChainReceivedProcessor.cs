using AElf;
using AElf.Contracts.MultiToken;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class CrossChainReceivedProcessor : AElfLogEventProcessorBase<CrossChainReceived, LogEventInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly UserBalanceProvider _balanceProvider;
    private readonly INFTOfferProvider _nftOfferProvider;
    private readonly INFTListingInfoProvider _nftListingInfoProvider;
    private readonly INFTInfoProvider _nftInfoProvider;
    private readonly INFTOfferChangeProvider _nftOfferChangeProvider;

    private readonly IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo>
        _tsmSeedSymbolIndexRepository;
    private readonly ILogger<AElfLogEventProcessorBase<CrossChainReceived, LogEventInfo>> _logger;
    private readonly INFTListingChangeProvider _listingChangeProvider;

    public CrossChainReceivedProcessor(ILogger<AElfLogEventProcessorBase<CrossChainReceived, LogEventInfo>> logger,
        IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> seedSymbolIndexRepository,
        IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> tsmSeedSymbolIndexRepository,
        IObjectMapper objectMapper,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        UserBalanceProvider balanceProvider,
        INFTOfferProvider nftOfferProvider,
        INFTListingInfoProvider nftListingInfoProvider,
        INFTOfferChangeProvider nftOfferChangeProvider,
        INFTListingChangeProvider listingChangeProvider,
        INFTInfoProvider nftInfoProvider) :
        base(logger)
    {
        _objectMapper = objectMapper;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _tsmSeedSymbolIndexRepository = tsmSeedSymbolIndexRepository;
        _contractInfoOptions = contractInfoOptions.Value;
        _balanceProvider = balanceProvider;
        _nftOfferProvider = nftOfferProvider;
        _nftListingInfoProvider = nftListingInfoProvider;
        _nftInfoProvider = nftInfoProvider;
        _nftOfferChangeProvider = nftOfferChangeProvider;
        _logger = logger;
        _listingChangeProvider = listingChangeProvider;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos?.FirstOrDefault(c => c?.ChainId == chainId)
            ?.TokenContractAddress;
    }

    protected override async Task HandleEventAsync(CrossChainReceived eventValue, LogEventContext context)
    {
        _logger.Debug("CrossChainReceived-1-eventValue"+JsonConvert.SerializeObject(eventValue));
        _logger.Debug("CrossChainReceived-2-context"+JsonConvert.SerializeObject(context));
        if (eventValue == null || context == null) return;
        var offerNum =
            await _nftOfferProvider.GetOfferNumAsync(eventValue.Symbol, eventValue.To.ToBase58(), context.ChainId);
        if (offerNum == 0 && SymbolHelper.CheckSymbolIsELF(eventValue.Symbol))
        {
            return;
        }
        var userBalance = await _balanceProvider.SaveUserBalanceAsync(eventValue.Symbol, eventValue.To.ToBase58(),
            eventValue.Amount,
            context);
        await _nftOfferProvider.UpdateOfferRealQualityAsync(eventValue.Symbol, userBalance, eventValue.To.ToBase58(), context);
        await _nftListingInfoProvider.UpdateListingInfoRealQualityAsync(eventValue.Symbol, userBalance, eventValue.To.ToBase58(), context);
        await _nftOfferChangeProvider.SaveNFTOfferChangeIndexAsync(context, eventValue.Symbol, EventType.Other);

        if (SymbolHelper.CheckSymbolIsSeedSymbol(eventValue.Symbol))
        {
            await HandleEventForSeedAsync(eventValue, context);
        }else if (SymbolHelper.CheckSymbolIsNoMainChainNFT(eventValue.Symbol, context.ChainId))
        {
            await _listingChangeProvider.SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
        }
    }

    private async Task HandleEventForSeedAsync(CrossChainReceived eventValue, LogEventContext context)
    {
        //Get the seed owned symbol from seed symbol index
        
        var seedSymbolIndexIdToChainId =
            IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        var seedSymbolIndexToChain = await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolIndexIdToChainId, context.ChainId);
        _logger.Debug("CrossChainReceived-3-seedSymbolIndexIdToChainId"+seedSymbolIndexIdToChainId);
        _logger.Debug("CrossChainReceived-4-seedSymbolIndexToChain"+JsonConvert.SerializeObject(seedSymbolIndexToChain));
        if(seedSymbolIndexToChain == null) return;
        
        seedSymbolIndexToChain.IsDeleteFlag = false;
        seedSymbolIndexToChain.ChainId = context.ChainId;
        seedSymbolIndexToChain.IssuerTo = eventValue.To.ToBase58();
        _objectMapper.Map(context, seedSymbolIndexToChain);
        seedSymbolIndexToChain.Supply = 1;
        //add calc minNftListing
        var minNftListing = await _nftInfoProvider.GetMinListingNftAsync(seedSymbolIndexIdToChainId);
        seedSymbolIndexToChain.OfMinNftListingInfo(minNftListing);
        await _seedSymbolIndexRepository.AddOrUpdateAsync(seedSymbolIndexToChain);

        //Set the tsm seed symbol index info to the to chain
        var tsmSeedSymbolIndexIdToChainId =
            IdGenerateHelper.GetSeedSymbolId(context.ChainId, seedSymbolIndexToChain.SeedOwnedSymbol);
        var tsmSeedSymbolIndexToChain = 
            await _tsmSeedSymbolIndexRepository.GetFromBlockStateSetAsync(tsmSeedSymbolIndexIdToChainId, context.ChainId);
        _logger.Debug("CrossChainReceived-5-tsmSeedSymbolIndexIdToChainId"+tsmSeedSymbolIndexIdToChainId);
        _logger.Debug("CrossChainReceived-6-tsmSeedSymbolIndexToChain"+JsonConvert.SerializeObject(tsmSeedSymbolIndexToChain));
        if(tsmSeedSymbolIndexToChain == null) return;
        tsmSeedSymbolIndexToChain.IsBurned = false;
        tsmSeedSymbolIndexToChain.ChainId = context.ChainId;
        tsmSeedSymbolIndexToChain.Owner = eventValue.To.ToBase58();
        _objectMapper.Map(context, tsmSeedSymbolIndexToChain);
        await _tsmSeedSymbolIndexRepository.AddOrUpdateAsync(tsmSeedSymbolIndexToChain);
        await _listingChangeProvider.SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
    }
}