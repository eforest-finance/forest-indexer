using AElf;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors;
using Forest.Whitelist;
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using Xunit;
using StrategyType = Forest.Whitelist.StrategyType;

namespace Forest.Indexer.Plugin.Tests.Processors;

public sealed class WhitelistLogEventProcessorTests : ForestIndexerPluginTestBase
{
    private readonly IAElfIndexerClientEntityRepository<WhitelistIndex, LogEventInfo> _whitelistInfoIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<WhiteListExtraInfoIndex, LogEventInfo>
        _whitelistExtraInfoIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<WhiteListManagerIndex, LogEventInfo>
        _whitelistManagerIndexRepository;

    public WhitelistLogEventProcessorTests()
    {
        _whitelistInfoIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<WhitelistIndex, LogEventInfo>>();
        _whitelistExtraInfoIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<WhiteListExtraInfoIndex, LogEventInfo>>();
        _whitelistManagerIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<WhiteListManagerIndex, LogEventInfo>>();
    }


    [Fact]
    public async Task HandleWhitelistCreatedAsync_Test()
    {
        var whitelistId = HashHelper.ComputeFrom("test1@gmail.com");
        var cloneFrom = HashHelper.ComputeFrom("test2@gmail.com");
        var strategyType = StrategyType.Price;
        var projectId = Hash.Empty;
        const string remark = "";
        var address1 = Address.FromPublicKey("AAA".HexToByteArray());
        var address2 = Address.FromPublicKey("BBB".HexToByteArray());
        var address3 = Address.FromPublicKey("CCC".HexToByteArray());
        var creator = Address.FromPublicKey("DDD".HexToByteArray());
        var manager = new Whitelist.AddressList() { Value = { address2 } };
        var logEventContext = MockLogEventContext(100);

        //step1: create blockStateSet
        var blockStateSetKey = await MockBlockState(logEventContext);

        //step2: create logEventInfo
        var whitelistCreated = new WhitelistCreated()
        {
            WhitelistId = whitelistId,
            IsAvailable = true,
            Creator = creator,
            Manager = manager,
            IsCloneable = true,
            CloneFrom = cloneFrom,
            StrategyType = strategyType,
            Remark = remark,
            ProjectId = projectId,
            ExtraInfoIdList = new ExtraInfoIdList()
            {
                Value =
                {
                    new ExtraInfoId()
                    {
                        Id = whitelistId,
                        AddressList = new Whitelist.AddressList()
                        {
                            Value =
                            {
                                address1,
                                address2,
                                address3,
                            }
                        }
                    }
                }
            }
        };

        var whitelistIdStr = whitelistId.ToHex();
        var whitelistCreatedProcessor = GetRequiredService<WhitelistCreatedLogEventProcessor>();
        whitelistCreatedProcessor.GetContractAddress(logEventContext.ChainId);
        var extraInfoTemp = whitelistCreated.ExtraInfoIdList;

        // create success
        whitelistCreated.ExtraInfoIdList = extraInfoTemp;
        var logEventInfo = MockLogEventInfo(whitelistCreated.ToLogEvent());
        await whitelistCreatedProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        var whitelistInfoIndexData =
            await _whitelistInfoIndexRepository.GetFromBlockStateSetAsync(whitelistIdStr, logEventContext.ChainId);
        whitelistInfoIndexData.ShouldNotBeNull();
        whitelistInfoIndexData.BlockHeight.ShouldBe(logEventContext.BlockHeight);
        whitelistInfoIndexData.BlockHash.ShouldBe(logEventContext.BlockHash);
        whitelistInfoIndexData.Id.ShouldBe(whitelistId.ToHex());
        whitelistInfoIndexData.ChainId.ShouldBe(logEventContext.ChainId);
        whitelistInfoIndexData.CloneFrom.ShouldBe(FullAddressHelper.ToFullAddress(cloneFrom.ToHex(),logEventContext.ChainId));
        whitelistInfoIndexData.Creator.ShouldBe(FullAddressHelper.ToFullAddress(creator.ToBase58(),logEventContext.ChainId));
        
        // whitelist exists
        logEventInfo = MockLogEventInfo(whitelistCreated.ToLogEvent());
        await whitelistCreatedProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        whitelistInfoIndexData =
            await _whitelistInfoIndexRepository.GetFromBlockStateSetAsync(whitelistIdStr, logEventContext.ChainId);
        whitelistInfoIndexData.ShouldNotBeNull();
        
        // check extraInfo
        var whitelistExtraInfoIndexData = await _whitelistExtraInfoIndexRepository.GetListAsync();
        whitelistExtraInfoIndexData.Item1.ShouldBe(3);

    }

    [Fact]
    public async Task HandleWhitelistDisableAsync_Test()
    {
        await HandleWhitelistCreatedAsync_Test();
        var whitelistId = HashHelper.ComputeFrom("test1@gmail.com");

        var logEventContext = MockLogEventContext(100);
        var blockStateSetKey = await MockBlockState(logEventContext);

        var whitelistIdStr = whitelistId.ToHex();
        var whitelistDisabled = new WhitelistDisabled()
        {
            WhitelistId = whitelistId,
            IsAvailable = false,
        };
        
        var whitelistDisabledProcessor = GetRequiredService<WhitelistDisabledLogEventProcessor>();
        whitelistDisabledProcessor.GetContractAddress(logEventContext.ChainId);

        // whitelist not exists, item will be not updated
        whitelistDisabled.WhitelistId = HashHelper.ComputeFrom("ERROR");
        var logEventInfo = MockLogEventInfo(whitelistDisabled.ToLogEvent());
        await whitelistDisabledProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        var whitelistInfoIndexData =
            await _whitelistInfoIndexRepository.GetFromBlockStateSetAsync(whitelistIdStr, logEventContext.ChainId);
        whitelistInfoIndexData.IsAvailable.ShouldBe(true);

        // item will be updated
        whitelistDisabled.WhitelistId = whitelistId;
        logEventInfo = MockLogEventInfo(whitelistDisabled.ToLogEvent());
        await whitelistDisabledProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        whitelistInfoIndexData =
            await _whitelistInfoIndexRepository.GetFromBlockStateSetAsync(whitelistIdStr, logEventContext.ChainId);
        whitelistInfoIndexData.BlockHeight.ShouldBe(logEventContext.BlockHeight);
        whitelistInfoIndexData.BlockHash.ShouldBe(logEventContext.BlockHash);
        whitelistInfoIndexData.Id.ShouldBe(whitelistId.ToHex());
        whitelistInfoIndexData.IsAvailable.ShouldBe(false);
    }

    [Fact]
    public async Task HandleWhitelistReenableAsync_Test()
    {
        await HandleWhitelistDisableAsync_Test();

        var whitelistId = HashHelper.ComputeFrom("test1@gmail.com");

        var logEventContext = MockLogEventContext(100);
        var blockStateSetKey = await MockBlockState(logEventContext);
        var whitelistIdStr = whitelistId.ToHex();
        var whitelistReenable = new WhitelistReenable()
        {
            WhitelistId = whitelistId,
            IsAvailable = true,
        };
        var whitelistReenableProcessor = GetRequiredService<WhitelistReenableLogEventProcessor>();
        whitelistReenableProcessor.GetContractAddress(logEventContext.ChainId);

        // whitelistId does not match, data will not be update
        whitelistReenable.WhitelistId = HashHelper.ComputeFrom("ERROR");
        var logEventInfo = MockLogEventInfo(whitelistReenable.ToLogEvent());
        await whitelistReenableProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        var whitelistInfoIndexData =
            await _whitelistInfoIndexRepository.GetFromBlockStateSetAsync(whitelistIdStr, logEventContext.ChainId);
        whitelistInfoIndexData.IsAvailable.ShouldBe(false);

        // data will be update
        whitelistReenable.WhitelistId = whitelistId;
        logEventInfo = MockLogEventInfo(whitelistReenable.ToLogEvent());
        await whitelistReenableProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        whitelistInfoIndexData =
            await _whitelistInfoIndexRepository.GetFromBlockStateSetAsync(whitelistIdStr, logEventContext.ChainId);
        whitelistInfoIndexData.BlockHeight.ShouldBe(logEventContext.BlockHeight);
        whitelistInfoIndexData.BlockHash.ShouldBe(logEventContext.BlockHash);
        whitelistInfoIndexData.Id.ShouldBe(whitelistId.ToHex());
        whitelistInfoIndexData.IsAvailable.ShouldBe(true);
    }

    [Fact]
    public async Task HandleWhitelistResetAsync_Test()
    {
        await HandleWhitelistCreatedAsync_Test();

        var whitelistId = HashHelper.ComputeFrom("test1@gmail.com");
        var projectId = HashHelper.ComputeFrom("test1");

        var logEventContext = MockLogEventContext(100);
        var blockStateSetKey = await MockBlockState(logEventContext);
        var whitelistIdStr = whitelistId.ToHex();
        var whitelistReset = new WhitelistReset()
        {
            WhitelistId = whitelistId,
            ProjectId = projectId
        };
        var whitelistResetProcessor = GetRequiredService<WhitelistResetLogEventProcessor>();
        whitelistResetProcessor.GetContractAddress(logEventContext.ChainId);

        // whitelistId not match, data will NOT be update
        whitelistReset.WhitelistId = HashHelper.ComputeFrom("ERROR");
        var logEventInfo = MockLogEventInfo(whitelistReset.ToLogEvent());
        await whitelistResetProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        var whitelistInfoIndexData =
            await _whitelistInfoIndexRepository.GetFromBlockStateSetAsync(whitelistIdStr, logEventContext.ChainId);
        whitelistInfoIndexData.ProjectId.ShouldNotBe(projectId.ToHex());

        // whitelistId maches, data will be update
        whitelistReset.WhitelistId = whitelistId;
        logEventInfo = MockLogEventInfo(whitelistReset.ToLogEvent());
        await whitelistResetProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        whitelistInfoIndexData =
            await _whitelistInfoIndexRepository.GetFromBlockStateSetAsync(whitelistIdStr, logEventContext.ChainId);
        whitelistInfoIndexData.BlockHeight.ShouldBe(logEventContext.BlockHeight);
        whitelistInfoIndexData.BlockHash.ShouldBe(logEventContext.BlockHash);
        whitelistInfoIndexData.Id.ShouldBe(whitelistId.ToHex());
        whitelistInfoIndexData.ProjectId.ShouldBe(projectId.ToHex());

        // todo: check tagInfo reset
    }

    [Fact]
    public async Task HandleWhitelistAddressInfoAddedAsync_Test()
    {
        await HandleWhitelistCreatedAsync_Test();

        var whitelistId = HashHelper.ComputeFrom("test1@gmail.com");
        var address4 = Address.FromPublicKey("FFF".HexToByteArray());

        var logEventContext = MockLogEventContext(100);
        var blockStateSetKey = await MockBlockState(logEventContext);

        var whitelistIdStr = whitelistId.ToHex();
        var whitelistAddressInfoAdded = new WhitelistAddressInfoAdded()
        {
            WhitelistId = whitelistId,
            ExtraInfoIdList = new ExtraInfoIdList()
            {
                Value =
                {
                    new ExtraInfoId()
                    {
                        Id = whitelistId,
                        AddressList = new Whitelist.AddressList()
                        {
                            Value =
                            {
                                address4,
                            }
                        }
                    }
                }
            }
        };
        var whitelistAddressInfoAddedProcessor = GetRequiredService<WhitelistAddressInfoAddedProcessor>();
        whitelistAddressInfoAddedProcessor.GetContractAddress(logEventContext.ChainId);
        var extraInfoTemp = whitelistAddressInfoAdded.ExtraInfoIdList;

        // extraInfo empty, no data will be add
        whitelistAddressInfoAdded.ExtraInfoIdList = null;
        var logEventInfo = MockLogEventInfo(whitelistAddressInfoAdded.ToLogEvent());
        await whitelistAddressInfoAddedProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        var whitelistInfoIndexData = await _whitelistExtraInfoIndexRepository.GetListAsync();
        whitelistInfoIndexData.Item1.ShouldBe(3);

        // extraInfo exists, address4 will be add
        whitelistAddressInfoAdded.ExtraInfoIdList = extraInfoTemp;
        logEventInfo = MockLogEventInfo(whitelistAddressInfoAdded.ToLogEvent());
        await whitelistAddressInfoAddedProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        whitelistInfoIndexData = await _whitelistExtraInfoIndexRepository.GetListAsync();
        whitelistInfoIndexData.Item1.ShouldBe(4);
    }

    [Fact]
    public async Task HandleWhitelistAddressInfoRemoveAsync_Test()
    {
        await HandleWhitelistCreatedAsync_Test();

        var whitelistId = HashHelper.ComputeFrom("test1@gmail.com");
        var address1 = Address.FromPublicKey("AAA".HexToByteArray());
        var logEventContext = MockLogEventContext(100);
        var blockStateSetKey = await MockBlockState(logEventContext);
        var whitelistIdStr = whitelistId.ToHex();
        var whitelistAddressInfoRemoved = new WhitelistAddressInfoRemoved()
        {
            WhitelistId = whitelistId,
            ExtraInfoIdList = new ExtraInfoIdList()
            {
                Value =
                {
                    new ExtraInfoId()
                    {
                        Id = whitelistId,
                        AddressList = new Whitelist.AddressList()
                        {
                            Value = { address1, }
                        }
                    }
                }
            }
        };

        var extraInfoTemp = whitelistAddressInfoRemoved.ExtraInfoIdList;
        var whitelistAddressInfoRemovedProcessor = GetRequiredService<WhitelistAddressInfoRemovedProcessor>();
        whitelistAddressInfoRemovedProcessor.GetContractAddress(logEventContext.ChainId);

        // whitelistId not match, no data will be remove
        whitelistAddressInfoRemoved.WhitelistId = HashHelper.ComputeFrom("ERROR");
        var logEventInfo = MockLogEventInfo(whitelistAddressInfoRemoved.ToLogEvent());
        await whitelistAddressInfoRemovedProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        var whitelistInfoIndexData = await _whitelistExtraInfoIndexRepository.GetListAsync();
        whitelistInfoIndexData.Item1.ShouldBe(3);
        whitelistAddressInfoRemoved.WhitelistId = whitelistId;
        
        // input extraInfo is empty, no data will be remove
        whitelistAddressInfoRemoved.ExtraInfoIdList = null;
        logEventInfo = MockLogEventInfo(whitelistAddressInfoRemoved.ToLogEvent());
        await whitelistAddressInfoRemovedProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        whitelistInfoIndexData = await _whitelistExtraInfoIndexRepository.GetListAsync();
        whitelistInfoIndexData.Item1.ShouldBe(3);

        // address1 will be remove
        whitelistAddressInfoRemoved.ExtraInfoIdList = extraInfoTemp;
        logEventInfo = MockLogEventInfo(whitelistAddressInfoRemoved.ToLogEvent());
        await whitelistAddressInfoRemovedProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        whitelistInfoIndexData = await _whitelistExtraInfoIndexRepository.GetListAsync();
        whitelistInfoIndexData.Item1.ShouldBe(2);

    }

    [Fact]
    public async Task UpdateExtrainfoProcessorTest()
    {
        
        await HandleWhitelistCreatedAsync_Test();

        var whitelistId = HashHelper.ComputeFrom("test1@gmail.com");
        var whitelistId2 = HashHelper.ComputeFrom("test2@gmail.com");
        var address1 = Address.FromPublicKey("AAA".HexToByteArray());
        var address2 = Address.FromPublicKey("BBB".HexToByteArray());
        var address3 = Address.FromPublicKey("CCC".HexToByteArray());
        var address4 = Address.FromPublicKey("FFF".HexToByteArray());

        var logEventContext = MockLogEventContext(100);
        var blockStateSetKey = await MockBlockState(logEventContext);

        var extraInfoIdBefore = new ExtraInfoId()
        {
            Id = whitelistId,
            AddressList = new Whitelist.AddressList(){ Value = { address1, address2 } }
        };
        var extraInfoIdAfter = new ExtraInfoId()
        {
            Id = whitelistId2,
            AddressList = new Whitelist.AddressList() { Value = { address1, address2 } }
        };
        var extraInfoUpdated = new ExtraInfoUpdated()
        {
            WhitelistId = whitelistId,
            ExtraInfoIdBefore = extraInfoIdBefore,
            ExtraInfoIdAfter = extraInfoIdAfter
        };
        var updateExtraInfoProcessor = GetRequiredService<UpdateExtraInfoProcessor>();
        updateExtraInfoProcessor.GetContractAddress(logEventContext.ChainId);

        var extraInfos = await _whitelistExtraInfoIndexRepository.GetListAsync();
        extraInfos.Item1.ShouldBe(3);
        
        var eventInfo = MockLogEventInfo(extraInfoUpdated.ToLogEvent());
        await updateExtraInfoProcessor.HandleEventAsync(eventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        extraInfos = await _whitelistExtraInfoIndexRepository.GetListAsync();
        extraInfos.Item1.ShouldBe(3);
        var ids = extraInfos.Item2.ToDictionary(k => k.Address, k => k.TagInfoId);
        ids.Values.Distinct().Count().ShouldBe(2);
        ids.Values.ShouldContain(whitelistId.ToHex());
        ids.Values.ShouldContain(whitelistId2.ToHex());
        ids[address1.ToBase58()].ShouldBe(whitelistId2.ToHex());
        ids[address2.ToBase58()].ShouldBe(whitelistId2.ToHex());


    }
}