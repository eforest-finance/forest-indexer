using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Drop.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp.ObjectMapping;
using Forest.Contracts.Drop;

namespace Drop.Indexer.Plugin.Processors;

public class DropClaimedLogEventProcessor : AElfLogEventProcessorBase<DropClaimAdded, LogEventInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<NFTDropClaimIndex, LogEventInfo> _nftDropClaimIndexRepository;
    private readonly ILogger<DropClaimedLogEventProcessor> _logger;
    
    public DropClaimedLogEventProcessor(ILogger<DropClaimedLogEventProcessor> logger, 
        IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<NFTDropClaimIndex, LogEventInfo> nftDropClaimIndexRepository,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions
    ) : base(logger)
    {
        _nftDropClaimIndexRepository = nftDropClaimIndexRepository;
        _logger = logger;
        _contractInfoOptions = contractInfoOptions.Value;
        _objectMapper = objectMapper;
    }
    
    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).NFTDropContractAddress;
    }
    
    
    protected override async Task HandleEventAsync(DropClaimAdded eventValue, LogEventContext context)
    {
        _logger.Debug("DropClaimed: {eventValue} context: {context}",JsonConvert.SerializeObject(eventValue), 
            JsonConvert.SerializeObject(context));
        var id = IdGenerateHelper.GetNFTDropClaimId(eventValue.DropId.ToHex(), eventValue.Address.ToBase58());
        var claimIndex = await _nftDropClaimIndexRepository.GetFromBlockStateSetAsync(id, context.ChainId);
        if (claimIndex == null)
        {
            claimIndex = new NFTDropClaimIndex
            {
                Id = id,
                DropId = eventValue.DropId.ToHex(),
                CreateTime = context.BlockTime,
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
        var newUpdatetime = context.BlockTime;
        var oldUpdatetime = claimIndex.UpdateTime;
        
        claimIndex.UpdateTime = DateTime.Compare(newUpdatetime, oldUpdatetime) == 1 ? newUpdatetime : oldUpdatetime;
        await _nftDropClaimIndexRepository.AddOrUpdateAsync(claimIndex);
    }
}