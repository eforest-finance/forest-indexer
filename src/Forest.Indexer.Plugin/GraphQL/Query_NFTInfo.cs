using AeFinder.Sdk;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using GraphQL;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("nftInfo")]
    public static async Task<NFTInfoDto> NFTInfo(
        [FromServices] IReadOnlyRepository<NFTInfoIndex> nftInfoRepo,
        [FromServices] IReadOnlyRepository<SeedSymbolIndex> seedSymbolRepo,
        [FromServices] IReadOnlyRepository<UserBalanceIndex> userBalanceRepo,
        [FromServices] IObjectMapper objectMapper,
        GetNFTInfoDto dto)
    {
        if (dto == null || dto.Id.IsNullOrWhiteSpace()) return null;
        var nftInfoQueryable = await nftInfoRepo.GetQueryableAsync();
        var seedSymbolQueryable = await seedSymbolRepo.GetQueryableAsync();
        var userBalanceQueryable = await userBalanceRepo.GetQueryableAsync();

        var isSeed = dto.Id.Match(ForestIndexerConstants.SeedIdPattern);
        var res = new NFTInfoDto();
        if (isSeed)
        {
            seedSymbolQueryable = seedSymbolQueryable.Where(i => i.Id == dto.Id);
            var result = seedSymbolQueryable.Skip(0).Take(1).ToList();
            if (result.IsNullOrEmpty())
            {
                res = null;
            }
            else
            {
                res = objectMapper.Map<SeedSymbolIndex, NFTInfoDto>(result.FirstOrDefault());
            }
        }
        else
        {
            nftInfoQueryable = nftInfoQueryable.Where(i => i.Id == dto.Id);
            var result = nftInfoQueryable.Skip(0).Take(1).ToList();
            if (result.IsNullOrEmpty())
            {
                res = null;
            }
            else
            {
                res = objectMapper.Map<NFTInfoIndex, NFTInfoDto>(result.FirstOrDefault());

            }
        }
        
        if (res == null) return null;

        if (isSeed)
        {
            res.SeedOwnedSymbol = EnumDescriptionHelper.GetExtraInfoValue(res.ExternalInfoDictionary,
                TokenCreatedExternalInfoEnum.SeedOwnedSymbol, res.TokenName);
        }

        userBalanceQueryable = userBalanceQueryable.Where(index => index.NFTInfoId == res.Id);
        userBalanceQueryable = userBalanceQueryable.Where(index => index.Amount > 0);

        var userBalanceIndexList = userBalanceQueryable.Skip(0).Take(1).ToList();

        res.Owner = userBalanceIndexList?.Count > 0 ? userBalanceIndexList.FirstOrDefault().Address : string.Empty;
        res.OwnerCount = userBalanceIndexList.IsNullOrEmpty() ? 0 : userBalanceIndexList.Count;
        if (dto.Address.IsNullOrEmpty()) return res;

        return res;
    }

    [Name("nftSymbol")]
    public static async Task<SymbolDto> NFTSymbol(
        [FromServices] IReadOnlyRepository<NFTInfoIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        string symbol)
    {
        var queryable = await repository.GetQueryableAsync();
        if (symbol.IsNullOrWhiteSpace()) return new SymbolDto();

        queryable = queryable.Where(f => f.Symbol == symbol);

        var result = queryable.Skip(0).Take(1).ToList();
        if (result.IsNullOrEmpty()) return new SymbolDto();

        return new SymbolDto
        {
            Symbol = result?.FirstOrDefault(new NFTInfoIndex())?.Symbol
        };
    }

    [Name("getSyncNftInfoRecords")]
    public static async Task<List<NFTInfoSyncDto>> GetSyncNftInfoRecordsAsync(
        [FromServices] IReadOnlyRepository<NFTInfoIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetChainBlockHeightDto dto)
    {
        var queryable = await repository.GetQueryableAsync();
        queryable = queryable.Where(f => f.ChainId == dto.ChainId);
        
        if (dto.StartBlockHeight > 0)
        {
            queryable = queryable.Where(f => f.BlockHeight >= dto.StartBlockHeight);
        }

        if (dto.EndBlockHeight > 0)
        {
            queryable = queryable.Where(f => f.BlockHeight <= dto.EndBlockHeight);
        }

        var result = queryable.OrderBy(o => o.BlockHeight).ToList();
        if (result.IsNullOrEmpty())
        {
            return new List<NFTInfoSyncDto>();
        }

        return objectMapper.Map<List<NFTInfoIndex>, List<NFTInfoSyncDto>>(result);
    }

    [Name("getSyncNFTInfoRecord")]
    public static async Task<NFTInfoSyncDto> GetSyncNFTInfoRecordAsync(
        [FromServices] IReadOnlyRepository<NFTInfoIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetSyncNFTInfoRecordDto dto)
    {
        if (dto == null || dto.Id.IsNullOrEmpty() || dto.ChainId.IsNullOrEmpty()) return null;
        var queryable = await repository.GetQueryableAsync();
        queryable = queryable.Where(f => f.Id == dto.Id);

        var result = queryable.Skip(0).Take(1).ToList().FirstOrDefault();
        if (result == null)
        {
            return null;
        }

        return objectMapper.Map<NFTInfoIndex, NFTInfoSyncDto>(result);
    }

    /*private static Tuple<SortOrder, Expression<Func<NFTInfoIndex, object>>> GetSorting(string sorting)
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
    }*/

    /*private static Tuple<SortOrder, Expression<Func<UserBalanceIndex, object>>> GetSortingForUserBalance(
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
    }*/
}