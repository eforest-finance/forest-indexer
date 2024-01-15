namespace Forest.Indexer.Plugin.GraphQL;

public class GetExpiredListingNftDto
{
    public string ChainId { get; set; }
    
    public long ExpireTimeGt { get; set; }
}