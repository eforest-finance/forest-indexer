using AeFinder.Sdk;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Microsoft.Extensions.Logging;
using Nest;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("queryUserBalanceByNftId")]
    public static async Task<NFTUserBalanceDto> QueryUserBalanceByNftIdAsync(
        [FromServices] IReadOnlyRepository<UserBalanceIndex> userBalanceRepo,
        [FromServices] IObjectMapper objectMapper,
        GetUserBalanceDto dto)
    {
        var queryable = await userBalanceRepo.GetQueryableAsync();
        queryable = queryable.Where(index => index.NFTInfoId == dto.nftInfoId && index.Amount>0);

        var result = queryable.Skip(0).Take(1).ToList();
        var totalCount = result?.Count;
        if (result?.Count == ForestIndexerConstants.EsLimitTotalNumber)
        {
            var queryableCount = await userBalanceRepo.GetQueryableAsync();
            queryableCount = queryableCount.Where(index => index.NFTInfoId == dto.nftInfoId && index.Amount>0);
            totalCount = queryableCount.Count();
        }
        
        return new NFTUserBalanceDto
        {
            Owner = result?.Count > 0 ? result.FirstOrDefault().Address : string.Empty,
            OwnerCount =  (long)(totalCount ?? 0),
        };
    }

    [Name("queryUserNftIds")]
    public static async Task<UserMatchedNftIds> QueryUserNftIdsAsync(
        [FromServices] IReadOnlyRepository<UserBalanceIndex> userBalanceRepository,
        [FromServices] ILogger<UserBalanceIndex> logger,
        GetNFTInfosDto dto)
    {
        //query match nft
        var script = dto.IsSeed
            ? ForestIndexerConstants.UserBalanceScriptForSeed
            : ForestIndexerConstants.UserBalanceScriptForNft;
        var nftIds = await GetMatchedNftIdsAsync(userBalanceRepository, logger, dto, script);

        return new UserMatchedNftIds
        {
            NftIds = nftIds
        };
    }
    
    
    [Name("queryUserNftIdsPage")]
    public static async Task<UserMatchedNftIdsPage> QueryUserNftIdsPageAsync(
        [FromServices] IReadOnlyRepository<UserBalanceIndex> userBalanceRepository,
        [FromServices] ILogger<UserBalanceIndex> logger,
        GetNFTInfosDto dto)
    {
        //query match nft
        /*var script = dto.IsSeed
            ? ForestIndexerConstants.UserBalanceScriptForSeed
            : ForestIndexerConstants.UserBalanceScriptForNft;*/
        var script = dto.IsSeed
            ? "UserBalanceScriptForSeed"
            : "UserBalanceScriptForNft";
        var result = await GetMatchedNftIdsPageAsync(userBalanceRepository, logger, dto, script);

        return new UserMatchedNftIdsPage
        {
            NftIds = result?.Item2,
            Count = result.Item1
        };
    }

    [Name("queryOwnersByNftId")]
    public static async Task<NFTOwnersPageResultDto> QueryOwnersByNftIdAsync(
        [FromServices] IReadOnlyRepository<UserBalanceIndex> userBalanceRepo,
        [FromServices] IObjectMapper objectMapper,
        GetNFTOwnersDto input)
    {
        var queryable = await userBalanceRepo.GetQueryableAsync();
        queryable = queryable.Where(index => index.NFTInfoId == input.NftInfoId);
        queryable = queryable.Where(index => index.Amount > 0);

        if (!input.ChainId.IsNullOrEmpty())
            queryable = queryable.Where(index => index.ChainId == input.ChainId);

        var result = queryable
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .OrderByDescending(a => a.Amount)
            .OrderBy(a => a.Address)
            .ToList();
        var totalCount = result?.Count;
        if (result?.Count == ForestIndexerConstants.EsLimitTotalNumber)
        {
            totalCount = queryable.Count();
        }
        
        return new NFTOwnersPageResultDto
        {
            TotalCount = (long)(totalCount == null ? 0 : totalCount),
            Data = objectMapper.Map<List<UserBalanceIndex>, List<NFTOwnerInfoDto>>(result)
        };
    }
    
    [Name("queryUserBalanceList")]
    public static async Task<UserBalancePageResultDto> queryUserBalanceListAsync(
        [FromServices] IReadOnlyRepository<UserBalanceIndex> userBalanceRepo,
        [FromServices] IObjectMapper objectMapper,
        GetUserBalancesDto input)
    {
        var queryable = await userBalanceRepo.GetQueryableAsync();

        var userBalanceQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>();
        if (!input.ChainId.IsNullOrEmpty())
        {
            queryable = queryable.Where(q=>q.ChainId == input.ChainId);
        }
        queryable = queryable.Where(f => f.BlockHeight >= input.BlockHeight);

        var result = queryable.Skip(input.SkipCount).Take(ForestIndexerConstants.QueryUserBalanceListDefaultSize).OrderBy(a=>a.BlockHeight)
            .ToList();

        var totalCount = result?.Count;
        if (result?.Count == ForestIndexerConstants.EsLimitTotalNumber)
        {
            totalCount = queryable.Count();
        }
        
        return new UserBalancePageResultDto
        {
            TotalCount = (long)(totalCount == null ? 0 : totalCount),
            Data = objectMapper.Map<List<UserBalanceIndex>, List<UserBalanceDto>>(result)
        };
    }
    private static Func<SortDescriptor<UserBalanceIndex>, IPromise<IList<ISort>>> GetSortForUserBalanceByBolockHeight()
    {
        SortDescriptor<UserBalanceIndex> sortDescriptor = new SortDescriptor<UserBalanceIndex>();
        sortDescriptor.Ascending(a=>a.BlockHeight);
        IPromise<IList<ISort>> promise = sortDescriptor;
        return s => promise;
    }
    private static Func<SortDescriptor<UserBalanceIndex>, IPromise<IList<ISort>>> GetSortForUserBalance()
    {
        SortDescriptor<UserBalanceIndex> sortDescriptor = new SortDescriptor<UserBalanceIndex>();
        sortDescriptor.Descending(a=>a.Amount);
        sortDescriptor.Ascending(a=>a.Address);
        IPromise<IList<ISort>> promise = sortDescriptor;
        return s => promise;
    }
}