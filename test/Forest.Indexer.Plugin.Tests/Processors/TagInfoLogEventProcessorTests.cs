using AElf;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors;
using Forest.Indexer.Plugin.Tests.Helper;
using Forest.Whitelist;
using Google.Protobuf;
using Shouldly;
using Xunit;
using TagInfo = Forest.Whitelist.TagInfo;

namespace Forest.Indexer.Plugin.Tests.Processors;

public class TagInfoLogEventProcessorTests : ForestIndexerPluginTestBase
{
    private readonly IAElfIndexerClientEntityRepository<TagInfoIndex, LogEventInfo>
        _tagInfoIndexRepository;
    
    public TagInfoLogEventProcessorTests()
    {
        _tagInfoIndexRepository = GetRequiredService<IAElfIndexerClientEntityRepository<TagInfoIndex, LogEventInfo>>();
    }

    [Fact]
    public async Task HandleTagInfoAddedAsync_Test()
    {
        const string chainId = "tDVW";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;
        const string tagName = "TEST";
        Hash whitelistId = HashHelper.ComputeFrom("test@gmail.com");
        Hash tagInfoId = HashHelper.ComputeFrom("test@gmail.com");


        var tagInfoAddedProcessor = GetRequiredService<TagInfoAddedLogEventProcessor>();
        tagInfoAddedProcessor.GetContractAddress(chainId);
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        //step1: create blockStateSet
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);

        //step2: create logEventInfo
        var tagInfoAdded = new TagInfoAdded()
        {
            WhitelistId = whitelistId,
            TagInfoId = tagInfoId,
            TagInfo = new TagInfo()
            {
                Info = ByteString.Empty,
                TagName = "TEST"
            },
        };

        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(tagInfoAdded.ToLogEvent());
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
        await tagInfoAddedProcessor.HandleEventAsync(logEventInfo, logEventContext);

        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(0);

        //step5: check result
        var tagInfoIndexData = await _tagInfoIndexRepository.GetFromBlockStateSetAsync(
            IdGenerateHelper.GetId(chainId, whitelistId.ToHex()), chainId);
        tagInfoIndexData.BlockHeight.ShouldBe(blockHeight);
        tagInfoIndexData.BlockHash.ShouldBe(blockHash);
        tagInfoIndexData.ChainId.ShouldBe(chainId);
        tagInfoIndexData.Name.ShouldBe(tagName);
        tagInfoIndexData.TagHash.ShouldBe(tagInfoId.ToHex());
    }

    [Fact]
    public async Task HandleTagInfoRemovedAsync_Test()
    {
        const string chainId = "tDVW";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;
        const string tagName = "TEST";
        Hash whitelistId = HashHelper.ComputeFrom("test@gmail.com");
        Hash tagInfoId = HashHelper.ComputeFrom("test@gmail.com");


        var tagInfoRemovedProcessor = GetRequiredService<TagInfoRemovedLogEventProcessor>();
        tagInfoRemovedProcessor.GetContractAddress(chainId);
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        //step1: create blockStateSet
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);

        //step2: create logEventInfo
        var tagInfoRemoved = new TagInfoRemoved()
        {
            WhitelistId = whitelistId,
            TagInfoId = tagInfoId,
            TagInfo = new TagInfo()
            {
                Info = ByteString.Empty,
                TagName = "TEST"
            },
        };

        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(tagInfoRemoved.ToLogEvent());
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
        await tagInfoRemovedProcessor.HandleEventAsync(logEventInfo, logEventContext);

        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(0);

        //step5: check result
        var tagInfoIndexData = await _tagInfoIndexRepository.GetFromBlockStateSetAsync(
            IdGenerateHelper.GetId(chainId, whitelistId.ToHex()), chainId);
        tagInfoIndexData.ShouldBe(null);
    }
}