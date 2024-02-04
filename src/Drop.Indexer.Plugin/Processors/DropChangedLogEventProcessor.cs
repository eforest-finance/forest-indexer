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

public class DropChangedLogEventProcessor : AElfLogEventProcessorBase<DropChanged, LogEventInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<NFTDropIndex, LogEventInfo> _nftDropIndexRepository;
    private readonly ILogger<DropChangedLogEventProcessor> _logger;
    
    public DropChangedLogEventProcessor(ILogger<DropChangedLogEventProcessor> logger, 
        IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<NFTDropIndex, LogEventInfo> nftDropIndexRepository,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions
        ) : base(logger)
    {
        _nftDropIndexRepository = nftDropIndexRepository;
         _logger = logger;
        _contractInfoOptions = contractInfoOptions.Value;
        _objectMapper = objectMapper;
    }
    
    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).NFTDropContractAddress;
    }
    
    protected override async Task HandleEventAsync(DropChanged eventValue, LogEventContext context)
    {
        _logger.Debug("DropChanged: {eventValue} context: {context}",JsonConvert.SerializeObject(eventValue), 
            JsonConvert.SerializeObject(context));
        var dropIndex = await _nftDropIndexRepository.GetFromBlockStateSetAsync(eventValue.DropId.ToHex(), context.ChainId);
        if (dropIndex == null)
        {
            _logger.Info("Drop Not Exist: {id}",eventValue.DropId.ToHex());
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
        await _nftDropIndexRepository.AddOrUpdateAsync(dropIndex);
    }
}