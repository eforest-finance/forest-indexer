using AElfIndexer.Client.GraphQL;

namespace Forest.Indexer.Plugin.GraphQL;

public class ForestIndexerPluginSchema : AElfIndexerClientSchema<Query>
{
    public ForestIndexerPluginSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}