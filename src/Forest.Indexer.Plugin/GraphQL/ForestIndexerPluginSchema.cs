using AeFinder.Sdk;

namespace Forest.Indexer.Plugin.GraphQL;

public class ForestIndexerPluginSchema : AppSchema<Query>
{
    public ForestIndexerPluginSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}