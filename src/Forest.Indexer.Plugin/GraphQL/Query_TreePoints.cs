using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("getSyncTreePointsRecords")]
    public static async Task<TreePointsChangeRecordPageResultDto> GetSyncTreePointsRecords(
        [FromServices] IReadOnlyRepository<TreePointsChangeRecordIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        [FromServices] IAeFinderLogger Logger,
        GetChainBlockHeightDto dto)
    {
       // Logger.LogInformation("GetSyncTreePointsRecords chainId:{A} start:{B} end:{C}", dto.ChainId,dto.StartBlockHeight, dto.EndBlockHeight);
        var queryable = await repository.GetQueryableAsync();
        
        if (dto.StartBlockHeight > 0)
        {
            queryable = queryable.Where(f => f.BlockHeight > dto.StartBlockHeight);
        }

        /*if (dto.EndBlockHeight > 0)
        {
            queryable = queryable.Where(f => f.BlockHeight <= dto.EndBlockHeight);
        }*/

        var result = queryable.OrderBy(o => o.BlockHeight).OrderBy(i=>i.BlockHeight).Skip(0).Take(20).ToList();
        //Logger.LogInformation("GetSyncTreePointsRecords resultCount:{A}", result.IsNullOrEmpty()?0: result.Count);

        if (result.IsNullOrEmpty() || result.Count == 0)
        {
            return new TreePointsChangeRecordPageResultDto()
            {
                TotalRecordCount = 0,
                Data = new List<TreePointsChangeRecordDto>()
            };
        }
        var count = queryable.Count();

        var dataList = objectMapper.Map<List<TreePointsChangeRecordIndex>, List<TreePointsChangeRecordDto>>(result);
        var totalCount = count;

        return new TreePointsChangeRecordPageResultDto
        {
            Data = dataList,
            TotalRecordCount = (long)(totalCount == null ? 0 : totalCount),
        };
    }
    
    [Name("getSyncTreePointsRecordsAll")]
    public static async Task<TreePointsChangeRecordPageResultDto> GetSyncTreePointsRecordsAll(
        [FromServices] IReadOnlyRepository<TreePointsChangeRecordIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        [FromServices] IAeFinderLogger Logger,
        GetChainBlockHeightDto dto)
    {
      // Logger.LogInformation("GetSyncTreePointsRecordsAll chainId:{A} start:{B} end:{C}", dto.ChainId, dto.StartBlockHeight, dto.EndBlockHeight);

        var queryable = await repository.GetQueryableAsync();
        var result = queryable.ToList();
        if (result.IsNullOrEmpty())
        {
            return new TreePointsChangeRecordPageResultDto();
        }
        var count = queryable.Count();

        var dataList = objectMapper.Map<List<TreePointsChangeRecordIndex>, List<TreePointsChangeRecordDto>>(result);
       // Logger.LogInformation("GetSyncTreePointsRecordsAll resultCount:{A}", dataList.IsNullOrEmpty()?0: dataList.Count);

        var totalCount = count;

        return new TreePointsChangeRecordPageResultDto
        {
            Data = dataList,
            TotalRecordCount = (long)(totalCount == null ? 0 : totalCount),
        };
    }
    [Name("getTreePointsRecords")]
    public static async Task<TreePointsChangeRecordPageResultDto> GetTreePointsRecords(
        [FromServices] IReadOnlyRepository<TreePointsChangeRecordIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        [FromServices] IAeFinderLogger Logger,
        GetTreePointsRecordDto dto)
    {
        var queryable = await repository.GetQueryableAsync();
        
        if (dto.MinTimestamp <= 0 || dto.MaxTimestamp <=0 || dto.MaxTimestamp < dto.MaxTimestamp)
        {
            return new TreePointsChangeRecordPageResultDto()
            {
                TotalRecordCount = 0,
                Data = new List<TreePointsChangeRecordDto>()
            };
        }

        var result = new List<TreePointsChangeRecordIndex>();
        queryable = queryable.Where(f => f.OpTime >= dto.MinTimestamp);
        queryable = queryable.Where(f => f.OpTime <= dto.MaxTimestamp);
        if(!dto.Addresses.IsNullOrEmpty())
        {
            var groupAddress = GroupAddresses(dto.Addresses);
            foreach (var addresses in groupAddress)
            {
                var subQueryable = queryable;
                subQueryable = subQueryable.Where(i => addresses.Contains(i.Address));
                var subResult = subQueryable.OrderBy(o => o.BlockHeight).OrderBy(i=>i.BlockHeight).Skip(0).Take(10000).ToList();
                result.AddRange(subResult);
            }
        }
        else
        {
            result = queryable.OrderBy(o => o.BlockHeight).OrderBy(i=>i.BlockHeight).Skip(0).Take(10000).ToList();
        }


        if (result.IsNullOrEmpty() || result.Count == 0)
        {
            return new TreePointsChangeRecordPageResultDto()
            {
                TotalRecordCount = 0,
                Data = new List<TreePointsChangeRecordDto>()
            };
        }
        var count = queryable.Count();

        var dataList = objectMapper.Map<List<TreePointsChangeRecordIndex>, List<TreePointsChangeRecordDto>>(result);
        var totalCount = count;

        return new TreePointsChangeRecordPageResultDto
        {
            Data = dataList,
            TotalRecordCount = (long)(totalCount == null ? 0 : totalCount),
        };
    }

    private static IEnumerable<List<string>> GroupAddresses(List<string> originalList)
    {
        const int groupPageCount = 100;
        var groupedList = new List<List<string>>();
 
        for (var i = 0; i < originalList.Count; i += groupPageCount)
        {
            var count = Math.Min(100, originalList.Count - i); 
            var subList = originalList.GetRange(i, count);
            groupedList.Add(subList);
        }

        return groupedList;
    }
}