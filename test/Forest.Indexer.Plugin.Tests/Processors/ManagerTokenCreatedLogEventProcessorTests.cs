using AElf.Contracts.MultiToken;
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
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;

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

    [Fact]
    public async Task SymbolMarketToken_Tem()
    {
    }

    
    [Fact]
    public async Task SymbolMarketToken_Add()
    {
        const string chainId = "AELF";
        const string toChainId = "tDVW"; 
        const string symbol = "SEED-1";
        const string seedOwnedSymbol = "seedOwnedSymbol1";
        await SymbolMarketTokenAdd(chainId, symbol, seedOwnedSymbol);
        
        var result0 = await Query.SymbolMarketTokenExist(_symbolMarketTokenIndexRepository, _objectMapper,
            new GetSymbolMarketTokenExistInput()
            {
                IssueChainId = "AELF",
                TokenSymbol = "seedOwnedSymbol1"
            });
        result0.ShouldNotBeNull();
        result0.Symbol.ShouldBe("seedOwnedSymbol1");

        //issue
        var result1 = await Query.SymbolMarketTokens(_symbolMarketTokenIndexRepository, _objectMapper, new GetSymbolMarketTokensInput()
        {
            SkipCount = 0,
            MaxResultCount = 10,
            Address = new List<string>()
            {
                "2YcGvyn7QPmhvrZ7aaymmb2MDYWhmAks356nV3kUwL8FkGSYeZ"
                //Address.FromPublicKey("FFF".HexToByteArray()).ToBase58()
            }
        });
        Assert.True(result1.TotalRecordCount==1);
        Assert.True(result1.Data[0].TotalSupply==1000);
        Assert.True(result1.Data[0].Decimals==2);
        
        //issue
        var result11 = await Query.SymbolMarketTokens(_symbolMarketTokenIndexRepository, _objectMapper, new GetSymbolMarketTokensInput()
        {
            SkipCount = 0,
            MaxResultCount = 10,
            Address = new List<string>()
            {
                Address.FromPublicKey("FFF".HexToByteArray()).ToBase58()
            }
        });
        Assert.True(result11.TotalRecordCount==0);
        
        //owner
        var result2 = await Query.SymbolMarketTokens(_symbolMarketTokenIndexRepository, _objectMapper, new GetSymbolMarketTokensInput()
        {
            SkipCount = 0,
            MaxResultCount = 10,
            Address = new List<string>()
            {
                "aLyxCJvWMQH6UEykTyeWAcYss9baPyXkrMQ37BHnUicxD2LL3"
            }
        });
        Assert.True(result2.TotalRecordCount==1);
        
        await MockNFTIssue(1, "seedOwnedSymbol1", 100,"AELF","seedOwnedSymbol1","CCC");

        //issueto
        var result3 = await Query.SymbolMarketTokens(_symbolMarketTokenIndexRepository, _objectMapper, new GetSymbolMarketTokensInput()
        {
            SkipCount = 0,
            MaxResultCount = 10,
            Address = new List<string>()
            {
                Address.FromPublicKey("CCC".HexToByteArray()).ToBase58()
            }
        });
        Assert.True(result3.TotalRecordCount==1);

        //crosschain-seed burned
        await SeedBurnedAsync_Test(chainId,symbol);
        
        //token create
        // Create NFT collection
        const string nftSymbol = "SYB-1";
        const string tokenName = "SYB Token";
        const bool isBurnable = true;
        const long totalSupply = 1;
        const int decimals = 8;
        const int issueChainId = 1931928;
        var logEventContext = MockLogEventContext(100,toChainId);
        var blockStateSetKey = await MockBlockState(logEventContext);

        var tokenCreated = new TokenCreated()
        {
            Symbol = seedOwnedSymbol,
            TokenName = tokenName,
            TotalSupply = totalSupply,
            Decimals = decimals,
            Issuer = Address.FromPublicKey("AAA".HexToByteArray()),
            Owner = Address.FromPublicKey("AAA".HexToByteArray()),
            IsBurnable = isBurnable,
            IssueChainId = issueChainId,
            ExternalInfo = new ExternalInfo()
        };
        var logEventInfo = MockLogEventInfo(tokenCreated.ToLogEvent());
        var tokenAddedLogEventProcessor = GetRequiredService<TokenCreatedLogEventProcessor>();
        await tokenAddedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);

        
        //crosschain-CrossChainReceivedProcessor
        await MockCrossChain(1, symbol, 100, "AELF", "tDVV", "READ Token", "AAA", "BBB");
        
        Assert.True(result3.TotalRecordCount==1);
    }

    [Fact]
    public async Task SeedCrossChain()
    {
        const string chainId = "AELF";
        const string symbol = "SEED-1";
        const string seedOwnedSymbol = "seedOwnedSymbol1";
        await SymbolSeedCrossChain(chainId, symbol, seedOwnedSymbol);
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
                "ELF_aLyxCJvWMQH6UEykTyeWAcYss9baPyXkrMQ37BHnUicxD2LL3_tDVV"
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