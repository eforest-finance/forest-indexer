using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class SeedsPriceChangedProcessor: AElfLogEventProcessorBase<SeedsPriceChanged, LogEventInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<SeedPriceIndex, LogEventInfo> _seedPriceIndexRepository;
    private readonly ILogger<AElfLogEventProcessorBase<SeedsPriceChanged, LogEventInfo>> _logger;
    public SeedsPriceChangedProcessor(
        ILogger<AElfLogEventProcessorBase<SeedsPriceChanged, LogEventInfo>> logger,
        IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<SeedPriceIndex, LogEventInfo> seedPriceIndexRepository,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions) : base(logger)
    {
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _seedPriceIndexRepository = seedPriceIndexRepository;
        _logger = logger;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos?.FirstOrDefault(c => c?.ChainId == chainId)
            ?.SymbolRegistrarContractAddress;
    }

    protected override async Task HandleEventAsync(SeedsPriceChanged eventValue, LogEventContext context)
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
        await _seedPriceIndexRepository.AddOrUpdateAsync(seedPriceIndex);
    }
}