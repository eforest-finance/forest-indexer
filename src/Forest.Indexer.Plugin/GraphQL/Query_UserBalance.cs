using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
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
        [FromServices] IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> userBalanceRepo,
        [FromServices] IObjectMapper objectMapper,
        GetUserBalanceDto dto)
    {
        var userBalanceQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>
        {
            q => q.Term(i 
                => i.Field(index => index.NFTInfoId).Value(dto.nftInfoId)),
            q => q.Range(i 
                => i.Field(index => index.Amount).GreaterThan(0))
        };

        QueryContainer UserBalanceFilter(QueryContainerDescriptor<UserBalanceIndex> f) =>
            f.Bool(b => b.Must(userBalanceQuery));

        var result = await userBalanceRepo.GetListAsync(UserBalanceFilter, limit: 1);

        return new NFTUserBalanceDto
        {
            Owner = result?.Item1 > 0 ? result.Item2[0].Address : string.Empty,
            OwnerCount = result?.Item1 ?? 0
        };
    }
    
    [Name("queryUserNftIds")]
    public static async Task<UserMatchedNftIds> QueryUserNftIdsAsync(
        [FromServices] IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> userBalanceRepository,
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
}