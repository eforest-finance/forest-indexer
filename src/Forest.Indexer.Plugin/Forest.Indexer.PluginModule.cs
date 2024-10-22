using Forest.Indexer.Plugin.GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Forest.Indexer.Plugin;

public class ForestIndexerPluginModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<ForestIndexerPluginModule>(); });

        context.Services.AddSingleton<ISchema, ForestIndexerPluginSchema>();
        
        // Add your LogEventProcessor implementation.
        //context.Services.AddSingleton<ILogEventProcessor, MyLogEventProcessor>();
    }
}