using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.GraphQL;
using Forest.Indexer.Plugin.Handlers;
using Forest.Indexer.Plugin.Processors;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Forest.Indexer.Plugin;

[DependsOn(typeof(AElfIndexerClientModule), typeof(AbpAutoMapperModule))]
public class ForestIndexerPluginModule : AElfIndexerClientPluginBaseModule<ForestIndexerPluginModule,
    ForestIndexerPluginSchema, Query>
{
    protected override void ConfigureServices(IServiceCollection serviceCollection)
    {
        var configuration = serviceCollection.GetConfiguration();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, ContractDeployedProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, OfferAddedLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, OfferChangedLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, OfferRemovedLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, OfferCanceledLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, UniqueSeedsPriceChangedLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, OfferCanceledByExpireTimeLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, WhitelistAddressInfoAddedProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, WhitelistAddressInfoRemovedProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, WhitelistCreatedLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, WhitelistDisabledLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, WhitelistReenableLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, WhitelistResetLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, TagInfoAddedLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, TagInfoRemovedLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, ListedNFTAddedLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, ListedNFTChangedLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, ListedNFTRemovedLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, TokenBurnedLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, TokenCreatedLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, TokenIssueLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, TokenTransferProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, SoldLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, CrossChainReceivedProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, TransactionFeeChargedLogEventProcessor>();

        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, ProxyAccountCreatedLogEventProcessor>();
        serviceCollection
            .AddSingleton<IAElfLogEventProcessor<LogEventInfo>, ProxyAccountManagementAddressAddedLogEventProcessor>();
        serviceCollection
            .AddSingleton<IAElfLogEventProcessor<LogEventInfo>,
                ProxyAccountManagementAddressRemovedLogEventProcessor>();
        serviceCollection
            .AddSingleton<IAElfLogEventProcessor<LogEventInfo>, ProxyAccountManagementAddressResetLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, UpdateExtraInfoProcessor>();
        Configure<ContractInfoOptions>(configuration.GetSection("ContractInfo"));
        Configure<GeneralityOptions>(configuration.GetSection("GeneralityInfo"));

        Configure<InitialInfoOptions>(configuration.GetSection("InitialInfo"));
        Configure<CleanDataOptions>(configuration.GetSection("CleanData"));
        Configure<NeedRecordBalanceOptions>(configuration.GetSection("NeedRecordBalance"));


        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, AuctionCreatedLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, BidPlacedLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, SeedCreatedProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, SeedsPriceChangedProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, BoughtProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, SpecialSeedAddedLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, SpecialSeedRemovedLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, AuctionTimeUpdatedLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, ClaimedProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<TransactionInfo>, ActivityForCreateFTAndNFTProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<TransactionInfo>, ActivityForIssueFTProcessor>();
        serviceCollection
            .AddSingleton<IAElfLogEventProcessor<TransactionInfo>, ActivityForSymbolMarketBidPlacedProcessor>();
        serviceCollection
            .AddSingleton<IAElfLogEventProcessor<TransactionInfo>, ActivityForSymbolMarketBoughtProcessor>();

        serviceCollection.AddSingleton<IAElfLogEventProcessor<LogEventInfo>, ManagerTokenCreatedLogEventProcessor>();
        serviceCollection.AddSingleton<IBlockChainDataHandler, ForestTransactionHandler>();
    }

    
    protected override string ClientId => "AElfIndexer_Forest";
    protected override string Version => "c4d50ae72b77453e9d0aae9ddc508632";
}