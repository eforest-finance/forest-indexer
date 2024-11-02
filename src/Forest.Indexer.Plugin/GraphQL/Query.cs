using System.Linq.Dynamic.Core;
using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using GraphQL;
using Microsoft.Extensions.Logging;
using Nest;
using Volo.Abp.ObjectMapping;
using DateTime = System.DateTime;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    private const string SortTypeNumber = "number";
    private const string SortScriptSourceValueLength = "doc['seedOwnedSymbol'].value.length()";
    
    // [Name("syncState")]
    // public static async Task<SyncStateDto> SyncState(
    //     [FromServices] IClusterClient clusterClient, [FromServices] IAppInfoProvider clientInfoProvider,
    //     [FromServices] IObjectMapper objectMapper, GetSyncStateDto dto)
    // {
    //     var version = clientInfoProvider.Version;
    //     var clientId = clientInfoProvider.AppId;
    //     var blockStateSetInfoGrain =
    //         clusterClient.GetGrain<IBlockStateSetInfoGrain>(
    //             GrainIdHelper.GenerateGrainId("BlockStateSetInfo", clientId, dto.ChainId, version));
    //     var confirmedHeight = await blockStateSetInfoGrain.GetConfirmedBlockHeight(dto.FilterType);
    //     return new SyncStateDto
    //     {
    //         ConfirmedBlockHeight = confirmedHeight todo v2
    //     };
    //     return new SyncStateDto
    //     {
    //         ConfirmedBlockHeight = 0
    //     };
    //
    // }

    [Name("nftOffers")]
    public static async Task<NftOfferPageResultDto> NftOffers(
        [FromServices] IReadOnlyRepository<OfferInfoIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        [FromServices] IReadOnlyRepository<TokenInfoIndex> tokenIndexRepository,
        GetNFTOffersDto dto)
    {
        var decimals = 0;
        if (!dto.NFTInfoId.IsNullOrEmpty())
        {
            var tokenId = dto.NFTInfoId;
            var queryable1 = await tokenIndexRepository.GetQueryableAsync();
            queryable1 = queryable1.Where(i => i.Id == tokenId);

            var tokenInfoIndex = queryable1.Skip(0).Take(1).ToList().FirstOrDefault();
            if (tokenInfoIndex != null)
            {
                decimals = tokenInfoIndex.Decimals;
            }
        }
       
        var queryable = await repository.GetQueryableAsync();

        if (!dto.OfferNotFrom.IsNullOrEmpty())
        {
            queryable = queryable.Where(i => i.OfferFrom != dto.OfferNotFrom);
        }

        if (!dto.ChainId.IsNullOrEmpty())
        {
            queryable = queryable.Where(i => i.ChainId == dto.ChainId);
        }

        if (!dto.ChainIdList.IsNullOrEmpty())
        {
            queryable = queryable.Where(i => dto.ChainIdList.Contains(i.ChainId));
        }

        if (!dto.NFTInfoId.IsNullOrEmpty())
        {
            queryable = queryable.Where(i => i.BizInfoId == dto.NFTInfoId);
        }

        if (!dto.NFTInfoIdList.IsNullOrEmpty())
        {
            queryable = queryable.Where(i => dto.NFTInfoIdList != null && dto.NFTInfoIdList.Contains(i.BizInfoId));
        }

        if (!dto.Symbol.IsNullOrEmpty())
        {
            queryable = queryable.Where(i => i.BizSymbol == dto.Symbol);
        }

        if (!dto.OfferFrom.IsNullOrEmpty())
        {
            queryable = queryable.Where(i => i.OfferFrom == dto.OfferFrom);
        }

        if (!dto.OfferTo.IsNullOrEmpty())
        {
            queryable = queryable.Where(i => i.OfferTo == dto.OfferTo);
        }

        if (dto.ExpireTimeGt != null && dto.ExpireTimeGt > 0)
        {
            var expireTimeGt = DateTimeHelper.FromUnixTimeMilliseconds((long)dto.ExpireTimeGt);
            queryable = queryable.Where(i => i.ExpireTime > expireTimeGt);
        }

        queryable = queryable.Where(i => i.RealQuantity > 0);
        
        var count = queryable.Count();
        var result = queryable
            .OrderByDescending(i => i.Price)
            .ThenBy(i => i.CreateTime)
            .ThenBy(i => i.ExpireTime)
            .Skip(dto.SkipCount)
            .Take(dto.MaxResultCount)
            .ToList();

        if (result.IsNullOrEmpty())
            return new NftOfferPageResultDto
            { 
                TotalRecordCount = 0,
                Data = new List<NFTOfferDto>()
            };
        
        var dataList = result.Where(i => i != null).Select(i =>
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
            TotalRecordCount = count,
            Data = dataList
        };
    }

    [Name("nftActivityList")]
    public static async Task<NFTActivityPageResultDto> NFTActivityListAsync(
        [FromServices] IReadOnlyRepository<NFTActivityIndex> _nftActivityIndexRepository,
        GetActivitiesDto input, [FromServices] IObjectMapper objectMapper)
    {
        
        var queryable = await _nftActivityIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(i=>i.NftInfoId == input.NFTInfoId);
        
        if (input.Types?.Count > 0)
        {
            queryable = queryable.Where(i=>input.Types.Contains(i.IntType));
        }

        if (input.TimestampMin is > 0)
        {
            var minDateTime = DateTime.UnixEpoch.AddMilliseconds((double)input.TimestampMin);
            queryable = queryable.Where(i => i.Timestamp >= minDateTime);
        }

        if (input.TimestampMax is > 0)
        {
            var maxDateTime = DateTime.UnixEpoch.AddMilliseconds((double)input.TimestampMax);
            queryable = queryable.Where(i => i.Timestamp <= maxDateTime);
        }

        var count = queryable.Count();
        var list = queryable
            .OrderByDescending(i => i.Timestamp)
            .ThenBy(i => i.IntType)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount).ToList();
        
        var dataList = objectMapper.Map<List<NFTActivityIndex>, List<NFTActivityDto>>(list);
        return new NFTActivityPageResultDto
        {
            Data = dataList,
            TotalRecordCount = count
        };
    }
    
    
    [Name("collectionActivityList")]
    public static async Task<NFTActivityPageResultDto> CollectionActivityListAsync(
        [FromServices] IReadOnlyRepository<NFTActivityIndex> _nftActivityIndexRepository,
        GetCollectionActivitiesDto input, [FromServices] IObjectMapper objectMapper)
    {
        var queryable = await _nftActivityIndexRepository.GetQueryableAsync();

        if (!input.BizIdList.IsNullOrEmpty())
        {
            queryable = queryable.Where(i => input.BizIdList.Contains(i.NftInfoId));
        }

        if (input.Types?.Count > 0)
        {
            queryable = queryable.Where(i => input.Types.Contains(i.IntType));
        }
        
        var collectionSymbolPre = TokenHelper.GetCollectionIdPre(input.CollectionId);
        queryable.Where(i => i.NftInfoId.Contains(collectionSymbolPre));

        var count = queryable.Count();
        var list = queryable
            .OrderByDescending(i => i.Timestamp)
            .OrderBy(i => i.IntType)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();
        
        var dataList = objectMapper.Map<List<NFTActivityIndex>, List<NFTActivityDto>>(list);
        
        var totalCount = count;

        return new NFTActivityPageResultDto
        {
            Data = dataList,
            TotalRecordCount = (long)(totalCount == null ? 0 : totalCount),
        };
    }

    [Name("messageActivityList")]
    public static async Task<NFTActivityPageResultDto> MessageActivityListAsync(
        [FromServices] IReadOnlyRepository<NFTActivityIndex> _nftActivityIndexRepository,
        GetMessageActivitiesDto input, [FromServices] IObjectMapper objectMapper)
    {
        var queryable = await _nftActivityIndexRepository.GetQueryableAsync();

        queryable.Where(i => i.ChainId != ForestIndexerConstants.MainChain);

        if (!input.ChainId.IsNullOrEmpty())
        {
            queryable.Where(i => i.ChainId == input.ChainId);
        }

        queryable.Where(i => i.BlockHeight >= input.BlockHeight);

        if (input.Types?.Count > 0)
        {
            queryable.Where(i => input.Types.Contains(i.IntType));
        }

        var count = queryable.Count();
        var list = queryable
            .OrderBy(i => i.BlockHeight)
            .Skip(input.SkipCount)
            .Take(ForestIndexerConstants.DefaultMaxCountNumber)
            .ToList();
        var dataList = objectMapper.Map<List<NFTActivityIndex>, List<NFTActivityDto>>(list);

        var totalCount = count;

        return new NFTActivityPageResultDto
        {
            Data = dataList,
            TotalRecordCount = (long)(totalCount == null ? 0 : totalCount),
        };
    }

    [Name("marketData")]
    public static async Task<MarketDataPageResultDto> MarketDataAsync(
        [FromServices] IReadOnlyRepository<NFTMarketDayIndex> _nftMarketDayIndexRepository,
        [FromServices] IObjectMapper objectMapper,
        GetNFTMarketDto input)
    {
        var utcNow = DateTime.UtcNow;
        var queryable = await _nftMarketDayIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(i => i.NFTInfoId == input.NFTInfoId);
        
        if (input.TimestampMin != 0)
        {
            queryable = queryable.Where(i =>
                i.DayBegin >= DateTimeOffset.FromUnixTimeMilliseconds(input.TimestampMin).UtcDateTime);
        }

        if (input.TimestampMax != 0)
        {
            queryable = queryable.Where(i =>
                i.DayBegin <= DateTimeOffset.FromUnixTimeMilliseconds(input.TimestampMax).UtcDateTime);
        }

        var count = queryable.Count();
        var list = queryable.OrderByDescending(i => i.DayBegin)
            .Skip(0)
            .Take(ForestIndexerConstants.MaxCountNumber)
            .ToList();

        return new MarketDataPageResultDto
        {
            TotalRecordCount = count,
            Data = objectMapper.Map<List<NFTMarketDayIndex>, List<NFTInfoMarketDataDto>>(list)
        };
    }

    private static async Task SetOwnerInfoAndAuctionInfoAsync
    (
        IReadOnlyRepository<UserBalanceIndex> userBalanceRepository,
        IReadOnlyRepository<SymbolAuctionInfoIndex> symbolAuctionInfoRepository,
        SeedInfoDto seedInfoDto)
    {
        if (seedInfoDto.Status == SeedStatus.UNREGISTERED)
        {
            var symbolAuctionInfoIndexQueryable = await symbolAuctionInfoRepository.GetQueryableAsync();
            symbolAuctionInfoIndexQueryable = symbolAuctionInfoIndexQueryable.Where(i => i.Symbol == seedInfoDto.SeedSymbol);
            
            var symbolAuctionInfoIndex =
                symbolAuctionInfoIndexQueryable.OrderByDescending(i => i.StartTime).Skip(0).Take(1).ToList().FirstOrDefault();
            if (symbolAuctionInfoIndex != null)
            {
                seedInfoDto.TopBidPrice = symbolAuctionInfoIndex.FinishPrice;
                seedInfoDto.AuctionEndTime = symbolAuctionInfoIndex.EndTime;
            }

            var userBalanceIndexQueryable = await userBalanceRepository.GetQueryableAsync();
            userBalanceIndexQueryable = userBalanceIndexQueryable.Where(i => i.Symbol == seedInfoDto.SeedSymbol);
            userBalanceIndexQueryable = userBalanceIndexQueryable.Where(i => i.Amount > 0);

            var userBalanceIndex = userBalanceIndexQueryable.Skip(0).Take(1).ToList().FirstOrDefault();
            if (userBalanceIndex != null)
            {
                seedInfoDto.Owner = userBalanceIndex.Address;
                seedInfoDto.CurrentChainId = userBalanceIndex.ChainId;
            }
        }
    }

    public static async Task<SeedInfoDto> GetSeedInfoAsync(
        [FromServices] IReadOnlyRepository<TsmSeedSymbolIndex> seedRepository,
        [FromServices] IReadOnlyRepository<UserBalanceIndex> userBalanceRepository,
        [FromServices]
        IReadOnlyRepository<SymbolAuctionInfoIndex> symbolAuctionInfoRepository,
        [FromServices] IObjectMapper objectMapper,
        GetSeedInput input)
    {
        var tsmSeedSymbolIndexQueryable = await seedRepository.GetQueryableAsync();
        
        tsmSeedSymbolIndexQueryable = tsmSeedSymbolIndexQueryable.Where(i => i.Symbol == input.Symbol);
        tsmSeedSymbolIndexQueryable = tsmSeedSymbolIndexQueryable.Where(i => i.IsBurned == false);

        var seedSymbolIndex = tsmSeedSymbolIndexQueryable.Skip(0).Take(1).ToList().FirstOrDefault();
        if (seedSymbolIndex == null)
        {
            //while seed is used for create token, it will be burned, so we need to query the seed info from the main chain event it is burned.
            var tsmSeedSymbolIndexQueryable2 = await seedRepository.GetQueryableAsync();
        
            tsmSeedSymbolIndexQueryable2 = tsmSeedSymbolIndexQueryable2.Where(i => i.Symbol == input.Symbol);
            tsmSeedSymbolIndexQueryable2 = tsmSeedSymbolIndexQueryable2.Where(i => i.ChainId == ForestIndexerConstants.MainChain);

            seedSymbolIndex = tsmSeedSymbolIndexQueryable2.Skip(0).Take(1).ToList().FirstOrDefault();
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
        [FromServices] IReadOnlyRepository<TsmSeedSymbolIndex> seedRepository,
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
        
        var queryable = await seedRepository.GetQueryableAsync();
        
        if (input.ChainIds != null && input.ChainIds.Any())
        {
            queryable = queryable.Where(i => input.ChainIds.Contains(i.ChainId));
        }

        if (input.TokenTypes != null && input.TokenTypes.Any())
        {
            var intTokenTypes = input.TokenTypes.Select(i => (int)i).ToList();
            queryable = queryable.Where(i => intTokenTypes.Contains(i.IntTokenType));
        }

        if (input.SeedTypes != null && input.SeedTypes.Any())
        {
            var intSeedTypes = input.SeedTypes.Select(i => (int)i).ToList();
            queryable = queryable.Where(i => intSeedTypes.Contains(i.IntSeedType));
        }

        if (input.SymbolLengthMin > 0)
        {
            queryable = queryable.Where(i => i.SymbolLength >= input.SymbolLengthMin);
        }
        
        if (input.SymbolLengthMax > 0)
        {
            queryable = queryable.Where(i => i.SymbolLength <= input.SymbolLengthMax);
        }

        if (input.PriceMin>0)
        {
            queryable = queryable.Where(i => i.TokenPrice.Amount >= input.PriceMin);
        }

        if (input.PriceMax > 0)
        {
            queryable = queryable.Where(i => i.TokenPrice.Amount <= input.PriceMax);
        }
        queryable = queryable.Where(i => i.IsBurned == false);
        queryable = queryable.Where(i => i.Status == SeedStatus.AVALIABLE || i.Status == SeedStatus.UNREGISTERED);

        var count = queryable.Count();
        
        var result = queryable.OrderBy(i => i.Symbol)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        var dataList = objectMapper.Map<List<TsmSeedSymbolIndex>, List<SeedInfoDto>>(result);
        var pageResult = new SpecialSeedsPageResultDto
        {
            TotalRecordCount = count,
            Data = dataList
        };
        return pageResult;
    }
    
    [Name("nftSoldData")]
    public static async Task<GetNFTSoldDataDto> NftSoldDataAsync(
        [FromServices] IReadOnlyRepository<SoldIndex> _nftSoldIndexRepository,
         GetNFTSoldDataInput input)
    {
        var queryable = await _nftSoldIndexRepository.GetQueryableAsync();
        
        if (input.StartTime != DateTime.MinValue)
        {
            queryable = queryable.Where(i =>
                i.DealTime >= input.StartTime);
        }

        if (input.EndTime != DateTime.MinValue)
        {
            queryable = queryable.Where(i =>
                i.DealTime < input.EndTime);
        }


        var count = queryable.Count();
        var list = queryable.Skip(0).Take(ForestIndexerConstants.DefaultMaxCountNumber).ToList();
        
        long totalTransAmount = 0;
        long totalNFTAmount = 0;
        long totalTransCount = count;

        HashSet<string> addressSet = new HashSet<string>();
        
        if (count > 0)
        {
            foreach (var nftSoldIndex in list)
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
        [FromServices] IReadOnlyRepository<NFTActivityIndex> _nftActivityIndexRepository,
        GetActivitiesConditionDto input, [FromServices] IObjectMapper objectMapper)
    {
        
        var queryable = await _nftActivityIndexRepository.GetQueryableAsync();
        
        queryable = queryable.Where(i => i.NftInfoId == input.NFTInfoId);

        if (input.Types?.Count > 0)
        {
            queryable = queryable.Where(i => input.Types.Contains(i.IntType));
        }

        if (input.TimestampMin != null && input.TimestampMin is > 0)
        {
            queryable = queryable.Where(i =>
                i.Timestamp >= DateTimeOffset
                    .FromUnixTimeMilliseconds((long)input.TimestampMin).UtcDateTime);
        }

        if (input.TimestampMax is > 0)
        {
            queryable = queryable.Where(i =>
                i.Timestamp <= DateTimeOffset
                    .FromUnixTimeMilliseconds((long)input.TimestampMax).UtcDateTime);
        }

        if (!input.FilterSymbol.IsNullOrEmpty())
        {
            queryable = queryable.Where(i =>
                i.NftInfoId.Contains(input.FilterSymbol));
        }

        var count = queryable.Count();
        if (input.SortType.IsNullOrEmpty() || input.SortType.Equals("DESC"))
        {
            queryable = queryable.OrderByDescending(i => i.Timestamp);
        }
        else
        {
            queryable = queryable.OrderBy(i => i.Timestamp);
        }
        var list = queryable.Skip(input.SkipCount).Take(input.MaxResultCount).ToList();
        var dataList = objectMapper.Map<List<NFTActivityIndex>, List<NFTActivityDto>>(list);
        dataList = dataList.Where(o => (double)(o.Amount * o.Price) > input.AbovePrice).ToList();
        return new NFTActivityPageResultDto
        {
            Data = dataList,
            TotalRecordCount = count
        };
    }

    public static async Task<SeedInfoDto> SearchSeedInfoAsync(
        [FromServices] IReadOnlyRepository<TsmSeedSymbolIndex> tsmSeedSymbolRepository,
        [FromServices] IReadOnlyRepository<SeedPriceIndex> seedPriceRepository,
        [FromServices] IReadOnlyRepository<UniqueSeedPriceIndex> uniqueSeedPriceRepository,
        [FromServices] IReadOnlyRepository<UserBalanceIndex> userBalanceRepository,
        [FromServices]
        IReadOnlyRepository<SymbolAuctionInfoIndex> symbolAuctionInfoRepository,
        [FromServices] IObjectMapper objectMapper,
        SearchSeedInput input)
    {

        var pair = GetSymbolKeyValuePair(input);
        var queryable = await tsmSeedSymbolRepository.GetQueryableAsync();
        queryable = queryable.Where(i => i.Symbol == pair.Key);
        queryable = queryable.Where(i => i.IsBurned == false);

        var symbol = pair.Key;
        var seedSymbolIndex = queryable.Skip(0).Take(1).ToList().FirstOrDefault();
        if (seedSymbolIndex == null)
        {
            //while seed is used for create token, it will be burned, so we need to query the seed info from the main chain event it is burned.
            var queryable2 = await tsmSeedSymbolRepository.GetQueryableAsync();
            queryable2 = queryable2.Where(i => i.Symbol == pair.Key);
            queryable2 = queryable2.Where(i => i.ChainId == ForestIndexerConstants.MainChain);


            seedSymbolIndex = queryable2.Skip(0).Take(1).ToList().FirstOrDefault();
            if (seedSymbolIndex == null)
            {
                seedSymbolIndex = new TsmSeedSymbolIndex()
                {
                    Symbol = symbol,
                    SeedName = IdGenerateHelper.GetSeedName(symbol),
                    Status = SeedStatus.AVALIABLE
                };
                seedSymbolIndex.OfType(TokenHelper.GetTokenType(symbol));
                seedSymbolIndex.OfType(SeedType.Regular);
            }
        }

        if (seedSymbolIndex.Status == SeedStatus.AVALIABLE)
        {
            var support = await IsSupportAsync(tsmSeedSymbolRepository, pair);
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
                var support = await IsSupportAsync(tsmSeedSymbolRepository, pair);
                if (!support.IsSupport)
                {
                    var seedInfoDtoNotSupport = objectMapper.Map<TsmSeedSymbolIndex, SeedInfoDto>(seedSymbolIndex);
                    seedInfoDtoNotSupport.NotSupportSeedStatus = support.NotSupportSeedStatus;
                    return seedInfoDtoNotSupport;
                }
        }

        var seedInfoDto = objectMapper.Map<TsmSeedSymbolIndex, SeedInfoDto>(seedSymbolIndex);
        // Logger.LogDebug("seedInfoDto NotSupportSeedStatus {Status}", seedInfoDto.NotSupportSeedStatus);
        if (seedSymbolIndex.Status == SeedStatus.AVALIABLE &&
            (seedSymbolIndex.IntSeedType == (int)SeedType.Regular ||
             seedSymbolIndex.IntSeedType == (int)SeedType.Unique))
        {
            var seedPriceId = IdGenerateHelper.GetSeedPriceId(input.TokenType, symbol.Length);

            var seedPriceQueryable = await seedPriceRepository.GetQueryableAsync();
            seedPriceQueryable = seedPriceQueryable.Where(i => i.Id == seedPriceId);
            var seedPriceIndex = seedPriceQueryable.Skip(0).Take(1).ToList().FirstOrDefault();
            
            if (seedPriceIndex != null)
            {
                var uniqueSeedPriceQueryable = await uniqueSeedPriceRepository.GetQueryableAsync();
                uniqueSeedPriceQueryable = uniqueSeedPriceQueryable.Where(i => i.Id == seedPriceId);
                var uniqueSeedPriceIndex = uniqueSeedPriceQueryable.Skip(0).Take(1).ToList().FirstOrDefault();
                
                seedInfoDto.TokenPrice = seedPriceIndex.TokenPrice;
                if (uniqueSeedPriceIndex != null && seedSymbolIndex.IntSeedType == (int)SeedType.Unique)
                {
                    seedInfoDto.TokenPrice.Amount += uniqueSeedPriceIndex.TokenPrice.Amount;
                }
            }
        }

        await SetOwnerInfoAndAuctionInfoAsync(userBalanceRepository, symbolAuctionInfoRepository, seedInfoDto);
        return seedInfoDto;
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
        IReadOnlyRepository<TsmSeedSymbolIndex> tsmSeedSymbolRepository,
        KeyValuePair<string, string> pair)
    {
        var queryable3 = await tsmSeedSymbolRepository.GetQueryableAsync();
        queryable3 = queryable3.Where(i => i.Symbol == pair.Key);
        queryable3 = queryable3.Where(i => i.IsBurned == false);
        
        var otherSeedSymbolIndex = queryable3.Skip(0).Take(1).ToList().FirstOrDefault();
        if (otherSeedSymbolIndex == null)
        {
            var queryable4 = await tsmSeedSymbolRepository.GetQueryableAsync();
            queryable4 = queryable4.Where(i => i.Symbol == pair.Key);
            queryable4 = queryable4.Where(i => i.ChainId == ForestIndexerConstants.MainChain);

            otherSeedSymbolIndex = queryable4.Skip(0).Take(1).ToList().FirstOrDefault();
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
}