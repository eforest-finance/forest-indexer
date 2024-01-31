using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Microsoft.Extensions.Logging;
using Nest;
using Volo.Abp.ObjectMapping;
using Forest.Contracts.Drop;
using Orleans.Runtime;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    
    [Name("nftDrop")]
    public static async Task<NFTDropInfoDto> NFTDropInfo(
        [FromServices] IAElfIndexerClientEntityRepository<NFTDropIndex, LogEventInfo> repository,
        [FromServices] IObjectMapper objectMapper,
        string dropId)
    {
        if (dropId.IsNullOrWhiteSpace()) return null;
        var nftDropIndex = await repository.GetAsync(dropId);
        if (nftDropIndex == null) return null;

        return objectMapper.Map<NFTDropIndex, NFTDropInfoDto>(nftDropIndex);
    }
    
    
    [Name("nftDropList")]
    public static async Task<NFTDropPageResultDto> NFTDropList(
        [FromServices] IAElfIndexerClientEntityRepository<NFTDropIndex, LogEventInfo> repository,
        [FromServices] IObjectMapper objectMapper,
        [FromServices] ILogger<NFTDropIndex> logger,
        GetNFTDropListDto dto)
    {
        if (dto == null)
            return new NFTDropPageResultDto
            {
                TotalRecordCount = 0,
                Data = new List<NFTDropInfoDto>()
            };
        

        var mustNotQuery = new List<Func<QueryContainerDescriptor<NFTDropIndex>, QueryContainer>>();
        HashSet<DropState> states = new HashSet<DropState>
        {
            DropState.Create,
            DropState.Cancel
        };
        mustNotQuery.Add(q => q.Terms(i => i.Field(f => f.State).Terms(states)));
        
        IPromise<IList<ISort>> Sort(SortDescriptor<NFTDropIndex> s) =>
            s.Script(script => script.Type(SortTypeNumber)
                .Script(scriptDescriptor => scriptDescriptor.Source(ForestIndexerConstants.QueryDropListScript))
                .Order(SortOrder.Ascending));
        
        if (dto.Type == SearchType.All)
        {
            QueryContainer Filter1(QueryContainerDescriptor<NFTDropIndex> f) =>
                f.Bool(b => b.MustNot(mustNotQuery));
            var result1 = await repository.GetSortListAsync(Filter1, sortFunc: Sort,
                skip: dto.SkipCount, limit: dto.MaxResultCount);
            var dataList1 = objectMapper.Map<List<NFTDropIndex>, List<NFTDropInfoDto>>(result1.Item2);
            var pageResult1 = new NFTDropPageResultDto
            {
                TotalRecordCount = result1.Item1,
                Data = dataList1
            };
            return pageResult1;
        }
        
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTDropIndex>, QueryContainer>>();
        var nowStr = DateTime.UtcNow.ToString("o");
        switch (dto.Type)
        {
            case SearchType.Ongoing:
            {
                mustQuery.Add(q => q.TermRange(i
                    => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.StartTime)).LessThan(nowStr)));
                mustQuery.Add(q => q.TermRange(i
                    => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime)).GreaterThan(nowStr)));
                break;
            }
            case SearchType.YetToBegin:
            {
                mustQuery.Add(q => q.TermRange(i
                    => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.StartTime)).GreaterThan(nowStr)));
                break;
            }
            case SearchType.Finished:
            {
                mustQuery.Add(q => q.TermRange(i
                    => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime)).LessThan(nowStr)));
                break;
            }
            default:
            {
                logger.LogInformation("unknown type: {totalCount}", dto.Type);
                break;
            }
        }
        
        QueryContainer Filter2(QueryContainerDescriptor<NFTDropIndex> f) =>
            f.Bool(b => b.MustNot(mustNotQuery).Must(mustQuery));
        var result = await repository.GetSortListAsync(Filter2, sortFunc: Sort, 
            skip: dto.SkipCount, limit: dto.MaxResultCount);
        var dataList = objectMapper.Map<List<NFTDropIndex>, List<NFTDropInfoDto>>(result.Item2);
        var pageResult = new NFTDropPageResultDto
        {
            TotalRecordCount = result.Item1,
            Data = dataList
        };
        return pageResult;
    }
    
    
    [Name("dropClaim")]
    public static async Task<NFTDropClaimDto> NFTDropClaim(
        [FromServices] IAElfIndexerClientEntityRepository<NFTDropClaimIndex, LogEventInfo> repository,
        [FromServices] IObjectMapper objectMapper,
        GetNFTDropClaimDto dto)
    {
        if (dto.DropId.IsNullOrWhiteSpace() || dto.Address.IsNullOrWhiteSpace()) return null;
        var id = IdGenerateHelper.GetNFTDropClaimId(dto.DropId, dto.Address);
        var nftDropClaimIndex = await repository.GetAsync(id);
        if (nftDropClaimIndex == null) return null;

        return objectMapper.Map<NFTDropClaimIndex, NFTDropClaimDto>(nftDropClaimIndex);
    }
    
    
    [Name("expiredDropList")]
    public static async Task<NFTDropPageResultDto> ExpiredDropList(
        [FromServices] IAElfIndexerClientEntityRepository<NFTDropIndex, LogEventInfo> repository,
        [FromServices] IObjectMapper objectMapper)
    {
        var mustNotQuery = new List<Func<QueryContainerDescriptor<NFTDropIndex>, QueryContainer>>();
        HashSet<DropState> states = new HashSet<DropState>
        {
            DropState.Create,
            DropState.Cancel,
            DropState.Finish
        };
        mustNotQuery.Add(q => q.Terms(i => i.Field(f => f.State).Terms(states)));
        
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTDropIndex>, QueryContainer>>();
        mustQuery.Add(q => q.DateRange(i => i.Field(f => f.ExpireTime).LessThan(DateTime.Now)));
        
        
        QueryContainer Filter(QueryContainerDescriptor<NFTDropIndex> f) =>
            f.Bool(b => b.MustNot(mustNotQuery).Must(mustQuery));
        var dropList = await repository.GetListAsync(Filter, limit: 100);
        
        var dataList = objectMapper.Map<List<NFTDropIndex>, List<NFTDropInfoDto>>(dropList.Item2);
        var pageResult = new NFTDropPageResultDto
        {
            TotalRecordCount = dropList.Item1,
            Data = dataList
        };
        return pageResult;
    }
}