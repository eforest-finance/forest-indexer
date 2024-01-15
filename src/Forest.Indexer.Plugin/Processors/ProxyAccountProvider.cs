using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Nest;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public interface IProxyAccountProvider
{
    Task<CollectionIndex> FillProxyAccountInfoForNFTCollectionIndexAsync(CollectionIndex collectionIndex
        , string chainId);
    
    Task<SeedSymbolMarketTokenIndex> FillProxyAccountInfoForSymbolMarketTokenIndexOwnerAsync(SeedSymbolMarketTokenIndex seedSymbolMarketTokenIndex
        , string chainId);
    
    Task<SeedSymbolMarketTokenIndex> FillProxyAccountInfoForSymbolMarketTokenIndexIssuerAsync(SeedSymbolMarketTokenIndex seedSymbolMarketTokenIndex
        , string chainId);

    Task<NFTInfoIndex> FillProxyAccountInfoForNFTInfoIndexAsync(NFTInfoIndex nftInfoIndex, string chainId);

    Task UpdateProxyAccountInfoForNFTCollectionIndexAsync(ProxyAccountIndex proxyAccountIndex, string chainId,
        LogEventContext context);

    Task UpdateProxyAccountInfoForNFTInfoIndexAsync(ProxyAccountIndex proxyAccountIndex, string chainId,
        LogEventContext context);
}

public class ProxyAccountProvider : IProxyAccountProvider, ISingletonDependency
{
    private readonly IAElfIndexerClientEntityRepository<ProxyAccountIndex, LogEventInfo> _proxyAccountIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<CollectionIndex, LogEventInfo> _nftCollectionIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> _nftInfoIndexRepository;
    private readonly IObjectMapper _objectMapper;

    public ProxyAccountProvider(
        IAElfIndexerClientEntityRepository<ProxyAccountIndex, LogEventInfo> proxyAccountIndexRepository,
        IAElfIndexerClientEntityRepository<CollectionIndex, LogEventInfo> nftCollectionIndexRepository,
        IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> nftInfoIndexRepository,
        IObjectMapper objectMapper)
    {
        _proxyAccountIndexRepository = proxyAccountIndexRepository;
        _nftCollectionIndexRepository = nftCollectionIndexRepository;
        _nftInfoIndexRepository = nftInfoIndexRepository;
        _objectMapper = objectMapper;
    }

    public async Task<SeedSymbolMarketTokenIndex> FillProxyAccountInfoForSymbolMarketTokenIndexOwnerAsync(SeedSymbolMarketTokenIndex seedSymbolMarketTokenIndex, string chainId)
    {
        if (seedSymbolMarketTokenIndex == null || chainId.IsNullOrEmpty())
            return seedSymbolMarketTokenIndex;
        if (seedSymbolMarketTokenIndex.Owner.IsNullOrEmpty())
            seedSymbolMarketTokenIndex.Owner = seedSymbolMarketTokenIndex.Issuer;
        var proxyAccountId = IdGenerateHelper.GetProxyAccountIndexId(seedSymbolMarketTokenIndex.Owner);
        var proxyAccount =
            await _proxyAccountIndexRepository.GetAsync(proxyAccountId);

        return FillOwnerSymbolMarketTokenIndex(seedSymbolMarketTokenIndex, proxyAccount);
    }

    public async Task<SeedSymbolMarketTokenIndex> FillProxyAccountInfoForSymbolMarketTokenIndexIssuerAsync(SeedSymbolMarketTokenIndex seedSymbolMarketTokenIndex, string chainId)
    {
        if (seedSymbolMarketTokenIndex == null || chainId.IsNullOrEmpty() || seedSymbolMarketTokenIndex.Issuer.IsNullOrEmpty()) return seedSymbolMarketTokenIndex;
        var proxyAccountId = IdGenerateHelper.GetProxyAccountIndexId(seedSymbolMarketTokenIndex.Issuer);
        var proxyAccount =
            await _proxyAccountIndexRepository.GetAsync(proxyAccountId);
        return FillSymbolMarketTokenIndexIssuer(seedSymbolMarketTokenIndex, proxyAccount);
    }

    public async Task<CollectionIndex> FillProxyAccountInfoForNFTCollectionIndexAsync(
        CollectionIndex collectionIndex,
        string chainId)
    {
        if (collectionIndex == null || chainId.IsNullOrEmpty())
            return collectionIndex;
        if (collectionIndex.Owner.IsNullOrEmpty())
            collectionIndex.Owner = collectionIndex.Issuer;
        var proxyAccountId = IdGenerateHelper.GetProxyAccountIndexId(collectionIndex.Owner);
        var proxyAccount =
            await _proxyAccountIndexRepository.GetAsync(proxyAccountId);

        return FillNFTCollectionIndex(collectionIndex, proxyAccount);
    }

    public async Task<NFTInfoIndex> FillProxyAccountInfoForNFTInfoIndexAsync(NFTInfoIndex nftInfoIndex, string chainId)
    {
        if (nftInfoIndex == null || chainId.IsNullOrEmpty() || nftInfoIndex.Issuer.IsNullOrEmpty()) return nftInfoIndex;
        var proxyAccountId = IdGenerateHelper.GetProxyAccountIndexId(nftInfoIndex.Issuer);
        var proxyAccount =
            await _proxyAccountIndexRepository.GetAsync(proxyAccountId);
        return FillNFTInfoIndex(nftInfoIndex, proxyAccount);
    }

    public async Task UpdateProxyAccountInfoForNFTCollectionIndexAsync(ProxyAccountIndex proxyAccountIndex, string chainId,LogEventContext context)
    {
        if (proxyAccountIndex == null || proxyAccountIndex.ProxyAccountAddress.IsNullOrEmpty() ||
            chainId.IsNullOrEmpty()) return;
        var mustQuery = new List<Func<QueryContainerDescriptor<CollectionIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.Owner).Value(proxyAccountIndex.ProxyAccountAddress)));

        QueryContainer Filter(QueryContainerDescriptor<CollectionIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await _nftCollectionIndexRepository.GetListAsync(Filter, skip: 0, limit: 1);
        if (result == null) return;
        var nftCollectionIndex = FillNFTCollectionIndex(result?.Item2?.FirstOrDefault(), proxyAccountIndex);
        if (nftCollectionIndex == null) return;
        _objectMapper.Map(context,nftCollectionIndex);
        await _nftCollectionIndexRepository.AddOrUpdateAsync(nftCollectionIndex);
    }

    public async Task UpdateProxyAccountInfoForNFTInfoIndexAsync(ProxyAccountIndex proxyAccountIndex, string chainId,LogEventContext context)
    {
        if (proxyAccountIndex == null || proxyAccountIndex.ProxyAccountAddress.IsNullOrEmpty() ||
            chainId.IsNullOrEmpty()) return;
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.Issuer).Value(proxyAccountIndex.ProxyAccountAddress)));

        QueryContainer Filter(QueryContainerDescriptor<NFTInfoIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await _nftInfoIndexRepository.GetListAsync(Filter, skip: 0, limit: 1);
        if (result == null) return;
        var nftInfoIndex = FillNFTInfoIndex(result?.Item2?.FirstOrDefault(), proxyAccountIndex);
        if (nftInfoIndex == null) return;
        _objectMapper.Map(context,nftInfoIndex);
        await _nftInfoIndexRepository.AddOrUpdateAsync(nftInfoIndex);
    }
    
    private SeedSymbolMarketTokenIndex FillOwnerSymbolMarketTokenIndex(SeedSymbolMarketTokenIndex seedSymbolMarketTokenIndex,
        ProxyAccountIndex proxyAccountIndex)
    {
        if (seedSymbolMarketTokenIndex == null) return seedSymbolMarketTokenIndex;

        if (proxyAccountIndex != null)
            seedSymbolMarketTokenIndex.OwnerManagerSet = proxyAccountIndex.ManagersSet;
        else
            seedSymbolMarketTokenIndex.OwnerManagerSet = new HashSet<string> { seedSymbolMarketTokenIndex.Owner };

        seedSymbolMarketTokenIndex.RandomOwnerManager = seedSymbolMarketTokenIndex.OwnerManagerSet?.FirstOrDefault("");

        return seedSymbolMarketTokenIndex;
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

    private SeedSymbolMarketTokenIndex FillSymbolMarketTokenIndexIssuer(SeedSymbolMarketTokenIndex seedSymbolMarketTokenIndex,
        ProxyAccountIndex proxyAccountIndex)
    {
        if (seedSymbolMarketTokenIndex == null) return seedSymbolMarketTokenIndex;

        if (proxyAccountIndex != null)
            seedSymbolMarketTokenIndex.IssueManagerSet = proxyAccountIndex.ManagersSet;
        else
            seedSymbolMarketTokenIndex.IssueManagerSet = new HashSet<string> { seedSymbolMarketTokenIndex.Issuer };

        seedSymbolMarketTokenIndex.RandomIssueManager = seedSymbolMarketTokenIndex.IssueManagerSet?.FirstOrDefault("");

        return seedSymbolMarketTokenIndex;
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