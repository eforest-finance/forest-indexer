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
    private const int ELF_DECIMAL = 8;
    
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<NFTDropIndex, LogEventInfo> _nftDropIndexRepository;
    private readonly ILogger<DropCreatedLogEventProcessor> _logger;
    
    public DropCreatedLogEventProcessor(ILogger<DropCreatedLogEventProcessor> logger, 
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
    
    
    protected override async Task HandleEventAsync(DropCreated eventValue, LogEventContext context)
    {
        _logger.Debug("DropCreated: eventValue: {eventValue} context: {context}",JsonConvert.SerializeObject(eventValue), 
            JsonConvert.SerializeObject(context));
        var dropIndex = await _nftDropIndexRepository.GetFromBlockStateSetAsync(eventValue.DropId.ToHex(), context.ChainId);
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
        _logger.Debug("DropCreatedUpdate: id: {eventValue}", dropIndex.Id);
        await _nftDropIndexRepository.AddOrUpdateAsync(dropIndex);
    }
}