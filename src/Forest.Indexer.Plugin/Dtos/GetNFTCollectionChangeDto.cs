namespace Forest.Indexer.Plugin.GraphQL;

public class GetNFTCollectionChangeDto
{
    public int SkipCount { get; set; }
    
    public string ChainId { get; set; }
    
    public long BlockHeight { get; set; }
}