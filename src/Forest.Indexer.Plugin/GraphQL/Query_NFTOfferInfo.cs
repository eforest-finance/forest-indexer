using AeFinder.Sdk;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors;
using GraphQL;
using Microsoft.Extensions.Logging;
using Nest;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("getMaxOfferInfo")]
    public static async Task<NFTOfferDto> GetMaxOfferInfoAsync(
        [FromServices] INFTInfoProvider nftInfoProvider,
        [FromServices] IObjectMapper objectMapper,
        GetMaxOfferInfoDto dto)
    {
        var offerInfo = await nftInfoProvider.GetMaxOfferInfoAsync(dto.NftInfoId);
        return objectMapper.Map<OfferInfoIndex, NFTOfferDto>(offerInfo);
    }
    
    [Name("getExpiredNftMaxOffer")]
    public static async Task<List<ExpiredNftMaxOfferDto>> GetNftMaxOfferAsync(
        [FromServices] IReadOnlyRepository<OfferInfoIndex> nftOfferRepository,
        [FromServices] INFTInfoProvider nftInfoProvider,
        [FromServices] ILogger<OfferInfoIndex> logger,
        GetExpiredNftMaxOfferDto input)
    {
        logger.LogDebug($"[getNftMaxOffer] INPUT: chainId={input.ChainId}, expired={input.ExpireTimeGt}");
        var queryable = await nftOfferRepository.GetQueryableAsync();
        queryable = queryable.Where(index => index.ChainId == input.ChainId);
        var expiredTimeStr = DateTimeOffset.FromUnixTimeMilliseconds((long)input.ExpireTimeGt).UtcDateTime.ToString("o");
        var nowStr = DateTime.UtcNow.ToString("o");
        queryable = queryable.Where(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime) >= long.Parse(expiredTimeStr));
        queryable = queryable.Where(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime) < long.Parse(nowStr));

        var result = queryable.Skip(0).ToList();
        logger.LogDebug($"[NFTListingInfo] STEP: query chainId={input.ChainId}, count={result.Count}");
        
        List<ExpiredNftMaxOfferDto> data = new();
        foreach (var item in result)
        {
            var offerInfo = await nftInfoProvider.GetMaxOfferInfoAsync(item.BizInfoId);
            if (offerInfo == null)
            {
                ExpiredNftMaxOfferDto offerDto = new ExpiredNftMaxOfferDto()
                {
                    Key = item.BizInfoId,
                    Value = null
                };
                
                data.Add(offerDto);
            }
            else
            {
                ExpiredNftMaxOfferInfo maxOffer = new ExpiredNftMaxOfferInfo()
                {
                    ExpireTime = offerInfo.ExpireTime,
                    Prices = offerInfo.Price,
                    Id = offerInfo.Id,
                    Symbol = offerInfo.BizSymbol
                };
                
                ExpiredNftMaxOfferDto offerDto = new ExpiredNftMaxOfferDto()
                {
                    Key = item.BizInfoId,
                    Value = maxOffer
                };
                
                data.Add(offerDto);
            }
        }
      
        return data;
    }

    [Name("getNftOfferChange")]
    public static async Task<List<NFTOfferChangeDto>> GetNFTOfferChangeAsync(
        [FromServices] IReadOnlyRepository<NFTOfferChangeIndex> repository,
        [FromServices] IObjectMapper objectMapper,
        GetNFTOfferChangeDto dto)
    {
        var queryable = await repository.GetQueryableAsync();

        var mustQuery = new List<Func<QueryContainerDescriptor<NFTOfferChangeIndex>, QueryContainer>>
        {
            q => q.Range(i
                => i.Field(f => f.BlockHeight).GreaterThanOrEquals(dto.BlockHeight)),
            q => q.Term(i 
                => i.Field(f => f.ChainId).Value(dto.ChainId))
        };
        queryable = queryable.Where(f => f.BlockHeight >= dto.BlockHeight);
        queryable = queryable.Where(f => f.ChainId == dto.ChainId);
        queryable = queryable.Where(index => index.NftId != IdGenerateHelper.GetNFTInfoId(dto.ChainId, ForestIndexerConstants.TokenSimpleElf));

        var result = queryable.OrderBy(o => o.BlockHeight).ToList();
        if (result.IsNullOrEmpty())
        {
            return new List<NFTOfferChangeDto>();
        }

        return objectMapper.Map<List<NFTOfferChangeIndex>, List<NFTOfferChangeDto>>(result);
    }
}