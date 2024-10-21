using System.Linq.Dynamic.Core;
using AeFinder.Sdk;
using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors.Provider;
using GraphQL;
using IdentityServer4.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Nest;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    private const int QuerySize = 1000;
    private const int ChunkSize = 100;
    
    [Name("nftCollections")]
    public static async Task<NFTCollectionPageResultDto> NFTCollections(
        [FromServices] IReadOnlyRepository<CollectionIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetNFTCollectionsDto dto)
    {
        var queryable = await repository.GetQueryableAsync();
        
        if (dto == null)
            return new NFTCollectionPageResultDto
            {
                TotalRecordCount = 0,
                Data = new List<NFTCollectionDto>()
            };

        if (!dto.CreatorAddress.IsNullOrWhiteSpace())
        {
            queryable = queryable.Where(q => q.OwnerManagerSet.Contains(dto.CreatorAddress));
        }
        
        if (!dto.CollectionType.IsNullOrEmpty())
        {
            queryable = queryable.Where(q => dto.CollectionType.Contains((int)q.CollectionType));
        }

        if (!dto.Param.IsNullOrEmpty())
        {
            queryable = queryable.Where(q => (q.Symbol == dto.Param || q.TokenName == dto.Param));
            queryable = queryable.Where(q => q.TokenName == dto.Param );
            
        }

        var result = queryable.Skip(dto.SkipCount).Take(dto.MaxResultCount).OrderByDescending(k => k.CreateTime).ToList();
        var dataList = objectMapper.Map<List<CollectionIndex>, List<NFTCollectionDto>>(result);
        var pageResult = new NFTCollectionPageResultDto
        {
            TotalRecordCount = result.Count,
            Data = dataList
        };
        return pageResult;
    }
    
    [Name("nftCollectionsByAddressList")]
    public static async Task<NFTCollectionPageResultDto> NFTCollectionsByAddressList(
        [FromServices] IReadOnlyRepository<CollectionIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetNFTCollectionsByAddressListDto dto)
    {
        var queryable = await repository.GetQueryableAsync();
        if (dto == null)
            return new NFTCollectionPageResultDto
            {
                TotalRecordCount = 0,
                Data = new List<NFTCollectionDto>()
            };

        if (!dto.AddressList.IsNullOrEmpty())
        {
            queryable = queryable.Where(q => q.OwnerManagerSet.Any(i=>dto.AddressList.Contains(i)));
        }
        
        if (!dto.CollectionType.IsNullOrEmpty())
        {
            queryable = queryable.Where(q => dto.CollectionType.Contains((int)q.CollectionType));
            
        }

        if (!dto.Param.IsNullOrEmpty())
        {
            queryable = queryable.Where(q => (q.Symbol == dto.Param || q.TokenName == dto.Param));
        }

        var result = queryable.Skip(dto.SkipCount).Take(dto.MaxResultCount).OrderByDescending(k => k.CreateTime)
            .ToList();
        var dataList = objectMapper.Map<List<CollectionIndex>, List<NFTCollectionDto>>(result);
        var pageResult = new NFTCollectionPageResultDto
        {
            TotalRecordCount = result.Count,
            Data = dataList
        };
        return pageResult;
    }

    [Name("nftCollectionByIds")]
    public static async Task<NFTCollectionPageResultDto> NFTCollectionByIds(
        [FromServices] IReadOnlyRepository<CollectionIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetNFTCollectionByIdsDto dto)
    {
        if (dto == null
            || dto.Ids == null
            || dto.Ids.Count == 0
            || dto.Ids.Count > 100)
            return new NFTCollectionPageResultDto
            {
                TotalRecordCount = 0,
                Data = new List<NFTCollectionDto>()
            };
        var queryable = await repository.GetQueryableAsync();
        queryable = queryable.Where(q => dto.Ids.Contains(q.Id));

        var result = queryable.Skip(0).Take(dto.Ids.Count).ToList();
        var dataList = objectMapper.Map<List<CollectionIndex>, List<NFTCollectionDto>>(result);
        var pageResult = new NFTCollectionPageResultDto
        {
            TotalRecordCount = result.Count,
            Data = dataList
        };
        return pageResult;
    }


    [Name("nftCollection")]
    public static async Task<NFTCollectionDto> NFTCollection(
        [FromServices] IReadOnlyRepository<CollectionIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetNFTCollectionDto dto)
    {
        if (dto == null || dto.Id.IsNullOrWhiteSpace()) return null;
        var queryable = await repository.GetQueryableAsync();
        queryable = queryable.Where(q => q.Id == dto.Id);
        var nftCollectionIndex = queryable.ToList();
        if (nftCollectionIndex.IsNullOrEmpty()) return null;

        return objectMapper.Map<CollectionIndex, NFTCollectionDto>(nftCollectionIndex.FirstOrDefault());
    }

    [Name("nftCollectionSymbol")]
    public static async Task<SymbolDto> NFTCollectionSymbol(
        [FromServices] IReadOnlyRepository<CollectionIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        string symbol)
    {
        if (symbol.IsNullOrWhiteSpace()) return new SymbolDto();
        var queryable = await repository.GetQueryableAsync();
        queryable = queryable.Where(i => i.Symbol == symbol);

        var result = queryable.Skip(0).Take(1).ToList();
        if (result.IsNullOrEmpty()) return new SymbolDto();

        return new SymbolDto
        {
            Symbol = result?.FirstOrDefault(new CollectionIndex())?.Symbol
        };
    }

    [Name("nftCollectionChange")]
    public static async Task<CollectionChangePageResultDto> NFTCollectionChange(
        [FromServices] IReadOnlyRepository<CollectionChangeIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetNFTCollectionChangeDto dto)
    {
        var queryable = await repository.GetQueryableAsync();
        queryable = queryable.Where(f => f.BlockHeight >= dto.BlockHeight);
        queryable = queryable.Where(f => f.ChainId == dto.ChainId);
        var result = queryable.Skip(dto.SkipCount).OrderBy(o => o.BlockHeight).ToList();
        if (result.IsNullOrEmpty())
        {
            return new CollectionChangePageResultDto
            {
                TotalRecordCount = 0,
                Data = null
            };
        }
        var dataList = objectMapper.Map<List<CollectionChangeIndex>, List<CollectionChangeDto>>(result);
        var pageResult = new CollectionChangePageResultDto
        {
            TotalRecordCount = dataList.Count,
            Data = dataList
        };
        return pageResult;
    }
    
    [Name("nftCollectionPriceChange")]
    public static async Task<CollectionPriceChangePageResultDto> NFTCollectionPriceChangeAsync(
        [FromServices] IReadOnlyRepository<CollectionPriceChangeIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetNFTCollectionPriceChangeDto dto)
    {
        var queryable = await repository.GetQueryableAsync();
        queryable = queryable.Where(f => f.BlockHeight >= dto.BlockHeight);
        queryable = queryable.Where(f => f.ChainId == dto.ChainId);
        var result = queryable.Skip(dto.SkipCount).OrderBy(o => o.BlockHeight).ToList();
        if (result.IsNullOrEmpty())
        {
            return new CollectionPriceChangePageResultDto
            {
                TotalRecordCount = 0,
                Data = null
            };
        }    
        var dataList = objectMapper.Map<List<CollectionPriceChangeIndex>, List<CollectionPriceChangeDto>>(result);
        var pageResult = new CollectionPriceChangePageResultDto
        {
            TotalRecordCount = dataList.Count,
            Data = dataList
        };
        return pageResult;
    }

    [Name("calcNFTCollectionPrice")]
    public static async Task<NFTCollectionPriceResultDto> CalcNFTCollectionPriceAsync(
        [FromServices] ICollectionProvider collectionProvider,
        [FromServices] IObjectMapper objectMapper,
        [FromServices] ILogger<CollectionIndex> _logger,
        GetNFTCollectionPriceDto dto)
    {
        var floorPrice = await collectionProvider.
            CalcCollectionFloorPriceAsync(dto.ChainId, dto.Symbol, dto.FloorPrice);
        return new NFTCollectionPriceResultDto
        {
            FloorPrice = floorPrice
        };
    }

    [Name("calcNFTCollectionTrade")]
    public static async Task<NFTCollectionTradeResultDto> CalcNFTCollectionTradeAsync(
        [FromServices] ICollectionProvider collectionProvider,
        [FromServices] IObjectMapper objectMapper,
        [FromServices] ILogger<CollectionIndex> _logger,
        CalNFTCollectionTradeDto dto)
    {
        var floorPrice =
            await collectionProvider.CalcCollectionFloorPriceWithTimestampAsync(dto.ChainId, dto.CollectionSymbol,
                dto.BeginUtcStamp, dto.EndUtcStamp);
        var tradeInfoDic =
            await collectionProvider.CalcNFTCollectionTradeAsync(dto.ChainId, dto.CollectionId, dto.BeginUtcStamp,
                dto.EndUtcStamp);

        if (tradeInfoDic.IsNullOrEmpty())
        {
            return new NFTCollectionTradeResultDto
            {
                FloorPrice = floorPrice
            };
        }

        return new NFTCollectionTradeResultDto
        {
            FloorPrice = floorPrice,
            VolumeTotal = tradeInfoDic.FirstOrDefault().Value,
            SalesTotal = tradeInfoDic.FirstOrDefault().Key
        };
    }

    [Name("generateNFTCollectionExtensionById")]
    public static async Task<NFTCollectionExtensionResultDto> GenerateNFTCollectionExtensionById(
        [FromServices] IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> seedSymbolRepository,
        [FromServices] IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> nftInfoRepository,
        [FromServices] IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> userBalanceRepository,
        [FromServices] ILogger<CollectionIndex> _logger,
        GetNFTCollectionGenerateDataDto dto)
    {
        if (ForestIndexerConstants.SeedCollectionSymbol.Equals(dto.Symbol))
        { 
            var collectionExtensionResultDto = 
                await CountSeedSymbolIndexAsync(seedSymbolRepository, userBalanceRepository, _logger, dto.ChainId);
            return collectionExtensionResultDto;
        }
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.ChainId)
                .Value(dto.ChainId)),
            q => q.Term(i => i.Field(f => f.CollectionSymbol)
                .Value(dto.Symbol))
        };
        var mustNotQuery = new List<Func<QueryContainerDescriptor<NFTInfoIndex>, QueryContainer>>();
        //Exclude 1.Burned All NFT ( supply = 0 and issued = totalSupply) 2.Create Failed (supply=0 and issued=0)
        mustNotQuery.Add(q => q
            .Script(sc => sc
                .Script(script =>
                    script.Source(
                        $"{ForestIndexerConstants.BurnedAllNftScript} || {ForestIndexerConstants.CreateFailedANftScript} || {ForestIndexerConstants.IssuedLessThenOneANftScript}")
                )
            )
        );
        QueryContainer Filter(QueryContainerDescriptor<NFTInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery).MustNot(mustNotQuery));
        var itemTotal = 0;
        var nftIdsSet = new HashSet<string>();
        var dataList = new List<NFTInfoIndex>();
        do
        {
            var result = await nftInfoRepository.GetListAsync(Filter, skip: itemTotal, limit: QuerySize,
                sortType: SortOrder.Ascending, sortExp: o => o.BlockHeight);
            _logger.LogInformation("[GenerateNFTCollectionExtensionByIds] : dataList totalCount:{totalCount}",
                result.Item1);
            dataList = result.Item2;
            if (dataList.IsNullOrEmpty())
            {
                break;
            }
            foreach (var nftId in dataList.Select(i => i.Id))
            {
                nftIdsSet.Add(nftId);
            }
            itemTotal += dataList.Count;
        } while (!dataList.IsNullOrEmpty());
        var splitList = await SplitHashSetAsync(nftIdsSet, ChunkSize);
        //Prevent the number of nftids from being split into multiple batches again
        //and the data is saved in userSet.
        var userSet = new HashSet<string>();
        foreach (var nftIds in splitList)
        {
            await GenerateUserCountByNFTIdsAsync(userBalanceRepository, _logger, nftIds, userSet);
        }
        var resultDto = new NFTCollectionExtensionResultDto
        {
            //Of ItemTotal
            ItemTotal = itemTotal,
            //Of OwnerTotal
            OwnerTotal = userSet.Count
        };
        return resultDto;
    }

    /**
     * Generate the number of accounts corresponding to nftIds
     */
    private static async Task GenerateUserCountByNFTIdsAsync(
        [FromServices] IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> repository,
        [FromServices] ILogger<CollectionIndex> _logger,
        HashSet<string> nftIds,
        HashSet<string> userSet)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i
            => i.Field(f => f.NFTInfoId).Terms(nftIds)));
        //query balance > 0 
        mustQuery.Add(q => q.Range(i
            => i.Field(f => f.Amount).GreaterThan(0)));

        QueryContainer Filter(QueryContainerDescriptor<UserBalanceIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var skipCount = 0;
        var dataList = new List<UserBalanceIndex>();
        do
        {
            var result = await repository.GetListAsync(Filter, skip: skipCount, limit: QuerySize,
                sortType: SortOrder.Ascending, sortExp: o => o.BlockHeight);
            _logger.LogInformation("[GenerateUserCountByNFTIds] : nftInfoList totalCount:{totalCount}", result.Item1);
            dataList = result.Item2;
            if (dataList.IsNullOrEmpty())
            {
                break;
            }
            foreach (var address in dataList.Select(i => i.Address))
            {
                userSet.Add(address);
            }
            skipCount += dataList.Count;
        } while (!dataList.IsNullOrEmpty());
    }
    
    private static async Task<NFTCollectionExtensionResultDto> CountSeedSymbolIndexAsync(
        [FromServices] IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> repository,
        [FromServices] IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> userBalanceRepository,
        [FromServices] ILogger<CollectionIndex> _logger,
        string chainId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>
        {
            q => q.Term(i => 
                i.Field(f => f.IsDeleteFlag).Value(false)),
            q => q.Term(i => 
                i.Field(f => f.ChainId).Value(chainId))
        };
        var mustNotQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();
        //Exclude 1.Burned All NFT ( supply = 0 and issued = totalSupply) 2.Create Failed (supply=0 and issued=0)
        mustNotQuery.Add(q => q
            .Script(sc => sc
                .Script(script =>
                    script.Source($"{ForestIndexerConstants.BurnedAllNftScript} || {ForestIndexerConstants.CreateFailedANftScript}")
                )
            )
        );
        
        QueryContainer Filter(QueryContainerDescriptor<SeedSymbolIndex> f) =>
            f.Bool(b => b.Must(mustQuery).MustNot(mustNotQuery));
        var itemTotal = 0;
        var nftIdsSet = new HashSet<string>();
        List<SeedSymbolIndex> dataList;
        do
        {
            var result = await repository.GetListAsync(Filter, skip: itemTotal, limit: QuerySize);
            _logger.LogInformation("[GenerateUserCountByNFTIds] : SeedSymbolIndexList totalCount:{totalCount}", result.Item1);
            dataList = result.Item2;
            if (dataList.IsNullOrEmpty())
            {
                break;
            }
            foreach (var nftId in dataList.Select(i => i.Id))
            {
                nftIdsSet.Add(nftId);
            }
            itemTotal += dataList.Count;
        } while (!dataList.IsNullOrEmpty());
        var splitList = await SplitHashSetAsync(nftIdsSet, ChunkSize);
        //Prevent the number of nftids from being split into multiple batches again
        var userSet = new HashSet<string>();
        foreach (var nftIds in splitList)
        {
            await GenerateUserCountByNFTIdsAsync(userBalanceRepository, _logger, nftIds, userSet);
        }
        var resultDto = new NFTCollectionExtensionResultDto
        {
            //Of ItemTotal
            ItemTotal = itemTotal,
            //Of OwnerTotal
            OwnerTotal = userSet.Count
        };
        return resultDto;
         
    }
    
    private static async Task<List<HashSet<T>>> SplitHashSetAsync<T>(HashSet<T> source, int chunkSize)
    {
        var listOfHashSets = new List<HashSet<T>>();
        var currentHashSet = new HashSet<T>();
        foreach (var item in source)
        {
            currentHashSet.Add(item);
            if (currentHashSet.Count == chunkSize)
            {
                listOfHashSets.Add(currentHashSet);
                currentHashSet = new HashSet<T>();
            }
        }
        if (currentHashSet.Count > 0)
        {
            listOfHashSets.Add(currentHashSet);
        }
        return listOfHashSets;
    }
}