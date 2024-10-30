using AeFinder.Sdk.Processor;
using Forest.Indexer.Plugin.GraphQL;
using Forest.Indexer.Plugin.Processors;
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
        context.Services.AddSingleton<ILogEventProcessor, ActivityForCreateFTAndNFTProcessor>();
        context.Services.AddSingleton<ILogEventProcessor, ActivityForIssueFTProcessor>();
        context.Services.AddSingleton<ILogEventProcessor, ActivityForSymbolMarketBidPlacedProcessor>();
        context.Services.AddSingleton<ILogEventProcessor, ActivityForSymbolMarketBoughtProcessor>();
        context.Services.AddSingleton<ILogEventProcessor, AuctionCreatedLogEventProcessor>();
        context.Services.AddSingleton<ILogEventProcessor, AuctionTimeUpdatedLogEventProcessor>();
        context.Services.AddSingleton<ILogEventProcessor, BidPlacedLogEventProcessor>();
        context.Services.AddSingleton<ILogEventProcessor, BoughtProcessor>();
        context.Services.AddSingleton<ILogEventProcessor, ClaimedProcessor>();
        context.Services.AddSingleton<ILogEventProcessor,ManagerTokenCreatedLogEventProcessor>();
        context.Services.AddSingleton<ILogEventProcessor,SeedCreatedProcessor>();
        context.Services.AddSingleton<ILogEventProcessor,SeedsPriceChangedProcessor>();
        context.Services.AddSingleton<ILogEventProcessor,SpecialSeedAddedLogEventProcessor>();
        context.Services.AddSingleton<ILogEventProcessor,SpecialSeedRemovedLogEventProcessor>();

        context.Services.AddSingleton<ILogEventProcessor,ContractDeployedProcessor>();
        context.Services.AddSingleton<ILogEventProcessor,OfferAddedLogEventProcessor>();
        context.Services.AddSingleton<ILogEventProcessor,OfferChangedLogEventProcessor>();
        context.Services.AddSingleton<ILogEventProcessor,OfferRemovedLogEventProcessor>();
        context.Services.AddSingleton<ILogEventProcessor,OfferCanceledLogEventProcessor>();
        context.Services.AddSingleton<ILogEventProcessor,UniqueSeedsPriceChangedLogEventProcessor>();
        context.Services.AddSingleton<ILogEventProcessor,OfferCanceledByExpireTimeLogEventProcessor>();
        context.Services.AddSingleton<ILogEventProcessor,ListedNFTAddedLogEventProcessor>();
        context.Services.AddSingleton<ILogEventProcessor,ListedNFTChangedLogEventProcessor>();
        context.Services.AddSingleton<ILogEventProcessor,ListedNFTRemovedLogEventProcessor>();
        context.Services.AddSingleton<ILogEventProcessor,TokenBurnedLogEventProcessor>();
        context.Services.AddSingleton<ILogEventProcessor,TokenCreatedLogEventProcessor>();
        context.Services.AddSingleton<ILogEventProcessor,TokenIssueLogEventProcessor>();
        context.Services.AddSingleton<ILogEventProcessor,TokenTransferProcessor>();
        context.Services.AddSingleton<ILogEventProcessor,SoldLogEventProcessor>();
        context.Services.AddSingleton<ILogEventProcessor,CrossChainReceivedProcessor>();
        context.Services.AddSingleton<ILogEventProcessor,TransactionFeeChargedLogEventProcessor>();

        context.Services.AddSingleton<ILogEventProcessor,ProxyAccountCreatedLogEventProcessor>();
        context.Services
            .AddSingleton<ILogEventProcessor,ProxyAccountManagementAddressAddedLogEventProcessor>();
        context.Services
            .AddSingleton<ILogEventProcessor,
                ProxyAccountManagementAddressRemovedLogEventProcessor>();
        context.Services
            .AddSingleton<ILogEventProcessor,ProxyAccountManagementAddressResetLogEventProcessor>();
    }
}