using AeFinder.Sdk;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Nest;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("getNftDealInfos")]
    public static async Task<NftDealInfoDtoPageResultDto> GetNftDealInfosAsync(
        [FromServices] IReadOnlyRepository<SoldIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetNftDealInfoDto dto)
    {
        var queryable = await repository.GetQueryableAsync();
        if (!dto.ChainId.IsNullOrEmpty())
        {
            queryable = queryable.Where(f=>f.ChainId == dto.ChainId);
        }

        if (!dto.Symbol.IsNullOrEmpty())
        {
            queryable = queryable.Where(f=>f.NftSymbol == dto.Symbol);
        }
        
        if (!dto.CollectionSymbol.IsNullOrEmpty())
        {
            queryable = queryable.Where(f=>f.CollectionSymbol == dto.CollectionSymbol);
        }

        var result = queryable.Skip(dto.SkipCount).Take(dto.MaxResultCount)
            .OrderByDescending(a => a.DealTime)
            .OrderByDescending(a => a.PurchaseAmount)
            .ToList();
        if (result.IsNullOrEmpty())
        {
            return new NftDealInfoDtoPageResultDto();
        }

        var pageResult = new NftDealInfoDtoPageResultDto
        {
            TotalRecordCount = result.Count,
            Data = objectMapper.Map<List<SoldIndex>, List<NftDealInfoDto>>(result)
        };
        return pageResult;
    }

    private static Func<SortDescriptor<SoldIndex>, IPromise<IList<ISort>>> GetSortFunc()
    {
        SortDescriptor<SoldIndex> sortDescriptor = new SortDescriptor<SoldIndex>();
        sortDescriptor.Descending(a => a.DealTime)
            .Descending(a => a.PurchaseAmount);
        return s => sortDescriptor;
    }
}