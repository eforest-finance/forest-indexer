using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using Drop.Indexer.Plugin.Entities;
using Drop.Indexer.Plugin.Util;
using Forest.Contracts.Drop;
using GraphQL;
using Volo.Abp.ObjectMapping;

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
            return new NFTDropPageResultDto
            {
                TotalRecordCount = 0,
                Data = new List<NFTDropInfoDto>()
            };
        var utcNow = DateTime.UtcNow;
        var queryable = await repository.GetQueryableAsync();
        queryable = queryable.Where(DropQueryFilters.DropStateMustNot());
        if (dto.Type == SearchType.All)
        {
            var count1 = queryable.Count();
            var list1 = queryable.ToList();
            var result1 = list1
                .OrderBy(drop =>
                {
                    if (drop.StartTime < utcNow && drop.ExpireTime > utcNow)
                        return 1;
                    if (drop.StartTime > utcNow)
                        return 2;
                    return 3;
                })
                .ThenBy(drop => drop.StartTime)
                .ThenBy(drop => drop.ExpireTime)
                .Skip(dto.SkipCount)
                .Take(dto.MaxResultCount)
                .ToList();
            if (result1.IsNullOrEmpty())
                return new NFTDropPageResultDto
                {
                    TotalRecordCount = count1,
                    Data = new List<NFTDropInfoDto>()
                };
            var dataList1 = objectMapper.Map<List<NFTDropIndex>, List<NFTDropInfoDto>>(result1);
            var pageResult1 = new NFTDropPageResultDto
            {
                TotalRecordCount = count1,
                Data = dataList1
            };
            return pageResult1;
        }


        switch (dto.Type)
        {
            case SearchType.Ongoing:
            {
                queryable = queryable.Where(DropQueryFilters.StartTimeBeforeMust(utcNow));
                queryable = queryable.Where(DropQueryFilters.ExpireTimeAfterMust(utcNow));
                break;
            }
            case SearchType.YetToBegin:
            {
                queryable = queryable.Where(DropQueryFilters.StartTimeAfterMust(utcNow));
                break;
            }
            case SearchType.Finished:
            {
                queryable = queryable.Where(DropQueryFilters.ExpireTimeBeforeMust(utcNow));
                break;
            }
            default:
            {
                //Logger.LogInformation("unknown type: {totalCount}", dto.Type);
                break;
            }
        }

        var count = queryable.Count();
        var list = queryable.ToList();
        var result = list
            .OrderBy(drop =>
            {
                if (drop.StartTime < utcNow && drop.ExpireTime > utcNow)
                    return 1;
                if (drop.StartTime > utcNow)
                    return 2;
                return 3;
            })
            .ThenBy(drop => drop.StartTime)
            .ThenBy(drop => drop.ExpireTime)
            .Skip(dto.SkipCount)
            .Take(dto.MaxResultCount)
            .ToList();
        if (result.IsNullOrEmpty())
            return new NFTDropPageResultDto
            {
                TotalRecordCount = count,
                Data = new List<NFTDropInfoDto>()
            };

        var dataList = objectMapper.Map<List<NFTDropIndex>, List<NFTDropInfoDto>>(result);
        var pageResult = new NFTDropPageResultDto
        {
            TotalRecordCount = count,
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
        var states = new HashSet<DropState>
        {
            DropState.Create,
            DropState.Cancel,
            DropState.Finish
        };
        queryable = queryable.Where(a => !states.Contains(a.State) && a.ExpireTime <= DateTime.Now).Take(100);


        var dropList = queryable.ToList();
        if (dropList.IsNullOrEmpty())
            return new NFTDropPageResultDto
            {
                TotalRecordCount = 0,
                Data = new List<NFTDropInfoDto>()
            };
        var dataList = objectMapper.Map<List<NFTDropIndex>, List<NFTDropInfoDto>>(dropList);
        var pageResult = new NFTDropPageResultDto
        {
            TotalRecordCount = dataList.Count,
            Data = dataList
        };
        return pageResult;
    }
}