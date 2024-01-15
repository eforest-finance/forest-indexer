using AElf.Contracts.CrossChain;
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

public class ActivityForSymbolMarketDealtProcessorTests : ForestIndexerPluginTestBase
{
    private readonly IAElfIndexerClientEntityRepository<SymbolMarketActivityIndex, TransactionInfo>
        _symbolMarketActivityIndexRepository;

    public ActivityForSymbolMarketDealtProcessorTests()
    {
        _symbolMarketActivityIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<SymbolMarketActivityIndex, TransactionInfo>>();
    }

    [Fact]
    public async Task SymbolMarketActivityIndexAdd()
    {
        const string sideChainId = "tDVW";
        const string symbol = "SEED-1";
        const string seedOwnedSymbol = "seedOwnedSymbol1";

        var logEventContext = MockLogEventContext(100);
        var blockStateSetKey = await MockBlockStateForTransactionInfo(logEventContext);
        await SeedAdd(SeedType.Unique, symbol, seedOwnedSymbol,sideChainId);
        var activityForSymbolMarketDealtProcessor = GetRequiredService<ActivityForSymbolMarketBoughtProcessor>();
        // activityForSymbolMarketDealtProcessor.GetContractAddress(chainId);

        var bought = new Bought()
        {
            Symbol = seedOwnedSymbol,
            Buyer = Address.FromPublicKey("AAA".HexToByteArray()),
            Price = new SymbolRegistrar.Price
            {
                Symbol = "ELF",
                Amount = 10,
            }
        };

        var logEventInfo = MockLogEventInfo(bought.ToLogEvent());

        await activityForSymbolMarketDealtProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<TransactionInfo>(blockStateSetKey);

        var seedSymbolId =
            "Buy-tDVW-seedOwnedSymbol1---c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        var symbolMarketActivityIndex =
            await _symbolMarketActivityIndexRepository.GetFromBlockStateSetAsync(seedSymbolId, sideChainId);
        Assert.True(symbolMarketActivityIndex.Id.Equals(seedSymbolId));
        Assert.True(symbolMarketActivityIndex.Symbol.Equals(seedOwnedSymbol));
    }
}