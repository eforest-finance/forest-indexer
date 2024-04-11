namespace Forest.Indexer.Plugin.GraphQL;

public class GetNFTCollectionTradeDto
{ 
    public string ChainId { get; set; }
    
    public string Symbol { get; set; }
    
    public decimal FloorPrice { get; set; }

}