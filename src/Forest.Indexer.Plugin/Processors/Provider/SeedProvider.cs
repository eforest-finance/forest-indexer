using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors.Provider;

public interface ISeedProvider
{
    public Task<TsmSeedSymbolIndex> GetSeedSymbolIndexAsync(string chainId, string symbol);
}


public class SeedProvider: ISeedProvider, ISingletonDependency
{
    private readonly IObjectMapper _objectMapper;
    private readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;

    public SeedProvider(IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> seedSymbolIndexRepository)
    {
        _objectMapper = objectMapper;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
    }

    public async Task<TsmSeedSymbolIndex> GetSeedSymbolIndexAsync(string chainId, string symbol)
    {
        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, symbol);
        var seedSymbolIndex = await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolId, chainId);
        if (seedSymbolIndex == null)
        {
            seedSymbolIndex = new TsmSeedSymbolIndex
            {
                Id = seedSymbolId,
                Symbol = symbol,
                SeedName = IdGenerateHelper.GetSeedName(symbol),
                TokenType = TokenHelper.GetTokenType(symbol)
            };
        }

        return seedSymbolIndex;
    }
}