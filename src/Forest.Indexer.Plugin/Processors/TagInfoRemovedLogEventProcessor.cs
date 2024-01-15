using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Whitelist;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class TagInfoRemovedLogEventProcessor : AElfLogEventProcessorBase<TagInfoRemoved, LogEventInfo>
{
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<WhitelistIndex, LogEventInfo> _whitelistIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<TagInfoIndex, LogEventInfo> _tagInfoIndexRepository;
    private ILogger<AElfLogEventProcessorBase<TagInfoRemoved, LogEventInfo>> _logger;
    private readonly IObjectMapper _objectMapper;


    public TagInfoRemovedLogEventProcessor(
        ILogger<AElfLogEventProcessorBase<TagInfoRemoved, LogEventInfo>> logger,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        IAElfIndexerClientEntityRepository<WhitelistIndex, LogEventInfo> whitelistIndexRepository,
        IAElfIndexerClientEntityRepository<TagInfoIndex, LogEventInfo> tagInfoIndexRepository,
        IObjectMapper objectMapper) : base(logger)
    {
        _whitelistIndexRepository = whitelistIndexRepository;
        _tagInfoIndexRepository = tagInfoIndexRepository;
        _objectMapper = objectMapper;
        _logger = logger;
        _contractInfoOptions = contractInfoOptions.Value;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).WhitelistContractAddress;
    }

    protected override async Task HandleEventAsync(TagInfoRemoved eventValue, LogEventContext context)
    {
        var tagInfoIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.TagInfoId.ToHex());

        _logger.Debug("[TagInfoRemoved] START: Id={TagInfoIndexId}", tagInfoIndexId);

        var tagInfoIndex = await _tagInfoIndexRepository.GetFromBlockStateSetAsync(tagInfoIndexId, context.ChainId);
        if (tagInfoIndex == null)
        {
            _logger.Debug("[TagInfoRemoved] FAIL: tagInfoIndex is null Id={TagInfoIndexId}", tagInfoIndexId);
            return;
        }

        _objectMapper.Map(context, tagInfoIndex);

        _logger.Debug("[TagInfoRemoved] SAVE: Id={TagInfoIndexId}", tagInfoIndexId);

        await _tagInfoIndexRepository.DeleteAsync(tagInfoIndex);

        _logger.Debug("[TagInfoRemoved] FINISH: Id={TagInfoIndexId}", tagInfoIndexId);
    }
}