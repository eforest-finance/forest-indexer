
namespace Forest.Indexer.Plugin.GraphQL;

public class GetExpiredNftMaxOfferDto
{
    public string ChainId { get; set; }
    public long ExpireTimeGt { get; set; }
}