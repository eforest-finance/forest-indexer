using AeFinder.Sdk;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors;
using GraphQL;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    
    
    [Name("getSeedPriceInfos")]
    public static async Task<List<SeedPriceDto>> GetSeedPriceInfosAsync(
        [FromServices] IReadOnlyRepository<SeedPriceIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetChainBlockHeightDto dto
    )
    {
        var queryable = await repository.GetQueryableAsync();

        queryable = queryable.Where(f=>f.ChainId == dto.ChainId);

        if (dto.StartBlockHeight > 0)
        {
            queryable = queryable.Where(f=>f.BlockHeight >= dto.StartBlockHeight);
        }

        if (dto.EndBlockHeight > 0)
        {
            queryable = queryable.Where(f=>f.BlockHeight <= dto.EndBlockHeight);
        }

        var result = queryable.OrderBy(o => o.BlockHeight).Skip(0).Take(QueryCurrentSize).ToList();
        if (result.IsNullOrEmpty())
        {
            return new List<SeedPriceDto>();
        }
        return objectMapper.Map<List<SeedPriceIndex>, List<SeedPriceDto>>(result);
    }

    
    [Name("getUniqueSeedPriceInfos")]
    public static async Task<List<UniqueSeedPriceDto>> GetUniqueSeedPriceInfosAsync(
        [FromServices] IReadOnlyRepository<UniqueSeedPriceIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetChainBlockHeightDto dto
    )
    {
        var queryable = await repository.GetQueryableAsync();
        queryable = queryable.Where(f=>f.ChainId == dto.ChainId);

        if (dto.StartBlockHeight > 0)
        {
            queryable = queryable.Where(f=>f.BlockHeight >= dto.StartBlockHeight);
        }

        if (dto.EndBlockHeight > 0)
        {
            queryable = queryable.Where(f=>f.BlockHeight <= dto.EndBlockHeight);
        }

        var result = queryable.OrderBy(o => o.BlockHeight).Skip(0).Take(QueryCurrentSize).ToList();
        if (result.IsNullOrEmpty())
        {
            return new List<UniqueSeedPriceDto>();
        }
        return objectMapper.Map<List<UniqueSeedPriceIndex>, List<UniqueSeedPriceDto>>(result);
    }
}