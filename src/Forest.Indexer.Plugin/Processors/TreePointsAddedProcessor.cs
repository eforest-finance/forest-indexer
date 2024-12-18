using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class TreePointsAddedProcessor: LogEventProcessorBase<TreePointsAdded>
{
    private readonly IObjectMapper _objectMapper;
    public TreePointsAddedProcessor(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetNFTForestContractAddress(chainId);
    }

    public override async Task ProcessAsync(TreePointsAdded eventValue, LogEventContext context)
    {
        Logger.LogInformation("TreePointsAddedProcessor address:{A} eventValue:{B}", eventValue.Owner.ToBase58(),JsonConvert.SerializeObject(eventValue));
        if (eventValue == null || context == null) return;
        
        var pointsType = GetValueFromEnum(eventValue.PointsType);
        if (pointsType == PointsType.DEFAULT) return;
        Logger.LogInformation("TreePointsAddedProcessor pointsType:{A}", pointsType);

        var opType = OpType.ADDED;
        var recordId = IdGenerateHelper.GetTreePointsAddedRecordId
            (context.ChainId, eventValue.Owner.ToBase58(),opType.ToString(), eventValue.OpTime);
        
        var recordIndex = await GetEntityAsync<TreePointsChangeRecordIndex>(recordId);
        Logger.LogInformation("TreePointsAddedProcessor recordId:{A}, index:{B}", recordId, JsonConvert.SerializeObject(recordIndex));

        if (recordIndex != null) return;


        recordIndex = new TreePointsChangeRecordIndex
        {
            Id = recordId,
            Address = eventValue.Owner.ToBase58(),
            TotalPoints = eventValue.TotalPoints,
            Points = eventValue.Points,
            OpTime = eventValue.OpTime,
            OpType = opType,
            PointsType = pointsType,
            ActivityId = "",
            TreeLevel = ""
        };
        _objectMapper.Map(context, recordIndex);
        recordIndex.BlockHeight = context.Block.BlockHeight;
        recordIndex.BlockHash = context.Block.BlockHash;
        recordIndex.PreviousBlockHash = context.Block.PreviousBlockHash;
        await SaveEntityAsync(recordIndex);
        Logger.LogInformation("TreePointsAddedProcessor add success address:{A} recordIndex:{B}", eventValue.Owner.ToBase58(),JsonConvert.SerializeObject(recordIndex));

    }
    
    private PointsType GetValueFromEnum(int value)
    {
        switch (value)
        {
            case (int)PointsType.NORMALONE:
                return PointsType.NORMALONE;
            case (int)PointsType.NORMALTWO:
                return PointsType.NORMALTWO;
            case (int)PointsType.INVITE:
                return PointsType.INVITE;
            default:
                return PointsType.DEFAULT;
        }
    }


}