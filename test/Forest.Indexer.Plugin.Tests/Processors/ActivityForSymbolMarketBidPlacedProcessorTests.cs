using AElf;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors;
using Forest.SymbolRegistrar;
using Google.Protobuf.WellKnownTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using Xunit;

namespace Forest.Indexer.Plugin.Tests.Processors;

public class ActivityForSymbolMarketBidPlacedProcessorTests : ForestIndexerPluginTestBase
{
    private readonly IAElfIndexerClientEntityRepository<SymbolMarketActivityIndex, TransactionInfo>
        _symbolMarketActivityIndexRepository;

    public ActivityForSymbolMarketBidPlacedProcessorTests()
    {
        _symbolMarketActivityIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<SymbolMarketActivityIndex, TransactionInfo>>();
    }

    [Fact]
    public async Task SymbolMarketActivityIndexAdd()
    {
        const string chainId = "AELF";
        const string sideChainId = "tDVW";
        const string symbol = "SEED-1";
        const string seedOwnedSymbol = "seedOwnedSymbol1";

        var logEventContext = MockLogEventContext(100);
        var blockStateSetKey = await MockBlockStateForTransactionInfo(logEventContext);
        await SeedAdd(SeedType.Unique, symbol, seedOwnedSymbol, chainId);
        await HandleSeedTokenCreate(chainId, symbol, seedOwnedSymbol);
        await HandleSeedIssueAsync(symbol,chainId,"AAA");
        
        //crosschain-seed burned
        await SeedBurnedAsync_Test(chainId,symbol);
        await HandleSeedTokenCreate(sideChainId, symbol, seedOwnedSymbol);
        //crosschain-CrossChainReceivedProcessor
        await MockCrossChain(1, symbol, 100, chainId, sideChainId, "READ Token", "AAA", "AAA");

        await MockAddAuctionInfo(symbol, sideChainId);
        
        var activityForSymbolMarketBidPlacedProcessor = GetRequiredService<ActivityForSymbolMarketBidPlacedProcessor>();
        //activityForSymbolMarketBidPlacedProcessor.GetContractAddress(chainId);

        var bidPlaced = new Forest.Contracts.Auction.BidPlaced()
        {
            Price = new Forest.Contracts.Auction.Price()
            {
                Amount = 1
            },
            AuctionId = HashHelper.ComputeFrom(symbol),
            Bidder = Address.FromPublicKey("AAA".HexToByteArray()),
            BidTime = new Timestamp()
            {
                Nanos = 1,
                Seconds = 1
            }
        };

        var logEventInfo = MockLogEventInfo(bidPlaced.ToLogEvent());

        await activityForSymbolMarketBidPlacedProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<TransactionInfo>(blockStateSetKey);

        var symbolMarketActivityId =
            "Buy-tDVW-seedOwnedSymbol1---c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        var symbolMarketActivityIndex =
            await _symbolMarketActivityIndexRepository.GetFromBlockStateSetAsync(symbolMarketActivityId, sideChainId);
        Assert.True(symbolMarketActivityIndex.Id.Equals(symbolMarketActivityId));
        Assert.True(symbolMarketActivityIndex.Symbol.Equals(symbol));
    }
}