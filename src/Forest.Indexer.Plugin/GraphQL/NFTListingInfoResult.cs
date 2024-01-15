namespace Forest.Indexer.Plugin.GraphQL;

public class NFTListingInfoResult
{
    public string ChainId { get; set; }
    
    public string Symbol { get; set; }
    
    public string NftInfoId { get; set; }

    public string CollectionSymbol { get; set; }
    
    public decimal Prices { get; set; }
    
    public DateTime ExpireTime { get; set; }
}