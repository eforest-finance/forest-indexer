using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors.Provider;

public interface INFTOfferChangeProvider
{
    public Task SaveNFTOfferChangeIndexAsync(LogEventContext context, string symbol, EventType eventType);
}



public class NFTOfferChangeProvider : INFTOfferChangeProvider, ISingletonDependency
{
    private readonly IAElfIndexerClientEntityRepository<NFTOfferChangeIndex, LogEventInfo>
        _nftOfferChangeIndexRepository;
    
    private readonly IObjectMapper _objectMapper;
    
    public NFTOfferChangeProvider(
        IAElfIndexerClientEntityRepository<NFTOfferChangeIndex, LogEventInfo> nftOfferChangeIndexRepository,
        IObjectMapper objectMapper)
    {
        _nftOfferChangeIndexRepository = nftOfferChangeIndexRepository;
        _objectMapper = objectMapper;
    }
    
    public async Task SaveNFTOfferChangeIndexAsync(LogEventContext context, string symbol, EventType eventType)
    {
        var nftOfferChangeIndex = new NFTOfferChangeIndex
        {
            Id = IdGenerateHelper.GetId(context.ChainId, symbol, Guid.NewGuid()),
            NftId = IdGenerateHelper.GetNFTInfoId(context.ChainId, symbol),
            EventType = eventType,
            CreateTime = context.BlockTime
        };
        
        _objectMapper.Map(context, nftOfferChangeIndex);
        await _nftOfferChangeIndexRepository.AddOrUpdateAsync(nftOfferChangeIndex);
    }
}