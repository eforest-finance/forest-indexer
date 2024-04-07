using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors;
using GraphQL;
using Microsoft.Extensions.Logging;
using Nest;
using Orleans.Runtime;
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
        [FromServices] IAElfIndexerClientEntityRepository<OfferInfoIndex, LogEventInfo> nftOfferRepository,
        [FromServices] INFTInfoProvider nftInfoProvider,
        [FromServices] ILogger<OfferInfoIndex> logger,
        GetExpiredNftMaxOfferDto input)
    {
        logger.Debug($"[getNftMaxOffer] INPUT: chainId={input.ChainId}, expired={input.ExpireTimeGt}");
        
        var offerQuery = new List<Func<QueryContainerDescriptor<OfferInfoIndex>, QueryContainer>>();

        offerQuery.Add(q => q.Term(i => i.Field(index => index.ChainId).Value(input.ChainId)));
        
        var expiredTimeStr = DateTimeOffset.FromUnixTimeMilliseconds((long)input.ExpireTimeGt).UtcDateTime.ToString("o");
        var nowStr = DateTime.UtcNow.ToString("o");
            
        offerQuery.Add(q => q.TermRange(i
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime)).GreaterThanOrEquals(expiredTimeStr)));
        
        offerQuery.Add(q => q.TermRange(i
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime)).LessThan(nowStr)));
        
        QueryContainer Filter(QueryContainerDescriptor<OfferInfoIndex> f) => f.Bool(b => b.Must(offerQuery));
        
        var result = await nftOfferRepository.GetSortListAsync(Filter, skip: 0);
        logger.Debug($"[NFTListingInfo] STEP: query chainId={input.ChainId}, count={result.Item1}");
        
        List<ExpiredNftMaxOfferDto> data = new();
        foreach (var item in result.Item2)
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
    public static async Task<List<NFTOfferChangeDto>> GetNFTOfferChangfofferseAsync(
        [FromServices] IAElfIndexerClientEntityRepository<NFTOfferChangeIndex, LogEventInfo> repository,
        [FromServices] IObjectMapper objectMapper,
        GetNFTOfferChangeDto dto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTOfferChangeIndex>, QueryContainer>>
        {
            q => q.Range(i
                => i.Field(f => f.BlockHeight).GreaterThanOrEquals(dto.BlockHeight)),
            q => q.Term(i 
                => i.Field(f => f.ChainId).Value(dto.ChainId))
        };
        var mustNotQuery = new List<Func<QueryContainerDescriptor<NFTOfferChangeIndex>, QueryContainer>>();
        mustNotQuery.Add(q => q.Term(i => i.Field(index => index.NftId).Value(IdGenerateHelper.GetNFTInfoId(dto.ChainId, ForestIndexerConstants.TokenSimpleElf))));
        
        QueryContainer Filter(QueryContainerDescriptor<NFTOfferChangeIndex> f) => f.Bool(b => b.Must(mustQuery).MustNot(mustNotQuery));
        var result = await repository.GetListAsync(Filter, sortExp: o => o.BlockHeight);
        
        return objectMapper.Map<List<NFTOfferChangeIndex>, List<NFTOfferChangeDto>>(result.Item2);
    }
}