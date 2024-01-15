using AElf.Contracts.TokenAdapterContract;
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

public class ActivityForCreateFTAndNFTProcessorTests : ForestIndexerPluginTestBase
{
    private readonly IAElfIndexerClientEntityRepository<SymbolMarketActivityIndex, TransactionInfo>
        _symbolMarketActivityIndexRepository;

    public ActivityForCreateFTAndNFTProcessorTests()
    {
        _symbolMarketActivityIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<SymbolMarketActivityIndex, TransactionInfo>>();
    }

    [Fact]
    public async Task Test()
    {
        
    }
    
    [Fact]
    public async Task SymbolMarketActivityIndexAdd()
    {
        const string chainId = "tDVW";
        const string symbol = "SEED-1";
        const string seedOwnedSymbol = "seedOwnedSymbol1";

        var logEventContext = MockLogEventContext(100);
        var blockStateSetKey = await MockBlockStateForTransactionInfo(logEventContext);
        await SeedAdd(SeedType.Regular, symbol, seedOwnedSymbol, chainId);

        const string tokenName = "READ Token";
        const long totalSupply = 0;
        const int decimals = 8;
        const bool isBurnable = true;
        const int issueChainId = 9992731;
        var activityForCreateFTAndNFTProcessor = GetRequiredService<ActivityForCreateFTAndNFTProcessor>();
        activityForCreateFTAndNFTProcessor.GetContractAddress(chainId);

        var managerTokenCreated = new ManagerTokenCreated()
        {
            Symbol = symbol,
            TokenName = tokenName,
            TotalSupply = totalSupply,
            Decimals = decimals,
            Issuer = Address.FromPublicKey("AAA".HexToByteArray()),
            IsBurnable = isBurnable,
            IssueChainId = issueChainId,
            RealIssuer = Address.FromPublicKey("AAA".HexToByteArray()),
            RealOwner = Address.FromPublicKey("AAA".HexToByteArray()),
            ExternalInfo = new ExternalInfos()
            {
                Value =
                {
                    { "__seed_owned_symbol", seedOwnedSymbol },
                    {
                        "__seed_exp_time",
                        new DateTimeOffset(DateTime.UtcNow.AddHours(1)).ToUnixTimeSeconds().ToString()
                    }
                }
            }
        };

        var logEventInfo = MockLogEventInfo(managerTokenCreated.ToLogEvent());

        await activityForCreateFTAndNFTProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<TransactionInfo>(blockStateSetKey);

        var symbolMarketActivityId = "Buy-tDVW-seedOwnedSymbol1---c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        var symbolMarketActivityIndex =
            await _symbolMarketActivityIndexRepository.GetFromBlockStateSetAsync(symbolMarketActivityId, chainId);
        Assert.True(symbolMarketActivityIndex.Id.Equals(symbolMarketActivityId));
        Assert.True(symbolMarketActivityIndex.Symbol.Equals(seedOwnedSymbol));
    }
}