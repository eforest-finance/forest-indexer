using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Drop.Indexer.Plugin.Entities;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;
using Forest.Contracts.Drop;

namespace Drop.Indexer.Plugin.Processors;

public class DropStateChangedLogEventProcessor : LogEventProcessorBase<DropStateChanged>
{
    private readonly IObjectMapper _objectMapper;
    
    public DropStateChangedLogEventProcessor(
        IObjectMapper objectMapper
        )
    {
        _objectMapper = objectMapper;
    }
    
    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetNFTDropContractAddress(chainId);
    }

    public override async Task ProcessAsync(DropStateChanged eventValue, LogEventContext context)
    {
        Logger.LogInformation("DropStateChanged: {eventValue} context: {context}",JsonConvert.SerializeObject(eventValue), 
            JsonConvert.SerializeObject(context));
        var id = eventValue.DropId.ToHex();
        var dropIndex = await GetEntityAsync<NFTDropIndex>(id);
        if (dropIndex == null)
        {
            Logger.LogInformation("Drop Not Exist: {id}",eventValue.DropId.ToHex());
            return;
        }
        dropIndex.State = eventValue.State;
        dropIndex.UpdateTime = eventValue.UpdateTime.ToDateTime();
        _objectMapper.Map(context, dropIndex);
        Logger.LogInformation("DropStateChangedUpdate: id: {eventValue}",dropIndex.Id);
        await SaveEntityAsync(dropIndex);
    }

}