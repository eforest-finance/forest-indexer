using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Drop.Indexer.Plugin.Entities;
using Forest.Contracts.Drop;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Drop.Indexer.Plugin.Processors;

public class DropChangedLogEventProcessor : LogEventProcessorBase<DropChanged>
{
    private readonly IObjectMapper _objectMapper;

    public DropChangedLogEventProcessor(
        IObjectMapper objectMapper
    )
    {
        _objectMapper = objectMapper;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetNFTDropContractAddress(chainId);
    }

    public override async Task ProcessAsync(DropChanged eventValue, LogEventContext context)
    {
        Logger.LogInformation("DropChanged: {eventValue} context: {context}", JsonConvert.SerializeObject(eventValue),
            JsonConvert.SerializeObject(context));
        var id = eventValue.DropId.ToHex();
        var dropIndex = await GetEntityAsync<NFTDropIndex>(id);
        if (dropIndex == null)
        {
            Logger.LogInformation("Drop Not Exist: {id}", eventValue.DropId.ToHex());
            return;
        }

        dropIndex.TotalAmount = Math.Max(dropIndex.TotalAmount, eventValue.TotalAmount);
        dropIndex.ClaimAmount = Math.Max(dropIndex.ClaimAmount, eventValue.ClaimAmount);
        dropIndex.MaxIndex = Math.Max(dropIndex.MaxIndex, eventValue.MaxIndex);
        dropIndex.CurrentIndex = Math.Max(dropIndex.CurrentIndex, eventValue.CurrentIndex);
        var newUpdatetime = eventValue.UpdateTime.ToDateTime();
        var oldUpdatetime = dropIndex.UpdateTime;
        dropIndex.UpdateTime = DateTime.Compare(newUpdatetime, oldUpdatetime) == 1 ? newUpdatetime : oldUpdatetime;

        _objectMapper.Map(context, dropIndex);
        await SaveEntityAsync(dropIndex);
    }
}