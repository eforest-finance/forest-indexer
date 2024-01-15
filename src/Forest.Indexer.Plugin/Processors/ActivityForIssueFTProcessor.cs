using AElf.Contracts.MultiToken;
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

public class ActivityForIssueFTProcessor: ActivityProcessorBase<Issued>
{
    private readonly IAElfIndexerClientEntityRepository<SymbolMarketActivityIndex, TransactionInfo>
        _symbolMarketActivityIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo>
        _seedSymbolIndexRepository;

    private readonly ILogger<AElfLogEventProcessorBase<Issued, TransactionInfo>> _logger;
    private readonly ContractInfoOptions _contractInfoOptions;

    public ActivityForIssueFTProcessor(
        IAElfIndexerClientEntityRepository<SymbolMarketActivityIndex, TransactionInfo>
            symbolMarketActivityIndexRepository,
        IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo>
            seedSymbolIndexRepository,
        IObjectMapper objectMapper,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        ILogger<AElfLogEventProcessorBase<Issued, TransactionInfo>> logger) : base(objectMapper,
        contractInfoOptions,
        logger)
    {
        _symbolMarketActivityIndexRepository = symbolMarketActivityIndexRepository;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _contractInfoOptions = contractInfoOptions.Value;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).TokenContractAddress;
    }

    protected override async Task HandleEventAsync(Issued eventValue, LogEventContext context)
    {
        if (eventValue == null || context == null) return;
        var seedSymbolIndexId = IdGenerateHelper.GetTsmSeedSymbolId(context.ChainId, eventValue.Symbol);
        var seedSymbolIndex =
            await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolIndexId,
                context.ChainId);
        if (seedSymbolIndex == null) return;
        if (!seedSymbolIndex.TokenType.Equals(TokenType.FT)) return;

        var symbolMarketActivityId = IdGenerateHelper.GetSymbolMarketActivityId(
            SymbolMarketActivityType.Buy.ToString(), context.ChainId, eventValue.Symbol,
            context.From, context.To, context.TransactionId);
        var symbolMarketActivityIndex =
            await _symbolMarketActivityIndexRepository.GetFromBlockStateSetAsync(symbolMarketActivityId,
                context.ChainId);
        if (symbolMarketActivityIndex != null) return;

        symbolMarketActivityIndex =
            await buildSymbolMarketActivityIndexAsync(symbolMarketActivityId, eventValue, context,
                seedSymbolIndex.SeedType);
        _objectMapper.Map(context, symbolMarketActivityIndex);
        symbolMarketActivityIndex.SeedSymbol = seedSymbolIndex.Symbol;
        await _symbolMarketActivityIndexRepository.AddOrUpdateAsync(symbolMarketActivityIndex);
    }

    private async Task<SymbolMarketActivityIndex> buildSymbolMarketActivityIndexAsync(string symbolMarketActivityId,
        Issued eventValue,
        LogEventContext context, SeedType seedType)
    {
        return new SymbolMarketActivityIndex
        {
            Id = symbolMarketActivityId,
            Type = SymbolMarketActivityType.Issue,
            TransactionDateTime = context.BlockTime,
            Symbol = eventValue.Symbol,
            Address = FullAddressHelper.ToFullAddress(eventValue.To.ToBase58(), context.ChainId),
            SeedType = seedType,
            TransactionFee = GetFeeTypeElfAmount(context.ExtraProperties),
            TransactionFeeSymbol = FeeMapTypeElf,
            TransactionId = context.TransactionId,
        };
    }
}