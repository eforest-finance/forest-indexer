using Forest.Indexer.Plugin.GraphQLL;
using JetBrains.Annotations;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetCollectedNFTListingDto : PagedResultRequestDto
{
    [CanBeNull]public List<string> ChainIdList { get; set; }
    [CanBeNull] public List<string> NFTInfoIdList { get; set; }
    public string Owner { get; set; }
    
    public long? ExpireTimeGt { get; set; }
}