using AeFinder.Sdk;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    /*
    [Obsolete("todo V2, unuse")]
     [Name("seedBriefInfos")]
    public static async Task<SeedBriefInfoPageResultDto> SeedBriefInfos(
        [FromServices] IReadOnlyRepository<SeedSymbolIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetSeedBriefInfosDto dto)
    {
        if (dto == null)
        {
            return new SeedBriefInfoPageResultDto
            {
                TotalRecordCount = 0,
                Data = new List<SeedBriefInfoDto>()
            };
        }
        var queryable = await repository.GetQueryableAsync();

        var mustQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();
        var shouldQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();
        var shouldQuery2 = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();

        if (!dto.SearchParam.IsNullOrWhiteSpace())
        {
            queryable = queryable.Where(f => f.TokenName == (ForestIndexerConstants.SeedNamePrefix +
                                                          ForestIndexerConstants.SymbolSeparator + dto.SearchParam) || f.TokenName == dto.SearchParam);
        }

        if (!dto.ChainList.IsNullOrEmpty())
        {
            queryable = queryable.Where(f => dto.ChainList.Contains(f.ChainId));
        }

        if (!dto.SymbolTypeList.IsNullOrEmpty())
        {
            queryable = queryable.Where(f => dto.SymbolTypeList.Contains(f.TokenType));
        }
        queryable = queryable.Where(f => f.Supply > 0 && f.IsDeleteFlag == false);
       
        if (!dto.HasListingFlag && !dto.HasAuctionFlag && !dto.HasOfferFlag)
        {
            AddQueryForMinListingPriceAndMaxAuctionPrice(mustQuery, shouldQuery, dto);
        }
        if (dto.HasListingFlag)
        {
            AddQueryForMinListingPrice(mustQuery, dto);
            mustQuery.Add(q => q.Bool(b => b.Must(m => m
                .Term(i => i.Field(f => f.HasListingFlag).Value(dto.HasListingFlag)))));
            
        }
        if (dto.HasAuctionFlag)
        {
            AddQueryForMaxAuctionPrice(mustQuery, dto);
            mustQuery.Add(q => q.Bool(b => b.Must(m => m
                .Term(i => i.Field(f => f.HasAuctionFlag).Value(dto.HasAuctionFlag)))));
        }
        if (dto.HasOfferFlag)
        {
            mustQuery.Add(q => q.Bool(b => b.Must(m => m
                .Term(i => i.Field(f => f.HasOfferFlag).Value(dto.HasOfferFlag)))));
        }

        if (shouldQuery.Any()){
            mustQuery.Add(q => q.Bool(b => b.Should(shouldQuery)));
        }
        if (shouldQuery2.Any()){
            mustQuery.Add(q => q.Bool(b => b.Should(shouldQuery2)));
        }

        QueryContainer Filter(QueryContainerDescriptor<SeedSymbolIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var sort = GetSortForSeedBrife(dto.Sorting);
        var result = await repository.GetSortListAsync(Filter, sortFunc: sort,
            skip: dto.SkipCount,
            limit: dto.MaxResultCount);
        if (result == null || result.Item2 == null)
            return new SeedBriefInfoPageResultDto
            {
                TotalRecordCount = 0,
                Data = new List<SeedBriefInfoDto>()
            };
        var resultList = result?.Item2;
        var dataList = ConvertMap(resultList, dto);
        var pageResult = new SeedBriefInfoPageResultDto
        {
            TotalRecordCount = result.Item1,
            Data = dataList
        };
        return pageResult;
    }
    */

    /*private static List<SeedBriefInfoDto> ConvertMap(List<SeedSymbolIndex> resultList, GetSeedBriefInfosDto dto)
    {
        if (resultList.IsNullOrEmpty() || dto == null)
        {
            return new List<SeedBriefInfoDto>();
        }

        return resultList?.Select(item => MapForSeedBriefInfoDto(item, dto)).ToList();
    }

    private static SeedBriefInfoDto MapForSeedBriefInfoDto(SeedSymbolIndex seedSymbolIndex, GetSeedBriefInfosDto dto)
    {
        var temDescription = "";
        decimal temPrice = -1;
        
        if (seedSymbolIndex.HasAuctionFlag)
        {
            temDescription = ForestIndexerConstants.BrifeInfoDescriptionTopBid;
            temPrice = seedSymbolIndex.MaxAuctionPrice;
        }
        else if (seedSymbolIndex.HasListingFlag)
        {
            temDescription = ForestIndexerConstants.BrifeInfoDescriptionPrice;
            temPrice = seedSymbolIndex.MinListingPrice;
        }
        else if (seedSymbolIndex.HasOfferFlag)
        {
            temDescription = ForestIndexerConstants.BrifeInfoDescriptionOffer;
            temPrice = seedSymbolIndex.MaxOfferPrice;
        }

        return new SeedBriefInfoDto
        {
            CollectionSymbol = ForestIndexerConstants.SeedCollectionSymbol,
            NFTSymbol = seedSymbolIndex.SeedOwnedSymbol,
            PreviewImage = seedSymbolIndex.SeedImage,
            PriceDescription = temDescription,
            Price = temPrice,
            Id = seedSymbolIndex.Id,
            TokenName = seedSymbolIndex.TokenName,
            IssueChainId = seedSymbolIndex.IssueChainId,
            IssueChainIdStr = ChainHelper.ConvertChainIdToBase58(seedSymbolIndex.IssueChainId),
            ChainId = ChainHelper.ConvertBase58ToChainId(seedSymbolIndex.ChainId),
            ChainIdStr = seedSymbolIndex.ChainId
        };
    }

    private static void AddQueryForMinListingPriceAndMaxAuctionPrice(
        List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>> mustQuery,
        List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>> shouldQuery,
        GetSeedBriefInfosDto dto)
    {
        if (dto == null)
        {
            return;
        }

        if (dto.PriceLow != null && dto.PriceHigh != null)
        {
            shouldQuery.Add(q =>
                q.Bool(m =>
                    m.Must(q =>
                        q.Term(i =>
                            i.Field(f => f.HasListingFlag).Value(true))
                        &&
                        q.Range(i =>
                            i.Field(f => f.MinListingPrice).GreaterThanOrEquals(Convert.ToDouble(dto.PriceLow)))
                        && q.Range(i =>
                            i.Field(f => f.MinListingPrice).LessThanOrEquals(Convert.ToDouble(dto.PriceHigh))))));

            shouldQuery.Add(q =>
                q.Bool(m =>
                    m.Must(q =>
                        q.Term(i =>
                            i.Field(f => f.HasAuctionFlag).Value(true))
                        &&
                        q.Range(i =>
                            i.Field(f => f.MaxAuctionPrice).GreaterThanOrEquals(Convert.ToDouble(dto.PriceLow)))
                        && q.Range(i =>
                            i.Field(f => f.MaxAuctionPrice).LessThanOrEquals(Convert.ToDouble(dto.PriceHigh))))));
            
            
        }
        else
        {
            if (dto.PriceLow != null)
            {
                shouldQuery.Add(q =>
                    q.Bool(m =>
                        m.Must(q =>
                            q.Term(i =>
                                i.Field(f => f.HasListingFlag).Value(true))
                            &&
                            q.Range(i =>
                                i.Field(f => f.MinListingPrice).GreaterThanOrEquals(Convert.ToDouble(dto.PriceLow))))));
                shouldQuery.Add(q =>
                    q.Bool(m =>
                        m.Must(q =>
                            q.Term(i =>
                                i.Field(f => f.HasAuctionFlag).Value(true))
                            &&
                            q.Range(i =>
                                i.Field(f => f.MaxAuctionPrice).GreaterThanOrEquals(Convert.ToDouble(dto.PriceLow))))
                    ));
            }
            
            if (dto.PriceHigh != null)
            {
                shouldQuery.Add(q =>
                    q.Bool(m =>
                        m.Must(q =>
                            q.Term(i =>
                                i.Field(f => f.HasListingFlag).Value(true))
                            &&
                            q.Range(i =>
                                i.Field(f => f.MinListingPrice).LessThanOrEquals(Convert.ToDouble(dto.PriceHigh))))));
                shouldQuery.Add(q =>
                    q.Bool(m =>
                        m.Must(q =>
                            q.Term(i =>
                                i.Field(f => f.HasAuctionFlag).Value(true))
                            &&
                            q.Range(i =>
                                i.Field(f => f.MaxAuctionPrice).LessThanOrEquals(Convert.ToDouble(dto.PriceHigh))))));
            }
        }
        
    }
    
    private static void AddQueryForMinListingPrice(
        List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>> mustQuery,
        GetSeedBriefInfosDto dto)
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

    private static void AddQueryForMaxAuctionPrice(
        List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>> mustQuery,
        GetSeedBriefInfosDto dto)
    {
        if (dto == null)
        {
            return;
        }

        if (dto.PriceLow != null)
        {
            mustQuery.Add(q =>
                q.Range(i => i.Field(f => f.MaxAuctionPrice).GreaterThanOrEquals(Convert.ToDouble(dto.PriceLow))));
        }

        if (dto.PriceHigh != null)
        {
            mustQuery.Add(q => q.Range(i => i.Field(f => f.MaxAuctionPrice).GreaterThanOrEquals(0)));
            mustQuery.Add(q =>
                q.Range(i => i.Field(f => f.MaxAuctionPrice).LessThanOrEquals(Convert.ToDouble(dto.PriceHigh))));
        }
    }
    

    private static Func<SortDescriptor<SeedSymbolIndex>, IPromise<IList<ISort>>> GetSortForSeedBrife(string sorting)
    {
        if (string.IsNullOrWhiteSpace(sorting)) throw new NotSupportedException();
        SortDescriptor<SeedSymbolIndex> sortDescriptor = new SortDescriptor<SeedSymbolIndex>();

        var sortingArray = sorting.Split(" ");

        switch (sortingArray[0])
        {
            case "Low":
                sortDescriptor.Descending(a => a.HasListingFlag);
                sortDescriptor.Descending(a => a.HasAuctionFlag);
                sortDescriptor.Ascending(a => a.MinListingPrice);
                sortDescriptor.Ascending(a => a.MaxAuctionPrice);
                sortDescriptor.Ascending(a => a.MaxOfferPrice);
                sortDescriptor.Descending(a => a.CreateTime);
                break;
            case "High":
                sortDescriptor.Descending(a => a.HasListingFlag);
                sortDescriptor.Descending(a => a.HasAuctionFlag);
                sortDescriptor.Descending(a => a.MinListingPrice);
                sortDescriptor.Descending(a => a.MaxAuctionPrice);
                sortDescriptor.Ascending(a => a.MaxOfferPrice);
                sortDescriptor.Descending(a => a.CreateTime);
                break;
            case "Recently":
                sortDescriptor.Descending(a => a.CreateTime);
                break;
            default:
                sortDescriptor.Descending(a => a.LatestListingTime);
                sortDescriptor.Descending(a => a.AuctionDateTime);
                sortDescriptor.Descending(a => a.BlockHeight);
                break;
        }
        
        IPromise<IList<ISort>> promise = sortDescriptor;

        return s => promise;
    }
    */
    [Name("getSyncSeedSymbolRecords")]
    public static async Task<List<SeedSymbolSyncDto>> GetSyncSeedSymbolRecordsAsync(
        [FromServices] IReadOnlyRepository<SeedSymbolIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetChainBlockHeightDto dto)
    {
        var queryable = await repository.GetQueryableAsync();
        queryable = queryable.Where(f=>f.ChainId == dto.ChainId);

        if (dto.StartBlockHeight > 0)
        {
            queryable = queryable.Where(f=>f.BlockHeight >= dto.StartBlockHeight);
        }

        if (dto.EndBlockHeight > 0)
        {
            queryable = queryable.Where(f=>f.BlockHeight <= dto.EndBlockHeight);
        }

        var result = queryable.OrderBy(o => o.BlockHeight).ToList();
        if (result.IsNullOrEmpty())
        {
            return new List<SeedSymbolSyncDto>();
        }

        return objectMapper.Map<List<SeedSymbolIndex>, List<SeedSymbolSyncDto>>(result);
    }
    
    [Name("getSyncSeedSymbolRecord")]
    public static async Task<SeedSymbolSyncDto> GetSyncSeedSymbolRecordAsync(
        [FromServices] IReadOnlyRepository<SeedSymbolIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetSyncSeedSymbolRecordDto dto)
    {
        if (dto == null || dto.Id.IsNullOrEmpty() || dto.ChainId.IsNullOrEmpty()) return null;
        var queryable = await repository.GetQueryableAsync();
        queryable = queryable.Where(f=>f.Id == dto.Id);

        var result = queryable.Skip(0).Take(1).ToList();
        if (result.IsNullOrEmpty())
        {
            return null;
        }
        return objectMapper.Map<SeedSymbolIndex, SeedSymbolSyncDto>(result.FirstOrDefault());
    }
}