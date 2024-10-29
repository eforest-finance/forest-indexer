namespace Forest.Indexer.Plugin.GraphQL;

public class NFTOfferDto
{
    public string Id { get; set; }
    
    public string ChainId { get; set; }
    
    public string From { get; set; }
    
    public string To { get; set; }
    
    public decimal Price { get; set; }
    
    public long Quantity { get; set; }
    
    public string BizInfoId { get; set; }
    
    public string BizSymbol { get; set; }
    
    public DateTime ExpireTime { get; set; }

    public TokenInfoDto PurchaseToken { get; set; }
    public long RealQuantity { get; set; }

}