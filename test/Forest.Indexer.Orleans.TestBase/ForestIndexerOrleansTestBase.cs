using Forest.Indexer.TestBase;
using Orleans.TestingHost;
using Volo.Abp.Modularity;

namespace Forest.Indexer.Orleans.TestBase;

public abstract class ForestIndexerOrleansTestBase<TStartupModule>:ForestIndexerTestBase<TStartupModule> 
    where TStartupModule : IAbpModule
{
    protected readonly TestCluster Cluster;

    public ForestIndexerOrleansTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
}