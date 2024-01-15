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

public class WhitelistAddressInfoRemovedProcessor : AElfLogEventProcessorBase<WhitelistAddressInfoRemoved, LogEventInfo>
{
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<WhitelistIndex, LogEventInfo> _whitelistIndexRepository;
    private readonly ILogger<AElfLogEventProcessorBase<WhitelistAddressInfoRemoved, LogEventInfo>> _logger;
    private readonly IWhiteListProvider _whitelistProvider;
    private readonly IObjectMapper _objectMapper;

    public WhitelistAddressInfoRemovedProcessor(
        ILogger<AElfLogEventProcessorBase<WhitelistAddressInfoRemoved, LogEventInfo>> logger,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        IWhiteListProvider whitelistProvider,
        IAElfIndexerClientEntityRepository<WhitelistIndex, LogEventInfo> whitelistIndexRepository,
        IObjectMapper objectMapper) : base(logger)
    {
        _contractInfoOptions = contractInfoOptions.Value;
        _logger = logger;
        _whitelistProvider = whitelistProvider;
        _whitelistIndexRepository = whitelistIndexRepository;
        _objectMapper = objectMapper;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).WhitelistContractAddress;
    }

    protected override async Task HandleEventAsync(WhitelistAddressInfoRemoved eventValue, LogEventContext context)
    {
        var whitelistId = eventValue.WhitelistId.ToHex();
        _logger.Debug("[WhitelistAddressInfoRemoved] START: Id={Id}, event={eventJson}", whitelistId,
            JsonConvert.SerializeObject(eventValue));

        try
        {
            var whitelist = await _whitelistIndexRepository.GetFromBlockStateSetAsync(whitelistId, context.ChainId);
            if (whitelist == null) throw new UserFriendlyException("whitelist NOT EXISTS");

            if (eventValue.ExtraInfoIdList == null || eventValue.ExtraInfoIdList.Value?.Count <= 0)
                throw new UserFriendlyException("extraInfo empty");

            _logger.Debug("[WhitelistAddressInfoRemoved] SAVE: Id={Id}", whitelistId);

        await _whitelistProvider.RemoveWhiteListExtraInfoAsync(context, eventValue.ExtraInfoIdList.Value?.ToList(), context.ChainId,
            whitelistId);

        _logger.Debug("[WhitelistAddressInfoRemoved] FINISH: Id={Id}", whitelistId);}
        catch (Exception e)
        {
            _logger.LogError(e, "[WhitelistAddressInfoRemoved] FAIL: Whitelist not found, Id={Id}", whitelistId);
            throw;
        }
    }
}