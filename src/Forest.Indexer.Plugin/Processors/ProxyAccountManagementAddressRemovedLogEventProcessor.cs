using AElf.Contracts.ProxyAccountContract;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class ProxyAccountManagementAddressRemovedLogEventProcessor : AElfLogEventProcessorBase<
    ProxyAccountManagementAddressRemoved,
    LogEventInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> _nftInfoIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<CollectionIndex, LogEventInfo> _nftCollectionIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<ProxyAccountIndex, LogEventInfo> _agentIndexRepository;
    private readonly IProxyAccountProvider _proxyAccountProvider;

    public ProxyAccountManagementAddressRemovedLogEventProcessor(
        ILogger<AElfLogEventProcessorBase<ProxyAccountManagementAddressRemoved, LogEventInfo>> logger,
        IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> nftInfoIndexRepository,
        IAElfIndexerClientEntityRepository<CollectionIndex, LogEventInfo> nftCollectionIndexRepository,
        IAElfIndexerClientEntityRepository<ProxyAccountIndex, LogEventInfo> agentIndexRepository,
        IProxyAccountProvider proxyAccountProvider,
        IObjectMapper objectMapper,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions) : base(logger)
    {
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _nftInfoIndexRepository = nftInfoIndexRepository;
        _nftCollectionIndexRepository = nftCollectionIndexRepository;
        _agentIndexRepository = agentIndexRepository;
        _proxyAccountProvider = proxyAccountProvider;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos?.FirstOrDefault(c => c?.ChainId == chainId)
            ?.ProxyAccountContractAddress;
    }

    protected override async Task HandleEventAsync(ProxyAccountManagementAddressRemoved eventValue,
        LogEventContext context)
    {
        if (eventValue == null || context == null) return;
        var agentId =
            IdGenerateHelper.GetProxyAccountIndexId(eventValue.ProxyAccountAddress.ToBase58());
        var agentIndex = await _agentIndexRepository.GetAsync(agentId);
        if (agentIndex == null) return;
        agentIndex.ManagersSet.Remove(eventValue.ManagementAddress.Address.ToBase58());
        _objectMapper.Map(context, agentIndex);
        await _agentIndexRepository.AddOrUpdateAsync(agentIndex);
        await _proxyAccountProvider.UpdateProxyAccountInfoForNFTCollectionIndexAsync(agentIndex, context.ChainId,context);
        await _proxyAccountProvider.UpdateProxyAccountInfoForNFTInfoIndexAsync(agentIndex, context.ChainId,context);
    }
}