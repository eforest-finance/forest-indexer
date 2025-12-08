namespace Forest.Indexer.Plugin.GraphQL;

public class SymbolBidInfoDto
{
    public string Id { get; set; }
  
    public string Symbol { get; set; }
  
    public string Bidder { get; set; }

    
    public long BidTime { get; set; }
    
    public string TransactionHash { get; set; }
    
    public long BlockHeight { get; set; }
    
    
    public long PriceAmount { get; set; }

    public string PriceSymbol { get; set; }
    
    public string AuctionId { get; set; }
}