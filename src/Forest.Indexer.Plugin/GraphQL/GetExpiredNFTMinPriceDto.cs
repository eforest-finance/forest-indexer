using JetBrains.Annotations;
using Volo.Abp.Application.Dtos;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetExpiredNFTMinPriceDto 
{
    public string ChainId { get; set; }
    public long? ExpireTimeGt { get; set; }
}