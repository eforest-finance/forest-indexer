using AElf.Contracts.TokenAdapterContract;
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

public class ActivityForCreateFTAndNFTProcessor: ActivityProcessorBase<ManagerTokenCreated>
{
    private readonly IAElfIndexerClientEntityRepository<SymbolMarketActivityIndex, TransactionInfo>
        _symbolMarketActivityIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo>
        _seedSymbolIndexRepository;

    private readonly ILogger<AElfLogEventProcessorBase<ManagerTokenCreated, TransactionInfo>> _logger;
    private readonly ContractInfoOptions _contractInfoOptions;

    public ActivityForCreateFTAndNFTProcessor(
        IAElfIndexerClientEntityRepository<SymbolMarketActivityIndex, TransactionInfo>
            symbolMarketActivityIndexRepository,
        IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo>
            seedSymbolIndexRepository,
        IObjectMapper objectMapper,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        ILogger<AElfLogEventProcessorBase<ManagerTokenCreated, TransactionInfo>> logger) : base(objectMapper,
        contractInfoOptions,
        logger)
    {
        _symbolMarketActivityIndexRepository = symbolMarketActivityIndexRepository;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _contractInfoOptions = contractInfoOptions.Value;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).TokenAdaptorContractAddress;
    }

    protected override async Task HandleEventAsync(ManagerTokenCreated eventValue, LogEventContext context)
    {
        if (eventValue == null || context == null) return;

        var seedOwnedSymbol = EnumDescriptionHelper.GetExtraInfoValueForSeedOwnedSymbol(eventValue.ExternalInfo);
        if (seedOwnedSymbol.IsNullOrEmpty()) return;

        var seedSymbolIndexId = IdGenerateHelper.GetTsmSeedSymbolId(context.ChainId, seedOwnedSymbol);
        var seedSymbolIndex =
            await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolIndexId,
                context.ChainId);
        if (seedSymbolIndex == null) return;
        if (!seedSymbolIndex.TokenType.Equals(TokenType.FT) && !seedSymbolIndex.TokenType.Equals(TokenType.NFT)) return;

        var symbolMarketActivityId = IdGenerateHelper.GetSymbolMarketActivityId(
            SymbolMarketActivityType.Buy.ToString(), context.ChainId, seedOwnedSymbol,
            context.From, context.To, context.TransactionId);
        var symbolMarketActivityIndex =
            await _symbolMarketActivityIndexRepository.GetFromBlockStateSetAsync(symbolMarketActivityId,
                context.ChainId);
        if (symbolMarketActivityIndex != null) return;

        symbolMarketActivityIndex = await buildSymbolMarketActivityIndexAsync(symbolMarketActivityId, eventValue,
            context, seedSymbolIndex.SeedType);
        _objectMapper.Map(context, symbolMarketActivityIndex);
        symbolMarketActivityIndex.Symbol = seedOwnedSymbol;
        symbolMarketActivityIndex.SeedSymbol = eventValue.Symbol;
        await _symbolMarketActivityIndexRepository.AddOrUpdateAsync(symbolMarketActivityIndex);
    }

    private async Task<SymbolMarketActivityIndex> buildSymbolMarketActivityIndexAsync(string symbolMarketActivityId,
        ManagerTokenCreated eventValue,
        LogEventContext context, SeedType seedType)
    {
        return new SymbolMarketActivityIndex
        {
            Id = symbolMarketActivityId,
            Type = SymbolMarketActivityType.Create,
            TransactionDateTime = context.BlockTime,
            Symbol = eventValue.Symbol,
            Address = FullAddressHelper.ToFullAddress(eventValue.RealOwner.ToBase58(), context.ChainId),
            SeedType = seedType,
            TransactionFee = GetFeeTypeElfAmount(context.ExtraProperties),
            TransactionFeeSymbol = FeeMapTypeElf,
            TransactionId = context.TransactionId,
        };
    }
}