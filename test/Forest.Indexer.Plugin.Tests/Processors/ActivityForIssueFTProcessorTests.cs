using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors;
using Forest.SymbolRegistrar;
using Nethereum.Hex.HexConvertors.Extensions;
using Xunit;

namespace Forest.Indexer.Plugin.Tests.Processors;

public class ActivityForIssueFTProcessorTests:ForestIndexerPluginTestBase
{
    private readonly IAElfIndexerClientEntityRepository<SymbolMarketActivityIndex, TransactionInfo>
        _symbolMarketActivityIndexRepository;

    public ActivityForIssueFTProcessorTests()
    {
        _symbolMarketActivityIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<SymbolMarketActivityIndex, TransactionInfo>>();
    }

    [Fact]
    public async Task SymbolMarketActivityIndexAdd()
    {
        const string chainId = "tDVW";
        const string symbol = "SEED-1";
        const string seedOwnedSymbol = "seedOwnedSymbol1";

        var logEventContext = MockLogEventContext(100);
        var blockStateSetKey = await MockBlockStateForTransactionInfo(logEventContext);
        await SeedAdd(SeedType.Unique, symbol, seedOwnedSymbol, chainId);
        var activityForIssueFTProcessor = GetRequiredService<ActivityForIssueFTProcessor>();
        activityForIssueFTProcessor.GetContractAddress(chainId);

        var issued = new Issued()
        {
            Symbol = seedOwnedSymbol,
            To = Address.FromPublicKey("AAA".HexToByteArray()),
            Amount = 1,
            Memo = "aaa"
        };

        var logEventInfo = MockLogEventInfo(issued.ToLogEvent());

        await activityForIssueFTProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<TransactionInfo>(blockStateSetKey);

        var symbolMarketActivityId = "Buy-tDVW-seedOwnedSymbol1---c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        var symbolMarketActivityIndex =
            _symbolMarketActivityIndexRepository.GetFromBlockStateSetAsync(symbolMarketActivityId, chainId);
        Assert.True(symbolMarketActivityIndex.Result.Id.Equals(symbolMarketActivityId));
        Assert.True(symbolMarketActivityIndex.Result.Symbol.Equals(seedOwnedSymbol));
    }
}