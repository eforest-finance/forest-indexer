using AeFinder.Sdk.Processor;
using AElf.Contracts.ProxyAccountContract;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Microsoft.Extensions.Logging;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class ProxyAccountCreatedLogEventProcessor : LogEventProcessorBase<ProxyAccountCreated>
{
    private readonly ILogger<ProxyAccountCreatedLogEventProcessor> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IProxyAccountProvider _proxyAccountProvider;


    public ProxyAccountCreatedLogEventProcessor(
        ILogger<ProxyAccountCreatedLogEventProcessor> logger,
        IProxyAccountProvider proxyAccountProvider,
        IObjectMapper objectMapper)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _proxyAccountProvider = proxyAccountProvider;
    }
    
    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetProxyAccountContractAddress(chainId);
    }

    public async override Task ProcessAsync(ProxyAccountCreated eventValue, LogEventContext context)
    {
        if (eventValue == null || context == null) return;
        var agentId =
            IdGenerateHelper.GetProxyAccountIndexId(eventValue.ProxyAccountAddress.ToBase58());
        var agentIndex = _objectMapper.Map<ProxyAccountCreated, ProxyAccountIndex>(eventValue);
        agentIndex.Id = agentId;
        _objectMapper.Map(context, agentIndex);
        agentIndex.CreateTime = DateTime.Now;
        await SaveEntityAsync(agentIndex);
        // await _proxyAccountProvider.UpdateProxyAccountInfoForNFTCollectionIndexAsync(agentIndex, context.ChainId,context);
        // await _proxyAccountProvider.UpdateProxyAccountInfoForNFTInfoIndexAsync(agentIndex, context.ChainId,context); todo v2
    }
}