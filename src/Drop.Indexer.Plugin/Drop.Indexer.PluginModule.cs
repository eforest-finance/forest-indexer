using AeFinder.Sdk.Processor;
using Drop.Indexer.Plugin.GraphQL;
using Drop.Indexer.Plugin.Processors;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Drop.Indexer.Plugin;

public class DropIndexerPluginModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<DropIndexerPluginModule>(); });
        context.Services.AddSingleton<ISchema, DropIndexerPluginSchema>();

        context.Services.AddTransient<ILogEventProcessor, DropCreatedLogEventProcessor>();
        context.Services.AddTransient<ILogEventProcessor, DropChangedLogEventProcessor>();
        context.Services.AddTransient<ILogEventProcessor, DropStateChangedLogEventProcessor>();
        context.Services.AddTransient<ILogEventProcessor, DropClaimedLogEventProcessor>();
        //context.Services.AddTransient<ILogEventProcessor, DropTransactionHandler>();
    }
}