using JetBrains.Annotations;
using Volo.Abp.Application.Dtos;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetNFTListingDto : PagedResultRequestDto
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public string Owner { get; set; }
    [CanBeNull] public string Address { get; set; }
    
    [CanBeNull] public string ExcludedAddress { get; set; }
    
    public long? ExpireTimeGt { get; set; }
    public long? BlockHeight { get; set; }

}