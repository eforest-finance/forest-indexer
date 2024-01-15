using Xunit;

namespace Forest.Indexer.Orleans.TestBase;

[CollectionDefinition(Name)]
public class ClusterCollection:ICollectionFixture<ClusterFixture>
{
    public const string Name = "ClusterCollection";
}