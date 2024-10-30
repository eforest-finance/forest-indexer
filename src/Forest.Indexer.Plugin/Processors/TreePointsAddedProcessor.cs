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
        
        var pointsType = GetValueFromEnum<PointsType>(eventValue.PointsType);
        if (!pointsType.HasValue) return;
        Logger.LogInformation("TreePointsAddedProcessor pointsType:{A}", pointsType.Value);

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