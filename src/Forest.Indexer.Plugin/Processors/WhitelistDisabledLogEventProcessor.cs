using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Whitelist;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class WhitelistDisabledLogEventProcessor : AElfLogEventProcessorBase<WhitelistDisabled, LogEventInfo>
{
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly ILogger<AElfLogEventProcessorBase<WhitelistDisabled, LogEventInfo>> _logger;
    private readonly IAElfIndexerClientEntityRepository<WhitelistIndex, LogEventInfo> _whitelistIndexRepository;

    private readonly IObjectMapper _objectMapper;


    public WhitelistDisabledLogEventProcessor(
        ILogger<AElfLogEventProcessorBase<WhitelistDisabled, LogEventInfo>> logger,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        IAElfIndexerClientEntityRepository<WhitelistIndex, LogEventInfo> whitelistIndexRepository,
        IObjectMapper objectMapper) : base(logger)
    {
        _logger = logger;
        _whitelistIndexRepository = whitelistIndexRepository;
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).WhitelistContractAddress;
    }

    protected override async Task HandleEventAsync(WhitelistDisabled eventValue, LogEventContext context)
    {
        var whitelistId = eventValue.WhitelistId.ToHex();
        _logger.Debug("[WhitelistDisabled] START: Id={Id}, IsAvailable={IsAvailable}",
            whitelistId, eventValue.IsAvailable);

        try
        {
            var whitelist = await _whitelistIndexRepository.GetFromBlockStateSetAsync(whitelistId, context.ChainId);
            if (whitelist == null) throw new UserFriendlyException("whitelist NOT FOUND");
            
            whitelist.IsAvailable = eventValue.IsAvailable;
            _objectMapper.Map(context, whitelist);
        
            _logger.Debug("[WhitelistDisabled] SAVE: Id={Id}, IsAvailable={IsAvailable}",
                whitelistId, eventValue.IsAvailable);
        
            await _whitelistIndexRepository.AddOrUpdateAsync(whitelist);
        
            _logger.Debug("[WhitelistDisabled] FINISH: Id={Id}, IsAvailable={IsAvailable}",
                whitelistId, eventValue.IsAvailable);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[WhitelistDisabled] FINISH: Id={Id}, , IsAvailable={IsAvailable}", whitelistId, eventValue.IsAvailable);
            throw;
        }
    }
}