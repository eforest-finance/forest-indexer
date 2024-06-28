using JetBrains.Annotations;
using Volo.Abp.Application.Dtos;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetNFTOffersDto : PagedResultRequestDto
{
    [CanBeNull] public string ChainId { get; set; }
    [CanBeNull] public string NFTInfoId { get; set; }
    [CanBeNull] public List<string> NFTInfoIdList { get; set; }
    [CanBeNull] public List<string> ChainIdList { get; set; }
    [CanBeNull] public string Symbol { get; set; }
    [CanBeNull] public string OfferTo { get; set; }
    [CanBeNull] public string OfferFrom { get; set; }
    [CanBeNull] public string OfferNotFrom { get; set; }
    
    public long? ExpireTimeGt { get; set; }
}