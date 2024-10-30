using AeFinder.Sdk.Processor;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
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
        if (eventValue == null || context == null) return;
        var recordId = IdGenerateHelper.GetTreePointsAddedRecordId
            (context.ChainId, eventValue.Owner.ToBase58(), eventValue.OpTime);
        var recordIndex = await GetEntityAsync<TreePointsChangeRecordIndex>(recordId);
        if (recordIndex != null) return;

        var pointsType = GetValueFromEnum<PointsType>(eventValue.PointsType);
        if (!pointsType.HasValue) return;
        recordIndex = new TreePointsChangeRecordIndex
        {
            Id = recordId,
            Address = eventValue.Owner.ToBase58(),
            TotalPoints = eventValue.TotalPoints,
            Points = eventValue.Points,
            OpTime = eventValue.OpTime,
            OpType = OpType.Added,
            PointsType = pointsType.Value,
            ActivityId = "",
            TreeLevel = ""
        };
        _objectMapper.Map(context, recordIndex);
        await SaveEntityAsync(recordIndex);
    }
    
    public static T? GetValueFromEnum<T>(int value) where T : struct, Enum  
    {  
        if (Enum.IsDefined(typeof(T), value))  
        {  
            return (T)Enum.ToObject(typeof(T), value);  
        }  
        return null;  
    } 
    
}