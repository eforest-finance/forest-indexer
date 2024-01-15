using AElf.CSharp.Core;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public abstract class ActivityProcessorBase<TEvent>: AElfLogEventProcessorBase<TEvent, TransactionInfo>
    where TEvent : IEvent<TEvent>, new()
{
    protected readonly ContractInfoOptions _contractInfoOptions;
    protected readonly IObjectMapper _objectMapper;
    private ILogger<AElfLogEventProcessorBase<TEvent, TransactionInfo>> _logger;
    protected const string FeeMapTypeElf = "ELF";
    private const string ExtraPropertiesKeyTransactionFee = "TransactionFee";
    private const string ExtraPropertiesKeyResourceFee = "ResourceFee";

    protected ActivityProcessorBase(
        IObjectMapper objectMapper, IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        ILogger<AElfLogEventProcessorBase<TEvent, TransactionInfo>> logger) : base(logger)
    {
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _logger = logger;
    }

    protected long GetFeeTypeElfAmount(Dictionary<string, string> extraProperties)
    {
        var feeMap = GetTransactionFee(extraProperties);
        if (feeMap.TryGetValue(FeeMapTypeElf, out var value))
        {
            return value;
        }

        return 0;
    }
    
    protected Dictionary<string, long> GetTransactionFee(Dictionary<string, string> extraProperties)
    {
        var feeMap = new Dictionary<string, long>();
        if (extraProperties.TryGetValue(ExtraPropertiesKeyTransactionFee, out var transactionFee))
        {
            _logger.LogDebug("TransactionFee {Fee}",transactionFee);
            feeMap = JsonConvert.DeserializeObject<Dictionary<string, long>>(transactionFee) ??
                     new Dictionary<string, long>();
        }

        if (extraProperties.TryGetValue(ExtraPropertiesKeyResourceFee, out var resourceFee))
        {
            _logger.LogDebug("ResourceFee {Fee}",resourceFee);
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