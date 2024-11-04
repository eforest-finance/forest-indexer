using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Newtonsoft.Json;
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
        Logger.LogInformation("getSyncTreePointsRecords params:{A}", JsonConvert.SerializeObject(dto));
        var queryable = await repository.GetQueryableAsync();
        
        if (dto.StartBlockHeight > 0)
        {
            queryable = queryable.Where(f => f.BlockHeight >= dto.StartBlockHeight);
        }

        if (dto.EndBlockHeight > 0)
        {
            queryable = queryable.Where(f => f.BlockHeight <= dto.EndBlockHeight);
        }

        var result = queryable.OrderBy(o => o.BlockHeight).OrderBy(i=>i.BlockHeight).Skip(0).Take(2000).ToList();
        Logger.LogInformation("getSyncTreePointsRecords resultCount:{A}", result.IsNullOrEmpty()?0:result.Count());

        if (result.IsNullOrEmpty() || result.Count == 0)
        {
            return new TreePointsChangeRecordPageResultDto()
            {
                TotalRecordCount = 0,
                Data = new List<TreePointsChangeRecordDto>()
            };
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
    
    [Name("getSyncTreePointsRecordsAll")]
    public static async Task<TreePointsChangeRecordPageResultDto> GetSyncTreePointsRecordsAll(
        [FromServices] IReadOnlyRepository<TreePointsChangeRecordIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetChainBlockHeightDto dto)
    {
        Logger.LogInformation("getSyncTreePointsRecordsAll params:{A}", JsonConvert.SerializeObject(dto));

        var queryable = await repository.GetQueryableAsync();
        var result = queryable.ToList();
        if (result.IsNullOrEmpty())
        {
            return new TreePointsChangeRecordPageResultDto();
        }
        var count = queryable.Count();

        var dataList = objectMapper.Map<List<TreePointsChangeRecordIndex>, List<TreePointsChangeRecordDto>>(result);
        Logger.LogInformation("getSyncTreePointsRecordsAll resultCount:{A}", dataList.IsNullOrEmpty()?0:dataList.Count());

        var totalCount = count;

        return new TreePointsChangeRecordPageResultDto
        {
            Data = dataList,
            TotalRecordCount = (long)(totalCount == null ? 0 : totalCount),
        };
    }
}