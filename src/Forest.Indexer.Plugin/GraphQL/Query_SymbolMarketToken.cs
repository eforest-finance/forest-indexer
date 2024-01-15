using AElf;
using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Nest;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("symbolMarketTokens")]
    public static async Task<SymbolMarkerTokenPageResultDto> SymbolMarketTokens(
        [FromServices]
        IAElfIndexerClientEntityRepository<SeedSymbolMarketTokenIndex, LogEventInfo> symbolMarketTokenIndexRepository,
        [FromServices] IObjectMapper objectMapper,
        GetSymbolMarketTokensInput dto)
    {
        if (dto == null)
        {
            return new SymbolMarkerTokenPageResultDto()
            {
                TotalRecordCount = 0,
                Data = new List<SymbolMarkerTokenDto>()
            };
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<SeedSymbolMarketTokenIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Exists(exists => exists
                               .Field(f => f.Symbol))
                           && q.Term(i => i
                               .Field(f => f.SameChainFlag).Value(true)));
        var shouldQuery = new List<Func<QueryContainerDescriptor<SeedSymbolMarketTokenIndex>, QueryContainer>>();
        shouldQuery.Add(q => q.Terms(i => i.Field(f => f.OwnerManagerSet).Terms(dto.Address)));
        shouldQuery.Add(q => q.Terms(i => i.Field(f => f.IssueManagerSet).Terms(dto.Address)));
        shouldQuery.Add(q => q.Terms(i => i.Field(f => f.IssueToSet).Terms(
                dto.Address)));
        
        mustQuery.Add(q => q.Bool(b => b.Should(shouldQuery)));

        QueryContainer Filter(QueryContainerDescriptor<SeedSymbolMarketTokenIndex> f) => f.Bool(b => b.Must(mustQuery));

        var result = await symbolMarketTokenIndexRepository.GetListAsync(Filter,
            sortExp: k => k.CreateTime, sortType: SortOrder.Descending, skip: dto.SkipCount, limit: dto.MaxResultCount);

        if (result == null)
        {
            return new SymbolMarkerTokenPageResultDto()
            {
                TotalRecordCount = 0,
                Data = new List<SymbolMarkerTokenDto>()
            };
        }

        var dataList = objectMapper.Map<List<SeedSymbolMarketTokenIndex>, List<SymbolMarkerTokenDto>>(result.Item2);
        var pageResult = new SymbolMarkerTokenPageResultDto
        {
            TotalRecordCount = result.Item1,
            Data = dataList
        };
        return pageResult;
    }

    [Name("symbolMarketTokenIssuer")]
    public static async Task<SymbolMarketTokenIssuerDto> SymbolMarketTokenIssuer(
        [FromServices]
        IAElfIndexerClientEntityRepository<SeedSymbolMarketTokenIndex, LogEventInfo> symbolMarketTokenIndexRepository,
        GetSymbolMarketTokenIssuerInput dto)
    {
        if (dto == null)
            return new SymbolMarketTokenIssuerDto()
            {
                SymbolMarketTokenIssuer = ""
            };

        var issueChainId = ChainHelper.ConvertChainIdToBase58(dto.IssueChainId);
        var SymbolMarketTokenId = IdGenerateHelper.GetSymbolMarketTokenId(issueChainId, dto.TokenSymbol);
        var result =
            await symbolMarketTokenIndexRepository.GetFromBlockStateSetAsync(SymbolMarketTokenId, issueChainId);
        return new SymbolMarketTokenIssuerDto()
        {
            SymbolMarketTokenIssuer = result == null ? "" : result.Issuer
        };
    }
}