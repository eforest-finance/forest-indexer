namespace Forest.Indexer.Plugin.GraphQL;

public class GetNFTCollectionPriceChangeDto
{
    public int SkipCount { get; set; }
    
    public string ChainId { get; set; }
    
    public long BlockHeight { get; set; }
}