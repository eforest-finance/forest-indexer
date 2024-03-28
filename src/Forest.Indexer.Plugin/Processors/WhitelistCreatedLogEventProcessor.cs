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

public class WhitelistCreatedLogEventProcessor : AElfLogEventProcessorBase<WhitelistCreated, LogEventInfo>
{
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<WhitelistIndex, LogEventInfo> _whitelistIndexRepository;
    private readonly ILogger<AElfLogEventProcessorBase<WhitelistCreated, LogEventInfo>> _logger;
    private readonly IWhiteListProvider _whitelistProvider;
    private readonly IObjectMapper _objectMapper;


    public WhitelistCreatedLogEventProcessor(ILogger<AElfLogEventProcessorBase<WhitelistCreated, LogEventInfo>> logger,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions, IWhiteListProvider whitelistProvider,
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

    protected override async Task HandleEventAsync(WhitelistCreated eventValue, LogEventContext context)
    {
        var whitelistId = eventValue.WhitelistId.ToHex();
        try
        {
            _logger.Debug("[WhitelistCreated] START: Id={Id}, Remark={Remark}, ExtraInfoIdList={ExtraInfoIdList}",
                whitelistId, eventValue.Remark, JsonConvert.SerializeObject(eventValue.ExtraInfoIdList));

            var whitelist = await _whitelistIndexRepository.GetFromBlockStateSetAsync(whitelistId, context.ChainId);
            if (whitelist != null)
            {
                _logger.LogInformation("WhiteList exists");
                return;
            }
            

            whitelist = _objectMapper.Map<WhitelistCreated, WhitelistIndex>(eventValue);
            whitelist.Id = whitelistId;
            whitelist.CloneFrom = eventValue.CloneFrom?.ToHex();
            whitelist.Creator = eventValue.Creator?.ToBase58();
            whitelist.ManagerInfoDictory = eventValue.Manager?.Value?.Select(x => x?.ToBase58()).ToList();
            whitelist.LastModifyTime = DateTimeHelper.GetTimeStampInMilliseconds();

            _objectMapper.Map(context, whitelist);

            _logger.Debug("[WhitelistCreated] SAVE: Id={Id}, Remark={Remark}", whitelistId, eventValue.Remark);

            await _whitelistIndexRepository.AddOrUpdateAsync(whitelist);

            _logger.Debug("[WhitelistCreated] Managers SAVE: Id={Id}, Remark={Remark}", whitelistId, eventValue.Remark);
            await _whitelistProvider.AddManagersAsync(context, eventValue.WhitelistId,
                eventValue.Manager?.Value?.Select(o => o.ToBase58()).ToList());

            if (eventValue.ExtraInfoIdList == null || eventValue.ExtraInfoIdList.Value?.Count <= 0)
            {
                _logger.LogInformation("ExtraInfoIdList empty");
                return;
            }

            _logger.Debug("[WhitelistCreated] ExtraInfoIdList SAVE Id={Id}", whitelistId);

            await _whitelistProvider.AddWhiteListExtraInfoAsync(context, eventValue.ExtraInfoIdList.Value?.ToList(),
                context.ChainId,
                whitelistId);

            _logger.Debug("[WhitelistCreated] ExtraInfoIdList FINISH Id={Id}", whitelistId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[WhitelistCreated] ExtraInfoIdList empty Id={Id}", whitelistId);
        }
    }
}