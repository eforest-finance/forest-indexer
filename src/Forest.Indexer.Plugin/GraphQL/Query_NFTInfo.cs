using System.Linq.Expressions;
using AElf;
using AElf.Types;
using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using GraphQL;
using Nest;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("nftInfo")]
    public static async Task<NFTInfoDto> NFTInfo(
        [FromServices] IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> nftInfoRepo,
        [FromServices] IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> seedSymbolRepo,
        [FromServices] IAElfIndexerClientEntityRepository<WhitelistIndex, LogEventInfo> whiteListRepo,
        [FromServices] IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> userBalanceRepo,
        [FromServices] IObjectMapper objectMapper,
        GetNFTInfoDto dto)
    {
        if (dto == null || dto.Id.IsNullOrWhiteSpace()) return null;

        var isSeed = dto.Id.Match(ForestIndexerConstants.SeedIdPattern);
        var res = isSeed
            ? objectMapper.Map<SeedSymbolIndex, NFTInfoDto>(await seedSymbolRepo.GetAsync(dto.Id))
            : objectMapper.Map<NFTInfoIndex, NFTInfoDto>(await nftInfoRepo.GetAsync(dto.Id));
        if (res == null) return null;

        if (isSeed)
        {
            res.SeedOwnedSymbol = EnumDescriptionHelper.GetExtraInfoValue(res.ExternalInfoDictionary,
                TokenCreatedExternalInfoEnum.SeedOwnedSymbol, res.TokenName);
        }

        var userBalanceQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(index => index.NFTInfoId).Value(res.Id)),
            q => q.Range(i => i.Field(index => index.Amount).GreaterThan(0))
        };

        QueryContainer UserBalanceFilter(QueryContainerDescriptor<UserBalanceIndex> f) =>
            f.Bool(b => b.Must(userBalanceQuery));

        var userBalanceIndexList = await userBalanceRepo.GetListAsync(UserBalanceFilter, limit: 1);

        res.Owner = userBalanceIndexList?.Item1 > 0 ? userBalanceIndexList.Item2[0].Address : string.Empty;
        res.OwnerCount = userBalanceIndexList?.Item1 == null ? 0 : userBalanceIndexList.Item1;
        if (dto.Address.IsNullOrEmpty()) return res;

        // generate projectId with same logic of contract
        var projectId = HashHelper.ComputeFrom(res.Symbol + Address.FromBase58(dto.Address));
        
        // query user Whitelist
        var whitelistQuery = new List<Func<QueryContainerDescriptor<WhitelistIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(index => index.ChainId).Value(res.ChainId)),
            q => q.Term(i => i.Field(index => index.ProjectId).Value(projectId.ToHex()))
        };

        QueryContainer WhitelistFilter(QueryContainerDescriptor<WhitelistIndex> f) =>
            f.Bool(b => b.Must(whitelistQuery));

        var whitelistIndex = await whiteListRepo.GetAsync(WhitelistFilter);
        if (whitelistIndex != null) res.WhitelistId = whitelistIndex.Id;


        return res;
    }

    [Name("nftSymbol")]
    public static async Task<SymbolDto> NFTSymbol(
        [FromServices] IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> repository,
        [FromServices] IObjectMapper objectMapper,
        string symbol)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoIndex>, QueryContainer>>();
        if (symbol.IsNullOrWhiteSpace()) return new SymbolDto();

        mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(symbol)));

        QueryContainer Filter(QueryContainerDescriptor<NFTInfoIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await repository.GetListAsync(Filter, skip: 0, limit: 1);
        if (result == null) return new SymbolDto();

        return new SymbolDto
        {
            Symbol = result.Item2?.FirstOrDefault(new NFTInfoIndex())?.Symbol
        };
    }

    [Name("getSyncNftInfoRecords")]
    public static async Task<List<NFTInfoSyncDto>> GetSyncNftInfoRecordsAsync(
        [FromServices] IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> repository,
        [FromServices] IObjectMapper objectMapper,
        GetChainBlockHeightDto dto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoIndex>, QueryContainer>>();
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

        QueryContainer Filter(QueryContainerDescriptor<NFTInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var result = await repository.GetListAsync(Filter, 
            sortType: SortOrder.Ascending, sortExp: o => o.BlockHeight);
        return objectMapper.Map<List<NFTInfoIndex>, List<NFTInfoSyncDto>>(result.Item2);
    }

    private static Tuple<SortOrder, Expression<Func<NFTInfoIndex, object>>> GetSorting(string sorting)
    {
        if (string.IsNullOrWhiteSpace(sorting)) throw new NotSupportedException();

        var result = new Tuple<SortOrder, Expression<Func<NFTInfoIndex, object>>>(SortOrder.Ascending,
            o => o.LatestListingTime);

        var sortingArray = sorting.Split(" ");

        switch (sortingArray[0])
        {
            case "ListingTime":
                result = new Tuple<SortOrder, Expression<Func<NFTInfoIndex, object>>>(
                    sortingArray[1] == "ASC" ? SortOrder.Ascending : SortOrder.Descending,
                    o => o.LatestListingTime);
                break;
            case "ListingPrice":
                result = new Tuple<SortOrder, Expression<Func<NFTInfoIndex, object>>>(
                    sortingArray[1] == "ASC" ? SortOrder.Ascending : SortOrder.Descending,
                    o => o.ListingPrice);
                break;

            default:
                result = new Tuple<SortOrder, Expression<Func<NFTInfoIndex, object>>>(
                    SortOrder.Descending,
                    o => o.LatestListingTime);
                break;
        }


        return result;
    }

    private static Tuple<SortOrder, Expression<Func<UserBalanceIndex, object>>> GetSortingForUserBalance(
        string sorting)
    {
        if (string.IsNullOrWhiteSpace(sorting)) throw new NotSupportedException();

        var result = new Tuple<SortOrder, Expression<Func<UserBalanceIndex, object>>>(SortOrder.Ascending,
            o => o.ListingTime);

        var sortingArray = sorting.Split(" ");
        switch (sortingArray[0])
        {
            case "ListingTime":
                result = new Tuple<SortOrder, Expression<Func<UserBalanceIndex, object>>>(
                    sortingArray[1] == "ASC" ? SortOrder.Ascending : SortOrder.Descending,
                    o => o.ListingTime);
                break;
            case "ListingPrice":
                result = new Tuple<SortOrder, Expression<Func<UserBalanceIndex, object>>>(
                    sortingArray[1] == "ASC" ? SortOrder.Ascending : SortOrder.Descending,
                    o => o.ListingPrice);
                break;

            default:
                result = new Tuple<SortOrder, Expression<Func<UserBalanceIndex, object>>>(
                    SortOrder.Descending,
                    o => o.ListingTime);
                break;
        }

        return result;
    }
}