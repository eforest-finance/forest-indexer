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

public class DropStateChangedLogEventProcessor : AElfLogEventProcessorBase<DropStateChanged, LogEventInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<NFTDropIndex, LogEventInfo> _nftDropIndexRepository;
    private readonly ILogger<DropStateChangedLogEventProcessor> _logger;
    
    public DropStateChangedLogEventProcessor(ILogger<DropStateChangedLogEventProcessor> logger, 
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
    
    
    protected override async Task HandleEventAsync(DropStateChanged eventValue, LogEventContext context)
    {
        _logger.Debug("DropStateChangedLogEventProcessor: {context}",JsonConvert.SerializeObject(context));
        var dropIndex = await _nftDropIndexRepository.GetFromBlockStateSetAsync(eventValue.DropId.ToString(), context.ChainId);
        if (dropIndex == null)
        {
            _logger.Info("Drop Not Exist: {id}",eventValue.DropId.ToString());
            return;
        }

        dropIndex.State = eventValue.State;
        dropIndex.UpdateTime = eventValue.UpdateTime.ToDateTime();
        await _nftDropIndexRepository.AddOrUpdateAsync(dropIndex);
    }
}