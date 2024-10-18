using AeFinder.Sdk.Processor;
using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Util;
using Microsoft.Extensions.Logging;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class UniqueSeedsPriceChangedLogEventProcessor: LogEventProcessorBase<UniqueSeedsExternalPriceChanged>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<UniqueSeedsPriceChangedLogEventProcessor> _logger;

    public UniqueSeedsPriceChangedLogEventProcessor(
        ILogger<UniqueSeedsPriceChangedLogEventProcessor> logger,
        IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetSymbolRegistrarContractAddress(chainId);
    }

    public async override Task ProcessAsync(UniqueSeedsExternalPriceChanged eventValue, LogEventContext context)
    {
        if (eventValue.FtPriceList != null)
        {
            _logger.LogDebug("UniqueSeedsChanged FtPriceList {Size}", eventValue.FtPriceList.Value.Count);
            foreach (var priceItem in eventValue.FtPriceList.Value)
            {
                await SaveUniqueSeedsIndexAsync(TokenType.FT, priceItem, context);
            }
        }


        if (eventValue.NftPriceList != null)
        {
            _logger.LogDebug("UniqueSeedsChanged NftPriceList {Size}", eventValue.FtPriceList.Value.Count);
            foreach (var priceItem in eventValue.NftPriceList.Value)
            {
                await SaveUniqueSeedsIndexAsync(TokenType.NFT, priceItem, context);
            }
        }
    }

    private async Task SaveUniqueSeedsIndexAsync(TokenType tokenType, PriceItem priceItem,
                                               LogEventContext context)
    {
        var uniqueSeedPriceIndex = new UniqueSeedPriceIndex()
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
        _objectMapper.Map(context, uniqueSeedPriceIndex);
        await SaveEntityAsync(uniqueSeedPriceIndex);
    }
}