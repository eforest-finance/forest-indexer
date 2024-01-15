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

public class WhitelistResetLogEventProcessor : AElfLogEventProcessorBase<WhitelistReset, LogEventInfo>
{
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<WhitelistIndex, LogEventInfo> _whitelistIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<TagInfoIndex, LogEventInfo> _tagInfoIndexRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<AElfLogEventProcessorBase<WhitelistReset, LogEventInfo>> _logger;

    private readonly IAElfIndexerClientEntityRepository<WhiteListExtraInfoIndex, LogEventInfo>
        _whitelistExtraInfoIndexRepository;

    private readonly IWhiteListProvider _whitelistProvider;


    public WhitelistResetLogEventProcessor(ILogger<AElfLogEventProcessorBase<WhitelistReset, LogEventInfo>> logger,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        IAElfIndexerClientEntityRepository<WhitelistIndex, LogEventInfo> whitelistIndexRepository,
        IAElfIndexerClientEntityRepository<TagInfoIndex, LogEventInfo> tagInfoIndexRepository,
        IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<WhiteListExtraInfoIndex, LogEventInfo> whitelistExtraInfoIndexRepository,
        IWhiteListProvider whitelistProvider) :
        base(logger)
    {
        _contractInfoOptions = contractInfoOptions.Value;
        _logger = logger;
        _whitelistIndexRepository = whitelistIndexRepository;
        _tagInfoIndexRepository = tagInfoIndexRepository;
        _objectMapper = objectMapper;
        _whitelistExtraInfoIndexRepository = whitelistExtraInfoIndexRepository;
        _whitelistProvider = whitelistProvider;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).WhitelistContractAddress;
    }

    protected override async Task HandleEventAsync(WhitelistReset eventValue, LogEventContext context)
    {
        // remove whiteListIndex from es.
        var whitelistId = eventValue.WhitelistId.ToHex();
        _logger.Debug("[WhitelistReset] START: Id={Id}, event={Event}", whitelistId,
            JsonConvert.SerializeObject(eventValue));
        try
        {
            var whitelist = await _whitelistIndexRepository.GetFromBlockStateSetAsync(whitelistId, context.ChainId);
            if (whitelist == null) throw new UserFriendlyException("whitelist NOT FOUND");


            whitelist.ProjectId = eventValue.ProjectId.ToHex();

            _objectMapper.Map(context, whitelist);
            await _whitelistIndexRepository.AddOrUpdateAsync(whitelist);

            // remove whiteListExtraInfoIndex from es by whiteListId.
            await _whitelistProvider.RemoveExtraInfosAsync(context, whitelistId);

            // remove tagInfoIndex from es by whiteListId.
            await _whitelistProvider.RemoveTagInfosAsync(context, whitelistId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[WhitelistReset] ERROR: whitelistExtraInfo exists Id={Id}", whitelistId);
            throw;
        }
    }
}