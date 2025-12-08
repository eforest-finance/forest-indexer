using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors;
using GraphQL;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("getMaxOfferInfo")]
    public static async Task<NFTOfferDto> GetMaxOfferInfoAsync(
        [FromServices] IReadOnlyRepository<OfferInfoIndex> _nftOfferIndexRepository,
        [FromServices] IObjectMapper objectMapper,
        GetMaxOfferInfoDto dto)
    {
        var queryable = await _nftOfferIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(index => index.ExpireTime > DateTime.UtcNow);
        queryable = queryable.Where(index => index.BizInfoId == dto.NftInfoId);
        queryable = queryable.Where(index => index.RealQuantity > 0);
        var result = queryable.OrderByDescending(k => k.Price).ToList();
        var offerInfos =  result ?? new List<OfferInfoIndex>();
        if (offerInfos.IsNullOrEmpty())
        {
            return new NFTOfferDto();
        }
        //order by price desc, expireTime desc
        offerInfos = offerInfos.Where(i=>i!=null).Where(index =>
                index.ExpireTime >= DateTime.UtcNow)
            .OrderByDescending(info => info.Price)
            .ThenByDescending(info => info.ExpireTime)
            .Skip(0)
            .Take(1)
            .ToList();
        var maxOfferInfo = offerInfos.FirstOrDefault();
        // Logger.LogDebug( 
        //     "GetMaxOfferInfoAsync nftInfoId:{nftInfoId} maxOfferInfo id:{id} maxOfferPrice:{maxOfferPrice}", dto.NftInfoId,
        //     maxOfferInfo?.Id, maxOfferInfo?.Price);
        var offerInfo =  maxOfferInfo;
        
        if (offerInfo == null || offerInfo.BizSymbol.IsNullOrEmpty())
        {
            return null;
        }

        return objectMapper.Map<OfferInfoIndex, NFTOfferDto>(offerInfo);
    }


    [Name("getExpiredNftMaxOffer")]
    public static async Task<List<ExpiredNftMaxOfferDto>> GetNftMaxOfferAsync(
        [FromServices] IReadOnlyRepository<OfferInfoIndex> nftOfferRepository,
        [FromServices] INFTInfoProvider nftInfoProvider,
        GetExpiredNftMaxOfferDto input)
    {
        //Logger.LogDebug("[getNftMaxOffer] INPUT: chainId={A}, expired={B}", input.ChainId, input.ExpireTimeGt);
        var utcNow = DateTime.UtcNow;
        var queryable = await nftOfferRepository.GetQueryableAsync();
        queryable = queryable.Where(index => index.ChainId == input.ChainId);

        if (input.ExpireTimeGt != null)
        {
            var expiredTime = DateTimeHelper.FromUnixTimeSeconds((long)input.ExpireTimeGt);
            queryable = queryable.Where(index => index.ExpireTime >= expiredTime);
        }
        queryable = queryable.Where(index => index.ExpireTime < utcNow);

        var result = queryable.Skip(0).Take(ForestIndexerConstants.DefaultMaxCountNumber).ToList();
        //Logger.LogDebug("[NFTListingInfo] STEP: query chainId={A}, count={B}", input.ChainId, result.Count);
        
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

        // var mustQuery = new List<Func<QueryContainerDescriptor<NFTOfferChangeIndex>, QueryContainer>>
        // {
        //     q => q.Range(i
        //         => i.Field(f => f.BlockHeight).GreaterThanOrEquals(dto.BlockHeight)),
        //     q => q.Term(i 
        //         => i.Field(f => f.ChainId).Value(dto.ChainId))
        // };
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