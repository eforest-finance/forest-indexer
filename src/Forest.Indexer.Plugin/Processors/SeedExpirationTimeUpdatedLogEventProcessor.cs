using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using AElf.Contracts.MultiToken;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class SeedExpirationTimeUpdatedLogEventProcessor : LogEventProcessorBase<SeedExpirationTimeUpdated>
{
    private readonly IObjectMapper _objectMapper;

    public SeedExpirationTimeUpdatedLogEventProcessor(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetTokenContractAddress(chainId);
    }

    public async override Task ProcessAsync(SeedExpirationTimeUpdated eventValue, LogEventContext context)
    {
        Logger.LogDebug("SeedExpirationTimeUpdatedLogEventProcessor-1 {A}",JsonConvert.SerializeObject(eventValue));
        Logger.LogDebug("SeedExpirationTimeUpdatedLogEventProcessor-2 {B}",JsonConvert.SerializeObject(context));
        if (eventValue == null || context == null) return;

        if (SymbolHelper.CheckSymbolIsELF(eventValue.Symbol)) return;
        if (SymbolHelper.CheckSymbolIsSeedCollection(eventValue.Symbol)) return;

        if (SymbolHelper.CheckSymbolIsSeedSymbol(eventValue.Symbol))
        {
            await HandleForSeedSymbolAsync(eventValue, context);
            return;
        }
    }

    private async Task HandleForSeedSymbolAsync(SeedExpirationTimeUpdated eventValue, LogEventContext context)
    {
        await DoHandleForSeedSymbolAsync(eventValue, context);
    }

    private async Task DoHandleForSeedSymbolAsync(SeedExpirationTimeUpdated eventValue, LogEventContext context)
    {
        var seedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        var seedSymbolIndex = await GetEntityAsync<SeedSymbolIndex>(seedSymbolIndexId);
        if (seedSymbolIndex == null) return;
        seedSymbolIndex.SeedExpTimeSecond = eventValue.NewExpirationTime;
        seedSymbolIndex.SeedExpTime = DateTimeHelper.FromUnixTimeSeconds(eventValue.NewExpirationTime);
        _objectMapper.Map(context, seedSymbolIndex);
        await SaveEntityAsync(seedSymbolIndex);
        await SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
        
        var tsmSeedSymbolIndexId =
            IdGenerateHelper.GetNewTsmSeedSymbolId(context.ChainId, seedSymbolIndex.Symbol,
                seedSymbolIndex.SeedOwnedSymbol);
        var tsmSeedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(tsmSeedSymbolIndexId);
        if (tsmSeedSymbolIndex == null)
        {
            Logger.LogDebug("new tsmSeedSymbolIndex is null id={A}",tsmSeedSymbolIndexId);
            tsmSeedSymbolIndexId = IdGenerateHelper.GetOldTsmSeedSymbolId(context.ChainId, seedSymbolIndex.SeedOwnedSymbol);
            
            tsmSeedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(tsmSeedSymbolIndexId);
            if (tsmSeedSymbolIndex == null)
            {
                Logger.LogDebug("old tsmSeedSymbolIndex is null id={A}",tsmSeedSymbolIndexId);
                return;
            }
            
        }

        tsmSeedSymbolIndex.ExpireTime = eventValue.NewExpirationTime;
        _objectMapper.Map(context, tsmSeedSymbolIndex);
        await SaveEntityAsync(tsmSeedSymbolIndex);
        
    }
    private async Task SaveNFTListingChangeIndexAsync(LogEventContext context, string symbol)
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