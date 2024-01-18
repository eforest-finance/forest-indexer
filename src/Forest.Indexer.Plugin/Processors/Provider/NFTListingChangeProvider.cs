using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors.Provider;

public interface INFTListingChangeProvider
{
    public Task SaveNFTListingChangeIndexAsync(LogEventContext context, string symbol);
}

public class NFTListingChangeProvider : INFTListingChangeProvider, ISingletonDependency
{
    private readonly IAElfIndexerClientEntityRepository<NFTListingChangeIndex, LogEventInfo>
        _nftListingChangeIndexRepository;

    private readonly IObjectMapper _objectMapper;

    public NFTListingChangeProvider(
        IAElfIndexerClientEntityRepository<NFTListingChangeIndex, LogEventInfo> nftListingChangeIndexRepository,
        IObjectMapper objectMapper)
    {
        _nftListingChangeIndexRepository = nftListingChangeIndexRepository;
        _objectMapper = objectMapper;
    }

    public async Task SaveNFTListingChangeIndexAsync(LogEventContext context, string symbol)
    {
        if (!SymbolHelper.CheckSymbolIsNFT(symbol)&& !SymbolHelper.CheckSymbolIsSeedSymbol(symbol))
        {
            return;
        }
        var nftListingChangeIndex = new NFTListingChangeIndex
        {
            Symbol = symbol,
            Id = IdGenerateHelper.GetId(context.ChainId, symbol),
            UpdateTime = context.BlockTime
        };
        _objectMapper.Map(context, nftListingChangeIndex);
        await _nftListingChangeIndexRepository.AddOrUpdateAsync(nftListingChangeIndex);
    }

}