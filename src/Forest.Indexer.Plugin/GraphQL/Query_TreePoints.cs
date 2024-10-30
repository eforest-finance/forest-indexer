using AeFinder.Sdk;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("getSyncTreePointsRecords")]
    public static async Task<List<TreePointsChangeRecordDto>> GetSyncTreePointsRecords(
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

        var result = queryable.OrderBy(o => o.BlockHeight).ToList();
        if (result.IsNullOrEmpty())
        {
            return new List<TreePointsChangeRecordDto>();
        }

        return objectMapper.Map<List<TreePointsChangeRecordIndex>, List<TreePointsChangeRecordDto>>(result);
    }
}