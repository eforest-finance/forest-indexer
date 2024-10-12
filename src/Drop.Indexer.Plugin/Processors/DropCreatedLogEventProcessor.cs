using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Drop.Indexer.Plugin.Entities;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;
using Forest.Contracts.Drop;


namespace Drop.Indexer.Plugin.Processors;

public class DropCreatedLogEventProcessor : LogEventProcessorBase<DropCreated>
{
    private const int ELF_DECIMAL = 8;
    
    private readonly IObjectMapper _objectMapper;
    
    public DropCreatedLogEventProcessor(
        IObjectMapper objectMapper
        )
    {
        _objectMapper = objectMapper;
    }
    
    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetNFTDropContractAddress(chainId);
    }

    public override async Task ProcessAsync(DropCreated eventValue, LogEventContext context)
    {
        Logger.LogInformation("DropCreated: eventValue: {eventValue} context: {context}",JsonConvert.SerializeObject(eventValue), 
            JsonConvert.SerializeObject(context));
        var id = eventValue.DropId.ToHex();
        var dropIndex = await GetEntityAsync<NFTDropIndex>(id);
        if (dropIndex != null) return;
        dropIndex = new NFTDropIndex
        {
            Id = eventValue.DropId.ToHex(),
            CollectionId = context.ChainId + "-" + eventValue.CollectionSymbol,
            StartTime = eventValue.StartTime.ToDateTime(),
            ExpireTime = eventValue.ExpireTime.ToDateTime(),
            ClaimMax = eventValue.ClaimMax,
            ClaimPrice = eventValue.ClaimPrice.Amount / (decimal)Math.Pow(10, ELF_DECIMAL),
            MaxIndex = eventValue.MaxIndex,
            ClaimSymbol = eventValue.ClaimPrice.Symbol,
            CurrentIndex = eventValue.CurrentIndex, 
    
            TotalAmount = eventValue.TotalAmount,
    
            ClaimAmount = eventValue.ClaimAmount,
            Owner = eventValue.Owner.ToBase58(),
            IsBurn = eventValue.IsBurn,
            State = eventValue.State,
            CreateTime = eventValue.CreateTime.ToDateTime(),
            UpdateTime = eventValue.UpdateTime.ToDateTime()
        };
        _objectMapper.Map(context, dropIndex);
        Logger.LogInformation("DropCreatedUpdate: id: {eventValue}", dropIndex.Id);
        await SaveEntityAsync(dropIndex);
    }
}