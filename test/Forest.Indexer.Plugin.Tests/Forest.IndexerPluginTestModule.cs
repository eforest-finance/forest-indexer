using AeFinder.App.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Forest.Indexer.Plugin;

[DependsOn(
    typeof(AeFinderAppTestBaseModule),
    typeof(ForestIndexerPluginModule))]
public class ForestIndexerPluginTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AeFinderAppEntityOptions>(options => { options.AddTypes<ForestIndexerPluginModule>(); });
        
        // Add your Processors.
        // context.Services.AddSingleton<MyLogEventProcessor>();
    }
}