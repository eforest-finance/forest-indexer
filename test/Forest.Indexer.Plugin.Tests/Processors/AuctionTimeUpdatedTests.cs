using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Contracts.Auction;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors;
using Forest.Indexer.Plugin.Tests.Helper;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace Forest.Indexer.Plugin.Tests.Processors;

public class AuctionTimeUpdatedTests : ForestIndexerPluginTestBase
{
    private readonly IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo> _symbolAuctionInfoIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;

    private readonly IAuctionInfoProvider _auctionInfoProvider;
    private readonly IObjectMapper _objectMapper;
    private const string chainId = "tDVW";
    private const string Symbol = "PWD-1";
    private const string from = "2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7";
    private const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
    private const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
    private const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
    private const string to = "Lmemfcp2nB8kAvQDLxsLtQuHWgpH5gUWVmmcEkpJ2kRY9Jv25";
    private const long blockHeight = 100;
    public AuctionTimeUpdatedTests()
    {
        _symbolAuctionInfoIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo>>();
        _auctionInfoProvider = GetRequiredService<IAuctionInfoProvider>();
        _objectMapper = GetRequiredService<IObjectMapper>();
        
        _seedSymbolIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo>>();
    }



    [Fact]
    public async Task HandleEventAsyncTest()
    {
        
        await MockSpecialSeedAddedAsync(Symbol, to);
        await MockTsmSeedSymbolInfoIndexAsync(chainId, Symbol);
        
        var auctionCreated = await MockAddAuctionInfo(Symbol, chainId);
        
        var startTime = Timestamp.FromDateTime(DateTime.UtcNow);
        var endTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(10));
        var maxEndTime = new DateTime(2099, 11, 10).AddDays(1).ToUniversalTime().ToTimestamp();
        
        var auctionTimeUpdatedLogEventProcessor = GetRequiredService<AuctionTimeUpdatedLogEventProcessor>();
        
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
        var auctionTimeUpdated = new  AuctionTimeUpdated
        {
            AuctionId = auctionCreated.AuctionId,
            StartTime = startTime,
            EndTime = endTime,
            MaxEndTime = maxEndTime,
        };
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(auctionTimeUpdated.ToLogEvent());
        logEventInfo.BlockHeight = blockHeight;
        logEventInfo.ChainId = chainId;
        logEventInfo.BlockHash = blockHash;
        logEventInfo.PreviousBlockHash = previousBlockHash;
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

        //step3: handle event and write result to blockStateSet
        await auctionTimeUpdatedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);

        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        
        //: check result
        var auctionInfoIndex =
            await _symbolAuctionInfoIndexRepository.GetFromBlockStateSetAsync(auctionCreated.AuctionId.ToHex(), chainId);
        
        var tsmSeedSymbolIndex =
            await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(IdGenerateHelper.GetSeedSymbolId(chainId, Symbol), chainId);
        
        auctionInfoIndex.StartTime.ShouldBe(startTime.Seconds);
        auctionInfoIndex.EndTime.ShouldBe(endTime.Seconds);
        auctionInfoIndex.MaxEndTime.ShouldBe(maxEndTime.Seconds);
        tsmSeedSymbolIndex.AuctionEndTime.ShouldBe(endTime.Seconds);
    }

}