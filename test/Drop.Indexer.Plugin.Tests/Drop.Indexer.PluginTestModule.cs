using AeFinder.App.TestBase;
using Drop.Indexer.Plugin.Processors;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Drop.Indexer.Plugin;

[DependsOn(
    typeof(AeFinderAppTestBaseModule),
    typeof(DropIndexerPluginModule))]
public class DropIndexerPluginTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AeFinderAppEntityOptions>(options => { options.AddTypes<DropIndexerPluginModule>(); });
        
        // Add your Processors.
        // context.Services.AddSingleton<MyLogEventProcessor>();
        context.Services.AddSingleton<DropCreatedLogEventProcessor>();
        context.Services.AddSingleton<DropChangedLogEventProcessor>();
        context.Services.AddSingleton<DropStateChangedLogEventProcessor>();
        context.Services.AddSingleton<DropClaimedLogEventProcessor>();
    }
}