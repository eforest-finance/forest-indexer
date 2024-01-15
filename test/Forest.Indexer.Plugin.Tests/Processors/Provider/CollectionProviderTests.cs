using Forest.Indexer.Plugin.Processors;
using Forest.Indexer.Plugin.Processors.Provider;
using Shouldly;
using Xunit;

namespace Forest.Indexer.Plugin.Tests.Processors.Provider;

public class CollectionProviderTests : ForestIndexerPluginTestBase
{
    private readonly ICollectionProvider _collectionProvider;
    
    public CollectionProviderTests()
    {
        _collectionProvider = GetRequiredService<CollectionProvider>();
    }

    [Fact]
    public async Task TestCalcCollectionFloorPriceAsync()
    {
        string chainId = "tDVW";
        string symbol = "SEED-0";
        decimal oriFloorPrice = 0;
        var result = await _collectionProvider.CalcCollectionFloorPriceAsync(chainId, symbol, oriFloorPrice);
        result.ShouldBe(-1);
    }
}