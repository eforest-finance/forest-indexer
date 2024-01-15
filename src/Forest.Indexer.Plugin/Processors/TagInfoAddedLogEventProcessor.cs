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

public class TagInfoAddedLogEventProcessor : AElfLogEventProcessorBase<TagInfoAdded, LogEventInfo>
{
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<WhitelistIndex, LogEventInfo> _whitelistIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<TagInfoIndex, LogEventInfo> _tagInfoIndexRepository;
    private ILogger<AElfLogEventProcessorBase<TagInfoAdded, LogEventInfo>> _logger;
    private readonly IObjectMapper _objectMapper;


    public TagInfoAddedLogEventProcessor(
        ILogger<AElfLogEventProcessorBase<TagInfoAdded, LogEventInfo>> logger,
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

    protected override async Task HandleEventAsync(TagInfoAdded eventValue, LogEventContext context)
    {
        var tagInfoIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.TagInfoId.ToHex());
        _logger.Debug("[TagInfoAdded] START: Id={TagInfoIndexId}", tagInfoIndexId);

        var whitelistId = eventValue.WhitelistId.ToHex();

        var tagInfoIndex = await _tagInfoIndexRepository.GetFromBlockStateSetAsync(tagInfoIndexId, context.ChainId);
        if (tagInfoIndex != null)
        {
            _logger.Debug("[TagInfoAdded] FAIL: tagInfoIndex exists Id={TagInfoIndexId}", tagInfoIndexId);
            return;
        }

        tagInfoIndex = _objectMapper.Map<TagInfoAdded, TagInfoIndex>(eventValue);
        tagInfoIndex.Id = tagInfoIndexId;
        tagInfoIndex.WhitelistId = whitelistId;
        tagInfoIndex.ChainId = context.ChainId;
        tagInfoIndex.WhitelistInfoId = eventValue.WhitelistId.ToHex();
        tagInfoIndex.Name = eventValue.TagInfo.TagName;
        tagInfoIndex.TagHash = eventValue.TagInfoId.ToHex();
        tagInfoIndex.Info = eventValue.TagInfo.Info.ToBase64();
        tagInfoIndex.LastModifyTime = DateTimeHelper.GetTimeStampInMilliseconds();

        _objectMapper.Map(context, tagInfoIndex);

        _logger.Debug("[TagInfoAdded] SAVE: Id={TagInfoIndexId}", tagInfoIndexId);

        await _tagInfoIndexRepository.AddOrUpdateAsync(tagInfoIndex);

        _logger.Debug("[TagInfoAdded] FINISH: Id={TagInfoIndexId}", tagInfoIndexId);
    }
}