using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp.ObjectMapping;
using Forest.Contracts.Drop;

namespace Forest.Indexer.Plugin.Processors;

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
        _logger.Debug("DropClaimedLogEventProcessor: {context}",JsonConvert.SerializeObject(context));
        var id = IdGenerateHelper.GetNFTDropClaimId(eventValue.DropId.ToString(), eventValue.Address.ToString());
        var claimIndex = await _nftDropClaimIndexRepository.GetFromBlockStateSetAsync(id, context.ChainId);
        if (claimIndex == null)
        {
            claimIndex = _objectMapper.Map<DropClaimAdded, NFTDropClaimIndex>(eventValue);
            claimIndex.CreateTime = context.BlockTime;
            claimIndex.Id = id;
        }
        else
        {
            _objectMapper.Map(eventValue, claimIndex);
        }
        
        _objectMapper.Map(context, claimIndex);
        claimIndex.UpdateTime = context.BlockTime;
        await _nftDropClaimIndexRepository.AddOrUpdateAsync(claimIndex);
    }
}