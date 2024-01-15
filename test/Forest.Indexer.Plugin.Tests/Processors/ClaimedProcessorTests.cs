using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Contracts.Auction;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Processors;
using Forest.Indexer.Plugin.Tests.Helper;
using Forest.SymbolRegistrar;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace Forest.Indexer.Plugin.Tests.Processors;

public class ClaimedProcessorTests : ForestIndexerPluginTestBase
{
    private readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo>
        _symbolAuctionInfoIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> _seedSymbolRepository;


    private readonly IObjectMapper _objectMapper;
    private const string chainId = "tDVW";
    private const string Symbol = "PWD-1";
    private const string from = "2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7";
    private const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
    private const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
    private const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
    private const string to = "Lmemfcp2nB8kAvQDLxsLtQuHWgpH5gUWVmmcEkpJ2kRY9Jv25";
    private const long blockHeight = 100;
    private const string AuctionId = "50dc9044596ec5e2f1fe40de9ec2b09a5a926ebc20483a1a6187fa7061cf78b9";

    public ClaimedProcessorTests()
    {
        _seedSymbolIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo>>();
        _symbolAuctionInfoIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo>>();
        _objectMapper = GetRequiredService<IObjectMapper>();
        _seedSymbolRepository = GetRequiredService<IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo>>();
    }


    [Fact]
    public async Task HandleClaimedProcessorAsync_Test()
    {
        await SpecialSeedAddedAsync();
        await MockTsmSeedSymbolInfoIndexAsync();
        var claimedProcessor = GetRequiredService<ClaimedProcessor>();
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        //step1: create blockStateSet
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);
        var logEventContext = new LogEventContext
        {
            ChainId = chainId,
            BlockHeight = blockHeight,
            BlockHash = blockHash,
            PreviousBlockHash = previousBlockHash,
            TransactionId = transactionId,
            BlockTime = DateTime.Now
        };

        var symbolAuctionInfoIndex = await MockSymbolAuctionInfoIndexAsync(logEventContext);
        Claimed claimed = new Claimed
        {
            AuctionId = Hash.LoadFromHex(AuctionId),
            FinishTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
            Bidder = Address.FromBase58(from)
        };
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(claimed.ToLogEvent());
        logEventInfo.BlockHeight = blockHeight;
        logEventInfo.ChainId = chainId;
        logEventInfo.BlockHash = blockHash;
        logEventInfo.PreviousBlockHash = previousBlockHash;
        logEventInfo.TransactionId = transactionId;


        //step3: handle event and write result to blockStateSet
        await claimedProcessor.HandleEventAsync(logEventInfo, logEventContext);

        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(0);

        //step5: check result
        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, Symbol);
        var tsmSeedSymbolIndex =
            await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolId, chainId);
        tsmSeedSymbolIndex.ChainId.ShouldBe(chainId);
        tsmSeedSymbolIndex.Status.ShouldBe(SeedStatus.UNREGISTERED);

        tsmSeedSymbolIndex.Owner.ShouldBe(symbolAuctionInfoIndex.FinishBidder);
        tsmSeedSymbolIndex.TokenPrice.Symbol.ShouldBe(symbolAuctionInfoIndex.FinishPrice.Symbol);
        tsmSeedSymbolIndex.TokenPrice.Amount.ShouldBe(symbolAuctionInfoIndex.FinishPrice.Amount);
        tsmSeedSymbolIndex.AuctionStatus.ShouldBe((int)SeedAuctionStatus.Finished);
    }

    private async Task SpecialSeedAddedAsync()
    {
        var logEventContext = MockLogEventContext();
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
                        AuctionType = Forest.SymbolRegistrar.AuctionType.English,
                        IssueChain = ForestIndexerConstants.MainChain,
                        IssueChainContractAddress = to,
                    }
                }
            }
        };

        var logEventInfo = MockLogEventInfo(specialSeedAdded.ToLogEvent());
        var seedCreatedProcessor = GetRequiredService<SpecialSeedAddedLogEventProcessor>();
        await seedCreatedProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
    }

    private async Task<SymbolAuctionInfoIndex> MockSymbolAuctionInfoIndexAsync(LogEventContext logEventContext)
    {
        SymbolAuctionInfoIndex symbolAuctionInfoIndex = new SymbolAuctionInfoIndex
        {
            Id = Hash.LoadFromHex(AuctionId)
                .ToHex(),
            Symbol = "SEED-1",
            StartTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.Now),
            EndTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.Now.AddDays(1)),
            MaxEndTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.Now.AddDays(5)),
            Duration = 0,
            MinMarkup = 0,
            FinishBidder = from,
            FinishTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.Now),
            FinishPrice = new TokenPriceInfo
            {
                Symbol = "ELF",
                Amount = 6
            }
        };
        _objectMapper.Map(logEventContext, symbolAuctionInfoIndex);
        await _symbolAuctionInfoIndexRepository.AddOrUpdateAsync(symbolAuctionInfoIndex);
        return symbolAuctionInfoIndex;
    }

    private async Task MockTsmSeedSymbolInfoIndexAsync()
    {
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var logEventContext = MockLogEventContext();
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);
        var tsmSeedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, Symbol);
        var tsmSeedSymbolIndex =
            await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(tsmSeedSymbolId, chainId);
        tsmSeedSymbolIndex.SeedSymbol = "SEED-1";
        _objectMapper.Map(logEventContext, tsmSeedSymbolIndex);
        await _seedSymbolIndexRepository.AddOrUpdateAsync(tsmSeedSymbolIndex);
        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, tsmSeedSymbolIndex.SeedSymbol);
        SeedSymbolIndex seedSymbolIndex = new SeedSymbolIndex();
        seedSymbolIndex.Id = seedSymbolId;
        seedSymbolIndex.Symbol = tsmSeedSymbolIndex.SeedSymbol;
        seedSymbolIndex.SeedOwnedSymbol = tsmSeedSymbolIndex.Symbol;
        _objectMapper.Map(logEventContext, seedSymbolIndex);
        await _seedSymbolRepository.AddOrUpdateAsync(seedSymbolIndex);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
    }
}