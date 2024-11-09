using System.Linq.Dynamic.Core;
using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using Forest.Indexer.Plugin.Entities;
using GraphQL;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    /*[Obsolete("todo V2 not use")]
    [Name("nftInfosForUserProfile")]
    public static async Task<NFTInfoPageResultDto> NFTInfosForUserProfileAsync(
        [FromServices] IReadOnlyRepository<NFTInfoIndex> repository,
        [FromServices] IReadOnlyRepository<UserBalanceIndex> userBalanceRepository,
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
    }*/
    /*[Obsolete("todo V2 not use")]
    [Name("seedInfosForUserProfile")]
    public static async Task<PageResultDto<SeedInfoProfileDto>> SeedInfosForUserProfileAsync(
        [FromServices] IReadOnlyRepository<SeedSymbolIndex> repository,
        [FromServices] IReadOnlyRepository<UserBalanceIndex> userBalanceRepository,
        [FromServices] ILogger<UserBalanceIndex> logger,
        [FromServices] IObjectMapper objectMapper,
        GetNFTInfosDto dto)
    {
        var seedSymbolIndexQueryable = await repository.GetQueryableAsync();

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

            seedSymbolIndexQueryable = seedSymbolIndexQueryable.Where(f => nftIds.Contains(f.Id));
        }
        else
        {
            if (!dto.IssueAddress.IsNullOrEmpty())
                seedSymbolIndexQueryable = seedSymbolIndexQueryable.Where(f => f.IssuerTo == dto.IssueAddress);
        }

        if (dto.PriceLow != null && dto.PriceLow != 0)
        {
            seedSymbolIndexQueryable = seedSymbolIndexQueryable.Where(f => f.MinListingPrice >= (decimal)dto.PriceLow);
        }

        if (dto.PriceHigh != null && dto.PriceHigh != 0)
        {
            seedSymbolIndexQueryable = seedSymbolIndexQueryable.Where(f => f.MinListingPrice <= (decimal)dto.PriceHigh);
        }
        
        if (!dto.NFTInfoIds.IsNullOrEmpty())
        {
            seedSymbolIndexQueryable = seedSymbolIndexQueryable.Where(f => dto.NFTInfoIds.Contains(f.Id));
        }

        //todo V2 script need test,code:done
        seedSymbolIndexQueryable = seedSymbolIndexQueryable.Where(f => f.Supply>0 && f.Issued != f.TotalSupply);

        /*
        //Exclude Burned All NFT ( supply = 0 and issued = totalSupply)
        mustNotQuery.Add(q => q
            .Script(sc => sc
                .Script(script =>
                    script.Source($"{ForestIndexerConstants.BurnedAllNftScript}")
                )
            )
        );#1#
        seedSymbolIndexQueryable = seedSymbolIndexQueryable.Where(f => f.ChainId != ForestIndexerConstants.MainChain);
        
        QueryContainer Filter(QueryContainerDescriptor<SeedSymbolIndex> f)
        {
            return f.Bool(b => b.Must(mustQuery).MustNot(mustNotQuery));
        }

        var result = seedSymbolIndexQueryable.Skip(dto.SkipCount).Take(dto.MaxResultCount).OrderByDescending(o => o.LatestListingTime).ToList();
        if (result.IsNullOrEmpty())
        {
            return new PageResultDto<SeedInfoProfileDto>();
        }

        var pageResult = new PageResultDto<SeedInfoProfileDto>
        {
            TotalRecordCount = result.Count,
            Data = objectMapper.Map<List<SeedSymbolIndex>, List<SeedInfoProfileDto>>(result)
        };
        return pageResult;
    }*/
    
    private static async Task<List<string>> GetMatchedNftIdsAsync(
        [FromServices] IReadOnlyRepository<UserBalanceIndex> userBalanceAppService,
        GetNFTInfosDto dto,
        string symbolPrefix)
    {
        var nftIds = new List<string>();
        var queryable = await userBalanceAppService.GetQueryableAsync();
        queryable = queryable.Where(f => f.Address == dto.Address);
        queryable = queryable.Where(f => f.Amount > 0);

        if (symbolPrefix.Equals("SEED-"))
        {
            queryable = queryable.Where(f => f.Symbol.StartsWith(symbolPrefix));
        }
        

        int skipCount = 0;
        List<UserBalanceIndex> userBalanceIndexList;
        do
        {
            var resultUserBalanceIndex = queryable.Skip(skipCount).Take(ForestIndexerConstants.DefaultMaxCountNumber).ToList();
            userBalanceIndexList = resultUserBalanceIndex;
            if (!userBalanceIndexList.IsNullOrEmpty())
            {
                nftIds.AddRange(userBalanceIndexList.Select(o => o.NFTInfoId).ToList());
                skipCount += userBalanceIndexList.Count;
            }
        } while (!userBalanceIndexList.IsNullOrEmpty());

        //Logger.LogInformation("User profile nft infos nftIds:{nftIds}", nftIds);
        return nftIds;
    }
    //todo V2 use script ,code:undo
    private static async Task<Tuple<long, List<string>>> GetMatchedNftIdsPageAsync(
        [FromServices] IReadOnlyRepository<UserBalanceIndex> userBalanceAppService,
        GetNFTInfosDto dto,
        string script)
    {
        var queryable = await userBalanceAppService.GetQueryableAsync();

        var nftIds = new List<string>();
        // var userBalanceMustNotQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>();
        queryable = queryable.Where(f => f.Address == dto.Address && f.Amount > 0);
        queryable = queryable.Where(f=>f.ChainId != ForestIndexerConstants.MainChain);
        queryable = queryable.Where(f => !f.Symbol.StartsWith("SGR-"));
        queryable = queryable.Where(f => (f.Amount/Math.Pow(10, 8))>=1);

        //todo V2 script need test, code:done
        /*userBalanceMustNotQuery.Add(q => q
            .Bool(b => b
                .Must(m => m
                        .Script(sc => sc.Script(s => s.Source(ForestIndexerConstants.SymbolIsSGR))
                        ),
                    m => m
                        .Script(sc => sc.Script(s => s.Source(ForestIndexerConstants.SymbolAmountLessThanOneSGR))
                        )
                )
            )
        );*/
        var dd = dto.IsSeed
            ? ForestIndexerConstants.UserBalanceScriptForSeed
            : ForestIndexerConstants.UserBalanceScriptForNft;
        if (!script.IsNullOrEmpty())
        {
            if (script.Equals("UserBalanceScriptForSeed"))
            {
                queryable = queryable.Where(f => f.Symbol.StartsWith("SEED-"));

            }

            if (script.Equals("UserBalanceScriptForNft"))
            {
                queryable = queryable.Where(f => !f.Symbol.StartsWith("SEED-"));

            }
        }

        var resultUserBalanceIndex = queryable.Skip(dto.SkipCount).Take(dto.MaxResultCount).ToList();

        var userBalanceIndexList = resultUserBalanceIndex;
        if (!userBalanceIndexList.IsNullOrEmpty())
        {
            nftIds.AddRange(userBalanceIndexList.Select(o => o.NFTInfoId).ToList());
        }

        var totalCount = resultUserBalanceIndex?.Count;
        if (resultUserBalanceIndex?.Count == ForestIndexerConstants.EsLimitTotalNumber)
        {
            totalCount = queryable.Count();
        }

        //Logger.LogInformation("User profile nft infos nftIds:{nftIds}", nftIds);
        var count = (long)(totalCount ?? 0);
        return new Tuple<long, List<string>>(count, nftIds);
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