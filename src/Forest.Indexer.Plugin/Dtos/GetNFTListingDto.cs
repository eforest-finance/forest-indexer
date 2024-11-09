using Forest.Indexer.Plugin.GraphQLL;
using JetBrains.Annotations;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetNFTListingDto : PagedResultRequestDto
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public string? Owner { get; set; }
    public string? Address { get; set; }
    
    public string? ExcludedAddress { get; set; }
    
    public long? ExpireTimeGt { get; set; }
    public long? BlockHeight { get; set; }

}