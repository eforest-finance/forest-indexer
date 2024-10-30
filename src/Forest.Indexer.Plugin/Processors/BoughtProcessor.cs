using AeFinder.Sdk.Processor;
using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Util;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class BoughtProcessor: LogEventProcessorBase<Bought>
{
    private readonly IObjectMapper _objectMapper;

    public BoughtProcessor(
        IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetSymbolRegistrarContractAddress(chainId);
    }

    public override async Task ProcessAsync(Bought eventValue, LogEventContext context)
    {
        var seedSymbolIndex = await GetSeedSymbolIndexAsync(context.ChainId, eventValue.Symbol);
        _objectMapper.Map(context, seedSymbolIndex);
        seedSymbolIndex.TokenPrice = new TokenPriceInfo
        {
            Symbol = eventValue.Price.Symbol,
            Amount = eventValue.Price.Amount
        };
        seedSymbolIndex.Status = SeedStatus.UNREGISTERED;
        seedSymbolIndex.Owner = eventValue.Buyer.ToBase58();
        await SaveEntityAsync(seedSymbolIndex);
    }
    public async Task<TsmSeedSymbolIndex> GetSeedSymbolIndexAsync(string chainId, string symbol)
    {
        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, symbol);
        var seedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(seedSymbolId);

        if (seedSymbolIndex == null)
        {
            seedSymbolIndex = new TsmSeedSymbolIndex
            {
                Id = seedSymbolId,
                Symbol = symbol,
                SeedName = IdGenerateHelper.GetSeedName(symbol)
            };
            seedSymbolIndex.OfType(TokenHelper.GetTokenType(symbol));
        }

        return seedSymbolIndex;
    }
}