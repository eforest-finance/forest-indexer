using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class TreeLevelUpdatedProcessor: LogEventProcessorBase<TreeLevelUpgraded>
{
    private readonly IObjectMapper _objectMapper;
    public TreeLevelUpdatedProcessor(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetNFTForestContractAddress(chainId);
    }

    public override async Task ProcessAsync(TreeLevelUpgraded eventValue, LogEventContext context)
    {
        Logger.LogInformation("TreeLevelUpdatedProcessor eventValue:{A}", JsonConvert.SerializeObject(eventValue));

        if (eventValue == null || context == null) return;

        var opType = OpType.Claim;

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
            ActivityId = "",
            TreeLevel = eventValue.UpgradeLevel.ToString()
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