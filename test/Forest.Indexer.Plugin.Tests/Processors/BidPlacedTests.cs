using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Processors;
using Nest;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace Forest.Indexer.Plugin.Tests.Processors;

public class BidPlacedTests : ForestIndexerPluginTestBase
{
    private readonly IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo> _symbolAuctionInfoIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<SymbolBidInfoIndex, LogEventInfo> _symbolBidInfoIndexRepository;

    private readonly IAuctionInfoProvider _auctionInfoProvider;

    private readonly IObjectMapper _objectMapper;

    private const string chainId = "tDVW";
    private const string Symbol = "PWD-1";
    private const string from = "2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7";
    private const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
    private const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
    private const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
    private const string to = "Lmemfcp2nB8kAvQDLxsLtQuHWgpH5gUWVmmcEkpJ2kRY9Jv25";
    private const string to_2 = "23GxsoW9TRpLqX1Z5tjrmcRMMSn5bhtLAf4HtPj8JX9BerqTqp";

    private const long blockHeight = 100;
    
    public BidPlacedTests()
    {
        _symbolAuctionInfoIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo>>();
        _symbolBidInfoIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<SymbolBidInfoIndex, LogEventInfo>>();
        _auctionInfoProvider = GetRequiredService<IAuctionInfoProvider>();
        _objectMapper = GetRequiredService<IObjectMapper>();
    }


    [Fact]
    public async Task HandleEventAsyncTest()
    {
        await MockSpecialSeedAddedAsync(Symbol, to);
        await MockTsmSeedSymbolInfoIndexAsync(chainId, Symbol);
        
        var auctionCreated = await MockAddAuctionInfo(Symbol, chainId);

        //BidPlaced three
        await MockBidPlaced(Symbol, chainId, auctionCreated, to,0);
        await MockBidPlaced(Symbol, chainId, auctionCreated, to_2,1);
        await MockBidPlaced(Symbol, chainId, auctionCreated, to,3);
        
        //: check result
        var auctionInfoIndex =
            await _symbolAuctionInfoIndexRepository.GetFromBlockStateSetAsync(auctionCreated.AuctionId.ToHex(), chainId);

        auctionInfoIndex.FinishPrice.Amount.ShouldBe(20003);
        auctionInfoIndex.FinishPrice.Symbol.ShouldBe("USDT");
        Thread.Sleep(3000);
        var mustQueryList = new List<Func<QueryContainerDescriptor<SymbolBidInfoIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.AuctionId).Value(auctionCreated.AuctionId.ToHex())),
        };

        QueryContainer ExtraInfoFilter(QueryContainerDescriptor<SymbolBidInfoIndex> f)
            => f.Bool(b => b.Must(mustQueryList));

        IPromise<IList<ISort>> Sort(SortDescriptor<SymbolBidInfoIndex> s) => s.Ascending(a => a.BidTime);
        var (totalCount, list) = await _symbolBidInfoIndexRepository.GetSortListAsync(ExtraInfoFilter, sortFunc: Sort);
        
        totalCount.ShouldBeGreaterThanOrEqualTo(1);
        foreach (var symbolBidInfoIndex in list)
        {
            symbolBidInfoIndex.AuctionId.ShouldBe(auctionCreated.AuctionId.ToHex());
            symbolBidInfoIndex.Symbol.ShouldBe(auctionCreated.Symbol);

        }
        
        var tsmSeedSymbolIndex =
            await _tsmSeedSymbolIndexRepository.GetFromBlockStateSetAsync(IdGenerateHelper.GetSeedSymbolId(chainId, Symbol), chainId);
        tsmSeedSymbolIndex.AuctionStatus.ShouldBe((int)SeedAuctionStatus.Bidding);
        tsmSeedSymbolIndex.BidsCount.ShouldBe(3);
        tsmSeedSymbolIndex.BiddersCount.ShouldBe(2);
    }
}