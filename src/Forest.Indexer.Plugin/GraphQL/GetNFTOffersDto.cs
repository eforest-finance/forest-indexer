using Forest.Indexer.Plugin.GraphQLL;
using JetBrains.Annotations;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetNFTOffersDto : PagedResultRequestDto
{
    public string? ChainId { get; set; }
    public string? NFTInfoId { get; set; }
    public List<string>? NFTInfoIdList { get; set; } = new List<string>();
    public List<string>? ChainIdList { get; set; } = new List<string>();
    public string? Symbol { get; set; }
    public string? OfferTo { get; set; }
    public string? OfferFrom { get; set; }
    public string? OfferNotFrom { get; set; }
    
    public long? ExpireTimeGt { get; set; }
}