using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Processors;
using Forest.Indexer.Plugin.Tests.Helper;
using Forest.SymbolRegistrar;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace Forest.Indexer.Plugin.Tests.Processors;

public class BoughtProcessorTests : ForestIndexerPluginTestBase
{
    private readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;
    private readonly IObjectMapper _objectMapper;
    private const string chainId = "TDVW";
    private const string Symbol = "PWD-1";
    private const string from = "2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7";
    private const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
    private const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
    private const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
    private const string to = "Lmemfcp2nB8kAvQDLxsLtQuHWgpH5gUWVmmcEkpJ2kRY9Jv25";
    private const long blockHeight = 100;

    public BoughtProcessorTests()
    {
        _seedSymbolIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo>>();
    }


    [Fact]
    public async Task HandleBoughtProcessorAsync_Test()
    {
        var dealtProcessor = GetRequiredService<BoughtProcessor>();
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
        Bought dealt = new Bought
        {
            Buyer = Address.FromBase58(to),
            Symbol = Symbol,
            Price = new SymbolRegistrar.Price()
            {
                Symbol = "ELF",
                Amount = 10
            },
        };
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(dealt.ToLogEvent());
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
        await dealtProcessor.HandleEventAsync(logEventInfo, logEventContext);

        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(0);

        //step5: check result
        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, Symbol);
        var tsmSeedSymbolIndex =
            await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolId, chainId);
        tsmSeedSymbolIndex.ChainId.ShouldBe(chainId);
        tsmSeedSymbolIndex.Symbol.ShouldBe(dealt.Symbol);
        tsmSeedSymbolIndex.Owner.ShouldBe(dealt.Buyer.ToBase58());
        tsmSeedSymbolIndex.Status.ShouldBe(SeedStatus.UNREGISTERED);
    }
}