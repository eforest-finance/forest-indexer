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

public class UniqueSeedsPriceChangedLogEventProcessor: AElfLogEventProcessorBase<UniqueSeedsExternalPriceChanged, LogEventInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<UniqueSeedPriceIndex, LogEventInfo> _uniqueSeedPriceIndexRepository;
    private readonly ILogger<AElfLogEventProcessorBase<UniqueSeedsExternalPriceChanged, LogEventInfo>> _logger;

    public UniqueSeedsPriceChangedLogEventProcessor(
        ILogger<AElfLogEventProcessorBase<UniqueSeedsExternalPriceChanged, LogEventInfo>> logger,
        IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<UniqueSeedPriceIndex, LogEventInfo> uniqueSeedPriceIndexRepository,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions) : base(logger)
    {
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _uniqueSeedPriceIndexRepository = uniqueSeedPriceIndexRepository;
        _logger = logger;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.FirstOrDefault(c => c.ChainId == chainId)
            .SymbolRegistrarContractAddress;
    }

    protected override async Task HandleEventAsync(UniqueSeedsExternalPriceChanged eventValue, LogEventContext context)
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
        await _uniqueSeedPriceIndexRepository.AddOrUpdateAsync(uniqueSeedPriceIndex);
    }
}