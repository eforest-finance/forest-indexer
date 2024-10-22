using AeFinder.Sdk;
using AElf;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Nest;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("nftInfos")]
    public static async Task<NFTInfoPageResultDto> NFTInfos(
        [FromServices] IReadOnlyRepository<NFTInfoIndex> repository,
        [FromServices] IReadOnlyRepository<UserBalanceIndex> userBalanceAppService,
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
        var repositoryQueryable = await repository.GetQueryableAsync();
        
        var sorting = GetSorting(dto.Sorting);

        var sortingForUserBalance = GetSortingForUserBalance(dto.Sorting);
        var sortingArray = dto.Sorting.Split(" ");

        if (dto.Status == ForestIndexerConstants.NFTInfoQueryStatusBuy)
        {
            if (dto.PriceLow != null && dto.PriceLow != 0)
            {
                repositoryQueryable = repositoryQueryable.Where(f => f.ListingPrice >= (decimal)dto.PriceLow);
            }
            
            if (dto.PriceHigh !=null && dto.PriceHigh != 0)
            {
                repositoryQueryable = repositoryQueryable.Where(f => f.ListingPrice >= 0);
                repositoryQueryable = repositoryQueryable.Where(f => f.ListingPrice <= (decimal)dto.PriceHigh);
            }

            if (!dto.Address.IsNullOrEmpty())
                repositoryQueryable = repositoryQueryable.Where(f => f.ListingAddress != dto.Address);
        }
        else if (dto.Status == ForestIndexerConstants.NFTInfoQueryStatusSelf)
        {
            var userBalanceQueryable= await userBalanceAppService.GetQueryableAsync();
            userBalanceQueryable = userBalanceQueryable.Where(f => f.Address == dto.Address);
            userBalanceQueryable = userBalanceQueryable.Where(f => f.Amount > 0);
            userBalanceQueryable = userBalanceQueryable.Where(f => !f.Symbol.StartsWith("SEED-"));
            

            if (dto.PriceLow !=null && dto.PriceLow != 0)
                userBalanceQueryable = userBalanceQueryable.Where(f => f.ListingPrice >= (decimal)dto.PriceLow);

            if (dto.PriceHigh !=null && dto.PriceHigh != 0)
            {
                userBalanceQueryable = userBalanceQueryable.Where(f => f.ListingPrice >=0 && f.ListingPrice <= (decimal)dto.PriceHigh);
            }

            List<UserBalanceIndex> resultUserBalanceIndex;
            switch (sortingArray[0])
            {
                case "ListingTime":
                    if (sortingArray[1] == "ASC")
                    {
                        resultUserBalanceIndex = userBalanceQueryable.Skip(dto.SkipCount).Take(dto.MaxResultCount)
                            .OrderBy(o => o.ListingTime).ToList();
                    }
                    else
                    {
                        resultUserBalanceIndex = userBalanceQueryable.Skip(dto.SkipCount).Take(dto.MaxResultCount)
                            .OrderByDescending(o => o.ListingTime).ToList();
                    }
                    break;
                case "ListingPrice":
                    if (sortingArray[1] == "ASC")
                    {
                        resultUserBalanceIndex = userBalanceQueryable.Skip(dto.SkipCount).Take(dto.MaxResultCount)
                            .OrderBy(o => o.ListingPrice).ToList();
                    }
                    else
                    {
                        resultUserBalanceIndex = userBalanceQueryable.Skip(dto.SkipCount).Take(dto.MaxResultCount)
                            .OrderByDescending(o => o.ListingPrice).ToList();
                    }
                    break;

                default:
                    resultUserBalanceIndex = userBalanceQueryable.Skip(dto.SkipCount).Take(dto.MaxResultCount)
                        .OrderByDescending(o => o.ListingTime).ToList();
                    break;
            }
            
            if (!resultUserBalanceIndex.IsNullOrEmpty()
                && resultUserBalanceIndex.Count > 0)
            {
                status2Count = resultUserBalanceIndex.Count;
                var nftIds = resultUserBalanceIndex.Select(o => o.NFTInfoId).ToList();
                if (nftIds != null && nftIds.Count != 0)
                {
                    repositoryQueryable = repositoryQueryable.Where(f => nftIds.Contains(f.Id));

                }
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
                repositoryQueryable = repositoryQueryable.Where(f => f.IssueManagerSet.Contains(dto.IssueAddress));
            
            if (dto.PriceLow != null && dto.PriceLow != 0)
                repositoryQueryable = repositoryQueryable.Where(f => f.ListingPrice >= (decimal)(dto.PriceLow));

            if (dto.PriceHigh != null && dto.PriceHigh != 0)
            {
                repositoryQueryable = repositoryQueryable.Where(f => f.ListingPrice >= 0 && f.ListingPrice <= (decimal)dto.PriceHigh);
            }
        }

        if (dto.NFTInfoIds != null) 
            repositoryQueryable = repositoryQueryable.Where(f => dto.NFTInfoIds.Contains(f.Id) );

        if (!dto.NftCollectionId.IsNullOrEmpty())
            repositoryQueryable = repositoryQueryable.Where(f => f.CollectionId == dto.NftCollectionId );

        List<NFTInfoIndex> result = null;
        switch (sortingArray[0])
        {
            case "ListingTime":
                if (sortingArray[1] == "ASC")
                {
                    result = repositoryQueryable.Skip(dto.Status == ForestIndexerConstants.NFTInfoQueryStatusSelf ? 0 : dto.SkipCount).Take(dto.MaxResultCount)
                        .OrderBy(o => o.LatestListingTime).ToList();
                }
                else
                {
                    result = repositoryQueryable.Skip(dto.Status == ForestIndexerConstants.NFTInfoQueryStatusSelf ? 0 : dto.SkipCount).Take(dto.MaxResultCount)
                        .OrderByDescending(o => o.LatestListingTime).ToList();
                }
                break;
            case "ListingPrice":
                if (sortingArray[1] == "ASC")
                {
                    result = repositoryQueryable.Skip(dto.Status == ForestIndexerConstants.NFTInfoQueryStatusSelf ? 0 : dto.SkipCount).Take(dto.MaxResultCount)
                        .OrderBy(o => o.ListingPrice).ToList();
                }
                else
                {
                    result = repositoryQueryable.Skip(dto.Status == ForestIndexerConstants.NFTInfoQueryStatusSelf ? 0 : dto.SkipCount).Take(dto.MaxResultCount)
                        .OrderByDescending(o => o.ListingPrice).ToList();
                }
                break;

            default:
                result = repositoryQueryable.Skip(dto.Status == ForestIndexerConstants.NFTInfoQueryStatusSelf ? 0 : dto.SkipCount).Take(dto.MaxResultCount)
                    .OrderByDescending(o => o.LatestListingTime).ToList();
                break;
        }
        
        if (result.IsNullOrEmpty())
            return new NFTInfoPageResultDto
            {
                TotalRecordCount = 0,
                Data = new List<NFTInfoDto>()
            };
        var resultList = result;
        var dataList = resultList?.Select(item => objectMapper.Map<NFTInfoIndex, NFTInfoDto>(item)).ToList();

        var pageResult = new NFTInfoPageResultDto
        {
            TotalRecordCount =
                dto.Status == ForestIndexerConstants.NFTInfoQueryStatusSelf ? status2Count : result.Count,
            Data = dataList
        };
        return pageResult;
    }
    
    /*[Obsolete("todo V2, unuse")]
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
    }*/

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