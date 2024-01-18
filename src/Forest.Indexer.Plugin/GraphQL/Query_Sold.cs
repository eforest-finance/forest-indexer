using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Nest;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("getNftDealInfos")]
    public static async Task<NftDealInfoDtoPageResultDto> GetNftDealInfosAsync(
        [FromServices] IAElfIndexerClientEntityRepository<SoldIndex, LogEventInfo> repository,
        [FromServices] IObjectMapper objectMapper,
        GetNftDealInfoDto dto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SoldIndex>, QueryContainer>>();

        if (!dto.ChainId.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.ChainId).Value(dto.ChainId)));
        }

        if (!dto.Symbol.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.NftSymbol).Value(dto.Symbol)));
        }
        
        if (!dto.CollectionSymbol.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.CollectionSymbol).Value(dto.CollectionSymbol)));
        }

        QueryContainer Filter(QueryContainerDescriptor<SoldIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var result = await repository.GetSortListAsync(Filter, skip: dto.SkipCount,
            limit: dto.MaxResultCount,
            sortFunc: GetSortFunc());
        var pageResult = new NftDealInfoDtoPageResultDto
        {
            TotalRecordCount = result.Item1,
            Data = objectMapper.Map<List<SoldIndex>, List<NftDealInfoDto>>(result.Item2)
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