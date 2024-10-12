using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Drop.Indexer.Plugin.Entities;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;
using Forest.Contracts.Drop;

namespace Drop.Indexer.Plugin.Processors;

public class DropClaimedLogEventProcessor : LogEventProcessorBase<DropClaimAdded>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    
    public DropClaimedLogEventProcessor(
        IObjectMapper objectMapper
    ) 
    {
        _objectMapper = objectMapper;
    }
    
    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).NFTDropContractAddress;
    }

    public override async Task ProcessAsync(DropClaimAdded eventValue, LogEventContext context)
    {
        Logger.LogInformation("DropClaimed: {eventValue} context: {context}",JsonConvert.SerializeObject(eventValue), 
            JsonConvert.SerializeObject(context));
        var id = IdGenerateHelper.GetNFTDropClaimId(eventValue.DropId.ToHex(), eventValue.Address.ToBase58());
        var claimIndex = await GetEntityAsync<NFTDropClaimIndex>(id);
        if (claimIndex == null)
        {
            claimIndex = new NFTDropClaimIndex
            {
                Id = id,
                DropId = eventValue.DropId.ToHex(),
                CreateTime = context.Block.BlockTime,
                Address = eventValue.Address.ToBase58(),
                ClaimTotal = eventValue.TotalAmount,
                ClaimAmount = eventValue.CurrentAmount
            };
        }
        else
        {
            claimIndex.ClaimTotal = Math.Max(claimIndex.ClaimTotal, eventValue.TotalAmount);
            claimIndex.ClaimAmount = Math.Max(claimIndex.ClaimAmount, eventValue.CurrentAmount);
        }
        _objectMapper.Map(context, claimIndex);
        var newUpdatetime = context.Block.BlockTime;
        var oldUpdatetime = claimIndex.UpdateTime;
        claimIndex.UpdateTime = DateTime.Compare(newUpdatetime, oldUpdatetime) == 1 ? newUpdatetime : oldUpdatetime;
        await SaveEntityAsync(claimIndex);
    }
}