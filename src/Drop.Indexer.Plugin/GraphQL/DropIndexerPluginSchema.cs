using AeFinder.Sdk;

namespace Drop.Indexer.Plugin.GraphQL;

public class DropIndexerPluginSchema : AppSchema<Query>
{
    public DropIndexerPluginSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}