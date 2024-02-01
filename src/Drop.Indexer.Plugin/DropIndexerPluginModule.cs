using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Drop.Indexer.Plugin.GraphQL;
using Drop.Indexer.Plugin.Handlers;
using Drop.Indexer.Plugin.Processors;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Drop.Indexer.Plugin;

[DependsOn(typeof(AElfIndexerClientModule), typeof(AbpAutoMapperModule))]
public class DropIndexerPluginModule : AElfIndexerClientPluginBaseModule<DropIndexerPluginModule,
    DropIndexerPluginSchema, Query>
{
    protected override void ConfigureServices(IServiceCollection serviceCollection)
    {
        var configuration = serviceCollection.GetConfiguration();
        
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, DropCreatedLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, DropChangedLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, DropStateChangedLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, DropClaimedLogEventProcessor>();
        serviceCollection.AddSingleton<IBlockChainDataHandler, DropTransactionHandler>();
       
    }
    
    protected override string ClientId => "AElfIndexer_Drop";
    protected override string Version => "183c915e3f264d019668ba3874d081de";
}