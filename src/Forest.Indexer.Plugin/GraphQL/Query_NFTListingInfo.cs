using AElf.Contracts.Whitelist;
using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors;
using GraphQL;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("nftListingInfo")]
    public static async Task<NftListingPageResultDto> NFTListingInfo(
        [FromServices] IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo> nftListingRepo,
        [FromServices] IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> tokenIndexRepository,
        [FromServices] IObjectMapper objectMapper,
        [FromServices] ILogger<NFTListingInfoIndex> _logger,
        GetNFTListingDto input)
    {
        if (input.Symbol.IsNullOrWhiteSpace() || input.ChainId.IsNullOrWhiteSpace())
            return new NftListingPageResultDto("invalid input param");

        _logger.Debug($"[NFTListingInfo] INPUT: chainId={input.ChainId}, symbol={input.Symbol}, owner={input.Owner}");
        
        var decimals = 0;
        var tokenId = IdGenerateHelper.GetTokenInfoId(input.ChainId, input.Symbol);
        var tokenInfoIndex = await tokenIndexRepository.GetAsync(tokenId);
        if (tokenInfoIndex != null)
        {
            decimals = tokenInfoIndex.Decimals;
        }
        
        // query listing info
        var listingQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();
        var listingNotQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();

        var minQuantity = (int)(1 * Math.Pow(10, decimals));
        listingQuery.Add(q => q.Term(i => i.Field(index => index.ChainId).Value(input.ChainId)));
        listingQuery.Add(q => q.Term(i => i.Field(index => index.Symbol).Value(input.Symbol)));
        listingQuery.Add(q => q.TermRange(i => i.Field(index => index.RealQuantity).GreaterThanOrEquals(minQuantity.ToString())));

        if (input.ExpireTimeGt != null)
        {
            var utcTimeStr = DateTimeOffset.FromUnixTimeMilliseconds((long)input.ExpireTimeGt).UtcDateTime
                .ToString("o");
            listingQuery.Add(q => q.TermRange(i
                => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime)).GreaterThan(utcTimeStr)));
        }

        if (!input.Owner.IsNullOrWhiteSpace())
            listingQuery.Add(q => q.Term(i => i.Field(index => index.Owner).Value(input.Owner)));
        
        if (!input.ExcludedAddress.IsNullOrWhiteSpace())
        {
            listingNotQuery.Add(q => q.Term(i => i.Field(index => index.Owner).Value(input.ExcludedAddress)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<NFTListingInfoIndex> f) => f.Bool(b => b.Must(listingQuery).MustNot(listingNotQuery));
        
        var result = await nftListingRepo.GetSortListAsync(Filter,sortFunc: GetSortForListingInfos(), skip: input.SkipCount, limit: input.MaxResultCount);
        _logger.Debug(
            $"[NFTListingInfo] SETP: query Pager chainId={input.ChainId}, symbol={input.Symbol}, owner={input.Owner}, count={result.Item1}");
        
        var dataList = result.Item2.Select(i =>
        {
           
            var item = objectMapper.Map<NFTListingInfoIndex, NFTListingInfoDto>(i);
            
           
            _logger.Debug("listing quantity={Quantity} after quantity {AQuantity} decimal {Decimals} ",item.Quantity,TokenHelper.GetIntegerDivision(item.Quantity, decimals),decimals);
            _logger.Debug("listing quantity real={Quantity} after quantity {AQuantity} decimal {Decimals}",item.RealQuantity,TokenHelper.GetIntegerDivision(item.RealQuantity, decimals),decimals);
            
            item.PurchaseToken = objectMapper.Map<TokenInfoIndex, TokenInfoDto>(i.PurchaseToken);
            item.Quantity = TokenHelper.GetIntegerDivision(item.Quantity, decimals);
            item.RealQuantity = TokenHelper.GetIntegerDivision(item.RealQuantity, decimals);
            return item;
        }).ToList();

        _logger.Debug(
            $"[NFTListingInfo] SETP: Convert Data chainId={input.ChainId}, symbol={input.Symbol}, owner={input.Owner}, count={result.Item1}");
        return new NftListingPageResultDto(result.Item1, dataList);
    }
    
    [Name("collectedNFTListingInfo")]
    public static async Task<NftListingPageResultDto> CollectedNFTListingInfo(
        [FromServices] IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo> nftListingRepo,
        [FromServices] IObjectMapper objectMapper,
        [FromServices] ILogger<NFTListingInfoIndex> _logger,
        GetCollectedNFTListingDto dto)
    {
        var listingQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();
        var listingNotQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();

        if (!dto.ChainIdList.IsNullOrEmpty())
        {
            listingQuery.Add(q => q.Terms(i => i.Field(index => index.ChainId).Terms(dto.ChainIdList)));
        }
        if (!dto.NFTInfoIdList.IsNullOrEmpty())
        {
            listingQuery.Add(q => q.Terms(i => i.Field(index => index.NftInfoId).Terms(dto.ChainIdList)));
        }
        
        listingQuery.Add(q => q.LongRange(i => i.Field(index => index.RealQuantity).GreaterThanOrEquals(0)));

        if (dto.ExpireTimeGt != null)
        {
            var utcTimeStr = DateTimeOffset.FromUnixTimeMilliseconds((long)dto.ExpireTimeGt).UtcDateTime
                .ToString("o");
            listingQuery.Add(q => q.TermRange(i
                => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime)).GreaterThan(utcTimeStr)));
        }

        if (!dto.Owner.IsNullOrWhiteSpace())
            listingQuery.Add(q => q.Term(i => i.Field(index => index.Owner).Value(dto.Owner)));

        QueryContainer Filter(QueryContainerDescriptor<NFTListingInfoIndex> f) => f.Bool(b => b.Must(listingQuery).MustNot(listingNotQuery));
        
        var result = await nftListingRepo.GetSortListAsync(Filter,sortFunc: GetSortForListingInfos(), skip: dto.SkipCount, limit: dto.MaxResultCount);

        var dataList = result.Item2.Select(i =>
        {
            var item = objectMapper.Map<NFTListingInfoIndex, NFTListingInfoDto>(i);

            item.PurchaseToken = objectMapper.Map<TokenInfoIndex, TokenInfoDto>(i.PurchaseToken);
            item.Quantity = item.Quantity;
            item.RealQuantity = item.RealQuantity;
            return item;
        }).ToList();
        
        return new NftListingPageResultDto(result.Item1, dataList);
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

    [Name("getMinListingNft")]
    public static async Task<NFTListingInfoDto> GetMinListingNftAsync(
        [FromServices] IObjectMapper objectMapper,
        [FromServices] IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo> _listedNftIndexRepository,
        [FromServices] IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> _userBalanceIndexRepository,
        GetMinListingNftDto dto)
    {
        var listingInfo = await GetMinListingNftAsync(dto.NftInfoId, async info =>
        {
            var listingInfo = await _listedNftIndexRepository.GetAsync(info.Id);
            return listingInfo != null;
        }, _listedNftIndexRepository, _userBalanceIndexRepository);
        return objectMapper.Map<NFTListingInfoIndex, NFTListingInfoDto>(listingInfo);
    }

    private static async Task<NFTListingInfoIndex> GetMinListingNftAsync(string nftInfoId,
        Func<NFTListingInfoIndex, Task<bool>> additionalConditionAsync,
        IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo> _listedNftIndexRepository,
        IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> _userBalanceIndexRepository)
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
    }

    private static async Task<List<NFTListingInfoIndex>> GetEffectiveNftListingInfos(string nftInfoId, IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo> _listedNftIndexRepository)
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
    }
    
    [Name("getExpiredNftMinPrice")]
    public static async Task<List<ExpiredNftMinPriceDto>> GetNftMinPriceAsync(
        [FromServices] IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo> nftListingRepo,
        [FromServices] ILogger<NFTListingInfoIndex> logger,
        GetExpiredNFTMinPriceDto input)
    {
        logger.Debug($"[getMinPriceNft] INPUT: chainId={input.ChainId}, expired={input.ExpireTimeGt}");
        
        var listingQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();
        listingQuery.Add(q => q.Term(i => i.Field(index => index.ChainId).Value(input.ChainId)));
        
        var expiredTimeStr = DateTimeOffset.FromUnixTimeMilliseconds((long)input.ExpireTimeGt).UtcDateTime.ToString("o");
        var nowStr = DateTime.UtcNow.ToString("o");
            
        listingQuery.Add(q => q.TermRange(i
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime)).GreaterThanOrEquals(expiredTimeStr)));
        
        listingQuery.Add(q => q.TermRange(i
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime)).LessThan(nowStr)));
        
        QueryContainer Filter(QueryContainerDescriptor<NFTListingInfoIndex> f) => f.Bool(b => b.Must(listingQuery));
        
        var result = await nftListingRepo.GetSortListAsync(Filter, skip: 0);
        logger.Debug($"[NFTListingInfo] STEP: query chainId={input.ChainId}, count={result.Item1}");
        
        List<ExpiredNftMinPriceDto> data = new();
        foreach (var item in result.Item2)
        {
            var listingInfo = await nftListingRepo.GetAsync(item.NftInfoId);
            if (listingInfo == null)
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
                    ExpireTime = listingInfo.ExpireTime,
                    Prices = listingInfo.Prices,
                    Id = listingInfo.Id,
                    Symbol = listingInfo.Symbol
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
        [FromServices] IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo> nftListingRepo,
        [FromServices] IObjectMapper objectMapper,
        GetExpiredListingNftDto dto)
    {
        var expiredListingNft = await GetExpiredListingNftAsync(nftListingRepo, dto.ChainId, dto.ExpireTimeGt);

        return objectMapper.Map<List<NFTListingInfoIndex>, List<NFTListingInfoResult>>(expiredListingNft);
    }

    private static async Task<List<NFTListingInfoIndex>> GetExpiredListingNftAsync(
        [FromServices] IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo> nftListingRepo,
        string chainId,
        long expireTimeGt)
    {
        var listingQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();
        listingQuery.Add(q => q.Term(i => i.Field(index => index.ChainId).Value(chainId)));

        var expiredTimeStr = DateTimeOffset.FromUnixTimeMilliseconds(expireTimeGt).UtcDateTime.ToString("o");
        var nowStr = DateTime.UtcNow.ToString("o");

        listingQuery.Add(q => q.TermRange(i
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime))
                .GreaterThanOrEquals(expiredTimeStr)));

        listingQuery.Add(q => q.TermRange(i
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime)).LessThan(nowStr)));

        QueryContainer Filter(QueryContainerDescriptor<NFTListingInfoIndex> f) => f.Bool(b => b.Must(listingQuery));

        var result = await nftListingRepo.GetSortListAsync(Filter);

        return result.Item2 ?? new List<NFTListingInfoIndex>();
    }

    [Name("nftListingChange")]
    public static async Task<NFTListingChangeDtoPageResultDto> NFTListingChangeAsync(
        [FromServices] IAElfIndexerClientEntityRepository<NFTListingChangeIndex, LogEventInfo> repository,
        [FromServices] IObjectMapper objectMapper,
        GetSeedMainChainChangeDto dto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTListingChangeIndex>, QueryContainer>>()
        {
            q => q.Term(i
                => i.Field(f => f.ChainId).Value(dto.ChainId))
        };
        
        mustQuery.Add(q => q.Range(i
            => i.Field(f => f.BlockHeight).GreaterThanOrEquals(dto.BlockHeight)));

        QueryContainer Filter(QueryContainerDescriptor<NFTListingChangeIndex> f) =>
            f.Bool(b => b.Must(mustQuery));
        var result = await repository.GetListAsync(Filter, skip: dto.SkipCount, sortExp: o => o.BlockHeight);
        var dataList = objectMapper.Map<List<NFTListingChangeIndex>, List<NFTListingChangeDto>>(result.Item2);
        var pageResult = new NFTListingChangeDtoPageResultDto
        {
            TotalRecordCount = result.Item1,
            Data = dataList
        };
        return pageResult;
    }
}