using AElf;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.GraphQL;
using Forest.Indexer.Plugin.Processors;
using Forest.SymbolRegistrar;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace Forest.Indexer.Plugin.Tests.GraphQL;

public class QueryTestBase : ForestIndexerPluginTestBase
{
    private const string Symbol = "PWD-1";
    private const string To = "Lmemfcp2nB8kAvQDLxsLtQuHWgpH5gUWVmmcEkpJ2kRY9Jv25";

    private async Task CreateSeed(string seedSymbol)
    {
        var logEventContext = MockLogEventContext(chainId: ForestIndexerConstants.MainChain);
        var blockStateSetKey = await MockBlockState(logEventContext);
        var seedCreated = new SeedCreated
        {
            Symbol = seedSymbol,
            OwnedSymbol = Symbol,
            ExpireTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.Now.AddDays(10)),
            SeedType = SeedType.Unique,
            To = Address.FromBase58(To),
        };
        
        var logEventInfo = MockLogEventInfo(seedCreated.ToLogEvent());
        var seedCreatedProcessor = GetRequiredService<SeedCreatedProcessor>();
        await seedCreatedProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);

    }

    private async Task CreateSeedToken()
    {
        var logEventContext = MockLogEventContext();
        var blockStateSetKey = await MockBlockState(logEventContext);
        var tokenCreated = new TokenCreated
        {
            Symbol = "SEED-1",
            TokenName = "SEED-1 token",
            TotalSupply = 1,
            Decimals = 1,
            Issuer = Address.FromBase58(To),
            IsBurnable = true,
            IssueChainId = ChainHelper.ConvertBase58ToChainId("tDVW"),
            ExternalInfo = new ExternalInfo
            {
                Value =
                {
                    ["__seed_owned_symbol"] = Symbol,
                    ["__seed_exp_time"] = "1725713310"
                }
            }
        };
        
        var logEventInfo = MockLogEventInfo(tokenCreated.ToLogEvent());
        var seedCreatedProcessor = GetRequiredService<TokenCreatedLogEventProcessor>();
        await seedCreatedProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
    }

    private async Task SpecialSeedAdded()
    {
        var logEventContext = MockLogEventContext(chainId: ForestIndexerConstants.MainChain);
        var blockStateSetKey = await MockBlockState(logEventContext);
        var specialSeedAdded = new SpecialSeedAdded()
        {
            AddList = new SpecialSeedList
            {
                Value =
                {
                    new SpecialSeed
                    {
                        SeedType = SeedType.Unique,
                        Symbol = Symbol,
                        PriceSymbol = ForestIndexerConstants.PriceSimpleElf,
                        PriceAmount = 1_0000_0000,
                        AuctionType = AuctionType.English,
                        IssueChain = ForestIndexerConstants.MainChain,
                        IssueChainContractAddress = To,
                    }
                }
            }
        };
        
        var logEventInfo = MockLogEventInfo(specialSeedAdded.ToLogEvent());
        var seedCreatedProcessor = GetRequiredService<SpecialSeedAddedLogEventProcessor>();
        await seedCreatedProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
    }

    [Fact]
    public async Task QuerySeedInfo()
    {
        await CreateSeed("SEED-0");
        await SpecialSeedAdded();
        await CreateSeed("SEED-1");
        await CreateSeedToken();
        
        var data = await Query.NFTInfo(
            GetRequiredService<IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo>>(),
            GetRequiredService<IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo>>(),
            GetRequiredService<IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo>>(),
            GetRequiredService<IObjectMapper>(),
            new GetNFTInfoDto()
            {
                Id = "tDVW-SEED-1"
            }
        );
        data.ShouldNotBeNull();

    }
    
    [Fact]
    public async Task CalcCollectionFloorPriceAsyncTest()
    {
        decimal one = await CalcCollectionFloorPriceAsync(null, null);
        
        Assert.Equal(-1, one);

        decimal two= await CalcCollectionFloorPriceAsync(2.33m, null);
        
        Assert.Equal(2.33m, two);

        decimal three= await CalcCollectionFloorPriceAsync(null, 1.99m);
        
        Assert.Equal(1.99m, three);

        decimal four= await CalcCollectionFloorPriceAsync(2.33m, 1.9m);
        
        Assert.Equal(1.9m, four);
    }
    
    
    public async Task<decimal> CalcCollectionFloorPriceAsync(decimal? auctionMinPrice, decimal? listingMinPrice)
    {
        if (auctionMinPrice == null && listingMinPrice == null)
        {
            return -1m;
        } 
        return Math.Min(auctionMinPrice ?? decimal.MaxValue, listingMinPrice ?? decimal.MaxValue);
    }
    
    
}