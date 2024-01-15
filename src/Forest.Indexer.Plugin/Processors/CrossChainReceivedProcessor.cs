using AElf;
using AElf.Contracts.MultiToken;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
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
    
    private readonly IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo>
        _tsmSeedSymbolIndexRepository;
    private readonly ILogger<AElfLogEventProcessorBase<CrossChainReceived, LogEventInfo>> _logger;

    public CrossChainReceivedProcessor(ILogger<AElfLogEventProcessorBase<CrossChainReceived, LogEventInfo>> logger,
        IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> seedSymbolIndexRepository,
        IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> tsmSeedSymbolIndexRepository,
        IObjectMapper objectMapper,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        UserBalanceProvider balanceProvider,
        INFTOfferProvider nftOfferProvider,
        INFTListingInfoProvider nftListingInfoProvider, 
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
        _logger = logger;
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
        var userBalance = await _balanceProvider.SaveUserBalanceAsync(eventValue.Symbol, eventValue.To.ToBase58(),
            eventValue.Amount,
            context);
        await _nftOfferProvider.UpdateOfferRealQualityAsync(eventValue.Symbol, userBalance, eventValue.To.ToBase58(), context);
        await _nftListingInfoProvider.UpdateListingInfoRealQualityAsync(eventValue.Symbol, userBalance, eventValue.To.ToBase58(), context);
        if (SymbolHelper.CheckSymbolIsSeedSymbol(eventValue.Symbol))
        {
            await HandleEventForSeedAsync(eventValue, context);
        }
    }

    private async Task HandleEventForSeedAsync(CrossChainReceived eventValue, LogEventContext context)
    {
        //Get the seed owned symbol from seed symbol index
        var fromChainId = ChainHelper.ConvertChainIdToBase58(eventValue.FromChainId);
        var seedSymbolIndexIdFromChain = IdGenerateHelper.GetSeedSymbolId(fromChainId, eventValue.Symbol);
        var seedSymbolIndexFromChain = await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolIndexIdFromChain, fromChainId);
        _logger.LogDebug(
            "[CrossChainReceived] blockHeight: {BlockHeight} seedSymbolIndexIdFromChain: {seedSymbolIndexIdFromChain}  seedSymbolIndexFromChain: {seedSymbolIndexFromChain}",
            context.BlockHeight, seedSymbolIndexIdFromChain, JsonConvert.SerializeObject(seedSymbolIndexFromChain));
        if (seedSymbolIndexFromChain == null) return;
        
        var seedSymbolIndexIdToChainId =
            IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        var seedSymbolIndexToChain =
            _objectMapper.Map<SeedSymbolIndex, SeedSymbolIndex>(seedSymbolIndexFromChain);
        seedSymbolIndexToChain.Id = seedSymbolIndexIdToChainId;
        seedSymbolIndexToChain.IsDeleteFlag = false;
        seedSymbolIndexToChain.ChainId = context.ChainId;
        seedSymbolIndexToChain.IssuerTo = eventValue.To.ToBase58();
        _objectMapper.Map(context, seedSymbolIndexToChain);
        seedSymbolIndexToChain.Supply = 1;
        //add calc minNftListing
        var minNftListing = await _nftInfoProvider.GetMinListingNftAsync(seedSymbolIndexIdToChainId);
        seedSymbolIndexToChain.OfMinNftListingInfo(minNftListing);
        await _seedSymbolIndexRepository.AddOrUpdateAsync(seedSymbolIndexToChain);
        
        var seedOwnedSymbol = seedSymbolIndexFromChain.SeedOwnedSymbol;

        //Get the tsm seed symbol index info from the from chain 
        var tsmSeedSymbolIndexIdFromChain =
            IdGenerateHelper.GetSeedSymbolId(fromChainId, seedOwnedSymbol);
        var tsmSeedSymbolIndexFromChain =
            await _tsmSeedSymbolIndexRepository.GetFromBlockStateSetAsync(tsmSeedSymbolIndexIdFromChain, fromChainId);
        _logger.LogDebug(
            "[CrossChainReceived] blockHeight: {BlockHeight} tsmSeedSymbolIndexIdFromChain: {tsmSeedSymbolIndexIdFromChain}  tsmSeedSymbolIndexFromChain: {tsmSeedSymbolIndexFromChain}",
            context.BlockHeight, tsmSeedSymbolIndexIdFromChain, tsmSeedSymbolIndexFromChain);
        if(tsmSeedSymbolIndexFromChain == null) return;
        
        //Set the tsm seed symbol index info to the to chain
        var tsmSeedSymbolIndexIdToChain =
            IdGenerateHelper.GetSeedSymbolId(context.ChainId, seedOwnedSymbol);
        var tsmSeedSymbolIndexToChain =
            _objectMapper.Map<TsmSeedSymbolIndex, TsmSeedSymbolIndex>(tsmSeedSymbolIndexFromChain);
        tsmSeedSymbolIndexToChain.Id = tsmSeedSymbolIndexIdToChain;
        tsmSeedSymbolIndexToChain.IsBurned = false;
        tsmSeedSymbolIndexToChain.ChainId = context.ChainId;
        tsmSeedSymbolIndexToChain.Owner = eventValue.To.ToBase58();
        _objectMapper.Map(context, tsmSeedSymbolIndexToChain);
        await _tsmSeedSymbolIndexRepository.AddOrUpdateAsync(tsmSeedSymbolIndexToChain);
    }
}