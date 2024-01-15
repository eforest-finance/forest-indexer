using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Whitelist;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class UpdateExtraInfoProcessor : AElfLogEventProcessorBase<ExtraInfoUpdated, LogEventInfo>
{
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<AElfLogEventProcessorBase<ExtraInfoUpdated, LogEventInfo>> _logger;

    private readonly IAElfIndexerClientEntityRepository<WhiteListExtraInfoIndex, LogEventInfo>
        _whitelistExtraInfoIndexRepository;

    public UpdateExtraInfoProcessor(ILogger<AElfLogEventProcessorBase<ExtraInfoUpdated, LogEventInfo>> logger,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions, IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<WhiteListExtraInfoIndex, LogEventInfo> whitelistExtraInfoIndexRepository) :
        base(logger)
    {
        _contractInfoOptions = contractInfoOptions.Value;
        _whitelistExtraInfoIndexRepository = whitelistExtraInfoIndexRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).WhitelistContractAddress;
    }


    protected override async Task HandleEventAsync(ExtraInfoUpdated eventValue, LogEventContext context)
    {
        _logger.LogInformation("[ExtraInfoUpdated] WHITELIST ID:{whitelistId}", eventValue.WhitelistId.ToHex());
        var oldTagInfoId = "";
        if (eventValue.ExtraInfoIdBefore.Id != null)
        {
            _logger.LogInformation("[ExtraInfoUpdated] Whitelist ExtraInfoIdBefore ID: {ExtraInfoIdBefore}",
                eventValue.ExtraInfoIdBefore.Id.ToHex());
            oldTagInfoId = eventValue.ExtraInfoIdBefore.Id.ToHex();
        }

        var newTagInfoId = "";
        if (eventValue.ExtraInfoIdAfter.Id != null)
        {
            _logger.LogInformation("[ExtraInfoUpdated] Whitelist extraInfoIdAfter ID: {ExtraInfoIdAfter}",
                eventValue.ExtraInfoIdAfter.Id.ToHex());
            newTagInfoId = eventValue.ExtraInfoIdAfter.Id.ToHex();
        }

        var addresses = eventValue.ExtraInfoIdBefore.AddressList.Value.Select(o => o.ToBase58()).ToList();
        _logger.LogInformation("[ExtraInfoUpdated] addresses count:{Count}", addresses.Count);
        foreach (var address in addresses)
        {
            if (!string.IsNullOrEmpty(oldTagInfoId))
            {
                var extraInfoId = IdGenerateHelper.GetId(context.ChainId, oldTagInfoId, address);
                var beforeExtraInfo = await _whitelistExtraInfoIndexRepository
                    .GetFromBlockStateSetAsync(extraInfoId, context.ChainId);

                if (beforeExtraInfo != null)
                {
                    _logger.LogInformation("[ExtraInfoUpdated][Delete]TagInfoId={TagInfoId} ExtraInfoId={ExtraInfoId}",
                        extraInfoId, extraInfoId);
                    _objectMapper.Map(context, beforeExtraInfo);
                    await _whitelistExtraInfoIndexRepository.DeleteAsync(beforeExtraInfo);
                }
            }

            if (!string.IsNullOrEmpty(newTagInfoId))
            {
                var extraInfo = new WhiteListExtraInfoIndex()
                {
                    Id = IdGenerateHelper.GetId(context.ChainId, newTagInfoId, address),
                    WhitelistInfoId = eventValue.WhitelistId.ToHex(),
                    Address = address,
                    TagInfoId = newTagInfoId,
                };

                await _whitelistExtraInfoIndexRepository.GetFromBlockStateSetAsync(extraInfo.Id, context.ChainId);
                _logger.LogInformation("[ExtraInfoUpdated][Update]TagInfoId={TagInfoId}, extraInfo={extraInfo}",
                    extraInfo.TagInfoId, extraInfo.Id);

                _objectMapper.Map(context, extraInfo);
                await _whitelistExtraInfoIndexRepository.AddOrUpdateAsync(extraInfo);
            }
        }
    }
}