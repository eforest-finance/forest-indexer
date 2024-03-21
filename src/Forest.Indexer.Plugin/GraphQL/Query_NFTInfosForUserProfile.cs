using System.Linq.Expressions;
using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Microsoft.Extensions.Logging;
using Nest;
using Orleans.Runtime;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("nftInfosForUserProfile")]
    public static async Task<NFTInfoPageResultDto> NFTInfosForUserProfileAsync(
        [FromServices] IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> repository,
        [FromServices] IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> userBalanceRepository,
        [FromServices] ILogger<UserBalanceIndex> logger,
        [FromServices] IObjectMapper objectMapper,
        GetNFTInfosDto dto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoIndex>, QueryContainer>>();
        var mustNotQuery = new List<Func<QueryContainerDescriptor<NFTInfoIndex>, QueryContainer>>();
        var sorting = new Tuple<SortOrder, Expression<Func<NFTInfoIndex, object>>>(SortOrder.Descending, o => o.LatestListingTime);
        if (dto.Status == ForestIndexerConstants.NFTInfoQueryStatusSelf)
        {
            //query match nft
            var nftIds = await GetMatchedNftIdsAsync(userBalanceRepository, logger, dto, ForestIndexerConstants.UserBalanceScriptForNft);
            if (nftIds.IsNullOrEmpty())
            {
                return BuildInitNftInfoPageResultDto();
            }
            mustQuery.Add(q => q.Ids(i => i.Values(nftIds)));
        }
        else
        {
            if (!dto.IssueAddress.IsNullOrEmpty())
                mustQuery.Add(q => q.Terms(i => i.Field(f => f.IssueManagerSet).Terms(dto.IssueAddress)));
        }
        
        if (dto.PriceLow != null && dto.PriceLow != 0)
            mustQuery.Add(q => q.Range(i => i.Field(f => f.MinListingPrice).GreaterThanOrEquals(dto.PriceLow)));

        if (dto.PriceHigh != null && dto.PriceHigh != 0)
        {
            mustQuery.Add(q => q.Range(i => i.Field(f => f.MinListingPrice).LessThanOrEquals(dto.PriceHigh)));
        }
        
        if (!dto.NFTInfoIds.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(dto.NFTInfoIds)));
        }

        if (!dto.NftCollectionId.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionId).Value(dto.NftCollectionId)));
        }
        //Exclude Burned All NFT ( supply = 0 and issued = totalSupply)
        mustNotQuery.Add(q => q
            .Script(sc => sc
                .Script(script =>
                    script.Source($"{ForestIndexerConstants.BurnedAllNftScript}")
                )
            )
        );
        
        QueryContainer Filter(QueryContainerDescriptor<NFTInfoIndex> f)
        {
            return f.Bool(b => b.Must(mustQuery).MustNot(mustNotQuery));
        }
        var result = await repository.GetListAsync(Filter, sortType: sorting.Item1, sortExp: sorting.Item2,
            skip: dto.SkipCount, limit: dto.MaxResultCount);
        var pageResult = new NFTInfoPageResultDto
        {
            TotalRecordCount = result.Item1,
            Data = objectMapper.Map<List<NFTInfoIndex>, List<NFTInfoDto>>(result.Item2)
        };
        return pageResult;
    }
    
    [Name("seedInfosForUserProfile")]
    public static async Task<PageResultDto<SeedInfoProfileDto>> SeedInfosForUserProfileAsync(
        [FromServices] IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> repository,
        [FromServices] IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> userBalanceRepository,
        [FromServices] ILogger<UserBalanceIndex> logger,
        [FromServices] IObjectMapper objectMapper,
        GetNFTInfosDto dto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();
        var mustNotQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();
        var sorting = new Tuple<SortOrder, Expression<Func<SeedSymbolIndex, object>>>(SortOrder.Descending, o => o.LatestListingTime);
        //only CollectionSymbol is xxx-SEED-O
        if (!dto.NftCollectionId.IsNullOrEmpty() && !dto.NftCollectionId.Match(ForestIndexerConstants.SeedZeroIdPattern))
        {
             return PageResultDto<SeedInfoProfileDto>.Initialize();
        }
        
        if (dto.Status == ForestIndexerConstants.NFTInfoQueryStatusSelf)
        {
            //query match seed
            var nftIds = await GetMatchedNftIdsAsync(userBalanceRepository, logger, dto, ForestIndexerConstants.UserBalanceScriptForSeed);
            if (nftIds.IsNullOrEmpty())
            {
                return PageResultDto<SeedInfoProfileDto>.Initialize();
            }
            mustQuery.Add(q => q.Ids(i => i.Values(nftIds)));
        }
        else
        {
            if (!dto.IssueAddress.IsNullOrEmpty())
                mustQuery.Add(q => q.Term(i => i.Field(f => f.IssuerTo).Value(dto.IssueAddress)));
        }

        if (dto.PriceLow != null && dto.PriceLow != 0)
            mustQuery.Add(q => q.Range(i => i.Field(f => f.MinListingPrice).GreaterThanOrEquals(dto.PriceLow)));

        if (dto.PriceHigh != null && dto.PriceHigh != 0)
        {
            mustQuery.Add(q => q.Range(i => i.Field(f => f.MinListingPrice).LessThanOrEquals(dto.PriceHigh)));
        }
        
        if (!dto.NFTInfoIds.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(dto.NFTInfoIds)));
        }

        //Exclude Burned All NFT ( supply = 0 and issued = totalSupply)
        mustNotQuery.Add(q => q
            .Script(sc => sc
                .Script(script =>
                    script.Source($"{ForestIndexerConstants.BurnedAllNftScript}")
                )
            )
        );
        mustNotQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(ForestIndexerConstants.MainChain)));
        
        QueryContainer Filter(QueryContainerDescriptor<SeedSymbolIndex> f)
        {
            return f.Bool(b => b.Must(mustQuery).MustNot(mustNotQuery));
        }
        var result = await repository.GetListAsync(Filter, sortType: sorting.Item1, sortExp: sorting.Item2,
            skip: dto.SkipCount, limit: dto.MaxResultCount);
        var pageResult = new PageResultDto<SeedInfoProfileDto>
        {
            TotalRecordCount = result.Item1,
            Data = objectMapper.Map<List<SeedSymbolIndex>, List<SeedInfoProfileDto>>(result.Item2)
        };
        return pageResult;
    }
    
    private static async Task<List<string>> GetMatchedNftIdsAsync(
        [FromServices] IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> userBalanceAppService,
        [FromServices] ILogger<UserBalanceIndex> logger,
        GetNFTInfosDto dto,
        string script)
    {
        var nftIds = new List<string>();
        var userBalanceMustQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>();
        userBalanceMustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(dto.Address)));
        userBalanceMustQuery.Add(q => q.Range(i => i.Field(f => f.Amount).GreaterThan(0)));
        if (!script.IsNullOrEmpty())
        {
            userBalanceMustQuery.Add(q=>q.Script(scriptDescriptor => scriptDescriptor
                .Script(s => s
                    .Source(script)
                    .Lang(ForestIndexerConstants.Painless))));
        }

        QueryContainer FilterForUserBalance(QueryContainerDescriptor<UserBalanceIndex> f)
        {
            return f.Bool(b => b.Must(userBalanceMustQuery));
        }

        int skipCount = 0;
        List<UserBalanceIndex> userBalanceIndexList;
        do
        {
            var resultUserBalanceIndex =
                await userBalanceAppService.GetListAsync(FilterForUserBalance, skip: skipCount);
            userBalanceIndexList = resultUserBalanceIndex.Item2;
            if (!userBalanceIndexList.IsNullOrEmpty())
            {
                nftIds.AddRange(userBalanceIndexList.Select(o => o.NFTInfoId).ToList());
                skipCount += userBalanceIndexList.Count;
            }
        } while (!userBalanceIndexList.IsNullOrEmpty());

        logger.LogInformation("User profile nft infos nftIds:{nftIds}", nftIds);
        return nftIds;
    }
    
    private static async Task<Tuple<long, List<string>>> GetMatchedNftIdsPageAsync(
        [FromServices] IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> userBalanceAppService,
        [FromServices] ILogger<UserBalanceIndex> logger,
        GetNFTInfosDto dto,
        string script)
    {
        var nftIds = new List<string>();
        var userBalanceMustQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>();
        var userBalanceMustNotQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>();
        userBalanceMustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(dto.Address)));
        userBalanceMustQuery.Add(q => q.Range(i => i.Field(f => f.Amount).GreaterThan(0)));
        userBalanceMustNotQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(ForestIndexerConstants.MainChain)));

        userBalanceMustNotQuery.Add(q => q
            .Bool(b => b
                .Must(m => m
                        .Script(sc => sc.Script(s => s.Source(ForestIndexerConstants.SymbolIsSGR))
                        ),
                    m => m
                        .Script(sc => sc.Script(s => s.Source(ForestIndexerConstants.SymbolAmountLessThanOneSGR))
                        )
                )
            )
        );
        
        if (!script.IsNullOrEmpty())
        {
            userBalanceMustQuery.Add(q=>q.Script(scriptDescriptor => scriptDescriptor
                .Script(s => s
                    .Source(script)
                    .Lang(ForestIndexerConstants.Painless))));
        }

        QueryContainer FilterForUserBalance(QueryContainerDescriptor<UserBalanceIndex> f)
        {
            return f.Bool(b => b.Must(userBalanceMustQuery).MustNot(userBalanceMustNotQuery));
        }

        var resultUserBalanceIndex =
            await userBalanceAppService.GetListAsync(FilterForUserBalance, skip: dto.SkipCount,
                limit: dto.MaxResultCount);
        var userBalanceIndexList = resultUserBalanceIndex.Item2;
        if (!userBalanceIndexList.IsNullOrEmpty())
        {
            nftIds.AddRange(userBalanceIndexList.Select(o => o.NFTInfoId).ToList());
        }

        var totalCount = resultUserBalanceIndex?.Item1;
        if (resultUserBalanceIndex?.Item1 == ForestIndexerConstants.EsLimitTotalNumber)
        {
            totalCount =
                await QueryRealCountAsync(userBalanceAppService, userBalanceMustQuery, userBalanceMustNotQuery);
        }

        logger.LogInformation("User profile nft infos nftIds:{nftIds}", nftIds);
        var count = (long)(totalCount == null ? 0 : totalCount);
        return new Tuple<long, List<string>>(count, nftIds);
    }
    private static async Task<long> QueryRealCountAsync(IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> userBalanceAppService,List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>> mustQuery,List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>> mustNotQuery)
    {
        var countRequest = new SearchRequest<UserBalanceIndex>
        {
            Query = new BoolQuery
            {
                Must = mustQuery != null && mustQuery.Any()
                ? mustQuery
                .Select(func => func(new QueryContainerDescriptor<UserBalanceIndex>()))
                .ToList()
                .AsEnumerable()
                : Enumerable.Empty<QueryContainer>(),
                MustNot = mustNotQuery != null && mustNotQuery.Any()
                    ? mustNotQuery
                        .Select(func => func(new QueryContainerDescriptor<UserBalanceIndex>()))
                        .ToList()
                        .AsEnumerable()
                    : Enumerable.Empty<QueryContainer>()
            },
            Size = 0
        };
        
        Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer> queryFunc = q => countRequest.Query;
        var realCount = await userBalanceAppService.CountAsync(queryFunc);
        return realCount.Count;
    }
    private static NFTInfoPageResultDto BuildInitNftInfoPageResultDto()
    {
        return new NFTInfoPageResultDto
        {
            TotalRecordCount = 0,
            Data = new List<NFTInfoDto>()
        };
    }
}