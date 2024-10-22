using AeFinder.Sdk;
using AeFinder.Sdk.Processor;
using AElf.Contracts.ProxyAccountContract;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class ProxyAccountManagementAddressRemovedLogEventProcessor : LogEventProcessorBase<
    ProxyAccountManagementAddressRemoved>
{
    private readonly IObjectMapper _objectMapper;
    private readonly IReadOnlyRepository<CollectionIndex> _collectionIndexRepository;
    private readonly IReadOnlyRepository<NFTInfoIndex> _nftInfoIndexRepository;

    public ProxyAccountManagementAddressRemovedLogEventProcessor(
        IObjectMapper objectMapper,
        IReadOnlyRepository<CollectionIndex> collectionIndexRepository,
        IReadOnlyRepository<NFTInfoIndex> nftInfoIndexRepository)
    {
        _objectMapper = objectMapper;
        _collectionIndexRepository = collectionIndexRepository;
        _nftInfoIndexRepository = nftInfoIndexRepository;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetProxyAccountContractAddress(chainId);
    }

    public async override Task ProcessAsync(ProxyAccountManagementAddressRemoved eventValue,
        LogEventContext context)
    {
        if (eventValue == null || context == null) return;
        var agentId =
            IdGenerateHelper.GetProxyAccountIndexId(eventValue.ProxyAccountAddress.ToBase58());
        var agentIndex = await GetEntityAsync<ProxyAccountIndex>(agentId);
        if (agentIndex == null) return;
        agentIndex.ManagersSet.Remove(eventValue.ManagementAddress.Address.ToBase58());
        _objectMapper.Map(context, agentIndex);
        await SaveEntityAsync(agentIndex);
        await UpdateProxyAccountInfoForNFTCollectionIndexAsync(agentIndex, context.ChainId,context);
        await UpdateProxyAccountInfoForNFTInfoIndexAsync(agentIndex, context.ChainId,context);
    }
    
    private async Task UpdateProxyAccountInfoForNFTCollectionIndexAsync(ProxyAccountIndex proxyAccountIndex, string chainId,LogEventContext context)
    {
        if (proxyAccountIndex == null || proxyAccountIndex.ProxyAccountAddress.IsNullOrEmpty() ||
            chainId.IsNullOrEmpty()) return;
        
        var queryable = await _collectionIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(x => x.Owner == proxyAccountIndex.ProxyAccountAddress);
        var result = queryable.Skip(0).Take(1).ToList();

        if (result.IsNullOrEmpty()) return;
        var nftCollectionIndex = FillNFTCollectionIndex(result.FirstOrDefault(), proxyAccountIndex);
        if (nftCollectionIndex == null) return;
        _objectMapper.Map(context,nftCollectionIndex);
        await SaveEntityAsync(nftCollectionIndex);
    }
    
    private async Task UpdateProxyAccountInfoForNFTInfoIndexAsync(ProxyAccountIndex proxyAccountIndex, string chainId,LogEventContext context)
    {
        if (proxyAccountIndex == null || proxyAccountIndex.ProxyAccountAddress.IsNullOrEmpty() ||
            chainId.IsNullOrEmpty()) return;
        
        var queryable = await _nftInfoIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(x => x.Issuer == proxyAccountIndex.ProxyAccountAddress);
        var result = queryable.Skip(0).Take(1).ToList();
        
        if (result.IsNullOrEmpty()) return;
        var nftInfoIndex = FillNFTInfoIndex(result.FirstOrDefault(), proxyAccountIndex);
        if (nftInfoIndex == null) return;
        _objectMapper.Map(context,nftInfoIndex);
        await SaveEntityAsync(nftInfoIndex);
    }
    private CollectionIndex FillNFTCollectionIndex(CollectionIndex collectionIndex,
        ProxyAccountIndex proxyAccountIndex)
    {
        if (collectionIndex == null) return collectionIndex;

        if (proxyAccountIndex != null)
            collectionIndex.OwnerManagerSet = proxyAccountIndex.ManagersSet;
        else
            collectionIndex.OwnerManagerSet = new HashSet<string> { collectionIndex.Owner };

        collectionIndex.RandomOwnerManager = collectionIndex.OwnerManagerSet?.FirstOrDefault("");

        return collectionIndex;
    }

    private NFTInfoIndex FillNFTInfoIndex(NFTInfoIndex nftInfoIndex,
        ProxyAccountIndex proxyAccountIndex)
    {
        if (nftInfoIndex == null) return nftInfoIndex;

        if (proxyAccountIndex != null)
            nftInfoIndex.IssueManagerSet = proxyAccountIndex.ManagersSet;
        else
            nftInfoIndex.IssueManagerSet = new HashSet<string> { nftInfoIndex.Issuer };

        nftInfoIndex.RandomIssueManager = nftInfoIndex.IssueManagerSet?.FirstOrDefault("");

        return nftInfoIndex;
    }
    
    
}