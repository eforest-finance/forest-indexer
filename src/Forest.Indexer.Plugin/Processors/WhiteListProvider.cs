using AElf.Types;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Whitelist;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public interface IWhiteListProvider
{
    public Task<List<WhiteListExtraInfoIndex>> AddWhiteListExtraInfoAsync(LogEventContext context,
        List<ExtraInfoId> extraInfoIdList, string chainId, string whiteListId);

    public Task<List<WhiteListExtraInfoIndex>> RemoveWhiteListExtraInfoAsync(LogEventContext context,
        List<ExtraInfoId> extraInfoIdList, string chainId, string whiteListId);

    public Task AddManagersAsync(LogEventContext context, Hash whitelistId, List<string> whitelistManagers);
    public Task RemoveExtraInfosAsync(LogEventContext context, string whitelistId);
    public Task RemoveTagInfosAsync(LogEventContext context, string whitelistId);
}

public class WhiteListProvider : IWhiteListProvider, ISingletonDependency
{
    private ILogger<WhiteListProvider> _logger;
    private readonly IAElfIndexerClientEntityRepository<TagInfoIndex, LogEventInfo> _tagInfoIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<WhiteListManagerIndex, LogEventInfo>
        _whitelistManagerIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<WhiteListExtraInfoIndex, LogEventInfo>
        _whitelistExtraInfoIndexRepository;

    private readonly IObjectMapper _objectMapper;

    public WhiteListProvider(
        IAElfIndexerClientEntityRepository<TagInfoIndex, LogEventInfo> tagInfoIndexRepository,
        IAElfIndexerClientEntityRepository<WhiteListManagerIndex, LogEventInfo> whitelistManagerIndexRepository,
        IAElfIndexerClientEntityRepository<WhiteListExtraInfoIndex, LogEventInfo> whitelistExtraInfoIndexRepository,
        IObjectMapper objectMapper, ILogger<WhiteListProvider> logger)
    {
        _tagInfoIndexRepository = tagInfoIndexRepository;
        _whitelistManagerIndexRepository = whitelistManagerIndexRepository;
        _whitelistExtraInfoIndexRepository = whitelistExtraInfoIndexRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task<List<WhiteListExtraInfoIndex>> AddWhiteListExtraInfoAsync(LogEventContext context,
        List<ExtraInfoId> extraInfoIdList, string chainId,
        string whiteListId)
    {
        var extraInfos = await GenerateExtraInfoAsync(extraInfoIdList, chainId, whiteListId);
        await AddExtraInfosAsync(context, extraInfos, chainId, whiteListId);
        return extraInfos;
    }

    public async Task<List<WhiteListExtraInfoIndex>> RemoveWhiteListExtraInfoAsync(LogEventContext context,
        List<ExtraInfoId> extraInfoIdList, string chainId,
        string whiteListId)
    {
        var extraInfos = await GenerateExtraInfoAsync(extraInfoIdList, chainId, whiteListId);
        await RemoveExtraInfosAsync(context, extraInfos, chainId, whiteListId);
        return extraInfos;
    }

    public async Task AddManagersAsync(LogEventContext context, Hash whitelistId, List<string> whitelistManagers)
    {
        foreach (var whitelistManager in whitelistManagers)
        {
            var index = new WhiteListManagerIndex()
            {
                Id = IdGenerateHelper.GetId(context.ChainId, whitelistId.ToHex(), whitelistManager),
                WhitelistInfoId = whitelistId.ToHex(),
                Manager = whitelistManager,
            };

            // copy block data
            _objectMapper.Map(context, index);
            await _whitelistManagerIndexRepository.AddOrUpdateAsync(index);
        }
    }

    public async Task RemoveExtraInfosAsync(LogEventContext context, string whitelistId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<WhiteListExtraInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(context.ChainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.WhitelistInfoId).Value(whitelistId)));

        QueryContainer Filter(QueryContainerDescriptor<WhiteListExtraInfoIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var (totalCount, extraInfoList) = await _whitelistExtraInfoIndexRepository.GetListAsync(Filter);
        _logger.LogInformation("[RemoveExtraInfosAsync] DO_DELETE: extraInfoList totalCount:{totalCount}", totalCount);
        foreach (var extraInfo in extraInfoList)
        {
            _logger.LogInformation("[RemoveExtraInfosAsync] DO_DELETE: extraInfoIndex={indexId}", extraInfo.Id);
            var extraInfoData =
                await _whitelistExtraInfoIndexRepository.GetFromBlockStateSetAsync(extraInfo.Id, context.ChainId);
            if (extraInfoData == null) continue;
            _objectMapper.Map(context, extraInfoData);
            await _whitelistExtraInfoIndexRepository.DeleteAsync(extraInfoData);
        }
    }

    private async Task RemoveExtraInfosAsync(LogEventContext context, List<WhiteListExtraInfoIndex> input,
        string chainId, string whiteListId)
    {
        foreach (var extraInfoIndex in input)
        {
            _objectMapper.Map(context, extraInfoIndex);
            _logger.Debug("[RemoveExtraInfosAsync] DO_DELETE: extraInfoIndex={indexId}", extraInfoIndex);
            await _whitelistExtraInfoIndexRepository.DeleteAsync(extraInfoIndex);
        }

        var gList = input.Where(o => !o.TagInfoId.IsNullOrEmpty()).GroupBy(o => o.TagInfoId).ToList();
        foreach (var g in gList)
        {
            var tagInfoIndexId = IdGenerateHelper.GetId(chainId, g.Key);
            var tagInfoIndex = await _tagInfoIndexRepository.GetFromBlockStateSetAsync(tagInfoIndexId, chainId);
            if (tagInfoIndex == null) continue;
            _objectMapper.Map(context, tagInfoIndex);
            await _tagInfoIndexRepository.AddOrUpdateAsync(tagInfoIndex);
        }
    }

    public async Task RemoveTagInfosAsync(LogEventContext context, string whitelistId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TagInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(context.ChainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.WhitelistInfoId).Value(whitelistId)));

        QueryContainer Filter(QueryContainerDescriptor<TagInfoIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var (totalCount, tagInfoList) = await _tagInfoIndexRepository.GetListAsync(Filter);
        _logger.LogInformation("[RemoveTagInfosAsync] DO_DELETE: tagInfoList totalCount:{totalCount}", totalCount);
        foreach (var tagInfo in tagInfoList)
        {
            _logger.LogInformation("[RemoveTagInfosAsync] DO_DELETE: tagInfoIndex={indexId}", tagInfo.Id);
            var tagInfoData = await _tagInfoIndexRepository.GetFromBlockStateSetAsync(tagInfo.Id, context.ChainId);
            if (tagInfoData == null) continue;
            _objectMapper.Map(context, tagInfoData);
            await _tagInfoIndexRepository.DeleteAsync(tagInfoData);
        }
    }

    private async Task<List<WhiteListExtraInfoIndex>> GenerateExtraInfoAsync(List<ExtraInfoId> extraInfoIdList,
        string chainId, string whiteListId)
    {
        _logger.Debug("[GenerateExtraInfoAsync] START : whiteListId={whiteListId}, chainId={chainId}",
            whiteListId, chainId);
        var extraInfos = new List<WhiteListExtraInfoIndex>();
        foreach (var extraInfo in extraInfoIdList)
        {
            var tagIdHex = extraInfo.Id == null ? null : extraInfo.Id.ToHex();
            if (tagIdHex == null) continue;

            extraInfos.AddRange(extraInfo.AddressList.Value.Select(address => new WhiteListExtraInfoIndex
            {
                Id = IdGenerateHelper.GetId(chainId, tagIdHex, address.ToBase58()),
                Address = address.ToBase58(),
                TagInfoId = tagIdHex
            }).ToList());
        }

        _logger.Debug("[GenerateExtraInfoAsync] FINISH : extraInfos={json}",
            JsonConvert.SerializeObject(extraInfos));
        return extraInfos;
    }

    private async Task AddExtraInfosAsync(LogEventContext context, List<WhiteListExtraInfoIndex> input, string chainId,
        string whiteListId)
    {
        foreach (var extraInfoIndex in input)
        {
            extraInfoIndex.ChainId = chainId;
            extraInfoIndex.WhitelistInfoId = whiteListId;
            extraInfoIndex.LastModifyTime = DateTimeHelper.GetTimeStampInMilliseconds();

            // copy block data to index
            _objectMapper.Map(context, extraInfoIndex);
            await _whitelistExtraInfoIndexRepository.AddOrUpdateAsync(extraInfoIndex);
        }


        var gList = input.Where(o => !o.TagInfoId.IsNullOrEmpty()).GroupBy(o => o.TagInfoId).ToList();
        foreach (var g in gList)
        {
            var tagInfoIndexId = IdGenerateHelper.GetId(chainId, whiteListId);
            var tagInfoIndex = await _tagInfoIndexRepository.GetFromBlockStateSetAsync(tagInfoIndexId, chainId);
            if (tagInfoIndex == null) continue;

            // copy block data to index
            _objectMapper.Map(context, tagInfoIndex);
            await _tagInfoIndexRepository.AddOrUpdateAsync(tagInfoIndex);
        }
    }
}