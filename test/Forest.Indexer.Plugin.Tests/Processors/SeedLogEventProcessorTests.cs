using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.GraphQL;
using Forest.Indexer.Plugin.Processors;
using Forest.Indexer.Plugin.Tests.Helper;
using Forest.SymbolRegistrar;
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace Forest.Indexer.Plugin.Tests.Processors;

public class SeedLogEventProcessorTests : ForestIndexerPluginTestBase
{
    private readonly IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;
    private readonly IObjectMapper _objectMapper;
    
    public SeedLogEventProcessorTests()
    {
        _seedSymbolIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo>>();
        _objectMapper = GetRequiredService<IObjectMapper>();
    }
    
    [Fact]
    public async Task QuerySeedSymbolsTest()
    {
        await HandleSeedIssueAsync_Test("SEED-1","AB-0");
        await HandleSeedIssueAsync_Test("SEED-4","ABAB-0");
        await HandleSeedIssueAsync_Test("SEED-7","ABABAB-0");
        await HandleSeedIssueAsync_Test("SEED-2","BA-0");
        await HandleSeedIssueAsync_Test("SEED-5","BABA-0");
        await HandleSeedIssueAsync_Test("SEED-8","BABABA-0");
        await HandleSeedIssueAsync_Test("SEED-3","CB-0");
        await HandleSeedIssueAsync_Test("SEED-6","CBCB-0");
        await HandleSeedIssueAsync_Test("SEED-9","CBCBCB1-0");
        await HandleSeedIssueAsync_Test("SEED-10","CBCBCB2-0");
        await HandleSeedIssueAsync_Test("SEED-11","CBCBCB3");
        await HandleSeedIssueAsync_Test("SEED-12","CBCBCB4");
        var result = await Query.SeedSymbols(_seedSymbolIndexRepository, _objectMapper, new GetSeedSymbolsDto()
        {
            SkipCount = 0,
            MaxResultCount = 12,
            Address = Address.FromPublicKey("AAA".HexToByteArray()).ToBase58(),
            SeedOwnedSymbol = ""
        });
        result.TotalRecordCount.ShouldBe(10L);
        result.Data[0].SeedOwnedSymbol.ShouldBe("AB-0");
        result.Data[1].SeedOwnedSymbol.ShouldBe("BA-0");
        result.Data[2].SeedOwnedSymbol.ShouldBe("CB-0");
        result.Data[3].SeedOwnedSymbol.ShouldBe("ABAB-0");
        result.Data[4].SeedOwnedSymbol.ShouldBe("BABA-0");
        result.Data[5].SeedOwnedSymbol.ShouldBe("CBCB-0");
        result.Data[6].SeedOwnedSymbol.ShouldBe("ABABAB-0");
        result.Data[7].SeedOwnedSymbol.ShouldBe("BABABA-0");
        result.Data[8].SeedOwnedSymbol.ShouldBe("CBCBCB1-0");
        result.Data[9].SeedOwnedSymbol.ShouldBe("CBCBCB2-0");


    }
    
    
    private async Task HandleSeedIssueAsync_Test(string symbol,string seedOwnedSymbol )
    {
        await SeedAdd(SeedType.Regular, symbol, seedOwnedSymbol, "AELF");

        await HandleSeedAddedAsync_Success( symbol, seedOwnedSymbol);
        const string chainId = "AELF";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;
        
        const string tokenName = "READ Token";
        const int decimals = 8;
        const bool isBurnable = true;
        const int issueChainId = 9992731;

        var seedSymbolIssueLogEventProcessor = GetRequiredService<TokenIssueLogEventProcessor>();
        seedSymbolIssueLogEventProcessor.GetContractAddress(chainId);
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);

        var issued = new Issued()
        {
            Symbol = symbol,
            Amount = 1,
            To = Address.FromPublicKey("AAA".HexToByteArray()),
            Memo = "DESC"
        };

        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(issued.ToLogEvent());
        logEventInfo.BlockHeight = blockHeight;
        logEventInfo.ChainId = chainId;
        logEventInfo.BlockHash = blockHash;
        logEventInfo.TransactionId = transactionId;
        var logEventContext = new LogEventContext
        {
            ChainId = chainId,
            BlockHeight = blockHeight,
            BlockHash = blockHash,
            PreviousBlockHash = previousBlockHash,
            TransactionId = transactionId,
            BlockTime = DateTime.Now
        };

        await seedSymbolIssueLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);

        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(0);

        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, symbol);
        var seedSymbolInfoIndexData = await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolId, chainId);
        seedSymbolInfoIndexData.IssuerTo.ShouldBe(Address.FromPublicKey("AAA".HexToByteArray()).ToBase58());
        seedSymbolInfoIndexData.SeedOwnedSymbol.ShouldBe(seedOwnedSymbol);
    }
    
    private async Task HandleSeedAddedAsync_Success(string symbol,string seedOwnedSymbol)
    {
        const string chainId = "AELF";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;
        
        const string tokenName = "READ Token";
        const long totalSupply = 0;
        const int decimals = 8;
        const bool isBurnable = true;
        const int issueChainId = 9992731;

        var seedSymbolCreatedLogEventProcessor = GetRequiredService<TokenCreatedLogEventProcessor>();
        seedSymbolCreatedLogEventProcessor.GetContractAddress(chainId);
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);

        var tokenCreated = new TokenCreated()
        {
            Symbol = symbol,
            TokenName = tokenName,
            TotalSupply = totalSupply,
            Decimals = decimals,
            Issuer = Address.FromPublicKey("AAA".HexToByteArray()),
            IsBurnable = isBurnable,
            IssueChainId = issueChainId,
            ExternalInfo = new ExternalInfo()
            {
                Value =
                {
                    { "__seed_owned_symbol", seedOwnedSymbol },
                    { "__seed_exp_time", new DateTimeOffset(DateTime.UtcNow.AddHours(1)).ToUnixTimeSeconds().ToString() }
                }
            }
        };
        
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(tokenCreated.ToLogEvent());
        logEventInfo.BlockHeight = blockHeight;
        logEventInfo.ChainId = chainId;
        logEventInfo.BlockHash = blockHash;
        logEventInfo.TransactionId = transactionId;
        var logEventContext = new LogEventContext
        {
            ChainId = chainId,
            BlockHeight = blockHeight,
            BlockHash = blockHash,
            PreviousBlockHash = previousBlockHash,
            TransactionId = transactionId,
            BlockTime = DateTime.Now
        };

        await seedSymbolCreatedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);

        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(0);

        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, symbol);
        var seedSymbolIndex = await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolId,chainId);
        seedSymbolIndex.Id.ShouldBe(seedSymbolId);
        seedSymbolIndex.Symbol.ShouldBe(symbol);
    }

}