using System.Linq.Dynamic.Core;
using AeFinder.Sdk;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("getTsmSeedInfos")]
    public static async Task<List<SeedInfoDto>> GetTsmSeedInfosAsync(
        [FromServices] IReadOnlyRepository<TsmSeedSymbolIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetChainBlockHeightDto dto
    )
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

        var result = queryable.OrderBy(o => o.BlockHeight).Skip(0).Take(QueryCurrentSize).ToList();
        if (result.IsNullOrEmpty())
        {
            return new List<SeedInfoDto>();
        }

        var dataList = objectMapper.Map<List<TsmSeedSymbolIndex>, List<SeedInfoDto>>(result);
        
        return dataList;
    }
    
    [Name("seedMainChainChange")]
    public static async Task<SeedMainChainChangePageResultDto> SeedMainChainChangeAsync(
        [FromServices] IReadOnlyRepository<SeedMainChainChangeIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetSeedMainChainChangeDto dto)
    {
        var queryable = await repository.GetQueryableAsync();
        queryable = queryable.Where(f => f.ChainId == dto.ChainId);
        queryable = queryable.Where(f => f.BlockHeight >= dto.BlockHeight);

        var result = queryable.OrderBy(o => o.BlockHeight).Skip(dto.SkipCount).Take(ForestIndexerConstants.DefaultMaxCountNumber).ToList();
        var dataList = objectMapper.Map<List<SeedMainChainChangeIndex>, List<SeedMainChainChangeDto>>(result);
        var pageResult = new SeedMainChainChangePageResultDto
        {
            TotalRecordCount = result.Count,
            Data = dataList
        };
        return pageResult;
    }
}