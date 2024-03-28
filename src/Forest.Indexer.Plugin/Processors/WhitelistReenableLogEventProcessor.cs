using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Whitelist;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class WhitelistReenableLogEventProcessor : AElfLogEventProcessorBase<WhitelistReenable, LogEventInfo>
{
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly ILogger<AElfLogEventProcessorBase<WhitelistReenable, LogEventInfo>> _logger;
    private readonly IAElfIndexerClientEntityRepository<WhitelistIndex, LogEventInfo> _whitelistIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<WhiteListExtraInfoIndex, LogEventInfo>
        _whitelistExtraInfoIndexRepository;
    private readonly IObjectMapper _objectMapper;


    public WhitelistReenableLogEventProcessor(
        ILogger<AElfLogEventProcessorBase<WhitelistReenable, LogEventInfo>> logger,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        IAElfIndexerClientEntityRepository<WhitelistIndex, LogEventInfo> whitelistIndexRepository,
        IAElfIndexerClientEntityRepository<WhiteListExtraInfoIndex, LogEventInfo> whitelistExtraInfoIndexRepository,
        IObjectMapper objectMapper) : base(logger)
    {
        _logger = logger;
        _whitelistIndexRepository = whitelistIndexRepository;
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _whitelistExtraInfoIndexRepository = whitelistExtraInfoIndexRepository;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).WhitelistContractAddress;
    }

    protected override async Task HandleEventAsync(WhitelistReenable eventValue, LogEventContext context)
    {
        var whitelistId = eventValue.WhitelistId.ToHex();
        _logger.Debug("[WhitelistReenable] START: Id={Id}, event={Event}", whitelistId,
            JsonConvert.SerializeObject(eventValue));

        try
        {
            var whitelist = await _whitelistIndexRepository.GetFromBlockStateSetAsync(whitelistId, context.ChainId);
            if (whitelist == null)
            {
                _logger.LogInformation("whitelist NOT FOUND");
                return;
            }
            whitelist.IsAvailable = eventValue.IsAvailable;
            _objectMapper.Map(context, whitelist);
        
            _logger.Debug("[WhitelistReenable] SAVE: Id={Id}", whitelistId);
            await _whitelistIndexRepository.AddOrUpdateAsync(whitelist);
            _logger.Debug("[WhitelistReenable] FINISH: Id={Id}", whitelistId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[WhitelistReenable] FAILED: Id={Id}", whitelistId);
            throw;
        }
    }
}