using Forest.Indexer.Plugin.GraphQLL;
using JetBrains.Annotations;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetNFTInfosDto : PagedResultRequestDto
{
    public string? NftCollectionId { get; set; }
    public string? Sorting { get; set; }
    public double? PriceLow { get; set; }
    public double? PriceHigh { get; set; }
    public int Status { get; set; }
    public string? Address { get; set; }
    public string? IssueAddress { get; set; }

    public List<string>? NFTInfoIds { get; set; } = new List<string>();
    
    public bool IsSeed { get; set; }
}