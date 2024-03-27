using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.GraphQL;
using Forest.Indexer.Plugin.Processors;
using Forest.Indexer.Plugin.Tests.Helper;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace Forest.Indexer.Plugin.Tests.Processors;

public class TransferProcessorTests : ForestIndexerPluginTestBase
{
    private readonly IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo> _nftActivityIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> _nftInfoIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> _userBalanceIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;
    private readonly IObjectMapper _objectMapper;
    private const string chainId = "TDVW";
    private const string Symbol = "PWD-1";
    private const string from = "2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7";
    private const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
    private const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
    private const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
    private const string to = "Lmemfcp2nB8kAvQDLxsLtQuHWgpH5gUWVmmcEkpJ2kRY9Jv25";
    private const long blockHeight = 100;

    public TransferProcessorTests()
    {
        _nftActivityIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo>>();
        _nftInfoIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo>>();
        _userBalanceIndexRepository = GetRequiredService<IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo>>();
        _objectMapper = GetRequiredService<IObjectMapper>();
        _seedSymbolIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo>>();
    }


    [Fact]
    public async Task HandleTransferProcessorAsync_Test()
    {
       
        var transferProcessor = GetRequiredService<TokenTransferProcessor>();
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
        var nftInfoId = IdGenerateHelper.GetNFTInfoId(chainId, Symbol);
        var nftInfoIndex = new NFTInfoIndex()
        {
            Id = nftInfoId,
            Symbol = Symbol,
            ChainId = chainId
        };
        nftInfoIndex.BlockHeight = blockHeight;
        nftInfoIndex.ChainId = chainId;
        nftInfoIndex.BlockHash = blockHash;
        nftInfoIndex.PreviousBlockHash = previousBlockHash;
        await _nftInfoIndexRepository.AddOrUpdateAsync(nftInfoIndex);

        var userBalanceFromId =
            IdGenerateHelper.GetUserBalanceId(from, chainId, nftInfoId);
        UserBalanceIndex userBalanceIndex = new UserBalanceIndex
        {
            ChainId = chainId,
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            PreviousBlockHash = previousBlockHash,
            Id = userBalanceFromId,
            Address = from,
            Amount = 10,
            NFTInfoId = nftInfoId,
            Decimals = 0
        };
        await _userBalanceIndexRepository.AddOrUpdateAsync(userBalanceIndex);
        var fromId = IdGenerateHelper.GetUserBalanceId(from, chainId, nftInfoId);
        Transferred transferred = new Transferred()
        {
            Symbol = Symbol,
            Amount = 10,
            From = Address.FromBase58(from),
            To = Address.FromBase58(to)
        };
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(transferred.ToLogEvent());
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
        await transferProcessor.HandleEventAsync(logEventInfo, logEventContext);

        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(0);

        //step5: check result
        var nftActivityIndexId = IdGenerateHelper.GetNftActivityId(chainId,Symbol , from,
            to, transactionId);
        var nftActivityIndex =
            await _nftActivityIndexRepository.GetFromBlockStateSetAsync(nftActivityIndexId, chainId);
        nftActivityIndex.ChainId.ShouldBe(chainId);
        nftActivityIndex.From.ShouldBe(from);
        nftActivityIndex.To.ShouldBe(to);
        nftActivityIndex.Amount.ShouldBe(transferred.Amount);
        nftActivityIndex.TransactionHash.ShouldBe(transactionId);
        nftActivityIndex.Type.ShouldBe(NFTActivityType.Transfer);
        var fromBalance = await _userBalanceIndexRepository.GetFromBlockStateSetAsync(fromId,chainId);
        fromBalance.Amount.ShouldBe(0L);
        var toId = IdGenerateHelper.GetUserBalanceId(to,chainId,nftInfoId );
        var toBalance = await _userBalanceIndexRepository.GetFromBlockStateSetAsync(toId,chainId);
        toBalance.Amount.ShouldBe(10L);
    }

    [Fact]
    public async Task NFTActivityListAsync_Test()
    {
        await HandleTransferProcessorAsync_Test();
        String nftInfoId = IdGenerateHelper.GetNFTInfoId(chainId, Symbol);
        var result = await Query.NFTActivityListAsync(_nftActivityIndexRepository, new GetActivitiesDto()
            {
                SkipCount = 0,
                MaxResultCount = 10,
                NFTInfoId = nftInfoId,
                Types = new List<int> { (int)NFTActivityType.Transfer },
                TimestampMin = DateTime.UnixEpoch.AddHours(-1).Millisecond,
                TimestampMax = DateTime.UnixEpoch.AddHours(1).Millisecond
            }, _objectMapper
        );
        result.TotalRecordCount.ShouldBe(1);
        result.Data.Count.ShouldBe(1);
        result.Data.First().NftInfoId.ShouldBe(nftInfoId);
        result.Data.First().From.ShouldBe(from);
        result.Data.First().To.ShouldBe(to);
        result.Data.First().TransactionHash.ShouldBe(transactionId);
        result.Data.First().Type.ShouldBe((int)NFTActivityType.Transfer);
    }

    [Fact]
    public async Task HandleSeedTransferProcessorAsync_Test()
    {
        const string chainId = "AELF";
        const string seedSymbol = "SEED-1";
        var transferProcessor = GetRequiredService<TokenTransferProcessor>();
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        //step1: create blockStateSet
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);
        var id = IdGenerateHelper.GetSeedSymbolId(chainId, seedSymbol);
        var symbolIndex = new SeedSymbolIndex
        {
            ChainId = chainId,
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            PreviousBlockHash = previousBlockHash,
            IsDeleted = false,
            Id = id,
            Symbol = seedSymbol,
            Decimals = 0,
            Supply = 1,
            TotalSupply = 1,
            IsBurnable = true,
            CreateTime = default,
            IssuerTo = from
        };
        await _seedSymbolIndexRepository.AddOrUpdateAsync(symbolIndex);
        Transferred transferred = new Transferred()
        {
            Symbol = seedSymbol,
            Amount = 10,
            From = Address.FromBase58(from),
            To = Address.FromBase58(to)
        };
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(transferred.ToLogEvent());
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

        await transferProcessor.HandleEventAsync(logEventInfo, logEventContext);

        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);

        var seedSymbolRe = await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(id, chainId);
        seedSymbolRe.Symbol.ShouldBe(seedSymbol);
        seedSymbolRe.IssuerTo.ShouldBe(to);
    }

}