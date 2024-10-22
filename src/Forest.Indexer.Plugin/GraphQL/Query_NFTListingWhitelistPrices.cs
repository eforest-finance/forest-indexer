/*using AElf.Contracts.Whitelist;
using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.ObjectMapping;*/

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    /*[Obsolete("todo V2, unuse")]
    [Name("nftListingWhitelistPrices")]
    public static async Task<NFTListingWhitelistPricePageResultDto> NftListingWhitelistPrices(
        [FromServices] IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> nftRepo,
        [FromServices] IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo> listingRepo,
        [FromServices] IAElfIndexerClientEntityRepository<WhiteListExtraInfoIndex, LogEventInfo> whiteListExtRepo,
        [FromServices] IAElfIndexerClientEntityRepository<TagInfoIndex, LogEventInfo> tagInfoRepo,
        [FromServices] IObjectMapper objectMapper,
        [FromServices] ILogger<NFTListingInfoIndex> logger,
        GetNFTListingWhitelistPricesDto dto)
    {
        try
        {
            if (dto.NftInfoIds.IsNullOrEmpty()) 
                return new NFTListingWhitelistPricePageResultDto(0, new List<NFTListingWhitelistPriceDto>());
            
            const int maxCount = 10;
            if (dto.NftInfoIds.Count > maxCount) 
                throw new UserFriendlyException("NFTInfoIds too many, The maximum items is " + maxCount);
            
            var nowDouble = DateTime.Now.Subtract(new DateTime(0)).TotalMilliseconds;
            logger.Debug("[nftListingWhitelistPrices] START: nowDouble={nowDouble}, address={address}, NftInfoIds={NftInfoIds}", 
                nowDouble, dto.Address, string.Join(",", dto.NftInfoIds));

            // async query LATEST listingInfo data in ExpireTime
            List<Task<Tuple<long, List<NFTListingInfoIndex>>>> listings = new();
            foreach (var dtoNftInfoId in dto.NftInfoIds.Where(nftId => !nftId.IsNullOrEmpty()).Distinct().ToList())
            {
                var mustQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>
                {
                    q => q.Term(i =>
                        i.Field(f => f.NftInfoId).Value(dtoNftInfoId))
                };

                QueryContainer ListingFilter(QueryContainerDescriptor<NFTListingInfoIndex> f) =>
                    f.Bool(b => b.Must(mustQuery));

                // only one entry or it is empty
                listings.Add(listingRepo.GetListAsync(ListingFilter,
                    sortType: SortOrder.Descending,
                    sortExp: k => k.BlockHeight,
                    skip: 0, limit: 1));
            }

            // await and get
            List<NFTListingInfoIndex> nftListingInfoIndices = new();
            foreach (var listingTask in listings)
            {
                var idx = (await listingTask).Item2?.FirstOrDefault();
                if (idx != null) nftListingInfoIndices.Add(idx);
            }

            if (nftListingInfoIndices.IsNullOrEmpty())
                return new NFTListingWhitelistPricePageResultDto(0, new List<NFTListingWhitelistPriceDto>());

            var whiteListIdList = nftListingInfoIndices
                .Where(i => !i.WhitelistId.IsNullOrEmpty())
                .Select(i => i.WhitelistId)
                .Distinct().ToList();
            
            var purchaseTokenDict = nftListingInfoIndices
                .Select(listing => listing.PurchaseToken)
                .GroupBy(listing => listing.Symbol)
                .ToDictionary(group => group.Key, group => group.First());

            logger.Debug("[nftListingWhitelistPrices] STEP: whiteListIdList={whiteListIds}", string.Join(",", whiteListIdList));

            
            // query whiteListExt by id & address
            var extInfoIndexList = new List<WhiteListExtraInfoIndex>();
            // whitelistId -> WhiteListExtra
            var extInfoDict = new Dictionary<string, List<WhiteListExtraInfoIndex>>();
            if (!dto.Address.IsNullOrEmpty())
            {
                var extInfoQuery = new List<Func<QueryContainerDescriptor<WhiteListExtraInfoIndex>, QueryContainer>>();
                extInfoQuery.Add(q => q.Terms(i =>
                    i.Field(f => f.WhitelistInfoId).Terms(whiteListIdList)));
                extInfoQuery.Add(q => q.Term(i =>
                    i.Field(f => f.Address).Value(dto.Address)));

                QueryContainer ExtInfoFilter(QueryContainerDescriptor<WhiteListExtraInfoIndex> f) =>
                    f.Bool(b => b.Must(extInfoQuery));

                var extInfoTuple = await whiteListExtRepo.GetListAsync(ExtInfoFilter);
                logger.Debug("[nftListingWhitelistPrices] STEP: extInfoTuple={extTupleCount}-{extTupleItems}", 
                    extInfoTuple.Item1, JsonConvert.SerializeObject(extInfoTuple));
                extInfoIndexList = extInfoTuple.Item2.IsNullOrEmpty() ? extInfoIndexList : extInfoTuple.Item2;
                extInfoDict = extInfoIndexList.GroupBy(i => i.WhitelistInfoId)
                    .ToDictionary(group => group.Key, group => group.ToList());
                logger.Debug("[nftListingWhitelistPrices] STEP: extInfoDict={extInfos}", string.Join(",", extInfoDict.Keys));
            }
            
            // query TagInfo
            var tagInfoIds = extInfoIndexList?
                .Where(i => !i.TagInfoId.IsNullOrEmpty())
                .Select(i => i.TagInfoId).Distinct().ToList() ?? new List<string>();
            // tagInfoId -> PriceTag
            var priceDict = new Dictionary<string, PriceTag>();
            if (!tagInfoIds.IsNullOrEmpty())
            {
                logger.Debug("[nftListingWhitelistPrices] STEP: tagInfoIds={tagInfoIds}", string.Join(",", tagInfoIds));

                var tagInfoQuery = new List<Func<QueryContainerDescriptor<TagInfoIndex>, QueryContainer>>();
                tagInfoQuery.Add(q => q.Terms(i =>
                    i.Field(f => f.WhitelistInfoId).Terms(whiteListIdList)));

                QueryContainer TagInfoFilter(QueryContainerDescriptor<TagInfoIndex> f) =>
                    f.Bool(b => b.Must(tagInfoQuery));

                var tagInfoTuple = await tagInfoRepo.GetListAsync(TagInfoFilter);
                logger.Debug("[nftListingWhitelistPrices] STEP: tgaInfoTuple={tagInfoCount}-{tagInfoItem}", 
                    tagInfoTuple.Item1, string.Join(",", tagInfoTuple.Item2.Select(i => i.Id).ToList()));

                // convert resp
                priceDict = tagInfoTuple.Item1 == 0 ? priceDict : tagInfoTuple.Item2?
                    .GroupBy(tag => tag.TagHash)
                    // use the min price when duplicate
                    .ToDictionary(
                        tag => tag.Key,
                        tag => tag
                            .OrderBy(t => t.DecodeInfo<PriceTag>().Amount)
                            .First().DecodeInfo<PriceTag>());
                logger.Debug("[nftListingWhitelistPrices] STEP: priceDict={priceDict}", string.Join(",", priceDict.Keys));

            }
            
            var listingDto =
                objectMapper.Map<List<NFTListingInfoIndex>, List<NFTListingWhitelistPriceDto>>(nftListingInfoIndices);
            foreach (var nftListingWhitelistPriceDto in listingDto)
            {
                if (nftListingWhitelistPriceDto.WhitelistId.IsNullOrEmpty() 
                    || !extInfoDict.ContainsKey(nftListingWhitelistPriceDto.WhitelistId))
                    continue;
                var extInfoList = extInfoDict.GetValueOrDefault(nftListingWhitelistPriceDto.WhitelistId);
                if (extInfoList.IsNullOrEmpty())
                    continue;
                var extInfoTagIds = extInfoList.Select(i => i.TagInfoId).Distinct().ToList();
                logger.Debug("[nftListingWhitelistPrices] STEP: BUILD, listId={whiteListId}, extInfoTagIds={tagIds}", 
                    JsonConvert.SerializeObject(priceDict), string.Join(",", extInfoTagIds));
                var priceTag = priceDict.Where(p => extInfoTagIds.Contains(p.Key))
                    .Select(p => p.Value)
                    .OrderBy(k => k.Amount)
                    .FirstOrDefault(new PriceTag());
                var purchaseToken = priceTag == null ? new TokenInfoIndex() : purchaseTokenDict.GetValueOrDefault(priceTag.Symbol);
                nftListingWhitelistPriceDto.WhiteListPrice = (priceTag?.Amount).ToPriceDecimal(purchaseToken.Decimals);
            }

            return new NFTListingWhitelistPricePageResultDto(listingDto.Count, listingDto);
        }
        catch (Exception e)
        {
            logger.LogError(e, "nftListingWhitelistPrices ERROR");
            throw;
        }
    }*/

}