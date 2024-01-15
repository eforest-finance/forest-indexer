using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Nest;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("getTsmSeedInfos")]
    public static async Task<List<SeedInfoDto>> GetTsmSeedInfosAsync(
        [FromServices] IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> repository,
        [FromServices] IObjectMapper objectMapper,
        GetChainBlockHeightDto dto
    )
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i
            => i.Field(f => f.ChainId).Value(dto.ChainId)));

        if (dto.StartBlockHeight > 0)
        {
            mustQuery.Add(q => q.Range(i
                => i.Field(f => f.BlockHeight).GreaterThanOrEquals(dto.StartBlockHeight)));
        }

        if (dto.EndBlockHeight > 0)
        {
            mustQuery.Add(q => q.Range(i
                => i.Field(f => f.BlockHeight).LessThanOrEquals(dto.EndBlockHeight)));
        }

        QueryContainer Filter(QueryContainerDescriptor<TsmSeedSymbolIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var result = await repository.GetListAsync(Filter, skip: 0, limit: QueryCurrentSize, sortType: SortOrder.Ascending, sortExp: o => o.BlockHeight);
        
        var dataList = objectMapper.Map<List<TsmSeedSymbolIndex>, List<SeedInfoDto>>(result.Item2);
        
        return dataList;
    }
    
    [Name("seedMainChainChange")]
    public static async Task<SeedMainChainChangePageResultDto> SeedMainChainChange(
        [FromServices] IAElfIndexerClientEntityRepository<SeedMainChainChangeIndex, LogEventInfo> repository,
        [FromServices] IObjectMapper objectMapper,
        GetSeedMainChainChangeDto dto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SeedMainChainChangeIndex>, QueryContainer>>()
        {
            q => q.Term(i
                => i.Field(f => f.ChainId).Value(dto.ChainId))
        };

        if (dto.SkipCount == 0)
        {
            mustQuery.Add(q => q.Range(i
                => i.Field(f => f.BlockHeight).GreaterThan(dto.BlockHeight)));
        }
        else
        {
            mustQuery.Add(q => q.Range(i
                => i.Field(f => f.BlockHeight).GreaterThanOrEquals(dto.BlockHeight)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<SeedMainChainChangeIndex> f) => 
            f.Bool(b => b.Must(mustQuery));
        var result = await repository.GetListAsync(Filter, skip:dto.SkipCount, sortExp: o => o.BlockHeight);
        var dataList = objectMapper.Map<List<SeedMainChainChangeIndex>, List<SeedMainChainChangeDto>>(result.Item2);
        var pageResult = new SeedMainChainChangePageResultDto
        {
            TotalRecordCount = result.Item1,
            Data = dataList
        };
        return pageResult;
    }
}