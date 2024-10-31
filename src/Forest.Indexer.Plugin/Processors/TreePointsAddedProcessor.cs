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
        Logger.LogInformation("TreePointsAddedProcessor eventValue:{A}", JsonConvert.SerializeObject(eventValue));
        if (eventValue == null || context == null) return;
        
        var pointsType = GetValueFromEnum(eventValue.PointsType);
        if (pointsType == PointsType.Default) return;
        Logger.LogInformation("TreePointsAddedProcessor pointsType:{A}", pointsType);

        var opType = OpType.Added;
        var recordId = IdGenerateHelper.GetTreePointsAddedRecordId
            (context.ChainId, eventValue.Owner.ToBase58(),opType.ToString(), eventValue.OpTime);
        
        var recordIndex = await GetEntityAsync<TreePointsChangeRecordIndex>(recordId);
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
        await SaveEntityAsync(recordIndex);
    }
    
    private PointsType GetValueFromEnum(int value)
    {
        switch (value)
        {
            case (int)PointsType.NormalOne:
                return PointsType.NormalOne;
            case (int)PointsType.NormalTwo:
                return PointsType.NormalTwo;
            case (int)PointsType.Invite:
                return PointsType.Invite;
            default:
                return PointsType.Default;
        }
    }


}