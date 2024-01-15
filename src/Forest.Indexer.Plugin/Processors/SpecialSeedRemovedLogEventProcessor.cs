using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class SpecialSeedRemovedLogEventProcessor: AElfLogEventProcessorBase<SpecialSeedRemoved, LogEventInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;
    
    public SpecialSeedRemovedLogEventProcessor(
        ILogger<AElfLogEventProcessorBase<SpecialSeedRemoved, LogEventInfo>> logger, 
        IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> seedSymbolIndexRepository,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions) : base(logger)
    {
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
    }
    
    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos?.FirstOrDefault(c => c?.ChainId == chainId)
            ?.SymbolRegistrarContractAddress;
    }

    protected override async Task HandleEventAsync(SpecialSeedRemoved eventValue, LogEventContext context)
    {
        if (eventValue == null) return;
        
        var seedList = eventValue.RemoveList.Value;
        foreach (var seed in seedList)
        {
            var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, seed.Symbol);
            var seedSymbol = await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolId, context.ChainId);
            if(seedSymbol == null) continue;
            
            _objectMapper.Map(context, seedSymbol);
            await _seedSymbolIndexRepository.DeleteAsync(seedSymbol);
        }
    }
}