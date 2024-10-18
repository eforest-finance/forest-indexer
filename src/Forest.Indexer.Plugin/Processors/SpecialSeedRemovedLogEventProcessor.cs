using AeFinder.Sdk.Processor;
using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class SpecialSeedRemovedLogEventProcessor: LogEventProcessorBase<SpecialSeedRemoved>
{
    private readonly IObjectMapper _objectMapper;

    public SpecialSeedRemovedLogEventProcessor(
        IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }
    
    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetSymbolRegistrarContractAddress(chainId);
    }

    public async override Task ProcessAsync(SpecialSeedRemoved eventValue, LogEventContext context)
    {
        if (eventValue == null) return;
        
        var seedList = eventValue.RemoveList.Value;
        foreach (var seed in seedList)
        {
            var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, seed.Symbol);
            var seedSymbol = await GetEntityAsync<TsmSeedSymbolIndex>(seedSymbolId);
            if(seedSymbol == null) continue;
            
            _objectMapper.Map(context, seedSymbol);
            await DeleteEntityAsync<TsmSeedSymbolIndex>(seedSymbolId);
        }
    }
}