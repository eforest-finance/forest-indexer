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

public class DropChangedLogEventProcessor : AElfLogEventProcessorBase<DropChanged, LogEventInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<NFTDropIndex, LogEventInfo> _nftDropIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> _tokenIndexRepository;
    private readonly ILogger<DropChangedLogEventProcessor> _logger;
    
    public DropChangedLogEventProcessor(ILogger<DropChangedLogEventProcessor> logger, 
        IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<NFTDropIndex, LogEventInfo> nftDropIndexRepository,
        IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> tokenIndexRepository,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions
        ) : base(logger)
    {
        _nftDropIndexRepository = nftDropIndexRepository;
        _tokenIndexRepository = tokenIndexRepository;
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
        _logger.Debug("DropChangedLogEventProcessor: {context}",JsonConvert.SerializeObject(context));
        var dropIndex = await _nftDropIndexRepository.GetFromBlockStateSetAsync(eventValue.DropId.ToString(), context.ChainId);
        if (dropIndex == null)
        {
            _logger.Info("Drop Not Exist: {id}",eventValue.DropId.ToString());
            return;
        }

        dropIndex.TotalAmount = eventValue.TotalAmount;
        dropIndex.ClaimAmount = eventValue.ClaimAmount;
        dropIndex.MaxIndex = eventValue.MaxIndex;
        dropIndex.UpdateTime = eventValue.UpdateTime.ToDateTime();
        await _nftDropIndexRepository.AddOrUpdateAsync(dropIndex);
    }
}