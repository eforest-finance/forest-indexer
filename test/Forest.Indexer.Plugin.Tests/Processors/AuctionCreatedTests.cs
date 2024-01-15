using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors;
using Forest.SymbolRegistrar;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace Forest.Indexer.Plugin.Tests.Processors;

public class AuctionCreatedTests : ForestIndexerPluginTestBase
{
    private readonly IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo> _symbolAuctionInfoIndexRepository;
    private readonly IAuctionInfoProvider _auctionInfoProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> _seedSymbolRepository;
    
    private const string chainId = "tDVW";
    private const string Symbol = "PWD-1";
    private const string from = "2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7";
    private const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
    private const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
    private const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
    private const string to = "Lmemfcp2nB8kAvQDLxsLtQuHWgpH5gUWVmmcEkpJ2kRY9Jv25";
    private const long blockHeight = 100;
    
    public AuctionCreatedTests()
    {
        _symbolAuctionInfoIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo>>();
        _auctionInfoProvider = GetRequiredService<IAuctionInfoProvider>();
        _objectMapper = GetRequiredService<IObjectMapper>();
        _seedSymbolRepository = GetRequiredService<IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo>>();
        _seedSymbolIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo>>();
    }
    
    [Fact]
    public async Task HandleEventAsyncTest()
    {
        var auctionCreated = await MockAddAuctionInfo(Symbol, chainId);
        //: check result
        var auctionInfoIndex =
            await _symbolAuctionInfoIndexRepository.GetFromBlockStateSetAsync(auctionCreated.AuctionId.ToHex(), chainId);
        auctionInfoIndex.Symbol.ShouldBe(Symbol);
        auctionInfoIndex.Id.ShouldBe(auctionCreated.AuctionId.ToHex());
        auctionInfoIndex.ReceivingAddress.ShouldBe(auctionCreated.ReceivingAddress.ToBase58());
        auctionInfoIndex.MinMarkup.ShouldBe(auctionCreated.AuctionConfig.MinMarkup);
        auctionInfoIndex.StartPrice.Amount.ShouldBe(auctionCreated.StartPrice.Amount);
        auctionInfoIndex.StartPrice.Symbol.ShouldBe("ELF");
        auctionInfoIndex.Creator.ShouldBe(auctionCreated.Creator.ToBase58());
    }
}