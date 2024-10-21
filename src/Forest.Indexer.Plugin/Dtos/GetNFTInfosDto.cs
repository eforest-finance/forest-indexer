using Forest.Indexer.Plugin.GraphQLL;
using JetBrains.Annotations;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetNFTInfosDto : PagedResultRequestDto
{
    [CanBeNull] public string NftCollectionId { get; set; }
    public string Sorting { get; set; }
    public double? PriceLow { get; set; }
    public double? PriceHigh { get; set; }
    public int Status { get; set; }
    [CanBeNull] public string Address { get; set; }
    [CanBeNull] public string IssueAddress { get; set; }

    [CanBeNull] public List<string> NFTInfoIds { get; set; }
    
    public bool IsSeed { get; set; }
}