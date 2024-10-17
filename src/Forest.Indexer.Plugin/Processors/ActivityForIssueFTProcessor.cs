using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using AElf.Contracts.MultiToken;
using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class ActivityForIssueFTProcessor: LogEventProcessorBase<Issued>
{
    private readonly IObjectMapper _objectMapper;
    protected const string FeeMapTypeElf = "ELF";
    private const string ExtraPropertiesKeyTransactionFee = "TransactionFee";
    private const string ExtraPropertiesKeyResourceFee = "ResourceFee";
    public ActivityForIssueFTProcessor(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetTokenContractAddress(chainId);
    }


    public override async Task ProcessAsync(Issued eventValue, LogEventContext context)
    {
        if (eventValue == null || context == null) return;
        var seedSymbolIndexId = IdGenerateHelper.GetTsmSeedSymbolId(context.ChainId, eventValue.Symbol);
        var seedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(seedSymbolIndexId);
        if (seedSymbolIndex == null) return;
        if (!seedSymbolIndex.TokenType.Equals(TokenType.FT)) return;

        var symbolMarketActivityId = IdGenerateHelper.GetSymbolMarketActivityId(
            SymbolMarketActivityType.Buy.ToString(), context.ChainId, eventValue.Symbol,
            context.Transaction.From, context.Transaction.To, context.Transaction.TransactionId);
        var symbolMarketActivityIndex =
            await GetEntityAsync<SymbolMarketActivityIndex>(symbolMarketActivityId);
        if (symbolMarketActivityIndex != null) return;

        symbolMarketActivityIndex =
            await buildSymbolMarketActivityIndexAsync(symbolMarketActivityId, eventValue, context,
                seedSymbolIndex.SeedType);
        _objectMapper.Map(context, symbolMarketActivityIndex);
        symbolMarketActivityIndex.SeedSymbol = seedSymbolIndex.Symbol;
        await SaveEntityAsync(symbolMarketActivityIndex);

    }

    private async Task<SymbolMarketActivityIndex> buildSymbolMarketActivityIndexAsync(string symbolMarketActivityId,
        Issued eventValue,
        LogEventContext context, SeedType seedType)
    {
        return new SymbolMarketActivityIndex
        {
            Id = symbolMarketActivityId,
            Type = SymbolMarketActivityType.Issue,
            TransactionDateTime = context.Block.BlockTime,
            Symbol = eventValue.Symbol,
            Address = FullAddressHelper.ToFullAddress(eventValue.To.ToBase58(), context.ChainId),
            SeedType = seedType,
            TransactionFee = GetFeeTypeElfAmount(context.Transaction.ExtraProperties),
            TransactionFeeSymbol = FeeMapTypeElf,
            TransactionId = context.Transaction.TransactionId,
        };
    }
    private long GetFeeTypeElfAmount(Dictionary<string, string> extraProperties)
    {
        var feeMap = GetTransactionFee(extraProperties);
        if (feeMap.TryGetValue(FeeMapTypeElf, out var value))
        {
            return value;
        }

        return 0;
    }
    
    private Dictionary<string, long> GetTransactionFee(Dictionary<string, string> extraProperties)
    {
        var feeMap = new Dictionary<string, long>();
        if (extraProperties.TryGetValue(ExtraPropertiesKeyTransactionFee, out var transactionFee))
        {
            Logger.LogDebug("TransactionFee {Fee}",transactionFee);
            feeMap = JsonConvert.DeserializeObject<Dictionary<string, long>>(transactionFee) ??
                     new Dictionary<string, long>();
        }

        if (extraProperties.TryGetValue(ExtraPropertiesKeyResourceFee, out var resourceFee))
        {
            Logger.LogDebug("ResourceFee {Fee}",resourceFee);
            var resourceFeeMap = JsonConvert.DeserializeObject<Dictionary<string, long>>(resourceFee) ??
                                 new Dictionary<string, long>();
            foreach (var (symbol, fee) in resourceFeeMap)
            {
                if (feeMap.ContainsKey(symbol))
                {
                    feeMap[symbol] += fee;
                }
                else
                {
                    feeMap[symbol] = fee;
                }
            }
        }

        return feeMap;
    }
}