using AElf.Contracts.MultiToken;
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
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace Forest.Indexer.Plugin.Tests.Processors;

public class AuctionInfoProviderTests : ForestIndexerPluginTestBase
{
    private readonly IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo> _symbolAuctionInfoIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;
    private readonly IObjectMapper _objectMapper;
    private const string chainId = "AELF";
    private const string from = "2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7";
    private const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
    private const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
    private const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
    private const string to = "Lmemfcp2nB8kAvQDLxsLtQuHWgpH5gUWVmmcEkpJ2kRY9Jv25";
    private const long blockHeight = 100;
    public AuctionInfoProviderTests()
    {
        _symbolAuctionInfoIndexRepository = GetService<IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo>>();
        _seedSymbolIndexRepository = GetService<IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo>>();
        _objectMapper = GetService<IObjectMapper>();
    }

    [Fact]
    public async Task SetSeedSymbolIndexPriceByAuctionInfoAsyncTest(){
        await SeedAdd(SeedType.Unique, "seed-1", "TEST-symbol", chainId);
        
        const string symbol = "SEED-1";
        const string tokenName = "READ Token";
        const long totalSupply = 1;
        const int decimals = 8;
        const bool isBurnable = true;
        const int issueChainId = 9992731;
        
        var seedSymbolCreatedLogEventProcessor = GetRequiredService<TokenCreatedLogEventProcessor>();
        seedSymbolCreatedLogEventProcessor.GetContractAddress(chainId);
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);
        
        var tokenCreated = new TokenCreated()
        {
            Symbol = symbol,
            TokenName = tokenName,
            TotalSupply = totalSupply,
            Decimals = decimals,
            Issuer = Address.FromPublicKey("AAA".HexToByteArray()),
            IsBurnable = isBurnable,
            IssueChainId = issueChainId,
            ExternalInfo = new ExternalInfo()
            {
                Value =
                {
                    { "__seed_owned_symbol", "TEST-symbol" },
                    { "__seed_exp_time", new DateTimeOffset(DateTime.UtcNow.AddHours(1)).ToUnixTimeSeconds().ToString() }
                }
            }
        };
        
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(tokenCreated.ToLogEvent());
        logEventInfo.BlockHeight = blockHeight;
        logEventInfo.ChainId = chainId;
        logEventInfo.BlockHash = blockHash;
        logEventInfo.TransactionId = transactionId;
        logEventInfo.BlockTime = DateTime.Now;
        var logEventContext = new LogEventContext
        {
            ChainId = chainId,
            BlockHeight = blockHeight,
            BlockHash = blockHash,
            PreviousBlockHash = previousBlockHash,
            TransactionId = transactionId,
            BlockTime = DateTime.Now
        };
        
        await seedSymbolCreatedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        
        await Task.Delay(1000);
        var auctionCreated = await MockAddAuctionInfo(symbol, chainId);
        
        await Task.Delay(2000);
        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, symbol);
        var seedIndex = await _seedSymbolIndexRepository.GetAsync(seedSymbolId);
        seedIndex.BeginAuctionPrice.ShouldBe(DecimalUntil.ConvertToElf(auctionCreated.StartPrice.Amount));
        seedIndex.MaxAuctionPrice.ShouldBe(DecimalUntil.ConvertToElf(auctionCreated.StartPrice.Amount));
        seedIndex.HasAuctionFlag.ShouldBe(true);
    }
}