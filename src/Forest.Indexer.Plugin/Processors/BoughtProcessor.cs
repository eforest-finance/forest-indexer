using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Processors.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class BoughtProcessor: AElfLogEventProcessorBase<Bought, LogEventInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;
    private readonly ISeedProvider _seedProvider;

    public BoughtProcessor(
        ILogger<AElfLogEventProcessorBase<Bought, LogEventInfo>> logger,
        IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> seedSymbolIndexRepository,
        ISeedProvider seedProvider,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions) : base(logger)
    {
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _seedProvider = seedProvider;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos?.FirstOrDefault(c => c?.ChainId == chainId)
            ?.SymbolRegistrarContractAddress;
    }

    protected override async Task HandleEventAsync(Bought eventValue, LogEventContext context)
    {
        var seedSymbolIndex = await _seedProvider.GetSeedSymbolIndexAsync(context.ChainId, eventValue.Symbol);
        _objectMapper.Map(context, seedSymbolIndex);
        seedSymbolIndex.TokenPrice = new TokenPriceInfo
        {
            Symbol = eventValue.Price.Symbol,
            Amount = eventValue.Price.Amount
        };
        seedSymbolIndex.Status = SeedStatus.UNREGISTERED;
        seedSymbolIndex.Owner = eventValue.Buyer.ToBase58();
        await _seedSymbolIndexRepository.AddOrUpdateAsync(seedSymbolIndex);
    }
}