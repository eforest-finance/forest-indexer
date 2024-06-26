using AElf.Contracts.Whitelist;
using AElfIndexer.Client;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Processors;
using Google.Type;
using GraphQL;
using Microsoft.Extensions.Logging;
using Nest;
using NFTMarketServer.NFT;
using Orleans;
using Volo.Abp.ObjectMapping;
using DateTime = System.DateTime;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    private const string SortTypeNumber = "number";
    private const string SortScriptSourceValueLength = "doc['seedOwnedSymbol'].value.length()";
    
    [Name("syncState")]
    public static async Task<SyncStateDto> SyncState(
        [FromServices] IClusterClient clusterClient, [FromServices] IAElfIndexerClientInfoProvider clientInfoProvider,
        [FromServices] IObjectMapper objectMapper, GetSyncStateDto dto)
    {
        var version = clientInfoProvider.GetVersion();
        var clientId = clientInfoProvider.GetClientId();
        var blockStateSetInfoGrain =
            clusterClient.GetGrain<IBlockStateSetInfoGrain>(
                GrainIdHelper.GenerateGrainId("BlockStateSetInfo", clientId, dto.ChainId, version));
        var confirmedHeight = await blockStateSetInfoGrain.GetConfirmedBlockHeight(dto.FilterType);
        return new SyncStateDto
        {
            ConfirmedBlockHeight = confirmedHeight
        };
    }

    [Name("seedSymbols")]
    public static async Task<SeedSymbolPageResultDto> SeedSymbols(
        [FromServices] IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> repository,
        [FromServices] IObjectMapper objectMapper,
        GetSeedSymbolsDto dto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();
        if (dto == null || dto.Address.IsNullOrEmpty())
        {
            return new SeedSymbolPageResultDto
            {
                TotalRecordCount = 0,
                Data = new List<SeedSymbolDto>()
            };
        }

        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.IssuerTo).Value(dto.Address)));

        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.IsDeleteFlag).Value(false)));
        
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.TokenType).Value(TokenType.NFT)));
        
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.ChainId).Value(ForestIndexerConstants.MainChain)));

        mustQuery.Add(q=>
                q.DateRange(i =>
                    i.Field(f => f.SeedExpTime)
                        .GreaterThan(DateTime.Now))
            );

        if (!dto.SeedOwnedSymbol.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Wildcard(i =>
                i.Field(f => f.SeedOwnedSymbol).Value("*" + dto.SeedOwnedSymbol + "*")));
        }

        QueryContainer Filter(QueryContainerDescriptor<SeedSymbolIndex> f) => f.Bool(b => b.Must(mustQuery));


        IPromise<IList<ISort>> Sort(SortDescriptor<SeedSymbolIndex> s) =>
            s.Script(script => script.Type(SortTypeNumber)
                    .Script(scriptDescriptor => scriptDescriptor.Source(SortScriptSourceValueLength))
                    .Order(SortOrder.Ascending))
                .Ascending(a => a.SeedOwnedSymbol)
                .Ascending(a => a.Id);

        var result = await repository.GetSortListAsync(Filter, sortFunc: Sort, limit: dto.MaxResultCount);
        var dataList = objectMapper.Map<List<SeedSymbolIndex>, List<SeedSymbolDto>>(result.Item2);
        var pageResult = new SeedSymbolPageResultDto
        {
            TotalRecordCount = result.Item1,
            Data = dataList
        };
        return pageResult;
    }
    
    [Name("allSeedSymbols")]
    public static async Task<SeedSymbolPageResultDto> AllSeedSymbolsAsync(
        [FromServices] IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> repository,
        [FromServices] IObjectMapper objectMapper,
        GetAllSeedSymbolsDto dto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();
        if (dto == null || dto.AddressList.IsNullOrEmpty())
        {
            return new SeedSymbolPageResultDto
            {
                TotalRecordCount = 0,
                Data = new List<SeedSymbolDto>()
            };
        }

        mustQuery.Add(q => q.Terms(i =>
            i.Field(f => f.IssuerTo).Terms(dto.AddressList)));

        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.IsDeleteFlag).Value(false)));
        
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.TokenType).Value(TokenType.NFT)));

        if (!dto.ChainList.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i =>
                i.Field(f => f.ChainId).Terms(dto.ChainList)));
        }

        mustQuery.Add(q=>
                q.DateRange(i =>
                    i.Field(f => f.SeedExpTime)
                        .GreaterThan(DateTime.Now))
            );

        if (!dto.SeedOwnedSymbol.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Wildcard(i =>
                i.Field(f => f.SeedOwnedSymbol).Value("*" + dto.SeedOwnedSymbol + "*")));
        }

        QueryContainer Filter(QueryContainerDescriptor<SeedSymbolIndex> f) => f.Bool(b => b.Must(mustQuery));


        IPromise<IList<ISort>> Sort(SortDescriptor<SeedSymbolIndex> s) =>
            s.Ascending(a => a.ChainId)
                .Script(script => script.Type(SortTypeNumber)
                    .Script(scriptDescriptor => scriptDescriptor.Source(SortScriptSourceValueLength))
                    .Order(SortOrder.Ascending))
                .Ascending(a => a.SeedOwnedSymbol)
                .Ascending(a => a.Id);

        var result = await repository.GetSortListAsync(Filter, sortFunc: Sort, limit: dto.MaxResultCount);
        var dataList = objectMapper.Map<List<SeedSymbolIndex>, List<SeedSymbolDto>>(result.Item2);
        var pageResult = new SeedSymbolPageResultDto
        {
            TotalRecordCount = result.Item1,
            Data = dataList
        };
        return pageResult;
    }

    [Name("nftOffers")]
    public static async Task<NftOfferPageResultDto> NftOffers(
        [FromServices] IAElfIndexerClientEntityRepository<OfferInfoIndex, LogEventInfo> repository,
        [FromServices] IObjectMapper objectMapper,
        [FromServices] IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> tokenIndexRepository,
        GetNFTOffersDto dto)
    {
        var decimals = 0;
        if (!dto.NFTInfoId.IsNullOrEmpty())
        {
            var tokenId = dto.NFTInfoId;
            var tokenInfoIndex = await tokenIndexRepository.GetAsync(tokenId);
            if (tokenInfoIndex != null)
            {
                decimals = tokenInfoIndex.Decimals;
            }
        }
        
        // query offer list
        var mustQuery = new List<Func<QueryContainerDescriptor<OfferInfoIndex>, QueryContainer>>();
        if (!dto.ChainId.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(dto.ChainId)));
        }
        if (!dto.ChainIdList.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.ChainId).Terms(dto.ChainIdList)));
        }
        
        mustQuery.Add(q => q.TermRange(i => i.Field(f => f.RealQuantity).GreaterThan(0.ToString())));
        if (!dto.NFTInfoId.IsNullOrEmpty())
            mustQuery.Add(q => q.Term(i => i.Field(f => f.BizInfoId).Value(dto.NFTInfoId)));
        
        if (!dto.NFTInfoIdList.IsNullOrEmpty())
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.BizInfoId).Terms(dto.NFTInfoIdList)));

        if (!dto.Symbol.IsNullOrEmpty())
            mustQuery.Add(q => q.Term(i => i.Field(f => f.BizSymbol).Value(dto.Symbol)));

        if (!dto.OfferFrom.IsNullOrEmpty())
            mustQuery.Add(q => q.Term(i => i.Field(f => f.OfferFrom).Value(dto.OfferFrom)));

        if (!dto.OfferTo.IsNullOrEmpty())
            mustQuery.Add(q => q.Term(i => i.Field(f => f.OfferTo).Value(dto.OfferTo)));

        if (dto.ExpireTimeGt != null)
        {
            var utcTimeStr = DateTimeOffset.FromUnixTimeMilliseconds((long)dto.ExpireTimeGt).UtcDateTime.ToString("o");
            mustQuery.Add(q => q.TermRange(i => i.Field(f => f.ExpireTime).GreaterThan(utcTimeStr)));
        }

        QueryContainer Filter(QueryContainerDescriptor<OfferInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        
        var result = await repository.GetSortListAsync(Filter, limit: dto.MaxResultCount,
            skip: dto.SkipCount, sortFunc: GetSortForNFTOfferIndexs());
        if (result == null
            || result.Item2 == null)
            return new NftOfferPageResultDto
            { 
                TotalRecordCount = 0,
                Data = new List<NFTOfferDto>()
            };

        ;
        var dataList = result.Item2.Select(i =>
        {
            var item = objectMapper.Map<OfferInfoIndex, NFTOfferDto>(i);
            var quantityNoDecimals = TokenHelper.GetIntegerDivision(item.Quantity, decimals);
            item.RealQuantity = (quantityNoDecimals == item.RealQuantity)
                ? item.RealQuantity
                : Math.Min(item.RealQuantity, quantityNoDecimals);
            item.Quantity = item.RealQuantity;
            
            return item;
        }).ToList();
        return new NftOfferPageResultDto
        {
            TotalRecordCount = result.Item1,
            Data = dataList
        };
    }

    [Name("nftActivityList")]
    public static async Task<NFTActivityPageResultDto> NFTActivityListAsync(
        [FromServices] IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo> _nftActivityIndexRepository,
        GetActivitiesDto input, [FromServices] IObjectMapper objectMapper)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTActivityIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.NftInfoId).Value(input.NFTInfoId)));
        if (input.Types?.Count > 0)
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Type).Terms(input.Types)));
        }

        if (input.TimestampMin is > 0)
        {
            mustQuery.Add(q => q.DateRange(i =>
                i.Field(f => f.Timestamp)
                    .GreaterThanOrEquals(DateTime.UnixEpoch.AddMilliseconds((double)input.TimestampMin))));
        }

        if (input.TimestampMax is > 0)
        {
            mustQuery.Add(q => q.DateRange(i =>
                i.Field(f => f.Timestamp)
                    .LessThanOrEquals(DateTime.UnixEpoch.AddMilliseconds((double)input.TimestampMax))));
        }

        QueryContainer Filter(QueryContainerDescriptor<NFTActivityIndex> f) => f.Bool(b => b.Must(mustQuery));

        var list = await _nftActivityIndexRepository.GetSortListAsync(Filter, limit: input.MaxResultCount,
            skip: input.SkipCount, sortFunc: GetSortForNFTActivityIndexs());
        var dataList = objectMapper.Map<List<NFTActivityIndex>, List<NFTActivityDto>>(list.Item2);
        return new NFTActivityPageResultDto
        {
            Data = dataList,
            TotalRecordCount = list.Item1
        };
    }
    
    
    [Name("collectionActivityList")]
    public static async Task<NFTActivityPageResultDto> CollectionActivityListAsync(
        [FromServices] IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo> _nftActivityIndexRepository,
        GetCollectionActivitiesDto input, [FromServices] IObjectMapper objectMapper)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTActivityIndex>, QueryContainer>>();

        if (!input.BizIdList.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.NftInfoId).Terms(input.BizIdList)));
        }
        if (input.Types?.Count > 0)
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Type).Terms(input.Types)));
        }
        
        var collectionSymbolPre = TokenHelper.GetCollectionIdPre(input.CollectionId);
        mustQuery.Add(q => q
            .Script(sc => sc
                .Script(script =>
                    script.Source($"{"doc['nftInfoId'].value.contains('"+collectionSymbolPre+"')"}")
                )
            )
        );

        QueryContainer Filter(QueryContainerDescriptor<NFTActivityIndex> f) => f.Bool(b => b.Must(mustQuery));

        var list = await _nftActivityIndexRepository.GetSortListAsync(Filter, limit: input.MaxResultCount,
            skip: input.SkipCount, sortFunc: GetSortForNFTActivityIndexs());
        var dataList = objectMapper.Map<List<NFTActivityIndex>, List<NFTActivityDto>>(list.Item2);
        
        var totalCount = list?.Item1;
        if (list?.Item1 == ForestIndexerConstants.EsLimitTotalNumber)
        {
            totalCount =
                await QueryRealCountAsync(_nftActivityIndexRepository, mustQuery, null);
        }
        
        return new NFTActivityPageResultDto
        {
            Data = dataList,
            TotalRecordCount = (long)(totalCount == null ? 0 : totalCount),
        };
    }

    [Name("messageActivityList")]
    public static async Task<NFTActivityPageResultDto> MessageActivityListAsync(
        [FromServices] IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo> _nftActivityIndexRepository,
        GetMessageActivitiesDto input, [FromServices] IObjectMapper objectMapper)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTActivityIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Range(i
            => i.Field(f => f.BlockHeight).GreaterThanOrEquals(input.BlockHeight)));

        if (input.Types?.Count > 0)
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Type).Terms(input.Types)));
        }

        QueryContainer Filter(QueryContainerDescriptor<NFTActivityIndex> f) => f.Bool(b => b.Must(mustQuery));
        
        var list = await _nftActivityIndexRepository.GetListAsync(Filter, skip: input.SkipCount, sortExp: o => o.BlockHeight);
        var dataList = objectMapper.Map<List<NFTActivityIndex>, List<NFTActivityDto>>(list.Item2);

        var totalCount = list?.Item1;
        if (list?.Item1 == ForestIndexerConstants.EsLimitTotalNumber)
        {
            totalCount =
                await QueryRealCountAsync(_nftActivityIndexRepository, mustQuery, null);
        }

        return new NFTActivityPageResultDto
        {
            Data = dataList,
            TotalRecordCount = (long)(totalCount == null ? 0 : totalCount),
        };
    }

    private static async Task<long> QueryRealCountAsync(IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo> repo,List<Func<QueryContainerDescriptor<NFTActivityIndex>, QueryContainer>> mustQuery,List<Func<QueryContainerDescriptor<NFTActivityIndex>, QueryContainer>> mustNotQuery)
    {
        var countRequest = new SearchRequest<NFTActivityIndex>
        {
            Query = new BoolQuery
            {
                Must = mustQuery != null && mustQuery.Any()
                    ? mustQuery
                        .Select(func => func(new QueryContainerDescriptor<NFTActivityIndex>()))
                        .ToList()
                        .AsEnumerable()
                    : Enumerable.Empty<QueryContainer>(),
                MustNot = mustNotQuery != null && mustNotQuery.Any()
                    ? mustNotQuery
                        .Select(func => func(new QueryContainerDescriptor<NFTActivityIndex>()))
                        .ToList()
                        .AsEnumerable()
                    : Enumerable.Empty<QueryContainer>()
            },
            Size = 0
        };
        
        Func<QueryContainerDescriptor<NFTActivityIndex>, QueryContainer> queryFunc = q => countRequest.Query;
        var realCount = await repo.CountAsync(queryFunc);
        return realCount.Count;
    }
    
    private static Func<SortDescriptor<NFTActivityIndex>, IPromise<IList<ISort>>> GetSortForNFTActivityIndexs()
    {
        SortDescriptor<NFTActivityIndex> sortDescriptor = new SortDescriptor<NFTActivityIndex>();
        sortDescriptor.Descending(a=>a.Timestamp);
        sortDescriptor.Ascending(a=>a.Type);
        IPromise<IList<ISort>> promise = sortDescriptor;
        return s => promise;
    } 
    
    private static Func<SortDescriptor<NFTActivityIndex>, IPromise<IList<ISort>>> GetSortForNFTActivityIndexs(string sortType)
    {
        SortDescriptor<NFTActivityIndex> sortDescriptor = new SortDescriptor<NFTActivityIndex>();
        if (sortType.IsNullOrEmpty() || sortType.Equals("DESC"))
        {
            sortDescriptor.Descending(a=>a.Timestamp);
        }else
        {
            sortDescriptor.Ascending(a=>a.Timestamp);
        }

        IPromise<IList<ISort>> promise = sortDescriptor;
        return s => promise;
    } 
    
    private static Func<SortDescriptor<OfferInfoIndex>, IPromise<IList<ISort>>> GetSortForNFTOfferIndexs()
    {
        SortDescriptor<OfferInfoIndex> sortDescriptor = new SortDescriptor<OfferInfoIndex>();
        sortDescriptor.Descending(a=>a.Price);
        sortDescriptor.Ascending(a=>a.CreateTime);
        sortDescriptor.Ascending(a=>a.ExpireTime);
        IPromise<IList<ISort>> promise = sortDescriptor;
        return s => promise;
    } 

    [Name("marketData")]
    public static async Task<MarketDataPageResultDto> MarketDataAsync(
        [FromServices] IAElfIndexerClientEntityRepository<NFTMarketDayIndex, LogEventInfo> _nftMarketDayIndexRepository,
        [FromServices] IObjectMapper objectMapper,
        GetNFTMarketDto input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTMarketDayIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.NFTInfoId).Value(input.NFTInfoId)));
        if (input.TimestampMin != 0)
        {
            mustQuery.Add(q => q.DateRange(i =>
                i.Field(f => f.DayBegin)
                    .GreaterThanOrEquals(DateTime.UnixEpoch.AddMilliseconds(input.TimestampMin))));
        }

        if (input.TimestampMax != 0)
        {
            mustQuery.Add(q => q.DateRange(i =>
                i.Field(f => f.DayBegin)
                    .LessThanOrEquals(DateTime.UnixEpoch.AddMilliseconds(input.TimestampMax))));
        }

        QueryContainer Filter(QueryContainerDescriptor<NFTMarketDayIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var list = await _nftMarketDayIndexRepository.GetListAsync(Filter, limit: ForestIndexerConstants.MaxCountNumber,
            skip: 0, sortExp: s => s.DayBegin, sortType: SortOrder.Descending);

        return new MarketDataPageResultDto
        {
            TotalRecordCount = list.Item1,
            Data = objectMapper.Map<List<NFTMarketDayIndex>, List<NFTInfoMarketDataDto>>(list.Item2)
        };
    }

    // whitelist query
    [Name("whitelistHash")]
    public static async Task<WhitelistInfoIndexDto> WhiteListHashAsync(
        [FromServices] IAElfIndexerClientEntityRepository<WhitelistIndex, LogEventInfo> repository,
        [FromServices] IObjectMapper objectMapper,
        GetWhiteListInfoDto input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<WhitelistIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Id).Value(input.WhitelistHash)));

        QueryContainer Filter(QueryContainerDescriptor<WhitelistIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var index = await repository.GetAsync(Filter);

        if (index.Id != input.WhitelistHash) return new WhitelistInfoIndexDto();

        return objectMapper.Map<WhitelistIndex, WhitelistInfoIndexDto>(index);
    }

    [Name("extraInfos")]
    public static async Task<ExtraInfoPageResultDto> ExtraInfosAsync(
        [FromServices] IAElfIndexerClientEntityRepository<WhiteListExtraInfoIndex, LogEventInfo> repository,
        [FromServices] IAElfIndexerClientEntityRepository<WhitelistIndex, LogEventInfo> whitelistRepository,
        [FromServices] IAElfIndexerClientEntityRepository<TagInfoIndex, LogEventInfo> tagInfoRepository,
        [FromServices] IObjectMapper objectMapper,
        [FromServices] ILogger<WhiteListExtraInfoIndex> _logger,
        GetWhitelistExtraInfoListInput input)
    {
        var whitelistIndexQuery = new List<Func<QueryContainerDescriptor<WhitelistIndex>, QueryContainer>>();
        whitelistIndexQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
        whitelistIndexQuery.Add(q => q.Term(i => i.Field(f => f.Id).Value(input.WhitelistHash)));
        whitelistIndexQuery.Add(q => q.Term(i => i.Field(f => f.ProjectId).Value(input.ProjectId)));

        QueryContainer Filter(QueryContainerDescriptor<WhitelistIndex> f)
            => f.Bool(b => b.Must(whitelistIndexQuery));

        var whitelistIndex = await whitelistRepository.GetAsync(Filter);

        if (whitelistIndex == null) return new ExtraInfoPageResultDto();

        _logger.LogInformation("whitelistIndex id :{whitelistIndexId}", whitelistIndex.Id);

        var mustQuery = new List<Func<QueryContainerDescriptor<WhiteListExtraInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(whitelistIndex.ChainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.WhitelistInfoId).Value(whitelistIndex.Id)));

        QueryContainer ExtraInfoFilter(QueryContainerDescriptor<WhiteListExtraInfoIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        IPromise<IList<ISort>> Sort(SortDescriptor<WhiteListExtraInfoIndex> s) => s.Descending(a => a.LastModifyTime);

        var (totalCount, list) = await repository.GetSortListAsync(ExtraInfoFilter, sortFunc: Sort,
            limit: input.MaxResultCount, skip: input.SkipCount);

        _logger.LogInformation("WhiteListExtraInfoIndex totalCount: {totalCount}", totalCount);

        var dtoList = objectMapper.Map<List<WhiteListExtraInfoIndex>, List<WhitelistExtraInfoIndexDto>>(list);
        dtoList.ForEach(item =>
        {
            item.ChainId = whitelistIndex.ChainId;
            item.WhitelistInfo = new WhitelistInfoBaseDto
            {
                ChainId = whitelistIndex.ChainId,
                WhitelistHash = whitelistIndex.Id,
                StrategyType = whitelistIndex.StrategyType,
                ProjectId = whitelistIndex.ProjectId
            };

            _logger.LogInformation("TagInfoId:{TagInfoId}", item.TagInfoId);

            var tagInfoIndexId = IdGenerateHelper.GetId(whitelistIndex.ChainId, item.TagInfoId);
            var tagInfoQuery = new List<Func<QueryContainerDescriptor<TagInfoIndex>, QueryContainer>>();
            tagInfoQuery.Add(q => q.Term(i => i.Field(f => f.Id).Value(tagInfoIndexId)));

            QueryContainer TagInfoFilter(QueryContainerDescriptor<TagInfoIndex> f)
                => f.Bool(b => b.Must(tagInfoQuery));

            var taginfo = tagInfoRepository.GetAsync(TagInfoFilter).Result;
            _logger.LogInformation("TagInfoIndex id :{TagInfoIndex}", taginfo.Id);

            item.TagInfo = objectMapper.Map<TagInfoIndex, TagInfoBaseDto>(taginfo);
            item.TagInfo.PriceTagInfo = new PriceTagInfoDto
            {
                Price = item.TagInfo.DecodeInfo<PriceTag>().Amount,
                Symbol = item.TagInfo.DecodeInfo<PriceTag>().Symbol
            };
        });


        return new ExtraInfoPageResultDto
        {
            TotalCount = totalCount,
            Items = dtoList
        };
    }

    [Name("managerList")]
    public static async Task<WhitelistManagerResultDto> ManagerListAsync(
        [FromServices] IAElfIndexerClientEntityRepository<WhiteListManagerIndex, LogEventInfo> managerRepository,
        [FromServices] IAElfIndexerClientEntityRepository<WhitelistIndex, LogEventInfo> whitelistRepository,
        [FromServices] ILogger<WhiteListManagerIndex> _logger,
        [FromServices] IObjectMapper objectMapper,
        GetWhitelistManagerListInput input)
    {
        var whitelistIndexQuery = new List<Func<QueryContainerDescriptor<WhitelistIndex>, QueryContainer>>();
        whitelistIndexQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
        whitelistIndexQuery.Add(q => q.Term(i => i.Field(f => f.Id).Value(input.WhitelistHash)));
        whitelistIndexQuery.Add(q => q.Term(i => i.Field(f => f.ProjectId).Value(input.ProjectId)));

        QueryContainer Filter(QueryContainerDescriptor<WhitelistIndex> f)
            => f.Bool(b => b.Must(whitelistIndexQuery));

        var whitelistIndex = await whitelistRepository.GetAsync(Filter);

        if (whitelistIndex == null) return new WhitelistManagerResultDto();

        _logger.LogInformation("whitelistIndex id :{whitelistIndexId}", whitelistIndex.Id);

        var mustQuery = new List<Func<QueryContainerDescriptor<WhiteListManagerIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(whitelistIndex.ChainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.WhitelistInfoId).Value(whitelistIndex.Id)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Manager).Value(input.Address)));

        QueryContainer ManagerFilter(QueryContainerDescriptor<WhiteListManagerIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var (totalCount, list) =
            await managerRepository.GetListAsync(ManagerFilter, limit: input.MaxResultCount, skip: input.SkipCount);

        var dtoList = objectMapper.Map<List<WhiteListManagerIndex>, List<WhitelistManagerIndexDto>>(list);
        dtoList.ForEach(item =>
        {
            item.ChainId = whitelistIndex.ChainId;
            item.WhitelistInfo = new WhitelistInfoBaseDto
            {
                ChainId = whitelistIndex.ChainId,
                WhitelistHash = whitelistIndex.Id,
                StrategyType = whitelistIndex.StrategyType,
                ProjectId = whitelistIndex.ProjectId
            };
        });
        _logger.LogInformation("WhiteListManagerIndex totalCount: {totalCount}", dtoList.Count);

        return new WhitelistManagerResultDto
        {
            TotalCount = totalCount,
            Items = dtoList
        };
    }

    [Name("tagList")]
    public static async Task<TagInfoResultDto> TagsListAsync(
        [FromServices] IAElfIndexerClientEntityRepository<TagInfoIndex, LogEventInfo> repository,
        [FromServices] IAElfIndexerClientEntityRepository<WhitelistIndex, LogEventInfo> whitelistRepository,
        [FromServices] IAElfIndexerClientEntityRepository<WhiteListExtraInfoIndex, LogEventInfo> extraRepository,
        [FromServices] ILogger<TagInfoIndex> _logger,
        [FromServices] IObjectMapper objectMapper,
        GetTagInfoListInput input)
    {
        var whitelistIndexQuery = new List<Func<QueryContainerDescriptor<WhitelistIndex>, QueryContainer>>();
        whitelistIndexQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
        whitelistIndexQuery.Add(q => q.Term(i => i.Field(f => f.Id).Value(input.WhitelistHash)));
        whitelistIndexQuery.Add(q => q.Term(i => i.Field(f => f.ProjectId).Value(input.ProjectId)));

        QueryContainer Filter(QueryContainerDescriptor<WhitelistIndex> f)
            => f.Bool(b => b.Must(whitelistIndexQuery));

        var whitelistIndex = await whitelistRepository.GetAsync(Filter);

        if (whitelistIndex == null) return new TagInfoResultDto();

        _logger.LogInformation("whitelistIndex id :{whitelistIndexId}", whitelistIndex.Id);

        var mustQuery = new List<Func<QueryContainerDescriptor<TagInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(whitelistIndex.ChainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.WhitelistId).Value(whitelistIndex.Id)));

        QueryContainer TagInfoFilter(QueryContainerDescriptor<TagInfoIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        IPromise<IList<ISort>> Sort(SortDescriptor<TagInfoIndex> s) => s.Descending(a => a.LastModifyTime);

        var (totalCount, list) = await repository.GetSortListAsync(TagInfoFilter, sortFunc: Sort,
            limit: input.MaxResultCount, skip: input.SkipCount);

        var dtoList = objectMapper.Map<List<TagInfoIndex>, List<TagInfoIndexDto>>(list);

        dtoList.ForEach(item =>
        {
            item.WhitelistInfo = new WhitelistInfoBaseDto
            {
                ChainId = whitelistIndex.ChainId,
                WhitelistHash = whitelistIndex.Id,
                StrategyType = whitelistIndex.StrategyType,
                ProjectId = whitelistIndex.ProjectId
            };

            var mustQuery = new List<Func<QueryContainerDescriptor<WhiteListExtraInfoIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.TagInfoId).Value(item.TagHash)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.WhitelistInfoId).Value(whitelistIndex.Id)));

            QueryContainer ExtraInfoFilter(QueryContainerDescriptor<WhiteListExtraInfoIndex> f)
                => f.Bool(b => b.Must(mustQuery));

            var result = extraRepository
                .GetListAsync(ExtraInfoFilter, limit: input.MaxResultCount, skip: input.SkipCount).Result;
            _logger.LogInformation("Whitelist tag:{tagHash} ExtraInfoIndex totalCount: {totalCount}", item.TagHash,
                result.Item1);
            item.AddressCount = result.Item1;

            try
            {
                item.PriceTagInfo = new PriceTagInfoDto
                {
                    Price = item.DecodeInfo<PriceTag>().Amount,
                    Symbol = item.DecodeInfo<PriceTag>().Symbol
                };
                _logger.LogInformation(
                    "tagInfoIndex priceTag info: {PriceTagInfo}, decode amount:{Amount}, decode symbol:{Symbol}",
                    item.Info, item.DecodeInfo<PriceTag>().Amount, item.DecodeInfo<PriceTag>().Symbol);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "tagInfoIndex priceTag {PriceTagInfo} DecodeInfo failed:", item.Info);
                item.PriceTagInfo = new PriceTagInfoDto();
            }
        });

        _logger.LogInformation("tagInfoIndex totalCount: {totalCount}", dtoList.Count);

        return new TagInfoResultDto
        {
            TotalCount = totalCount,
            Items = dtoList
        };
    }

    public static async Task<SeedInfoDto> SearchSeedInfoAsync(
        [FromServices] IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> seedRepository,
        [FromServices] IAElfIndexerClientEntityRepository<SeedPriceIndex, LogEventInfo> seedPriceRepository,
        [FromServices] IAElfIndexerClientEntityRepository<UniqueSeedPriceIndex, LogEventInfo> uniqueSeedPriceRepository,
        [FromServices] IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> userBalanceRepository,
        [FromServices]
        IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo> symbolAuctionInfoRepository,
        [FromServices] IObjectMapper objectMapper,
        [FromServices] ILogger<TsmSeedSymbolIndex> _logger,
        SearchSeedInput input)
    {

        var pair = GetSymbolKeyValuePair(input);

        var indexQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
        indexQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(pair.Key)));
        indexQuery.Add(q => q.Term(i => i.Field(f => f.IsBurned).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<TsmSeedSymbolIndex> f)
            => f.Bool(b => b.Must(indexQuery));

        var symbol = pair.Key;
        var seedSymbolIndex = await seedRepository.GetAsync(Filter);
        if (seedSymbolIndex == null)
        {
            //while seed is used for create token, it will be burned, so we need to query the seed info from the main chain event it is burned.
            var mustQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(pair.Key)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(ForestIndexerConstants.MainChain)));

            QueryContainer FilterMustQuery(QueryContainerDescriptor<TsmSeedSymbolIndex> f)
                => f.Bool(b => b.Must(mustQuery));

            seedSymbolIndex = await seedRepository.GetAsync(FilterMustQuery);
            if (seedSymbolIndex == null)
            {
                seedSymbolIndex = new TsmSeedSymbolIndex()
                {
                    Symbol = symbol,
                    SeedName = IdGenerateHelper.GetSeedName(symbol),
                    TokenType = TokenHelper.GetTokenType(symbol),
                    SeedType = SeedType.Regular,
                    Status = SeedStatus.AVALIABLE
                };
            }
        }

        if (seedSymbolIndex.Status == SeedStatus.AVALIABLE)
        {
            var support = await IsSupportAsync(seedRepository, pair);
            if (!support.IsSupport && (support.NotSupportSeedStatus == SeedStatus.REGISTERED ||
                                       support.NotSupportSeedStatus == SeedStatus.UNREGISTERED))
            {
                var seedInfoDtoNotSupport = objectMapper.Map<TsmSeedSymbolIndex, SeedInfoDto>(seedSymbolIndex);
                seedInfoDtoNotSupport.Status = SeedStatus.NOTSUPPORT;
                seedInfoDtoNotSupport.NotSupportSeedStatus = support.NotSupportSeedStatus;
                return seedInfoDtoNotSupport;
            }
        }
        else if (seedSymbolIndex.Status == SeedStatus.NOTSUPPORT)
        {
                var support = await IsSupportAsync(seedRepository, pair);
                if (!support.IsSupport)
                {
                    var seedInfoDtoNotSupport = objectMapper.Map<TsmSeedSymbolIndex, SeedInfoDto>(seedSymbolIndex);
                    seedInfoDtoNotSupport.NotSupportSeedStatus = support.NotSupportSeedStatus;
                    return seedInfoDtoNotSupport;
                }
        }

        var seedInfoDto = objectMapper.Map<TsmSeedSymbolIndex, SeedInfoDto>(seedSymbolIndex);
        _logger.LogDebug("seedInfoDto NotSupportSeedStatus {Status}", seedInfoDto.NotSupportSeedStatus);
        if (seedSymbolIndex.Status == SeedStatus.AVALIABLE && 
            (seedSymbolIndex.SeedType == SeedType.Regular|| seedSymbolIndex.SeedType == SeedType.Unique))
        {
            var seedPriceId = IdGenerateHelper.GetSeedPriceId(input.TokenType, symbol.Length);
            var seedPriceIndex = await seedPriceRepository.GetAsync(seedPriceId);
            var uniqueSeedPriceIndex = await uniqueSeedPriceRepository.GetAsync(seedPriceId);
            if (seedPriceIndex != null)
            {
                seedInfoDto.TokenPrice = seedPriceIndex.TokenPrice;
                if (uniqueSeedPriceIndex != null && seedSymbolIndex.SeedType == SeedType.Unique)
                {
                    seedInfoDto.TokenPrice.Amount += uniqueSeedPriceIndex.TokenPrice.Amount;
                }
            }
        }

        await SetOwnerInfoAndAuctionInfoAsync(userBalanceRepository, symbolAuctionInfoRepository, seedInfoDto);
        return seedInfoDto;
    }

    private static async Task SetOwnerInfoAndAuctionInfoAsync
    (
        IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> userBalanceRepository,
        IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo> symbolAuctionInfoRepository,
        SeedInfoDto seedInfoDto)
    {
        if (seedInfoDto.Status == SeedStatus.UNREGISTERED)
        {
            var auctionQuery = new List<Func<QueryContainerDescriptor<SymbolAuctionInfoIndex>, QueryContainer>>();
            auctionQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(seedInfoDto.SeedSymbol)));

            QueryContainer AuctionFilter(QueryContainerDescriptor<SymbolAuctionInfoIndex> f)
                => f.Bool(b => b.Must(auctionQuery));

            var symbolAuctionInfoIndex =
                await symbolAuctionInfoRepository.GetAsync(AuctionFilter, null, s => s.StartTime, SortOrder.Descending);
            if (symbolAuctionInfoIndex != null)
            {
                seedInfoDto.TopBidPrice = symbolAuctionInfoIndex.FinishPrice;
                seedInfoDto.AuctionEndTime = symbolAuctionInfoIndex.EndTime;
            }

            var mustQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(seedInfoDto.SeedSymbol)));
            mustQuery.Add(q => q.Range(i => i.Field(f => f.Amount).GreaterThan(0)));

            QueryContainer UserBalanceFilter(QueryContainerDescriptor<UserBalanceIndex> f)
                => f.Bool(b => b.Must(mustQuery));

            var userBalanceIndex = await userBalanceRepository.GetAsync(UserBalanceFilter);
            if (userBalanceIndex != null)
            {
                seedInfoDto.Owner = userBalanceIndex.Address;
                seedInfoDto.CurrentChainId = userBalanceIndex.ChainId;
            }
        }
    }

    private static KeyValuePair<string, string> GetSymbolKeyValuePair(SearchSeedInput input)
    {
        KeyValuePair<string, string> pair;
        if (input.TokenType.Equals(TokenType.FT.ToString()))
        {
            pair = new KeyValuePair<string, string>(input.Symbol, TokenHelper.GetNftSymbol(input.Symbol));
        }
        else
        {
            pair = new KeyValuePair<string, string>(TokenHelper.GetNftSymbol(input.Symbol), input.Symbol);
        }

        return pair;
    }

    private static async Task<NotSupportInfo> IsSupportAsync(
        IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> seedRepository,
        KeyValuePair<string, string> pair)
    {
        var queries = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
        queries.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(pair.Value)));
        queries.Add(q => q.Term(i => i.Field(f => f.IsBurned).Value(false)));
        QueryContainer Filter(QueryContainerDescriptor<TsmSeedSymbolIndex> f)
            => f.Bool(b =>
                b.Must(queries));

        var otherSeedSymbolIndex = await seedRepository.GetAsync(Filter);
        if (otherSeedSymbolIndex == null)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(pair.Value)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(ForestIndexerConstants.MainChain)));

            QueryContainer FilterMustQuery(QueryContainerDescriptor<TsmSeedSymbolIndex> f)
                => f.Bool(b => b.Must(mustQuery));

            otherSeedSymbolIndex = await seedRepository.GetAsync(FilterMustQuery);
          
        }

        if (otherSeedSymbolIndex == null)
        {
            return new NotSupportInfo
            {
                IsSupport = true
            };
        }
        return new NotSupportInfo
        {
            IsSupport = false,
            NotSupportSeedStatus = otherSeedSymbolIndex.Status
        };
    }

    [Name("mySeed")]
    public static async Task<MySeedDto> MySeedAsync(
        [FromServices] IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> seedRepository,
        MySeedInput input)
    {
        if (input.AddressList.IsNullOrEmpty())
        {
            return null;
        }
        
        var mustQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();
        
        if (input.TokenType != null)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.TokenType).Value(input.TokenType)));
        }

        if (!string.IsNullOrEmpty(input.ChainId))
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
        }

        var shouldQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();

        if (input.Status == null)
        {
            BuildForSeedStatusNull(input,shouldQuery);
        }else {
            BuildForSeedStatusNoNull(input, shouldQuery, mustQuery);
        }
        
        if (shouldQuery.Any())
        {
            mustQuery.Add(q =>
                q.Bool(b => b.Should(shouldQuery)));
        }

        QueryContainer Filter(QueryContainerDescriptor<SeedSymbolIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var mySeedSymbolIndex = await seedRepository.GetListAsync(Filter, null, sortExp: o => o.SeedExpTimeSecond,
            SortOrder.Descending, input.MaxResultCount, input.SkipCount);
        List<SeedListDto> seedInfoDtos = new List<SeedListDto>();
        if (mySeedSymbolIndex.Item1 > 0)
        {
            foreach (var seedSymbolIndex in mySeedSymbolIndex.Item2)
            {
                var seedListDto = new SeedListDto();
                seedListDto.SeedSymbol = seedSymbolIndex.Symbol;
                seedListDto.ChainId = seedSymbolIndex.ChainId;
                seedListDto.SeedName = seedSymbolIndex.TokenName;
                seedListDto.Id = IdGenerateHelper.GetTsmSeedSymbolId(seedSymbolIndex.ChainId,seedSymbolIndex.SeedOwnedSymbol);
                seedListDto.Symbol = seedSymbolIndex.SeedOwnedSymbol;
                seedListDto.ExpireTime = DateTimeHelper.ToUnixTimeMilliseconds(seedSymbolIndex.SeedExpTime);
                seedListDto.TokenType = seedSymbolIndex.TokenType;
                seedListDto.Status = seedSymbolIndex.SeedStatus ?? SeedStatus.UNREGISTERED;
                seedListDto.IssuerTo = seedSymbolIndex.IssuerTo;
                if (seedSymbolIndex.ExternalInfoDictionary != null)
                {
                    var key = EnumDescriptionHelper.GetEnumDescription(TokenCreatedExternalInfoEnum.NFTImageUrl);
                    seedListDto.SeedImage = seedSymbolIndex.ExternalInfoDictionary.Where(kv => kv.Key.Equals(key))
                        .Select(kv => kv.Value)
                        .FirstOrDefault("");
                }
                seedInfoDtos.Add(seedListDto);
            }
        }

        return new MySeedDto()
        {
            TotalRecordCount = mySeedSymbolIndex.Item1,
            Data = seedInfoDtos
        };
    }

    private static void BuildForSeedStatusNoNull(MySeedInput input,
        List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>> shouldQuery,
        List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>> mustQuery)
    {
        input?.AddressList
            .ForEach(address =>
            {
                var parts = address.Split(ForestIndexerConstants.UNDERLINE);
                if (parts.Length < 2)
                {
                    shouldQuery.Add(q =>
                        q.Term(i => i.Field(f => f.IssuerTo).Value(address)));
                }
                else
                {
                    shouldQuery.Add(q =>
                        q.Term(i => i.Field(f => f.IssuerTo).Value(parts[1])) &&
                        q.Term(i => i.Field(f => f.ChainId).Value(parts.Last())));
                }

                if (input.Status != SeedStatus.UNREGISTERED)
                {
                    mustQuery.Add(q => q.Term(i =>
                        i.Field(f => f.SeedStatus).Value(input.Status)));
                }
                
                if (input.Status != SeedStatus.REGISTERED)
                {
                    mustQuery.Add(q => q.Term(i =>
                        i.Field(f => f.IsDeleteFlag).Value(false)));
                }
            });
    }
    
    private static void BuildForSeedStatusNull(MySeedInput input,List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>> shouldQuery)
    {
        input?.AddressList
            .ForEach(address =>
            {
                var parts = address.Split(ForestIndexerConstants.UNDERLINE);
                if (parts.Length < 2)
                {
                    shouldQuery.Add(q =>
                        q.Term(i => i.Field(f => f.IssuerTo).Value(address)) &&
                        q.Term(i =>
                            i.Field(f => f.SeedStatus).Value(SeedStatus.REGISTERED)));
                    shouldQuery.Add(q =>
                        q.Term(i => i.Field(f => f.IssuerTo).Value(address)) &&
                        q.Term(i =>
                            i.Field(f => f.IsDeleteFlag).Value(false)));
                }
                else
                {
                    shouldQuery.Add(q =>
                        q.Term(i => i.Field(f => f.IssuerTo).Value(parts[1])) &&
                        q.Term(i => i.Field(f => f.ChainId).Value(parts.Last()))&&
                        q.Term(i =>
                            i.Field(f => f.SeedStatus).Value(SeedStatus.REGISTERED)));
                    shouldQuery.Add(q =>
                        q.Term(i => i.Field(f => f.IssuerTo).Value(parts[1])) &&
                        q.Term(i => i.Field(f => f.ChainId).Value(parts.Last()))&&
                        q.Term(i =>
                            i.Field(f => f.IsDeleteFlag).Value(false)));
                }
            });
    }

    public static async Task<SeedInfoDto> GetSeedInfoAsync(
        [FromServices] IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> seedRepository,
        [FromServices] IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> userBalanceRepository,
        [FromServices]
        IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo> symbolAuctionInfoRepository,
        [FromServices] IObjectMapper objectMapper,
        GetSeedInput input)
    {
        var indexQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
        indexQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(input.Symbol)));
        indexQuery.Add(q => q.Term(i => i.Field(f => f.IsBurned).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<TsmSeedSymbolIndex> f)
            => f.Bool(b => b.Must(indexQuery));

        var seedSymbolIndex = await seedRepository.GetAsync(Filter);
        if (seedSymbolIndex == null)
        {
            //while seed is used for create token, it will be burned, so we need to query the seed info from the main chain event it is burned.
            var mustQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(input.Symbol)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(ForestIndexerConstants.MainChain)));
            QueryContainer FilterMustQuery(QueryContainerDescriptor<TsmSeedSymbolIndex> f)
                => f.Bool(b => b.Must(mustQuery));

            seedSymbolIndex = await seedRepository.GetAsync(FilterMustQuery);
            if (seedSymbolIndex == null)
            {
                return new SeedInfoDto();
            }
        }

        var seedInfoDto = objectMapper.Map<TsmSeedSymbolIndex, SeedInfoDto>(seedSymbolIndex);
        await SetOwnerInfoAndAuctionInfoAsync(userBalanceRepository, symbolAuctionInfoRepository, seedInfoDto);
        return seedInfoDto;
    }
    

    public static async Task<SpecialSeedsPageResultDto> SpecialSeeds(
        [FromServices] IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> seedRepository,
        [FromServices] IObjectMapper objectMapper,
        GetSpecialSeedsInput input)
    {
        if (input == null)
        {
            return new SpecialSeedsPageResultDto()
            {
                TotalRecordCount = 0,
                Data = new List<SeedInfoDto>()
            };
        }
        
        var mustQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
        if (input.ChainIds != null && input.ChainIds.Any())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.ChainId).Terms(input.ChainIds)));
        }

        if (input.TokenTypes != null && input.TokenTypes.Any())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.TokenType).Terms(input.TokenTypes)));
        }

        if (input.SeedTypes != null && input.SeedTypes.Any())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.SeedType).Terms(input.SeedTypes)));
        }

        if (input.SymbolLengthMin > 0)
        {
            mustQuery.Add(q => q.Range(i => i.Field(f => f.SymbolLength).GreaterThanOrEquals(input.SymbolLengthMin)));
        }
        
        if (input.SymbolLengthMax > 0)
        {
            mustQuery.Add(q => q.Range(i => i.Field(f => f.SymbolLength).LessThanOrEquals(input.SymbolLengthMax)));
        }

        if (input.PriceMin>0)
        {
            mustQuery.Add(q => q.Range(i => i.Field(f => f.TokenPrice.Amount).GreaterThanOrEquals(input.PriceMin)));
        }

        if (input.PriceMax > 0)
        {
            mustQuery.Add(q => q.Range(i => i.Field(f => f.TokenPrice.Amount).LessThanOrEquals(input.PriceMax)));
        }
        mustQuery.Add(q => q.Term(i => i.Field(f => f.IsBurned).Value(false)));
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Status).Terms(SeedStatus.AVALIABLE, SeedStatus.UNREGISTERED)));
        
        QueryContainer Filter(QueryContainerDescriptor<TsmSeedSymbolIndex> f) => f.Bool(b => b.Must(mustQuery));
        var result = await seedRepository.GetListAsync(Filter, sortExp: k => k.Symbol,
            sortType: SortOrder.Ascending, skip: input.SkipCount, limit: input.MaxResultCount);
        var dataList = objectMapper.Map<List<TsmSeedSymbolIndex>, List<SeedInfoDto>>(result.Item2);
        var pageResult = new SpecialSeedsPageResultDto
        {
            TotalRecordCount = result.Item1,
            Data = dataList
        };
        return pageResult;
    }
    
    [Name("nftSoldData")]
    public static async Task<GetNFTSoldDataDto> NftSoldDataAsync(
        [FromServices] IAElfIndexerClientEntityRepository<SoldIndex, LogEventInfo> _nftSoldIndexRepository,
         GetNFTSoldDataInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SoldIndex>, QueryContainer>>();
        if (input.StartTime != DateTime.MinValue)
        {
            mustQuery.Add(q => q.DateRange(i =>
                i.Field(f => f.DealTime)
                    .GreaterThanOrEquals(input.StartTime)));
        }

        if (input.EndTime != DateTime.MinValue)
        {
            mustQuery.Add(q => q.DateRange(i => 
                i.Field(f => f.DealTime)
                    .LessThan(input.EndTime)));
        }

        QueryContainer Filter(QueryContainerDescriptor<SoldIndex> f) => f.Bool(b => b.Must(mustQuery));

        var list = await _nftSoldIndexRepository.GetListAsync(Filter, skip: 0);

        long totalTransAmount = 0;
        long totalNFTAmount = 0;
        long totalTransCount = list.Item1;

        HashSet<string> addressSet = new HashSet<string>();
        
        if (list.Item1 > 0)
        {
            foreach (var nftSoldIndex in list.Item2)
            {
                totalTransAmount += nftSoldIndex.PurchaseAmount;
                totalNFTAmount += long.Parse(nftSoldIndex.NftQuantity);
                addressSet.Add(nftSoldIndex.NftFrom);
                addressSet.Add(nftSoldIndex.NftTo);
            }
        }
        
        return new GetNFTSoldDataDto
        {
            TotalTransCount = totalTransCount,
            TotalTransAmount = totalTransAmount,
            TotalNftAmount = totalNFTAmount,
            TotalAddressCount = addressSet.Count
        };
    }
    [Name("nftActivityListByCondition")]
    public static async Task<NFTActivityPageResultDto> NFTActivityListByConditionAsync(
        [FromServices] IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo> _nftActivityIndexRepository,
        GetActivitiesConditionDto input, [FromServices] IObjectMapper objectMapper)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTActivityIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.NftInfoId).Value(input.NFTInfoId)));
        if (input.Types?.Count > 0)
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Type).Terms(input.Types)));
        }

        if (input.TimestampMin is > 0)
        {
            mustQuery.Add(q => q.DateRange(i =>
                i.Field(f => f.Timestamp)
                    .GreaterThanOrEquals(DateTime.UnixEpoch.AddMilliseconds((double)input.TimestampMin))));
        }

        if (input.TimestampMax is > 0)
        {
            mustQuery.Add(q => q.DateRange(i =>
                i.Field(f => f.Timestamp)
                    .LessThanOrEquals(DateTime.UnixEpoch.AddMilliseconds((double)input.TimestampMax))));
        }

        if (!input.FilterSymbol.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Regexp(i => i.Field(f => f.NftInfoId).Value(".*"+input.FilterSymbol+".*")));
        }

        QueryContainer Filter(QueryContainerDescriptor<NFTActivityIndex> f) => f.Bool(b => b.Must(mustQuery));

        var list = await _nftActivityIndexRepository.GetSortListAsync(Filter, limit: input.MaxResultCount,
            skip: input.SkipCount, sortFunc: GetSortForNFTActivityIndexs(input.SortType));
        var dataList = objectMapper.Map<List<NFTActivityIndex>, List<NFTActivityDto>>(list.Item2);
        dataList = dataList.Where(o => (double)(o.Amount * o.Price) > input.AbovePrice).ToList();
        return new NFTActivityPageResultDto
        {
            Data = dataList,
            TotalRecordCount = list.Item1
        };
    }

}