using Forest.Indexer.Plugin.GraphQLL;
using JetBrains.Annotations;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetCollectedNFTListingDto : PagedResultRequestDto
{
    public List<string>? ChainIdList { get; set; } = new List<string>();
    public List<string>? NFTInfoIdList { get; set; } = new List<string>();
    public string Owner { get; set; }
    
    public long? ExpireTimeGt { get; set; }
}