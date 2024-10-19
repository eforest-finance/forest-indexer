using System.Linq.Expressions;
using AElf;
using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Nest;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("nftInfos")]
    public static async Task<NFTInfoPageResultDto> NFTInfos(
        [FromServices] IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> repository,
        [FromServices] IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> userBalanceAppService,
        [FromServices] IObjectMapper objectMapper,
        GetNFTInfosDto dto)
    {
        if (dto == null)
            return new NFTInfoPageResultDto
            {
                TotalRecordCount = 0,
                Data = new List<NFTInfoDto>()
            };
        var status2Count = 0L;
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoIndex>, QueryContainer>>();
        var mustNotQuery = new List<Func<QueryContainerDescriptor<NFTInfoIndex>, QueryContainer>>();
        var sorting = GetSorting(dto.Sorting);

        var sortingForUserBalance = GetSortingForUserBalance(dto.Sorting);

        if (dto.Status == ForestIndexerConstants.NFTInfoQueryStatusBuy)
        {
            if (dto.PriceLow !=null && dto.PriceLow != 0)
                mustQuery.Add(q => q.Range(i => i.Field(f => f.ListingPrice).GreaterThanOrEquals(dto.PriceLow)));

            if (dto.PriceHigh !=null && dto.PriceHigh != 0)
            {
                mustQuery.Add(q => q.Range(i => i.Field(f => f.ListingPrice).GreaterThanOrEquals(0)));
                mustQuery.Add(q => q.Range(i => i.Field(f => f.ListingPrice).LessThanOrEquals(dto.PriceHigh)));
            }

            if (!dto.Address.IsNullOrEmpty())
                mustNotQuery.Add(q => q.Term(i => i.Field(f => f.ListingAddress).Value(dto.Address)));
        }
        else if (dto.Status == ForestIndexerConstants.NFTInfoQueryStatusSelf)
        {
            var userBalanceMustQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>();
            userBalanceMustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(dto.Address)));
            userBalanceMustQuery.Add(q => q.Range(i => i.Field(f => f.Amount).GreaterThan(0)));
            userBalanceMustQuery.Add(q=>q.Script(scriptDescriptor => scriptDescriptor
                .Script(script => script
                    .Source(ForestIndexerConstants.UserBalanceScriptForNft)
                    .Lang(ForestIndexerConstants.Painless))));

            if (dto.PriceLow !=null && dto.PriceLow != 0)
                userBalanceMustQuery.Add(q =>
                    q.Range(i => i.Field(f => f.ListingPrice).GreaterThanOrEquals(dto.PriceLow)));

            if (dto.PriceHigh !=null && dto.PriceHigh != 0)
            {
                userBalanceMustQuery.Add(q => q.Range(i => i.Field(f => f.ListingPrice).GreaterThanOrEquals(0)));
                userBalanceMustQuery.Add(
                    q =>
                        q.Range(i => i.Field(f => f.ListingPrice).LessThanOrEquals(dto.PriceHigh)));
            }

            QueryContainer FilterForUserBalance(QueryContainerDescriptor<UserBalanceIndex> f)
            {
                return f.Bool(b => b.Must(userBalanceMustQuery));
            }

            var resultUserBalanceIndex = await userBalanceAppService.GetListAsync(FilterForUserBalance,
                sortType: sortingForUserBalance.Item1,
                sortExp: sortingForUserBalance.Item2, skip: dto.SkipCount, limit: dto.MaxResultCount);
            if (resultUserBalanceIndex?.Item2 != null
                && resultUserBalanceIndex.Item2.Count > 0)
            {
                status2Count = resultUserBalanceIndex.Item1;
                var nftIds = resultUserBalanceIndex.Item2.Select(o => o.NFTInfoId).ToList();
                if (nftIds != null && nftIds.Count != 0) mustQuery.Add(q => q.Ids(i => i.Values(nftIds)));
            }
            else
            {
                return new NFTInfoPageResultDto
                {
                    TotalRecordCount = 0,
                    Data = new List<NFTInfoDto>()
                };
            }
        }
        else
        {
            if (!dto.IssueAddress.IsNullOrEmpty())
                mustQuery.Add(q => q.Terms(i => i.Field(f => f.IssueManagerSet).Terms(dto.IssueAddress)));
            if (dto.PriceLow != null && dto.PriceLow != 0)
                mustQuery.Add(q => q.Range(i => i.Field(f => f.ListingPrice).GreaterThanOrEquals(dto.PriceLow)));

            if (dto.PriceHigh != null && dto.PriceHigh != 0)
            {
                mustQuery.Add(q => q.Range(i => i.Field(f => f.ListingPrice).GreaterThanOrEquals(0)));
                mustQuery.Add(q => q.Range(i => i.Field(f => f.ListingPrice).LessThanOrEquals(dto.PriceHigh)));
            }
        }

        if (dto.NFTInfoIds != null) mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(dto.NFTInfoIds)));

        if (!dto.NftCollectionId.IsNullOrEmpty())
            mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionId).Value(dto.NftCollectionId)));

        QueryContainer Filter(QueryContainerDescriptor<NFTInfoIndex> f)
        {
            return f.Bool(b => b.Must(mustQuery).MustNot(mustNotQuery));
        }

        var result = await repository.GetListAsync(Filter, sortType: sorting.Item1,
            sortExp: sorting.Item2,
            skip: dto.Status == ForestIndexerConstants.NFTInfoQueryStatusSelf ? 0 : dto.SkipCount,
            limit: dto.MaxResultCount);
        if (result == null || result.Item2 == null)
            return new NFTInfoPageResultDto
            {
                TotalRecordCount = 0,
                Data = new List<NFTInfoDto>()
            };
        var resultList = result?.Item2;
        var dataList = resultList?.Select(item => objectMapper.Map<NFTInfoIndex, NFTInfoDto>(item)).ToList();

        var pageResult = new NFTInfoPageResultDto
        {
            TotalRecordCount =
                dto.Status == ForestIndexerConstants.NFTInfoQueryStatusSelf ? status2Count : result.Item1,
            Data = dataList
        };
        return pageResult;
    }
    
    [Name("nftBriefInfos")]
    public static async Task<NFTBriefInfoPageResultDto> NFTBriefInfos(
        [FromServices] IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> repository,
        GetNFTBriefInfosDto dto)
    {
        if (dto == null)
        {
            return new NFTBriefInfoPageResultDto
            {
                TotalRecordCount = 0,
                Data = new List<NFTBriefInfoDto>()
            };
        }
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoIndex>, QueryContainer>>();
        var shouldQuery = new List<Func<QueryContainerDescriptor<NFTInfoIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionId).Value(dto.CollectionId)));
        
        if (!dto.SearchParam.IsNullOrEmpty())
        {
            shouldQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(dto.SearchParam)));
            shouldQuery.Add(q => q.Term(i => i.Field(f => f.TokenName).Value(dto.SearchParam)));
        }

        if (!dto.ChainList.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.ChainId).Terms(dto.ChainList)));
        }
        mustQuery.Add(q =>
            q.Range(i => i.Field(f => f.Supply).GreaterThan(0)));

        if (!dto.HasListingFlag && !dto.HasOfferFlag)
        {
            AddQueryForMinListingPrice(mustQuery, dto);
        }
        if (dto.HasListingFlag)
        {
            AddQueryForMinListingPrice(mustQuery, dto);
            mustQuery.Add(q => q.Bool(b => b.Must(m => m
                .Term(i => i.Field(f => f.HasListingFlag).Value(dto.HasListingFlag)))));
        }
        if (dto.HasOfferFlag)
        {
            mustQuery.Add(q => q.Bool(b => b.Must(m => m
                .Term(i => i.Field(f => f.HasOfferFlag).Value(dto.HasOfferFlag)))));
        }
        
        if (shouldQuery.Any()){
            mustQuery.Add(q => q.Bool(b => b.Should(shouldQuery)));
        }

        QueryContainer Filter(QueryContainerDescriptor<NFTInfoIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var sort = GetSortForNFTBrife(dto.Sorting);
        var result = await repository.GetSortListAsync(Filter, sortFunc: sort,
            skip: dto.SkipCount,
            limit: dto.MaxResultCount);
        if (result == null || result.Item2 == null)
            return new NFTBriefInfoPageResultDto
            {
                TotalRecordCount = 0,
                Data = new List<NFTBriefInfoDto>()
            };
        var resultList = result?.Item2;
        var dataList = ConvertMap(resultList, dto);
        var pageResult = new NFTBriefInfoPageResultDto
        {
            TotalRecordCount = result.Item1,
            Data = dataList
        };
        return pageResult;
    }

    private static Func<SortDescriptor<NFTInfoIndex>, IPromise<IList<ISort>>> GetSortForNFTBrife(string sorting)
    {
        if (string.IsNullOrWhiteSpace(sorting)) throw new NotSupportedException();
        SortDescriptor<NFTInfoIndex> sortDescriptor = new SortDescriptor<NFTInfoIndex>();
        
        var sortingArray = sorting.Split(" ");
        
        switch (sortingArray[0])
        {
            case "Low":
                sortDescriptor.Descending(a => a.HasListingFlag);
                sortDescriptor.Ascending(a => a.MinListingPrice);
                sortDescriptor.Descending(a => a.CreateTime);
                break;
            case "High":
                sortDescriptor.Descending(a => a.HasListingFlag);
                sortDescriptor.Descending(a => a.MinListingPrice);
                sortDescriptor.Descending(a => a.CreateTime);
                break;
            case "Recently":
                sortDescriptor.Descending(a => a.CreateTime);
                break;
            default:
                sortDescriptor.Descending(a => a.LatestListingTime);
                sortDescriptor.Descending(a => a.BlockHeight);
                break;
        }
        
        IPromise<IList<ISort>> promise = sortDescriptor;

        return s => promise;
    }

    private static List<NFTBriefInfoDto> ConvertMap(List<NFTInfoIndex> resultList, GetNFTBriefInfosDto dto)
    {
        if (resultList.IsNullOrEmpty() || dto == null)
        {
            return new List<NFTBriefInfoDto>();
        }

        return resultList?.Select(item => MapForNFTBriefInfoDto(item, dto)).ToList();
    }

    private static NFTBriefInfoDto MapForNFTBriefInfoDto(NFTInfoIndex nftInfoIndex, GetNFTBriefInfosDto dto)
    {
        var temDescription = "";
        decimal temPrice = -1;
        if (nftInfoIndex.HasListingFlag)
        {
            temDescription = ForestIndexerConstants.BrifeInfoDescriptionPrice;
            temPrice = nftInfoIndex.MinListingPrice;
        }
        else if (nftInfoIndex.HasOfferFlag)
        {
            temDescription = ForestIndexerConstants.BrifeInfoDescriptionOffer;
            temPrice = nftInfoIndex.MaxOfferPrice;
        }

        return new NFTBriefInfoDto
        {
            CollectionSymbol = nftInfoIndex.CollectionSymbol,
            NFTSymbol = nftInfoIndex.Symbol,
            PreviewImage = nftInfoIndex.ImageUrl,
            PriceDescription = temDescription,
            Price = temPrice,
            Id = nftInfoIndex.Id,
            TokenName = nftInfoIndex.TokenName,
            IssueChainId = nftInfoIndex.IssueChainId,
            IssueChainIdStr = ChainHelper.ConvertChainIdToBase58(nftInfoIndex.IssueChainId),
            ChainId = ChainHelper.ConvertBase58ToChainId(nftInfoIndex.ChainId),
            ChainIdStr = nftInfoIndex.ChainId
            
        };
    }

    private static void AddQueryForMinListingPrice(
        List<Func<QueryContainerDescriptor<NFTInfoIndex>, QueryContainer>> mustQuery,
        GetNFTBriefInfosDto dto)
    {
        if (dto == null)
        {
            return;
        }

        if (dto.PriceLow != null)
        {
            mustQuery.Add(q =>
                q.Range(i => i.Field(f => f.MinListingPrice).GreaterThanOrEquals(Convert.ToDouble(dto.PriceLow))));
        }

        if (dto.PriceHigh != null)
        {
            mustQuery.Add(q => q.Range(i => i.Field(f => f.MinListingPrice).GreaterThanOrEquals(0)));
            mustQuery.Add(q =>
                q.Range(i => i.Field(f => f.MinListingPrice).LessThanOrEquals(Convert.ToDouble(dto.PriceHigh))));
        }
    }
}