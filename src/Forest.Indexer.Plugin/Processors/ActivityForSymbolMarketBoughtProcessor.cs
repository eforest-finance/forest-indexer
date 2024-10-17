using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class ActivityForSymbolMarketBoughtProcessor : LogEventProcessorBase<Bought>
{
    private readonly IObjectMapper _objectMapper;
    protected const string FeeMapTypeElf = "ELF";
    private const string ExtraPropertiesKeyTransactionFee = "TransactionFee";
    private const string ExtraPropertiesKeyResourceFee = "ResourceFee";

    public ActivityForSymbolMarketBoughtProcessor(
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
        Logger.LogInformation("event Bought symobl:{A}", eventValue.Symbol);
        if (eventValue == null || context == null) return;
        var seedSymbolIndexId = IdGenerateHelper.GetTsmSeedSymbolId(context.ChainId, eventValue.Symbol);
        var seedSymbolIndex =
            await GetEntityAsync<TsmSeedSymbolIndex>(seedSymbolIndexId);

        var seedType = SeedType.Regular;
        if (seedSymbolIndex != null)
        {
            seedType = seedSymbolIndex.SeedType;
        }

        var symbolMarketActivityId = IdGenerateHelper.GetSymbolMarketActivityId(
            SymbolMarketActivityType.Buy.ToString(), context.ChainId, eventValue.Symbol,
            context.Transaction.From, context.Transaction.To, context.Transaction.TransactionId);
        var symbolMarketActivityIndex =
            await GetEntityAsync<SymbolMarketActivityIndex>(symbolMarketActivityId);
        if (symbolMarketActivityIndex != null) return;

        symbolMarketActivityIndex = await buildSymbolMarketActivityIndexAsync(symbolMarketActivityId, eventValue,
            context, seedType);
        _objectMapper.Map(context, symbolMarketActivityIndex);
        await SaveEntityAsync(symbolMarketActivityIndex);

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

            TransactionDateTime = context.Block.BlockTime,
            Symbol = eventValue.Symbol,
            Address = FullAddressHelper.ToFullAddress(eventValue.Buyer.ToBase58(), context.ChainId),
            SeedType = seedType,
            TransactionFee = GetFeeTypeElfAmount(context.Transaction.ExtraProperties),
            TransactionFeeSymbol = FeeMapTypeElf,
            TransactionId = context.Transaction.TransactionId
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
            Logger.LogDebug("ActivityForSymbolMarketBoughtProcessor TransactionFee {Fee}",transactionFee);
            feeMap = JsonConvert.DeserializeObject<Dictionary<string, long>>(transactionFee) ??
                     new Dictionary<string, long>();
        }

        if (extraProperties.TryGetValue(ExtraPropertiesKeyResourceFee, out var resourceFee))
        {
            Logger.LogDebug("ActivityForSymbolMarketBoughtProcessor ResourceFee {Fee}",resourceFee);
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