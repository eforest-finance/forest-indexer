using AElfIndexer.Client.GraphQL;

namespace Drop.Indexer.Plugin.GraphQL;

public class DropIndexerPluginSchema : AElfIndexerClientSchema<Query>
{
    public DropIndexerPluginSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}