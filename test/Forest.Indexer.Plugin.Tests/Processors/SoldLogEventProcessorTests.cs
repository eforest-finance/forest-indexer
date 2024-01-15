using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.GraphQL;
using Forest.Indexer.Plugin.Processors;
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace Forest.Indexer.Plugin.Tests.Processors;

public sealed class SoldLogEventProcessorTests : ForestIndexerPluginTestBase
{


    private readonly IAElfIndexerClientEntityRepository<SoldIndex, LogEventInfo> _nftSoldIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<NFTMarketDayIndex, LogEventInfo> _nftMarketDayIndexRepository;
    private readonly IObjectMapper _objectMapper;
    private const string symbol = "SYB-1";
    public SoldLogEventProcessorTests()
    {
        _nftSoldIndexRepository = GetRequiredService<IAElfIndexerClientEntityRepository<SoldIndex, LogEventInfo>>();
        _nftMarketDayIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<NFTMarketDayIndex, LogEventInfo>>();
        _objectMapper = GetRequiredService<IObjectMapper>();
    }

    [Fact]
    public async Task SoldProcessAsyncTest()
    {
        await MockCreateNFT();
        
        const string symbol = "SYB-1";
        const string tokenSymbol = "SYB-1";
        const long amount = 10000000000;
        const long decimals = 8;
        const long quantity = 1;
        var address1 = Address.FromPublicKey("AAA".HexToByteArray());
        var address2 = Address.FromPublicKey("BBB".HexToByteArray());
        var address3 = Address.FromPublicKey("CCC".HexToByteArray());
        var sold = new Sold()
        {
            NftFrom = address1,
            NftTo = address2,
            NftQuantity = quantity,
            NftSymbol = symbol,
            PurchaseAmount = amount,
            PurchaseSymbol = tokenSymbol
        };
        
        
        var logEventContext = MockLogEventContext(100);
        var blockStateSetKey = await MockBlockState(logEventContext);
        
        var logEventProcessor = GetRequiredService<SoldLogEventProcessor>();
        logEventProcessor.GetContractAddress(logEventContext.ChainId);

        // nft not exists
        sold.NftSymbol = "NOTFOUND";
        var logEvent = MockLogEventInfo(sold.ToLogEvent());
        await logEventProcessor.HandleEventAsync(logEvent, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        var soldIndexList = await _nftSoldIndexRepository.GetListAsync();
        soldIndexList.Item1.ShouldBe(0);
        sold.NftSymbol = symbol;

        // nft not exists
        sold.PurchaseSymbol = "NOTFOUND";
        logEvent = MockLogEventInfo(sold.ToLogEvent());
        await logEventProcessor.HandleEventAsync(logEvent, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        soldIndexList = await _nftSoldIndexRepository.GetListAsync();
        soldIndexList.Item1.ShouldBe(0);
        sold.PurchaseSymbol = tokenSymbol;

        // suscess
        logEvent = MockLogEventInfo(sold.ToLogEvent());
        await logEventProcessor.HandleEventAsync(logEvent, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        soldIndexList = await _nftSoldIndexRepository.GetListAsync();
        soldIndexList.Item1.ShouldBe(1);

        // another sold
        sold.PurchaseAmount = 2000000000000;
        logEventContext.TransactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da3";
        logEvent = MockLogEventInfo(sold.ToLogEvent());
        await logEventProcessor.HandleEventAsync(logEvent, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        soldIndexList = await _nftSoldIndexRepository.GetListAsync();
        soldIndexList.Item1.ShouldBe(2);
        

    }

    [Fact]
    public async Task MarketDataAsync_Test()
    {
        await SoldProcessAsyncTest();
        var marketDate = await Query.MarketDataAsync(_nftMarketDayIndexRepository, _objectMapper, new GetNFTMarketDto
        {
            MaxResultCount = 10,
            SkipCount = 0,
            NFTInfoId = IdGenerateHelper.GetNFTInfoId(MockLogEventContext(100).ChainId, symbol),
            TimestampMin = DateTime.UnixEpoch.AddDays(-1).Millisecond,
            TimestampMax = DateTime.UnixEpoch.AddDays(1).Millisecond
        });
        marketDate.TotalRecordCount.ShouldBe(1);
        marketDate.Data[0].Price.ShouldBe(new decimal(10050));
    }

    [Fact]
    public async Task NftSoldDataAsync_Test()
    {
        await SoldProcessAsyncTest();
        var soldData = await Query.NftSoldDataAsync(_nftSoldIndexRepository, new GetNFTSoldDataInput
        {
           StartTime = Convert.ToDateTime("12/31/2022"),
           EndTime = Convert.ToDateTime("01/01/2023")
        });
        soldData.TotalTransCount.ShouldBe(2);
        soldData.TotalNftAmount.ShouldBe(2);
        soldData.TotalTransAmount.ShouldBe(2010000000000);
    }
}