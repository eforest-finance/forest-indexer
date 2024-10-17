using AeFinder.Sdk.Processor;
using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Util;
using Microsoft.Extensions.Logging;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class SeedsPriceChangedProcessor : LogEventProcessorBase<SeedsPriceChanged>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<SeedsPriceChangedProcessor> _logger;

    public SeedsPriceChangedProcessor(
        ILogger<SeedsPriceChangedProcessor> logger,
        IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetSymbolRegistrarContractAddress(chainId);
    }

    public async override Task ProcessAsync(SeedsPriceChanged eventValue, LogEventContext context)
    {
        if (eventValue.FtPriceList != null)
        {
            _logger.LogDebug("SeedsPriceChanged FtPriceList {Size}", eventValue.FtPriceList.Value.Count);
            foreach (var priceItem in eventValue.FtPriceList.Value)
            {
                await SaveSeedPriceIndexAsync(TokenType.FT, priceItem, context);
            }
        }


        if (eventValue.NftPriceList != null)
        {
            _logger.LogDebug("SeedsPriceChanged NftPriceList {Size}", eventValue.FtPriceList.Value.Count);
            foreach (var priceItem in eventValue.NftPriceList.Value)
            {
                await SaveSeedPriceIndexAsync(TokenType.NFT, priceItem, context);
            }
        }
    }

    private async Task SaveSeedPriceIndexAsync(TokenType tokenType, PriceItem priceItem,
        LogEventContext context)
    {
        SeedPriceIndex seedPriceIndex = new SeedPriceIndex()
        {
            Id = IdGenerateHelper.GetSeedPriceId(tokenType.ToString(), priceItem.SymbolLength),
            TokenType = tokenType.ToString(),
            SymbolLength = priceItem.SymbolLength,
            TokenPrice = new TokenPriceInfo()
            {
                Symbol = priceItem.Symbol,
                Amount = priceItem.Amount
            }
        };
        _objectMapper.Map(context, seedPriceIndex);
        await SaveEntityAsync(seedPriceIndex);
    }
}