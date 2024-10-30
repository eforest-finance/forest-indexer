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
        var recordIndex = await GetEntityAsync<TreePointsAddedRecordIndex>(recordId);
        if (recordIndex != null) return;
        
        recordIndex = new TreePointsAddedRecordIndex
        {
            Id = recordId,
            Address = eventValue.Owner.ToBase58(),
            TotalPoints = eventValue.TotalPoints,
            Points = eventValue.Points,
            PointsType = eventValue.PointsType,
            OpTime = eventValue.OpTime
        };
        _objectMapper.Map(context, recordIndex);
        await SaveEntityAsync(recordIndex);
    }
    
}