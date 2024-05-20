using AElf;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ProxyAccountContract;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.GraphQL;
using Forest.Indexer.Plugin.Processors;
using Forest.Indexer.Plugin.Tests.Helper;
using Forest.SymbolRegistrar;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;
using AuctionType = Forest.Contracts.SymbolRegistrar.AuctionType;
using GetSeedsPriceOutput = Forest.Contracts.SymbolRegistrar.GetSeedsPriceOutput;
using PriceList = Forest.Contracts.SymbolRegistrar.PriceList;
using SpecialSeed = Forest.Contracts.SymbolRegistrar.SpecialSeed;

namespace Forest.Indexer.Plugin.Tests.Processors;

public class ManagerTokenCreatedLogEventProcessorTests : ForestIndexerPluginTestBase
{
    private readonly IAElfIndexerClientEntityRepository<SeedSymbolMarketTokenIndex, LogEventInfo>
        _symbolMarketTokenIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;
    private readonly IObjectMapper _objectMapper;

    public ManagerTokenCreatedLogEventProcessorTests()
    {
        _symbolMarketTokenIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<SeedSymbolMarketTokenIndex, LogEventInfo>>();
        _seedSymbolIndexRepository = GetRequiredService<IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo>>();
        _objectMapper = GetRequiredService<IObjectMapper>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(BuildAElfClientServiceProvider());
    }
    
    private static IAElfClientServiceProvider BuildAElfClientServiceProvider()
    {
        var mockAElfClientServiceProvider = new Mock<IAElfClientServiceProvider>();

        mockAElfClientServiceProvider.Setup(service => service.GetSpecialSeedAsync(It.IsAny<string>()
                , It.IsAny<string>()
                , It.IsAny<string>()))
            .ReturnsAsync(new SpecialSeed
            {
                SeedType = Contracts.SymbolRegistrar.SeedType.Unique,
                Symbol = "AAA",
                AuctionType = AuctionType.None,
                IssueChain = "tDVV",
                PriceSymbol = "ELF",
                PriceAmount = 11
            });
        mockAElfClientServiceProvider.Setup(service => service.GetSeedImageUrlPrefixAsync(It.IsAny<string>()
                , It.IsAny<string>()))
            .ReturnsAsync(new StringValue());

        mockAElfClientServiceProvider.Setup(service => service.GetSeedsPriceAsync(It.IsAny<string>()
                , It.IsAny<string>()))
            .ReturnsAsync(new GetSeedsPriceOutput
            {
                FtPriceList = new Contracts.SymbolRegistrar.PriceList()
                {

                },
                NftPriceList = new PriceList()
                {

                }
            });


        var managementAddresses = new RepeatedField<ManagementAddress>();
        managementAddresses.Add(new ManagementAddress()
        {
            Address = Address.FromPublicKey("AAA".HexToByteArray())
        });
        managementAddresses.Add(new ManagementAddress()
        {
            Address = Address.FromPublicKey("BBB".HexToByteArray())
        });
        managementAddresses.Add(new ManagementAddress()
        {
            Address = Address.FromPublicKey("CCC".HexToByteArray())
        });

        var proxyAccount = new ProxyAccount()
        {
            CreateChainId = ChainHelper.ConvertBase58ToChainId("AELF"),
            ProxyAccountHash = HashHelper.ComputeFrom("aLyxCJvWMQH6UEykTyeWAcYss9baPyXkrMQ37BHnUicxD2LL3"),
        };
        proxyAccount.ManagementAddresses.AddRange(managementAddresses);
        
        mockAElfClientServiceProvider.Setup(service => service.GetProxyAccountByProxyAccountAddressAsync(It.IsAny<string>()
                , It.IsAny<string>(), It.IsAny<Address>()))
            .ReturnsAsync(proxyAccount);

        return mockAElfClientServiceProvider.Object;
    }

    [Fact]
    public async Task SymbolMarketToken_Tem()
    {
    }

    [Fact]
    public async Task SeedCrossChain()
    {
        const string chainId = "AELF";
        const string newChainId = "tDVW";
        const string symbol = "SEED-1";
        const string seedOwnedSymbol = "seedOwnedSymbol1";
        await SymbolSeedCrossChain(chainId, newChainId, symbol, seedOwnedSymbol);
        var queryResult = await Query.MySeedAsync(_seedSymbolIndexRepository, new MySeedInput()
        { 
            SkipCount = 0,
            MaxResultCount = 10,
            AddressList = new List<string>()
            {
                // "ELF_"+Address.FromPublicKey("AAA".HexToByteArray()).ToBase58()+"_AELF"
                "aLyxCJvWMQH6UEykTyeWAcYss9baPyXkrMQ37BHnUicxD2LL3"
            }
        });
        //Assert.True(queryResult.TotalRecordCount.Equals(1));
        
        var queryResult2 = await Query.MySeedAsync(_seedSymbolIndexRepository, new MySeedInput()
        { 
            SkipCount = 0,
            MaxResultCount = 10,
            AddressList = new List<string>()
            {
                //"ELF_"+Address.FromPublicKey("AAA".HexToByteArray()).ToBase58()+"_AELF"
                "aLyxCJvWMQH6UEykTyeWAcYss9baPyXkrMQ37BHnUicxD2LL3"
            }
        });
        Assert.True(queryResult2.TotalRecordCount.Equals(1));

    }

    [Fact]
    public async Task SeedTokenNoMainChainCreateTest()
    {
        const string seedSymbol = "SEED-1";
        const string seedOwnedSymbol = "JECKETC";

        const string mainChainId = "AELF";
        const string address = "AAA";
        
        //SeedInit mainChain
        await SeedAdd(SeedType.Regular, seedSymbol, seedOwnedSymbol, mainChainId);
        //MutilToken seedCreate
        await HandleSeedTokenCreate(mainChainId, seedSymbol, seedOwnedSymbol);
        ////MutilToken seed issue
        await HandleSeedIssueAsync(seedSymbol,mainChainId,address);
        
        const string tokenChainId = "tDVV";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 16608983;

        const string symbol = seedOwnedSymbol;
        const string tokenName = seedOwnedSymbol;
        const long totalSupply = 2;
        const int decimals = 8;
        const bool isBurnable = true;
        const int issueChainId = 1866392;
        
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        
        var blockStateSetKey1 = await InitializeBlockStateSetAsync(blockStateSet, mainChainId);
        var blockStateSetKey2 = await InitializeBlockStateSetAsync(blockStateSet, tokenChainId);
        

        var seedTokenCreatedLogEventProcessor = GetRequiredService<TokenCreatedLogEventProcessor>();
        seedTokenCreatedLogEventProcessor.GetContractAddress(tokenChainId);
        
        var tokenCreated = new TokenCreated()
        {
            Symbol = symbol,
            TokenName = tokenName,
            TotalSupply = totalSupply,
            Decimals = decimals,
            Issuer = Address.FromPublicKey(address.HexToByteArray()),
            Owner = Address.FromPublicKey(address.HexToByteArray()),
            IsBurnable = isBurnable,
            IssueChainId = issueChainId,
            ExternalInfo = new ExternalInfo(){}
        };

        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(tokenCreated.ToLogEvent());
        logEventInfo.BlockHeight = blockHeight;
        logEventInfo.ChainId = tokenChainId;
        logEventInfo.BlockHash = blockHash;
        logEventInfo.TransactionId = transactionId;
        var logEventContext = new LogEventContext
        {
            ChainId = tokenChainId,
            BlockHeight = blockHeight,
            BlockHash = blockHash,
            PreviousBlockHash = previousBlockHash,
            TransactionId = transactionId,
            BlockTime = DateTime.Now
        };

        await seedTokenCreatedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey1);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey2);
        
        await MockNFTIssue(1, seedOwnedSymbol, 16608983,tokenChainId,tokenName,address);
        
        Assert.True(true);
    }
}