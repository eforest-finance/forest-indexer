namespace Forest.Indexer.Plugin.GraphQL;

public class GetTreePointsRecordDto
{
    public long MinTimestamp { get; set; }
    
    public long MaxTimestamp { get; set; }
    
    public List<string> ? Addresses { get; set; }

}