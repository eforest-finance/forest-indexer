namespace Forest.Indexer.Plugin.GraphQL;

public class GetNFTCollectionPriceDto
{ 
    public string ChainId { get; set; }
    
    public string Symbol { get; set; }
    
    public decimal FloorPrice { get; set; }

}