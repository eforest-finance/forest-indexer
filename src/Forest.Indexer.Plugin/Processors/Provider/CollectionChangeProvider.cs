using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors.Provider;

public interface ICollectionChangeProvider
{
    public Task SaveCollectionChangeIndexAsync(LogEventContext context, string symbol);
    
    public Task SaveCollectionPriceChangeIndexAsync(LogEventContext context, string symbol);
}

public class CollectionChangeProvider : ICollectionChangeProvider, ISingletonDependency
{
    private readonly IAElfIndexerClientEntityRepository<CollectionChangeIndex, LogEventInfo>
        _collectionChangeIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<CollectionPriceChangeIndex, LogEventInfo>
        _collectionPriceChangeIndexRepository;
    
    private readonly IObjectMapper _objectMapper;

    public CollectionChangeProvider(
        IAElfIndexerClientEntityRepository<CollectionChangeIndex, LogEventInfo> collectionChangeIndexRepository,
        IAElfIndexerClientEntityRepository<CollectionPriceChangeIndex, LogEventInfo> collectionPriceChangeIndexRepository,
        IObjectMapper objectMapper)
    {
        _collectionChangeIndexRepository = collectionChangeIndexRepository;
        _collectionPriceChangeIndexRepository = collectionPriceChangeIndexRepository;
        _objectMapper = objectMapper;
    }

    public async Task SaveCollectionChangeIndexAsync(LogEventContext context, string symbol)
    {
        var collectionChangeIndex = new CollectionChangeIndex();
        var nftCollectionSymbol = SymbolHelper.GetNFTCollectionSymbol(symbol);
        if (nftCollectionSymbol == null)
        {
            return;
        }

        collectionChangeIndex.Symbol = nftCollectionSymbol;
        collectionChangeIndex.Id = IdGenerateHelper.GetNFTCollectionId(context.ChainId, nftCollectionSymbol);
        collectionChangeIndex.UpdateTime = context.BlockTime;
        _objectMapper.Map(context, collectionChangeIndex);
        await _collectionChangeIndexRepository.AddOrUpdateAsync(collectionChangeIndex);
    }
    
    public async Task SaveCollectionPriceChangeIndexAsync(LogEventContext context, string symbol)
    {
        var collectionPriceChangeIndex = new CollectionPriceChangeIndex();
        var nftCollectionSymbol = SymbolHelper.GetNFTCollectionSymbol(symbol);
        if (nftCollectionSymbol == null)
        {
            return;
        }

        collectionPriceChangeIndex.Symbol = nftCollectionSymbol;
        collectionPriceChangeIndex.Id = IdGenerateHelper.GetNFTCollectionId(context.ChainId, nftCollectionSymbol);
        collectionPriceChangeIndex.UpdateTime = context.BlockTime;
        _objectMapper.Map(context, collectionPriceChangeIndex);
        await _collectionPriceChangeIndexRepository.AddOrUpdateAsync(collectionPriceChangeIndex);
    }
    
}