using AElf.CSharp.Core.Extension;
using AElf.Standards.ACS0;
using AElf.Types;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.GraphQL;
using Forest.Indexer.Plugin.Processors;
using Forest.Indexer.Plugin.Tests.Helper;
using Forest.Contracts.Drop;
using Google.Protobuf.WellKnownTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;
using Microsoft.Extensions.Logging;

namespace Forest.Indexer.Plugin.Tests.Processors;

public class NFTDropEventProcessorTests : ForestIndexerPluginTestBase
{
    private readonly IAElfIndexerClientEntityRepository<NFTDropIndex, LogEventInfo> _nftDropIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<NFTDropClaimIndex, LogEventInfo> _nftDropClaimIndexRepository;
    
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<NFTDropIndex> _logger ;

    private const string HEX = "44e2ed1fe3c6100450818c28e5a122c1858b71cf641a96871e9576165497b7c1";
    
    public NFTDropEventProcessorTests()
    {
        _nftDropIndexRepository = GetRequiredService<IAElfIndexerClientEntityRepository<NFTDropIndex, LogEventInfo>>();
        _nftDropClaimIndexRepository = GetRequiredService<IAElfIndexerClientEntityRepository<NFTDropClaimIndex, LogEventInfo>>();
        _objectMapper = GetRequiredService<IObjectMapper>();
    }


    [Fact]
    public async Task HandleDropCreatedLogEventAsync_Test()
    {
        // await HandleNFTIssueAsync_Test();
        await CreateTokenInfo();
        await MockCreateNFT();

        const string chainId = "tDVW";
        const string symbol = "SYB-1";
        const string blockHash = "1d29110ef8085744e8bd4ca4ddca9070036d07f4705b79c549b07115ea1f145b";
        const string previousBlockHash = "4d2986852e78f9d84a1f856ffd0d66264edc91e767d463bf08304f91fedb3d9f";
        const string transactionId = "7a4c16a8aa4bb415b1128d060bb3e356ca7bab9ff77be5838a0ce5c4f5b1fe19";
        const long blockHeight = 111;

        //step1: create blockStateSet
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);

        //step2: create logEventInfo
        var dropCreated = new DropCreated()
        {
            StartTime = new DateTime(2023, 11, 10).AddDays(1).ToUniversalTime().ToTimestamp(),
            ExpireTime = new DateTime(2024, 12, 10).AddDays(1).ToUniversalTime().ToTimestamp(),
            DropId = Hash.LoadFromHex(HEX),
            ClaimPrice = new Contracts.Drop.Price
            {
                Amount = 500,
                Symbol = "SYB"
            },
            Owner =  Address.FromPublicKey("AAA".HexToByteArray()),
            IsBurn = true,
            TotalAmount = 100,
            ClaimAmount = 0,
            ClaimMax = 2,
            CollectionSymbol = "SYB-0",
            State = DropState.Submit,
            CreateTime = new DateTime(2023, 1, 10).AddDays(1).ToUniversalTime().ToTimestamp(),
            UpdateTime = new DateTime(2023, 11, 10).AddDays(1).ToUniversalTime().ToTimestamp(),
        };
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(dropCreated.ToLogEvent());
        logEventInfo.BlockHeight = blockHeight;
        logEventInfo.ChainId = chainId;
        logEventInfo.BlockHash = blockHash;
        logEventInfo.TransactionId = transactionId;
        var logEventContext = new LogEventContext
        {
            ChainId = chainId,
            BlockHeight = blockHeight,
            BlockHash = blockHash,
            PreviousBlockHash = previousBlockHash,
            TransactionId = transactionId
        };

        //step3: handle event and write result to blockStateSet
        var dropCreatedLogEventProcessor = GetRequiredService<DropCreatedLogEventProcessor>();
        await dropCreatedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        dropCreatedLogEventProcessor.GetContractAddress(chainId);
        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);

        //step5: check result
        var dropIndexData =
            await _nftDropIndexRepository.GetFromBlockStateSetAsync(dropCreated.DropId.ToString(), chainId);

        dropIndexData.ShouldNotBeNull();
        dropIndexData.Id.ShouldBe(dropCreated.DropId.ToString());
    }
    
    private async Task CreateTokenInfo()
    {
        const string chainId = "tDVW";
        const string blockHash = "0f4f79c709ee39c597795689f99be3c1384148dbb1a0b1b0fa21fc91229164e3";
        const string previousBlockHash = "f2316fb0e7646259a4238d8cd4700c9c6451a432e89df48c2368418c55c22b81";
        const string transactionId = "7a4c16a8aa4bb415b1128d060bb3e356ca7bab9ff77be5838a0ce5c4f5b1fe19";
        const long blockHeight = 10;

        //step1: create blockStateSet
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);

        //step2: create logEventInfo
        var contractDeployed = new ContractDeployed
        {
            Address = Address.FromPublicKey("BBB".HexToByteArray())
        };
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(contractDeployed.ToLogEvent());
        logEventInfo.BlockHeight = blockHeight;
        logEventInfo.ChainId = chainId;
        logEventInfo.BlockHash = blockHash;
        logEventInfo.TransactionId = transactionId;
        var logEventContext = new LogEventContext
        {
            ChainId = chainId,
            BlockHeight = blockHeight,
            BlockHash = blockHash,
            PreviousBlockHash = previousBlockHash,
            TransactionId = transactionId
        };

        //step3: handle event and write result to blockStateSet
        var contractDeployedProcessor = GetRequiredService<ContractDeployedProcessor>();
        await contractDeployedProcessor.HandleEventAsync(logEventInfo, logEventContext);

        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(1000);
    }
    
    [Fact]
    public async Task NFTDropQueryAsync_Test()
    {
        await HandleDropCreatedLogEventAsync_Test();
        var dropInfo = await Query.NFTDropInfo(_nftDropIndexRepository, _objectMapper,
            Hash.LoadFromHex(HEX).ToString());
        dropInfo.ShouldNotBeNull();
    }
    
    [Fact]
    public async Task NFTALLDropListQueryAsync_Test()
    {
        await HandleDropCreatedLogEventAsync_Test();
        var dto = new GetNFTDropListDto
        {
            Type = SearchType.All
        };
        
        var dropInfo = await Query.NFTDropList(_nftDropIndexRepository, _objectMapper, _logger, dto);
        dropInfo.TotalRecordCount.ShouldBe(1);
    }
    
    [Fact]
    public async Task NFTYetToBeginDropListQueryAsync_Test()
    {
        await HandleDropCreatedLogEventAsync_Test();
        var dto = new GetNFTDropListDto
        {
            Type = SearchType.YetToBegin
        };
        
        var dropInfo = await Query.NFTDropList(_nftDropIndexRepository, _objectMapper, _logger, dto);
        dropInfo.TotalRecordCount.ShouldBe(0);
    }
    
    [Fact]
    public async Task NFTExpiredDropListQueryAsync_Test()
    {
        await HandleDropCreatedLogEventAsync_Test();
        var dropInfo = await Query.ExpiredDropList(_nftDropIndexRepository, _objectMapper);
        dropInfo.TotalRecordCount.ShouldBe(0);
    }
    
    
    [Fact]
    public async Task HandleDropClaimedLogEventAsync_Test()
    {
        const string chainId = "tDVW";
        const string symbol = "SYB-1";
        const string blockHash = "1d29110ef8085744e8bd4ca4ddca9070036d07f4705b79c549b07115ea1f145b";
        const string previousBlockHash = "4d2986852e78f9d84a1f856ffd0d66264edc91e767d463bf08304f91fedb3d9f";
        const string transactionId = "7a4c16a8aa4bb415b1128d060bb3e356ca7bab9ff77be5838a0ce5c4f5b1fe19";
        const long blockHeight = 111;

        //step1: create blockStateSet
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);

        //step2: create logEventInfo
        var dropClaimed = new DropClaimAdded()
        {
            Address = Address.FromPublicKey("BBB".HexToByteArray()),
            DropId = Hash.LoadFromHex(HEX),
            TotalAmount = 3,
            CurrentAmount = 1
        };
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(dropClaimed.ToLogEvent());
        logEventInfo.BlockHeight = blockHeight;
        logEventInfo.ChainId = chainId;
        logEventInfo.BlockHash = blockHash;
        logEventInfo.TransactionId = transactionId;
        var logEventContext = new LogEventContext
        {
            ChainId = chainId,
            BlockHeight = blockHeight,
            BlockHash = blockHash,
            PreviousBlockHash = previousBlockHash,
            TransactionId = transactionId
        };

        //step3: handle event and write result to blockStateSet
        var dropClaimedLogEventProcessor = GetRequiredService<DropClaimedLogEventProcessor>();
        await dropClaimedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        dropClaimedLogEventProcessor.GetContractAddress(chainId);
        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);

        //step5: check result
        var id = IdGenerateHelper.GetNFTDropClaimId(dropClaimed.DropId.ToString(), dropClaimed.Address.ToString());
        var dropClaimIndexData =
            await _nftDropClaimIndexRepository.GetFromBlockStateSetAsync(id, chainId);

        dropClaimIndexData.ShouldNotBeNull();
        dropClaimIndexData.Id.ShouldBe(id);
        dropClaimIndexData.ClaimAmount.ShouldBe(1);
    }
    
    
     [Fact]
    public async Task HandleDropClaimedUpdateLogEventAsync_Test()
    {
        
        await HandleDropClaimedLogEventAsync_Test();
        
        const string chainId = "tDVW";
        const string symbol = "SYB-1";
        const string blockHash = "1d29110ef8085744e8bd4ca4ddca9070036d07f4705b79c549b07115ea1fcc";
        const string previousBlockHash = "1d29110ef8085744e8bd4ca4ddca9070036d07f4705b79c549b07115ea1f14";
        const string transactionId = "7a4c16a8aa4bb415b1128d060bb3e356ca7bab9ff77be5838a0ce5c4f5b1fecc";
        const long blockHeight = 121;

        //step1: create blockStateSet
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);

        //step2: create logEventInfo
        var dropClaimed = new DropClaimAdded()
        {
            Address = Address.FromPublicKey("BBB".HexToByteArray()),
            DropId = Hash.LoadFromHex(HEX),
            TotalAmount = 3,
            CurrentAmount = 2
        };
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(dropClaimed.ToLogEvent());
        logEventInfo.BlockHeight = blockHeight;
        logEventInfo.ChainId = chainId;
        logEventInfo.BlockHash = blockHash;
        logEventInfo.TransactionId = transactionId;
        var logEventContext = new LogEventContext
        {
            ChainId = chainId,
            BlockHeight = blockHeight,
            BlockHash = blockHash,
            PreviousBlockHash = previousBlockHash,
            TransactionId = transactionId
        };

        //step3: handle event and write result to blockStateSet
        var dropClaimedLogEventProcessor = GetRequiredService<DropClaimedLogEventProcessor>();
        await dropClaimedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        dropClaimedLogEventProcessor.GetContractAddress(chainId);
        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);

        //step5: check result
        var id = IdGenerateHelper.GetNFTDropClaimId(dropClaimed.DropId.ToString(), dropClaimed.Address.ToString());
        var dropClaimIndexData =
            await _nftDropClaimIndexRepository.GetFromBlockStateSetAsync(id, chainId);

        dropClaimIndexData.ShouldNotBeNull();
        dropClaimIndexData.Id.ShouldBe(id);
        dropClaimIndexData.ClaimAmount.ShouldBe(2);
    }
    
    
    
    [Fact]
    public async Task NFTDropClaimQueryAsync_Test()
    {
        await HandleDropClaimedLogEventAsync_Test();
        var dto = new GetNFTDropClaimDto
        {
            Address = Address.FromPublicKey("BBB".HexToByteArray()).ToString(),
            DropId = Hash.LoadFromHex(HEX).ToString(),
        };
        var claimInfo = await Query.NFTDropClaim(_nftDropClaimIndexRepository, _objectMapper,
            dto);
        claimInfo.ShouldNotBeNull();
    }
}