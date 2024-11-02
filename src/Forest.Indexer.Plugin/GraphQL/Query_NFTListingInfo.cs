using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("nftListingInfo")]
    public static async Task<NftListingPageResultDto> NFTListingInfo(
        [FromServices] IReadOnlyRepository<NFTListingInfoIndex> nftListingRepo,
        [FromServices] IReadOnlyRepository<TokenInfoIndex> tokenIndexRepository,
        [FromServices] IObjectMapper objectMapper,
        GetNFTListingDto input)
    {
        if (input.Symbol.IsNullOrWhiteSpace() || input.ChainId.IsNullOrWhiteSpace())
            return new NftListingPageResultDto("invalid input param");

        // Logger.LogDebug($"[NFTListingInfo] INPUT: chainId={input.ChainId}, symbol={input.Symbol}, owner={input.Owner}");
        
        var decimals = 0;
        var tokenId = IdGenerateHelper.GetTokenInfoId(input.ChainId, input.Symbol);
        var queryableToken = await tokenIndexRepository.GetQueryableAsync();
        queryableToken = queryableToken.Where(i => i.Id == tokenId);
        var tokenInfoIndex = queryableToken.Skip(0).Take(1).ToList().FirstOrDefault();
        if (tokenInfoIndex != null)
        {
            decimals = tokenInfoIndex.Decimals;
        }

        // query listing info
        var queryableListing = await nftListingRepo.GetQueryableAsync();
        var minQuantity = (int)(1 * Math.Pow(10, decimals));

        queryableListing = queryableListing.Where(index => index.ChainId == input.ChainId);
        queryableListing = queryableListing.Where(index => index.Symbol == input.Symbol);
        queryableListing = queryableListing.Where(index => index.RealQuantity >= minQuantity);

        if (input.ExpireTimeGt != null && input.ExpireTimeGt>0)
        {
            var expireTimeGt = DateTimeHelper.FromUnixTimeMilliseconds((long)input.ExpireTimeGt);
            queryableListing = queryableListing.Where(index => index.ExpireTime > expireTimeGt);
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
            .ThenBy(a => a.StartTime)
            .ThenBy(a => a.ExpireTime)
            .ToList();
        
        // Logger.LogDebug(
        //     "[NFTListingInfo] SETP: query Pager chainId={A}, symbol={B}, owner={C}, count={D}",input.ChainId,input.Symbol,input.Owner,result);

        var dataList = result.Where(i => i != null).Select(i =>
        {
            var item = objectMapper.Map<NFTListingInfoIndex, NFTListingInfoDto>(i);
           
            // Logger.LogDebug("listing quantity={Quantity} after quantity {AQuantity} decimal {Decimals} ",item.Quantity,TokenHelper.GetIntegerDivision(item.Quantity, decimals),decimals);
            // Logger.LogDebug("listing quantity real={Quantity} after quantity {AQuantity} decimal {Decimals}",item.RealQuantity,TokenHelper.GetIntegerDivision(item.RealQuantity, decimals),decimals);
            
            item.PurchaseToken = objectMapper.Map<TokenInfoIndex, TokenInfoDto>(i.PurchaseToken);
            
            item.Quantity = TokenHelper.GetIntegerDivision(item.Quantity, decimals);
            item.RealQuantity = TokenHelper.GetIntegerDivision(item.RealQuantity, decimals);
            return item;
        }).ToList();

        // Logger.LogDebug(
        //     "[NFTListingInfo] SETP: query Pager chainId={A}, symbol={B}, owner={C}, count={D}", input.ChainId,
        //     input.Symbol, input.Owner, result);
        return new NftListingPageResultDto(result.Count, dataList, "success");
    }
    
    [Name("collectedNFTListingInfo")]
    public static async Task<NftListingPageResultDto> CollectedNFTListingInfo(
        [FromServices] IReadOnlyRepository<NFTListingInfoIndex> nftListingRepo,
        [FromServices] IObjectMapper objectMapper,
        GetCollectedNFTListingDto dto)
    {
        var utcNow = DateTime.UtcNow;
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
            queryable = queryable.Where(index => index.ExpireTime > utcNow);
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
    
    /*private static Func<SortDescriptor<NFTListingInfoIndex>, IPromise<IList<ISort>>> GetSortForListingInfos()
    {
        SortDescriptor<NFTListingInfoIndex> sortDescriptor = new SortDescriptor<NFTListingInfoIndex>();
        sortDescriptor.Ascending(a=>a.Prices);
        sortDescriptor.Ascending(a=>a.StartTime);
        sortDescriptor.Ascending(a => a.ExpireTime);
        IPromise<IList<ISort>> promise = sortDescriptor;
        return s => promise;
    }*/
    
    /*private static Func<SortDescriptor<NFTListingInfoIndex>, IPromise<IList<ISort>>> GetSortForListingInfosByBlockHeight()
    {
        SortDescriptor<NFTListingInfoIndex> sortDescriptor = new SortDescriptor<NFTListingInfoIndex>();
        sortDescriptor.Ascending(a=>a.BlockHeight);
        IPromise<IList<ISort>> promise = sortDescriptor;
        return s => promise;
    }*/

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
        GetExpiredNFTMinPriceDto input)
    {
        var utcNow = DateTime.UtcNow;
        var queryable = await nftListingRepo.GetQueryableAsync();

        Logger.LogDebug($"[getMinPriceNft] INPUT: chainId={input.ChainId}, expired={input.ExpireTimeGt}");
        
        queryable = queryable.Where(index => index.ChainId == input.ChainId);
        
        if (input.ExpireTimeGt != null && (long)input.ExpireTimeGt > 0)
        {
            queryable = queryable.Where(index =>
                index.ExpireTime >= DateTimeHelper.FromUnixTimeMilliseconds((long)input.ExpireTimeGt));
        }
        
        queryable = queryable.Where(index => index.ExpireTime < utcNow);

        var result = queryable.Skip(0).ToList();
        Logger.LogDebug($"[NFTListingInfo] STEP: query chainId={input.ChainId}, count={result?.Count}");
        
        List<ExpiredNftMinPriceDto> data = new();
        foreach (var item in result)
        {
            var queryableListing = await nftListingRepo.GetQueryableAsync();
            queryableListing = queryableListing.Where(i => i.Id == item.NftInfoId);
            var listingInfo = queryableListing.Skip(0).Take(1).ToList();
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
        var utcNow = DateTime.UtcNow;
        var queryableListing = await nftListingRepo.GetQueryableAsync();
        queryableListing = queryableListing.Where(index => index.ChainId == chainId);

        var expiredTime = DateTimeHelper.FromUnixTimeMilliseconds(expireTimeGt);

        queryableListing = queryableListing.Where(index => index.ExpireTime >= expiredTime);
        queryableListing = queryableListing.Where(index => index.ExpireTime < utcNow);

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

        var result = queryable.Skip(dto.SkipCount).Take(1000)
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
        GetNFTListingDto input)
    {
        if (input.ChainId.IsNullOrWhiteSpace())
            return new NftListingPageResultDto("invalid input param");

        Logger.LogDebug("[NFTListingInfoAll] INPUT: chainId={A}, blockHeight={B}", input.ChainId, input.BlockHeight);
        var queryable = await nftListingRepo.GetQueryableAsync();
        // query listing info
        queryable = queryable.Where(f=>f.ChainId == input.ChainId);
        queryable = queryable.Where(f=>f.BlockHeight >= input.BlockHeight);
        if (input.ExpireTimeGt != null)
        {
            var expiredTime = DateTimeHelper.FromUnixTimeMilliseconds((long)input.ExpireTimeGt);
            queryable = queryable.Where(index=>index.ExpireTime > expiredTime);
        }

        var result = queryable.Skip(input.SkipCount).Take(input.MaxResultCount).OrderBy(a=>a.BlockHeight).ToList();
        var count = result.IsNullOrEmpty() ? 0 : result.Count;
        Logger.LogDebug(
            "[NFTListingInfoAll] SETP: query Pager chainId={A}, height={B}, count={C}", input.ChainId, input.BlockHeight,count);
        
        var dataList = result.Select(i =>
        {
            var item = objectMapper.Map<NFTListingInfoIndex, NFTListingInfoDto>(i);
            item.PurchaseToken = objectMapper.Map<TokenInfoIndex, TokenInfoDto>(i.PurchaseToken);
            item.Quantity = item.Quantity;
            item.RealQuantity = item.RealQuantity;
            return item;
        }).ToList();

        Logger.LogDebug(
            "[NFTListingInfoAll] SETP: Convert Data chainId={A}, height={B}, count={C}",input.ChainId, input.BlockHeight,dataList.Count);
        return new NftListingPageResultDto(result.Count, dataList);
    }
}