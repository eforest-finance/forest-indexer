using AElfIndexer.Client;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    private const int QueryCurrentSize = 1000;

    [Name("getSymbolAuctionInfos")]
    public static async Task<List<SymbolAuctionInfoDto>> GetSymbolAuctionInfoAsync(
        [FromServices] IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo> repository,
        [FromServices] IObjectMapper objectMapper,
        GetChainBlockHeightDto dto
    )
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SymbolAuctionInfoIndex>, QueryContainer>>();
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

        QueryContainer Filter(QueryContainerDescriptor<SymbolAuctionInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var result = await repository.GetListAsync(Filter, skip: 0, limit: QueryCurrentSize, sortType: SortOrder.Ascending, sortExp: o => o.BlockHeight);
        return objectMapper.Map<List<SymbolAuctionInfoIndex>, List<SymbolAuctionInfoDto>>(result.Item2);
    }


    [Name("getSymbolBidInfos")]
    public static async Task<List<SymbolBidInfoDto>> GetSymbolBidInfosAsync(
        [FromServices] IAElfIndexerClientEntityRepository<SymbolBidInfoIndex, LogEventInfo> repository,
        [FromServices] IObjectMapper objectMapper,
        [FromServices] ILogger<SymbolBidInfoIndex> logger,
        GetChainBlockHeightDto dto
    )
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SymbolBidInfoIndex>, QueryContainer>>();
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

        QueryContainer Filter(QueryContainerDescriptor<SymbolBidInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var result = await repository.GetListAsync(Filter, skip: 0, limit: QueryCurrentSize, sortType: SortOrder.Ascending, sortExp: o => o.BlockHeight);
        var symbolBidInfoIndices = result.Item2;
        
        return objectMapper.Map<List<SymbolBidInfoIndex>, List<SymbolBidInfoDto>>(symbolBidInfoIndices);
    }
}