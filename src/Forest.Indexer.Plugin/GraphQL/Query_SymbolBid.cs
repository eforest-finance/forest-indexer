using AeFinder.Sdk;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    private const int QueryCurrentSize = 1000;

    [Name("getSymbolAuctionInfos")]
    public static async Task<List<SymbolAuctionInfoDto>> GetSymbolAuctionInfoAsync(
        [FromServices] IReadOnlyRepository<SymbolAuctionInfoIndex> repository,
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
            return new List<SymbolAuctionInfoDto>();
        }

        return objectMapper.Map<List<SymbolAuctionInfoIndex>, List<SymbolAuctionInfoDto>>(result);
    }


    [Name("getSymbolBidInfos")]
    public static async Task<List<SymbolBidInfoDto>> GetSymbolBidInfosAsync(
        [FromServices] IReadOnlyRepository<SymbolBidInfoIndex> repository,
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
        var symbolBidInfoIndices = result;
        if (symbolBidInfoIndices.IsNullOrEmpty())
        {
            return new List<SymbolBidInfoDto>();
        }

        return objectMapper.Map<List<SymbolBidInfoIndex>, List<SymbolBidInfoDto>>(symbolBidInfoIndices);
    }
}