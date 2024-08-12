using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class ActivityForSymbolMarketBoughtProcessor : ActivityProcessorBase<Bought>
{
    private readonly IAElfIndexerClientEntityRepository<SymbolMarketActivityIndex, TransactionInfo>
        _symbolMarketActivityIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, TransactionInfo>
        _seedSymbolIndexRepository;

    private readonly ILogger<AElfLogEventProcessorBase<Bought, TransactionInfo>> _logger;
    private readonly ContractInfoOptions _contractInfoOptions;

    public ActivityForSymbolMarketBoughtProcessor(
        IAElfIndexerClientEntityRepository<SymbolMarketActivityIndex, TransactionInfo>
            symbolMarketActivityIndexRepository,
        IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, TransactionInfo>
            seedSymbolIndexRepository,
        IObjectMapper objectMapper,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        ILogger<AElfLogEventProcessorBase<Bought, TransactionInfo>> logger) : base(objectMapper, contractInfoOptions,
        logger)
    {
        _symbolMarketActivityIndexRepository = symbolMarketActivityIndexRepository;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _contractInfoOptions = contractInfoOptions.Value;
        _logger = logger;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).SymbolRegistrarContractAddress;
    }

    protected override async Task HandleEventAsync(Bought eventValue, LogEventContext context)
    {
        _logger.LogInformation("event Bought symobl:{A}", eventValue.Symbol);
        if (eventValue == null || context == null) return;
        var seedSymbolIndexId = IdGenerateHelper.GetTsmSeedSymbolId(context.ChainId, eventValue.Symbol);
        var seedSymbolIndex =
            await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolIndexId,
                context.ChainId);
        var seedType = SeedType.Regular;
        if (seedSymbolIndex != null)
        {
            seedType = seedSymbolIndex.SeedType;
        }

        var symbolMarketActivityId = IdGenerateHelper.GetSymbolMarketActivityId(
            SymbolMarketActivityType.Buy.ToString(), context.ChainId, eventValue.Symbol,
            context.From, context.To, context.TransactionId);
        var symbolMarketActivityIndex =
            await _symbolMarketActivityIndexRepository.GetFromBlockStateSetAsync(symbolMarketActivityId,
                context.ChainId);
        if (symbolMarketActivityIndex != null) return;

        symbolMarketActivityIndex = await buildSymbolMarketActivityIndexAsync(symbolMarketActivityId, eventValue,
            context, seedType);
        _objectMapper.Map(context, symbolMarketActivityIndex);
        await _symbolMarketActivityIndexRepository.AddOrUpdateAsync(symbolMarketActivityIndex);
    }

    private async Task<SymbolMarketActivityIndex> buildSymbolMarketActivityIndexAsync(string symbolMarketActivityId,
        Bought eventValue,
        LogEventContext context, SeedType seedType)
    {
        return new SymbolMarketActivityIndex
        {
            Id = symbolMarketActivityId,
            Type = SymbolMarketActivityType.Buy,
            Price = eventValue.Price.Amount,
            PriceSymbol = eventValue.Price.Symbol,

            TransactionDateTime = context.BlockTime,
            Symbol = eventValue.Symbol,
            Address = FullAddressHelper.ToFullAddress(eventValue.Buyer.ToBase58(), context.ChainId),
            SeedType = seedType,
            TransactionFee = GetFeeTypeElfAmount(context.ExtraProperties),
            TransactionFeeSymbol = FeeMapTypeElf,
            TransactionId = context.TransactionId
        };
    }
}