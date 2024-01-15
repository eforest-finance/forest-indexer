using AElf.Contracts.ProxyAccountContract;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class ProxyAccountCreatedLogEventProcessor : AElfLogEventProcessorBase<ProxyAccountCreated, LogEventInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
     private readonly IAElfIndexerClientEntityRepository<ProxyAccountIndex, LogEventInfo> _agentIndexRepository;
    private readonly IProxyAccountProvider _proxyAccountProvider;


    public ProxyAccountCreatedLogEventProcessor(
        ILogger<AElfLogEventProcessorBase<ProxyAccountCreated, LogEventInfo>> logger,
        IAElfIndexerClientEntityRepository<ProxyAccountIndex, LogEventInfo> agentIndexRepository,
        IProxyAccountProvider proxyAccountProvider,
        IObjectMapper objectMapper,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions) : base(logger)
    {
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _agentIndexRepository = agentIndexRepository;
        _proxyAccountProvider = proxyAccountProvider;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos?.FirstOrDefault(c => c?.ChainId == chainId)
            ?.ProxyAccountContractAddress;
    }

    protected override async Task HandleEventAsync(ProxyAccountCreated eventValue, LogEventContext context)
    {
        if (eventValue == null || context == null) return;
        var agentId =
            IdGenerateHelper.GetProxyAccountIndexId(eventValue.ProxyAccountAddress.ToBase58());
        var agentIndex = _objectMapper.Map<ProxyAccountCreated, ProxyAccountIndex>(eventValue);
        agentIndex.Id = agentId;
        _objectMapper.Map(context, agentIndex);
        agentIndex.CreateTime = DateTime.Now;
        await _agentIndexRepository.AddOrUpdateAsync(agentIndex);
        await _proxyAccountProvider.UpdateProxyAccountInfoForNFTCollectionIndexAsync(agentIndex, context.ChainId,context);
        await _proxyAccountProvider.UpdateProxyAccountInfoForNFTInfoIndexAsync(agentIndex, context.ChainId,context);
    }
}