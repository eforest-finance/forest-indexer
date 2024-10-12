using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using Drop.Indexer.Plugin.Entities;
using GraphQL;
using Nest;
using Volo.Abp.ObjectMapping;
using Forest.Contracts.Drop;

namespace Drop.Indexer.Plugin.GraphQL;

public class Query
{
    private const string SortTypeNumber = "number";
    private static readonly IAeFinderLogger Logger;
    
    [Name("nftDrop")]
    public static async Task<NFTDropInfoDto> NFTDropInfo(
        [FromServices] IReadOnlyRepository<NFTDropIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        string dropId)
    {
        if (dropId.IsNullOrWhiteSpace()) return null;
        var queryable = await repository.GetQueryableAsync();
        queryable = queryable.Where(a => a.Id == dropId);
        var nftDropIndex = queryable.ToList();
        if (nftDropIndex.IsNullOrEmpty()) return null;

        return objectMapper.Map<NFTDropIndex, NFTDropInfoDto>(nftDropIndex.FirstOrDefault());
    }
    
    
    [Name("nftDropList")]
    public static async Task<NFTDropPageResultDto> NFTDropList(
        [FromServices] IReadOnlyRepository<NFTDropIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetNFTDropListDto dto)
    {
        if (dto == null)
        {
            return new NFTDropPageResultDto
            {
                TotalRecordCount = 0,
                Data = new List<NFTDropInfoDto>()
            };
        }
        var queryable = await repository.GetQueryableAsync();
        HashSet<DropState> states = new HashSet<DropState>
        {
            DropState.Create,
            DropState.Cancel
        };
        queryable = queryable.Where(a => !states.Contains(a.State));
        
        //todo v2
        // IPromise<IList<ISort>> Sort(SortDescriptor<NFTDropIndex> s) =>
        //     s.Script(script => script.Type(SortTypeNumber)
        //         .Script(scriptDescriptor => scriptDescriptor.Source(DropIndexerConstants.QueryDropListScript))
        //         .Order(SortOrder.Ascending));
        //
        if (dto.Type == SearchType.All)
        {
            queryable = queryable.Skip(dto.SkipCount).Take(dto.MaxResultCount);
            // todo sort for v2
            /*QueryContainer Filter1(QueryContainerDescriptor<NFTDropIndex> f) =>
                f.Bool(b => b.MustNot(mustNotQuery));
            var result1 = await repository.GetSortListAsync(Filter1, sortFunc: Sort,
                skip: dto.SkipCount, limit: dto.MaxResultCount);*/
            var result1 = queryable.ToList();
            if (result1.IsNullOrEmpty())
            {
                return new NFTDropPageResultDto
                {
                    TotalRecordCount = 0,
                    Data = new List<NFTDropInfoDto>()
                };
            }

            var dataList1 = objectMapper.Map<List<NFTDropIndex>, List<NFTDropInfoDto>>(result1);
            var pageResult1 = new NFTDropPageResultDto
            {
                TotalRecordCount = dataList1.Count,
                Data = dataList1
            };
            return pageResult1;
        }
        
        //todo v2
        // var mustQuery = new List<Func<QueryContainerDescriptor<NFTDropIndex>, QueryContainer>>();
        var nowStr = long.Parse(DateTime.UtcNow.ToString("o"));
        switch (dto.Type)
        {
            case SearchType.Ongoing:
            {
                queryable = queryable.Where(a => DateTimeHelper.ToUnixTimeMilliseconds(a.StartTime) <= nowStr);
                queryable = queryable.Where(a => DateTimeHelper.ToUnixTimeMilliseconds(a.ExpireTime) >= nowStr);

                break;
            }
            case SearchType.YetToBegin:
            {
                queryable = queryable.Where(a => DateTimeHelper.ToUnixTimeMilliseconds(a.StartTime) >= nowStr);
                break;
            }
            case SearchType.Finished:
            {
                queryable = queryable.Where(a => DateTimeHelper.ToUnixTimeMilliseconds(a.ExpireTime) <= nowStr);
                break;
            }
            default:
            {
                // todo log for v2
                Logger.LogInformation("unknown type: {totalCount}", dto.Type);
                break;
            }
        }
        

        /*var result = await repository.GetSortListAsync(Filter2, sortFunc: Sort, 
            skip: dto.SkipCount, limit: dto.MaxResultCount);*/
        // todo sort for v2
        queryable = queryable.Skip(dto.SkipCount).Take(dto.MaxResultCount);
        var result = queryable.ToList();
        if (result.IsNullOrEmpty())
        {
            return new NFTDropPageResultDto
            {
                TotalRecordCount = 0,
                Data = new List<NFTDropInfoDto>()
            };
        }
        var dataList = objectMapper.Map<List<NFTDropIndex>, List<NFTDropInfoDto>>(result);
        var pageResult = new NFTDropPageResultDto
        {
            TotalRecordCount = result.Count,
            Data = dataList
        };
        return pageResult;
    }
    
    
    [Name("dropClaim")]
    public static async Task<NFTDropClaimDto> NFTDropClaim(
        [FromServices] IReadOnlyRepository<NFTDropClaimIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetNFTDropClaimDto dto)
    {
        if (dto.DropId.IsNullOrWhiteSpace() || dto.Address.IsNullOrWhiteSpace()) return null;
        var id = IdGenerateHelper.GetNFTDropClaimId(dto.DropId, dto.Address);
        var queryable = await repository.GetQueryableAsync();
        queryable = queryable.Where(a => a.Id == id);

        var nftDropClaimIndex = queryable.ToList();
        if (nftDropClaimIndex.IsNullOrEmpty()) return null;

        return objectMapper.Map<NFTDropClaimIndex, NFTDropClaimDto>(nftDropClaimIndex.FirstOrDefault());
    }
    
    
    [Name("expiredDropList")]
    public static async Task<NFTDropPageResultDto> ExpiredDropList(
        [FromServices] IReadOnlyRepository<NFTDropIndex> repository,
        [FromServices] IObjectMapper objectMapper)
    {
        var queryable = await repository.GetQueryableAsync();
        HashSet<DropState> states = new HashSet<DropState>
        {
            DropState.Create,
            DropState.Cancel,
            DropState.Finish
        };
        queryable = queryable.Where(a => !states.Contains(a.State) && a.ExpireTime <= DateTime.Now).Take(100);

  
        var dropList = queryable.ToList();
        if (dropList.IsNullOrEmpty())
        {
            return new NFTDropPageResultDto
            {
                TotalRecordCount = 0,
                Data = new List<NFTDropInfoDto>()
            };
        }
        var dataList = objectMapper.Map<List<NFTDropIndex>, List<NFTDropInfoDto>>(dropList);
        var pageResult = new NFTDropPageResultDto
        {
            TotalRecordCount = dataList.Count,
            Data = dataList
        };
        return pageResult;
    }
}