using AeFinder.Sdk;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("getSyncTreePointsRecords")]
    public static async Task<TreePointsChangeRecordPageResultDto> GetSyncTreePointsRecords(
        [FromServices] IReadOnlyRepository<TreePointsChangeRecordIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetChainBlockHeightDto dto)
    {
        var queryable = await repository.GetQueryableAsync();
        queryable = queryable.Where(f => f.ChainId == dto.ChainId);
        
        if (dto.StartBlockHeight > 0)
        {
            queryable = queryable.Where(f => f.BlockHeight >= dto.StartBlockHeight);
        }

        if (dto.EndBlockHeight > 0)
        {
            queryable = queryable.Where(f => f.BlockHeight <= dto.EndBlockHeight);
        }

        var result = queryable.OrderBy(o => o.BlockHeight).Skip(0).Take(5000).ToList();
        if (result.IsNullOrEmpty())
        {
            return new TreePointsChangeRecordPageResultDto();
        }
        var count = queryable.Count();

        var dataList = objectMapper.Map<List<TreePointsChangeRecordIndex>, List<TreePointsChangeRecordDto>>(result);
        var totalCount = count;

        return new TreePointsChangeRecordPageResultDto
        {
            Data = dataList,
            TotalRecordCount = (long)(totalCount == null ? 0 : totalCount),
        };
    }
}