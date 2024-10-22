using AeFinder.Sdk;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Microsoft.Extensions.Logging;
using Nest;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("nftListingInfo")]
    public static async Task<NftListingPageResultDto> NFTListingInfo(
        [FromServices] IReadOnlyRepository<NFTListingInfoIndex> nftListingRepo,
        [FromServices] IReadOnlyRepository<TokenInfoIndex> tokenIndexRepository,
        [FromServices] IObjectMapper objectMapper,
        [FromServices] ILogger<NFTListingInfoIndex> _logger,
        GetNFTListingDto input)
    {
        if (input.Symbol.IsNullOrWhiteSpace() || input.ChainId.IsNullOrWhiteSpace())
            return new NftListingPageResultDto("invalid input param");

        _logger.LogDebug($"[NFTListingInfo] INPUT: chainId={input.ChainId}, symbol={input.Symbol}, owner={input.Owner}");
        
        var decimals = 0;
        var tokenId = IdGenerateHelper.GetTokenInfoId(input.ChainId, input.Symbol);
        var queryableToken = await tokenIndexRepository.GetQueryableAsync();
        queryableToken = queryableToken.Where(i => i.Id == tokenId);
        var tokenInfoIndex = queryableToken.Skip(0).Take(1).ToList();
        if (!tokenInfoIndex.IsNullOrEmpty())
        {
            decimals = tokenInfoIndex.FirstOrDefault().Decimals;
        }
        
        // query listing info
        var queryableListing = await nftListingRepo.GetQueryableAsync();
        var minQuantity = (int)(1 * Math.Pow(10, decimals));

        queryableListing = queryableListing.Where(index => index.ChainId == input.ChainId);
        queryableListing = queryableListing.Where(index => index.Symbol == input.Symbol);
        queryableListing = queryableListing.Where(index => index.RealQuantity >= minQuantity);
        
        if (input.ExpireTimeGt != null)
        {
            var utcTimeStr = DateTimeOffset.FromUnixTimeMilliseconds((long)input.ExpireTimeGt).UtcDateTime
                .ToString("o");
            queryableListing = queryableListing.Where(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime) > long.Parse(utcTimeStr));
        }

        if (!input.Owner.IsNullOrWhiteSpace())
        {
            queryableListing = queryableListing.Where(index => index.Owner == input.Owner);
        }
        
        if (!input.ExcludedAddress.IsNullOrWhiteSpace())
        {
            queryableListing = queryableListing.Where(index => index.Owner != input.ExcludedAddress);
        }
        var result = queryableListing
            .Skip(input.SkipCount).Take(input.MaxResultCount)
            .OrderBy(a => a.Prices)
            .OrderBy(a => a.StartTime)
            .OrderBy(a => a.ExpireTime)
            .ToList();
        _logger.LogDebug(
            $"[NFTListingInfo] SETP: query Pager chainId={input.ChainId}, symbol={input.Symbol}, owner={input.Owner}, count={result}");
        
        var dataList = result.Select(i =>
        {
            var item = objectMapper.Map<NFTListingInfoIndex, NFTListingInfoDto>(i);
           
            _logger.LogDebug("listing quantity={Quantity} after quantity {AQuantity} decimal {Decimals} ",item.Quantity,TokenHelper.GetIntegerDivision(item.Quantity, decimals),decimals);
            _logger.LogDebug("listing quantity real={Quantity} after quantity {AQuantity} decimal {Decimals}",item.RealQuantity,TokenHelper.GetIntegerDivision(item.RealQuantity, decimals),decimals);
            
            item.PurchaseToken = objectMapper.Map<TokenInfoIndex, TokenInfoDto>(i.PurchaseToken);
            item.Quantity = TokenHelper.GetIntegerDivision(item.Quantity, decimals);
            item.RealQuantity = TokenHelper.GetIntegerDivision(item.RealQuantity, decimals);
            return item;
        }).ToList();

        _logger.LogDebug(
            $"[NFTListingInfo] SETP: Convert Data chainId={input.ChainId}, symbol={input.Symbol}, owner={input.Owner}, count={result.Count}");
        return new NftListingPageResultDto(result.Count, dataList);
    }
    
    [Name("collectedNFTListingInfo")]
    public static async Task<NftListingPageResultDto> CollectedNFTListingInfo(
        [FromServices] IReadOnlyRepository<NFTListingInfoIndex> nftListingRepo,
        [FromServices] IObjectMapper objectMapper,
        [FromServices] ILogger<NFTListingInfoIndex> _logger,
        GetCollectedNFTListingDto dto)
    {
        var queryable = await nftListingRepo.GetQueryableAsync();

        if (!dto.ChainIdList.IsNullOrEmpty())
        {
            queryable = queryable.Where(index => dto.ChainIdList.Contains(index.ChainId));
        }
        if (!dto.NFTInfoIdList.IsNullOrEmpty())
        {
            queryable = queryable.Where(index => dto.NFTInfoIdList.Contains(index.NftInfoId));
        }
        queryable = queryable.Where(index => index.RealQuantity > 0);

        if (dto.ExpireTimeGt != null)
        {
            var utcTimeStr = long.Parse(DateTimeOffset.FromUnixTimeMilliseconds((long)dto.ExpireTimeGt).UtcDateTime
                .ToString("o"));
            queryable = queryable.Where(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime) > utcTimeStr);
        }

        if (!dto.Owner.IsNullOrWhiteSpace())
        {
            queryable = queryable.Where(index => index.Owner == dto.Owner);
        }

        var result = queryable.Skip(dto.SkipCount).Take(dto.MaxResultCount)
            .OrderBy(a=>a.Prices)
            .OrderBy(a=>a.StartTime)
            .OrderBy(a => a.ExpireTime)
            .ToList();

        var dataList = result.Select(i =>
        {
            var item = objectMapper.Map<NFTListingInfoIndex, NFTListingInfoDto>(i);

            item.PurchaseToken = objectMapper.Map<TokenInfoIndex, TokenInfoDto>(i.PurchaseToken);
            item.Quantity = item.Quantity;
            item.RealQuantity = item.RealQuantity;
            return item;
        }).ToList();
        
        return new NftListingPageResultDto(result.Count, dataList);
    }
    
    private static Func<SortDescriptor<NFTListingInfoIndex>, IPromise<IList<ISort>>> GetSortForListingInfos()
    {
        SortDescriptor<NFTListingInfoIndex> sortDescriptor = new SortDescriptor<NFTListingInfoIndex>();
        sortDescriptor.Ascending(a=>a.Prices);
        sortDescriptor.Ascending(a=>a.StartTime);
        sortDescriptor.Ascending(a => a.ExpireTime);
        IPromise<IList<ISort>> promise = sortDescriptor;
        return s => promise;
    }
    
    private static Func<SortDescriptor<NFTListingInfoIndex>, IPromise<IList<ISort>>> GetSortForListingInfosByBlockHeight()
    {
        SortDescriptor<NFTListingInfoIndex> sortDescriptor = new SortDescriptor<NFTListingInfoIndex>();
        sortDescriptor.Ascending(a=>a.BlockHeight);
        IPromise<IList<ISort>> promise = sortDescriptor;
        return s => promise;
    }

    /*[Obsolete("todo V2 not use")]
    [Name("getMinListingNft")]
    public static async Task<NFTListingInfoDto> GetMinListingNftAsync(
        [FromServices] IObjectMapper objectMapper,
        [FromServices] IReadOnlyRepository<NFTListingInfoIndex> _listedNftIndexRepository,
        [FromServices] IReadOnlyRepository<UserBalanceIndex> _userBalanceIndexRepository,
        GetMinListingNftDto dto)
    {
        var listingInfo = await GetMinListingNftAsync(dto.NftInfoId, async info =>
        {
            var queryable = await _listedNftIndexRepository.GetQueryableAsync();
            queryable = queryable.Where(i => i.Id == info.Id);
            var listingInfo = queryable.ToList();
            return !listingInfo.IsNullOrEmpty();
        }, _listedNftIndexRepository, _userBalanceIndexRepository);
        return objectMapper.Map<NFTListingInfoIndex, NFTListingInfoDto>(listingInfo);
    }*/

    /*private static async Task<NFTListingInfoIndex> GetMinListingNftAsync(string nftInfoId,
        Func<NFTListingInfoIndex, Task<bool>> additionalConditionAsync,
        IReadOnlyRepository<NFTListingInfoIndex> _listedNftIndexRepository,
        IReadOnlyRepository<UserBalanceIndex> _userBalanceIndexRepository)
    {
        //Get Effective NftListingInfos
        var nftListingInfos =
            await GetEffectiveNftListingInfos(nftInfoId, _listedNftIndexRepository);
        
        //order by price asc, expireTime desc
        nftListingInfos = nftListingInfos.Where(index =>
                DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime) >= DateTime.UtcNow.ToUtcMilliSeconds())
            .OrderBy(info => info.Prices)
            .ThenByDescending(info => DateTimeHelper.ToUnixTimeMilliseconds(info.ExpireTime))
            .ToList();

        NFTListingInfoIndex minNftListing = null;
        //find first listingInfo match: userBalance > 0 and additionalCondition
        foreach (var info in nftListingInfos)
        {
            var userBalanceId = IdGenerateHelper.GetUserBalanceId(info.Owner, info.ChainId, nftInfoId);

            var userBalance = await _userBalanceIndexRepository.GetAsync(userBalanceId);

            if (userBalance?.Amount > 0 && await additionalConditionAsync(info))
            {
                minNftListing = info;
                break;
            }
        }

        return minNftListing;
    }*/

    /*private static async Task<List<NFTListingInfoIndex>> GetEffectiveNftListingInfos(string nftInfoId, IReadOnlyRepository<NFTListingInfoIndex, LogEventInfo> _listedNftIndexRepository)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();

        mustQuery.Add(q => q.TermRange(i
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime))
                .GreaterThan(DateTime.UtcNow.ToString("O"))));
        mustQuery.Add(q => q.Term(i => i.Field(index => index.NftInfoId).Value(nftInfoId)));

        QueryContainer Filter(QueryContainerDescriptor<NFTListingInfoIndex> f) => f.Bool(b => b.Must(mustQuery));

        var result = await _listedNftIndexRepository.GetListAsync(Filter, sortExp: k => k.Prices,
            sortType: SortOrder.Ascending, skip: 0);
        return result.Item2??new List<NFTListingInfoIndex>();
    }*/
    
    [Name("getExpiredNftMinPrice")]
    public static async Task<List<ExpiredNftMinPriceDto>> GetNftMinPriceAsync(
        [FromServices] IReadOnlyRepository<NFTListingInfoIndex> nftListingRepo,
        [FromServices] ILogger<NFTListingInfoIndex> logger,
        GetExpiredNFTMinPriceDto input)
    {
        var queryable = await nftListingRepo.GetQueryableAsync();

        logger.LogDebug($"[getMinPriceNft] INPUT: chainId={input.ChainId}, expired={input.ExpireTimeGt}");
        
        queryable = queryable.Where(index => index.ChainId == input.ChainId);
        
        var expiredTimeStr = DateTimeOffset.FromUnixTimeMilliseconds((long)input.ExpireTimeGt).UtcDateTime.ToString("o");
        var nowStr = DateTime.UtcNow.ToString("o");
        queryable = queryable.Where(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime) >= long.Parse(expiredTimeStr));
        queryable = queryable.Where(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime) < long.Parse(nowStr));

        var result = queryable.Skip(0).ToList();
        logger.LogDebug($"[NFTListingInfo] STEP: query chainId={input.ChainId}, count={result?.Count}");
        
        List<ExpiredNftMinPriceDto> data = new();
        foreach (var item in result)
        {
            var queryableListing = await nftListingRepo.GetQueryableAsync();
            queryableListing = queryableListing.Where(i => i.Id == item.NftInfoId);
            var listingInfo = queryableListing.ToList();
            if (listingInfo.IsNullOrEmpty())
            {
                ExpiredNftMinPriceDto priceDto = new ExpiredNftMinPriceDto()
                {
                    Key = item.NftInfoId,
                    Value = null
                };
                
                data.Add(priceDto);
            }
            else
            {
                ExpiredNftMinPriceInfo priceInfo = new ExpiredNftMinPriceInfo()
                {
                    ExpireTime = listingInfo.FirstOrDefault().ExpireTime,
                    Prices = listingInfo.FirstOrDefault().Prices,
                    Id = listingInfo.FirstOrDefault().Id,
                    Symbol = listingInfo.FirstOrDefault().Symbol
                };
                
                
                ExpiredNftMinPriceDto priceDto = new ExpiredNftMinPriceDto()
                {
                    Key = item.NftInfoId,
                    Value = priceInfo
                };
                
                data.Add(priceDto);
            }
        }
      
        return data;
    }

    [Name("getExpiredListingNft")]
    public static async Task<List<NFTListingInfoResult>> GetExpiredListingNftAsync(
        [FromServices] IReadOnlyRepository<NFTListingInfoIndex> nftListingRepo,
        [FromServices] IObjectMapper objectMapper,
        GetExpiredListingNftDto dto)
    {
        var expiredListingNft = await GetExpiredListingNftAsync(nftListingRepo, dto.ChainId, dto.ExpireTimeGt);

        return objectMapper.Map<List<NFTListingInfoIndex>, List<NFTListingInfoResult>>(expiredListingNft);
    }

    private static async Task<List<NFTListingInfoIndex>> GetExpiredListingNftAsync(
        [FromServices] IReadOnlyRepository<NFTListingInfoIndex> nftListingRepo,
        string chainId,
        long expireTimeGt)
    {
        var queryableListing = await nftListingRepo.GetQueryableAsync();
        queryableListing = queryableListing.Where(index => index.ChainId == chainId);

        var expiredTimeStr = DateTimeOffset.FromUnixTimeMilliseconds(expireTimeGt).UtcDateTime.ToString("o");
        var nowStr = DateTime.UtcNow.ToString("o");
        queryableListing = queryableListing.Where(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime) >=long.Parse(expiredTimeStr));
        queryableListing = queryableListing.Where(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime) <long.Parse(nowStr));

        var result = queryableListing.ToList();

        return result ?? new List<NFTListingInfoIndex>();
    }

    [Name("nftListingChange")]
    public static async Task<NFTListingChangeDtoPageResultDto> NFTListingChangeAsync(
        [FromServices] IReadOnlyRepository<NFTListingChangeIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetSeedMainChainChangeDto dto)
    {
        var queryable = await repository.GetQueryableAsync();
        queryable = queryable.Where(f=>f.ChainId == dto.ChainId);
        queryable = queryable.Where(f=>f.BlockHeight >= dto.BlockHeight);

        var result = queryable.Skip(dto.SkipCount)
            .OrderBy(o => o.BlockHeight).ToList();
        var dataList = objectMapper.Map<List<NFTListingChangeIndex>, List<NFTListingChangeDto>>(result);
        var pageResult = new NFTListingChangeDtoPageResultDto
        {
            TotalRecordCount = result.Count,
            Data = dataList
        };
        return pageResult;
    }
   [Name("nftListingInfoAll")]
    public static async Task<NftListingPageResultDto> NFTListingInfoAll(
        [FromServices] IReadOnlyRepository<NFTListingInfoIndex> nftListingRepo,
        [FromServices] IReadOnlyRepository<TokenInfoIndex> tokenIndexRepository,
        [FromServices] IObjectMapper objectMapper,
        [FromServices] ILogger<NFTListingInfoIndex> _logger,
        GetNFTListingDto input)
    {
        if (input.ChainId.IsNullOrWhiteSpace())
            return new NftListingPageResultDto("invalid input param");

        _logger.LogDebug("[NFTListingInfoAll] INPUT: chainId={A}, blockHeight={B}", input.ChainId, input.BlockHeight);

        var queryable = await nftListingRepo.GetQueryableAsync();
        // query listing info
        queryable = queryable.Where(f=>f.ChainId == input.ChainId);
        queryable = queryable.Where(f=>f.BlockHeight >= input.BlockHeight);
        if (input.ExpireTimeGt != null)
        {
            var utcTimeStr = DateTimeOffset.FromUnixTimeMilliseconds((long)input.ExpireTimeGt).UtcDateTime
                .ToString("o");
            queryable = queryable.Where(index=>DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime) > long.Parse(utcTimeStr));
        }

        var result = queryable.Skip(input.SkipCount).Take(input.MaxResultCount).OrderBy(a=>a.BlockHeight).ToList();
        var count = result.IsNullOrEmpty() ? 0 : result.Count;
        _logger.LogDebug(
            "[NFTListingInfoAll] SETP: query Pager chainId={A}, height={B}, count={C}", input.ChainId, input.BlockHeight,count);
        
        var dataList = result.Select(i =>
        {
            var item = objectMapper.Map<NFTListingInfoIndex, NFTListingInfoDto>(i);
            item.PurchaseToken = objectMapper.Map<TokenInfoIndex, TokenInfoDto>(i.PurchaseToken);
            item.Quantity = item.Quantity;
            item.RealQuantity = item.RealQuantity;
            return item;
        }).ToList();

        _logger.LogDebug(
            "[NFTListingInfoAll] SETP: Convert Data chainId={A}, height={B}, count={C}",input.ChainId, input.BlockHeight,dataList.Count);
        return new NftListingPageResultDto(result.Count, dataList);
    }
}