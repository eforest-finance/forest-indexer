using AElf.CSharp.Core.Extension;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.GraphQL;
using Forest.Indexer.Plugin.Processors;
using Forest.Indexer.Plugin.Tests.Helper;
using Forest.Contracts.SymbolRegistrar;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace Forest.Indexer.Plugin.Tests.Processors;

public class SpecialSeedLogEventProcessorTests: ForestIndexerPluginTestBase
{
    private readonly IObjectMapper _objectMapper;
    private readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;
    
    private const string chainId = "AELF";
    private const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
    private const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
    private const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
    private const long blockHeight = 100;
    
    public SpecialSeedLogEventProcessorTests()
    {
        _seedSymbolIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo>>();
        _objectMapper = GetRequiredService<IObjectMapper>();
    }
    
    [Fact]
    public async Task HandleSpecialSeedAddedLogEventProcessor_Test()
    {
        var specialSeedAddedLogEventProcessor = GetRequiredService<SpecialSeedAddedLogEventProcessor>();
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        //step1: create blockStateSet
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);
        //step2: create nftInfo userBalance logEventInfo
        SpecialSeedAdded specialSeedAdded = new SpecialSeedAdded
        {
            AddList = new SpecialSeedList()
            {
                Value =
                {
                    new SpecialSeed()
                    {
                        Symbol = "ABC",
                        IssueChain = chainId,
                        IssueChainContractAddress = "2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7",
                        SeedType = SeedType.Notable,
                        AuctionType = AuctionType.Dutch,
                        PriceAmount = 500,
                        PriceSymbol = "ELF"
                    },
                    new SpecialSeed()
                    {
                        Symbol = "XYZ",
                        IssueChain = chainId,
                        IssueChainContractAddress = "2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7",
                        SeedType = SeedType.Unique,
                        AuctionType = AuctionType.Dutch,
                        PriceAmount = 800,
                        PriceSymbol = "ELF"
                    },
                    new SpecialSeed()
                    {
                        Symbol = "YANGTZE",
                        IssueChain = chainId,
                        IssueChainContractAddress = "2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7",
                        SeedType = SeedType.Unique,
                        AuctionType = AuctionType.Dutch,
                        PriceAmount = 2000,
                        PriceSymbol = "ELF"
                    }
                }
            }
        };
        
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(specialSeedAdded.ToLogEvent());
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
            TransactionId = transactionId
        };
        
        //step3: handle event and write result to blockStateSet
        await specialSeedAddedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);

        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);

        //step5: check result
        foreach (var specialSeed in specialSeedAdded.AddList.Value)
        {
            var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, specialSeed.Symbol);
            var tsmSeedSymbolIndex =
                await _seedSymbolIndexRepository.GetAsync(seedSymbolId);
            tsmSeedSymbolIndex.ChainId.ShouldBe(chainId);
            tsmSeedSymbolIndex.Symbol.ShouldBe(specialSeed.Symbol);
            tsmSeedSymbolIndex.SymbolLength.ShouldBe(specialSeed.Symbol.Length);
            tsmSeedSymbolIndex.SeedType.ShouldBe(specialSeed.SeedType);
            tsmSeedSymbolIndex.AuctionType.ShouldBe(specialSeed.AuctionType);
            tsmSeedSymbolIndex.SeedName.ShouldBe(IdGenerateHelper.GetSeedName(specialSeed.Symbol));
            tsmSeedSymbolIndex.TokenPrice.Amount.ShouldBe(specialSeed.PriceAmount);
        }
        
    }

    [Fact]
    public async Task HandleSpecialSeedRemovedLogEventProcessor_Test()
    {
        await HandleSpecialSeedAddedLogEventProcessor_Test();
        
        var specialSeedRemovedLogEventProcessor = GetRequiredService<SpecialSeedRemovedLogEventProcessor>();
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        //step1: create blockStateSet
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);
        //step2: create nftInfo userBalance logEventInfo
        SpecialSeedRemoved specialSeedRemoved = new SpecialSeedRemoved
        {
            RemoveList = new SpecialSeedList()
            {
                Value =
                {
                    // new SpecialSeed()
                    // {
                    //     Symbol = "ABC",
                    //     IssueChain = chainId,
                    //     IssueChainContractAddress = "2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7",
                    //     SeedType = SeedType.Notable,
                    //     AuctionType = AuctionType.Dutch,
                    //     PriceAmount = 500,
                    //     PriceSymbol = "ELF"
                    // },
                    new SpecialSeed()
                    {
                        Symbol = "XYZ",
                        IssueChain = chainId,
                        IssueChainContractAddress = "2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7",
                        SeedType = SeedType.Unique,
                        AuctionType = AuctionType.Dutch,
                        PriceAmount = 800,
                        PriceSymbol = "ELF"
                    },
                    new SpecialSeed()
                    {
                        Symbol = "YANGTZE",
                        IssueChain = chainId,
                        IssueChainContractAddress = "2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7",
                        SeedType = SeedType.Unique,
                        AuctionType = AuctionType.Dutch,
                        PriceAmount = 2000,
                        PriceSymbol = "ELF"
                    }
                }
            }
        };
        
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(specialSeedRemoved.ToLogEvent());
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
            TransactionId = transactionId
        };
        
        //step3: handle event and write result to blockStateSet
        await specialSeedRemovedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        specialSeedRemovedLogEventProcessor.GetContractAddress(chainId);
        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        
        //step5: check result
        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, "XYZ");
        var tsmSeedSymbolIndex =
            await _seedSymbolIndexRepository.GetAsync(seedSymbolId);
        tsmSeedSymbolIndex.ShouldBeNull();
        seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, "ABC");
        tsmSeedSymbolIndex =
            await _seedSymbolIndexRepository.GetAsync(seedSymbolId);
        tsmSeedSymbolIndex.SeedName.ShouldBe(IdGenerateHelper.GetSeedName("ABC"));
    }
    
    [Fact]
    public async Task QuerySpecialSeeds_Test()
    {
        await HandleSpecialSeedAddedLogEventProcessor_Test();

        var result1 = await Query.SpecialSeeds(_seedSymbolIndexRepository, _objectMapper, new GetSpecialSeedsInput()
        {
            SkipCount = 0,
            MaxResultCount = 10,
            SymbolLengthMin = 4,
            SymbolLengthMax = 8
        });
        result1.TotalRecordCount.ShouldBe(1);
        result1.Data.Count.ShouldBe(1);
        result1.Data.First().Symbol.ShouldBe("YANGTZE");
        result1.Data.First().TokenPrice.Amount.ShouldBe(2000);

        var result2 = await Query.SpecialSeeds(_seedSymbolIndexRepository, _objectMapper, new GetSpecialSeedsInput()
        {
            SkipCount = 0,
            MaxResultCount = 10,
            SeedTypes = new List<SeedType>(){ SeedType.Unique }
        });
        result2.TotalRecordCount.ShouldBe(2);
        result2.Data.Count.ShouldBe(2);
    }
    
}