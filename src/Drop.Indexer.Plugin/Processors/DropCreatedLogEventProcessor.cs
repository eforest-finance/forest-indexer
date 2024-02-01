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

public class DropCreatedLogEventProcessor : AElfLogEventProcessorBase<DropCreated, LogEventInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<NFTDropIndex, LogEventInfo> _nftDropIndexRepository;
    // private readonly IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> _tokenIndexRepository;
    private readonly ILogger<DropCreatedLogEventProcessor> _logger;
    
    public DropCreatedLogEventProcessor(ILogger<DropCreatedLogEventProcessor> logger, 
        IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<NFTDropIndex, LogEventInfo> nftDropIndexRepository,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions
        ) : base(logger)
    {
        _nftDropIndexRepository = nftDropIndexRepository;
        // _tokenIndexRepository = tokenIndexRepository;
        _logger = logger;
        _contractInfoOptions = contractInfoOptions.Value;
        _objectMapper = objectMapper;
    }
    
    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).NFTDropContractAddress;
    }
    
    
    protected override async Task HandleEventAsync(DropCreated eventValue, LogEventContext context)
    {
        _logger.Debug("DropCreatedLogEventProcessor: {context}",JsonConvert.SerializeObject(context));
        var dropIndex = await _nftDropIndexRepository.GetFromBlockStateSetAsync(eventValue.DropId.ToString(), context.ChainId);
        if (dropIndex != null) return;
        
        dropIndex = _objectMapper.Map<DropCreated, NFTDropIndex>(eventValue);
        
        var tokenIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.ClaimPrice.Symbol);
        // var tokenIndex = await _tokenIndexRepository.GetFromBlockStateSetAsync(tokenIndexId, context.ChainId);
        
        // dropIndex.ClaimPrice = eventValue.ClaimPrice.Amount / (decimal)Math.Pow(10, tokenIndex.Decimals);
        dropIndex.ClaimSymbol = eventValue.ClaimPrice.Symbol;
        dropIndex.CollectionId = context.ChainId + "-" + eventValue.CollectionSymbol;
        
        _objectMapper.Map(context, dropIndex);
        await _nftDropIndexRepository.AddOrUpdateAsync(dropIndex);
    }
}