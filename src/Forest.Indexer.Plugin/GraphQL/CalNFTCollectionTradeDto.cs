namespace Forest.Indexer.Plugin.GraphQL;

public class CalNFTCollectionTradeDto
{
    public string ChainId { get; set; }
    public string CollectionSymbol { get; set; }
    
    public string CollectionId { get; set; }
    public long BeginUtcStamp { get; set; }
    public long EndUtcStamp { get; set; }
}