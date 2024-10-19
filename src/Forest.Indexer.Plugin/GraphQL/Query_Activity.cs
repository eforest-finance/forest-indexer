using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Nest;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("symbolMarketActivities")]
    public static async Task<SymbolMarkerActivityPageResultDto> SymbolMarketActivities(
        [FromServices]
        IAElfIndexerClientEntityRepository<SymbolMarketActivityIndex, LogEventInfo> symbolMarketActivityIndexRepository,
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

        var mustQuery = new List<Func<QueryContainerDescriptor<SymbolMarketActivityIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Address).Terms(dto.Address)));
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Type).Terms(dto.Types)));
        QueryContainer Filter(QueryContainerDescriptor<SymbolMarketActivityIndex> f) => f.Bool(b => b.Must(mustQuery));
        var result = await symbolMarketActivityIndexRepository.GetListAsync(Filter, sortExp: k => k.TransactionDateTime,
            sortType: SortOrder.Descending, skip: dto.SkipCount, limit: dto.MaxResultCount);
        var dataList = objectMapper.Map<List<SymbolMarketActivityIndex>, List<SymbolMarkerActivityDto>>(result.Item2);
        var pageResult = new SymbolMarkerActivityPageResultDto
        {
            TotalRecordCount = result.Item1,
            Data = dataList
        };
        return pageResult;
    }
}