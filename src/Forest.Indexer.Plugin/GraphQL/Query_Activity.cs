using System.Linq.Dynamic.Core;
using AeFinder.Sdk;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Nest;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("symbolMarketActivities")]
    public static async Task<SymbolMarkerActivityPageResultDto> SymbolMarketActivities(
        [FromServices] IReadOnlyRepository<SymbolMarketActivityIndex> symbolMarketActivityIndexRepository,
        [FromServices] IObjectMapper objectMapper,
        GetActivitiesInput dto)
    {
        if (dto == null)
        {
            return new SymbolMarkerActivityPageResultDto()
            {
                TotalRecordCount = 0,
                Data = new List<SymbolMarkerActivityDto>()
            };
        }

        var queryable = await symbolMarketActivityIndexRepository.GetQueryableAsync();
        
        queryable = queryable.Where(q => dto.Address.Contains(q.Address) );
        if (!dto.Types.IsNullOrEmpty())
        {
            var intTypes = dto.Types.Select(i => (int)i).ToList();
            queryable = queryable.Where(q => intTypes.Contains(q.IntType));
            
        }
        var result = queryable.OrderByDescending(k=>k.TransactionDateTime).Skip(dto.SkipCount).Take(dto.MaxResultCount).ToList();
        
        var dataList = objectMapper.Map<List<SymbolMarketActivityIndex>, List<SymbolMarkerActivityDto>>(result);
        var pageResult = new SymbolMarkerActivityPageResultDto
        {
            TotalRecordCount = result.Count,
            Data = dataList
        };
        return pageResult;
    }
}