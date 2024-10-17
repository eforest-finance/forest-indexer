using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class OfferInfoIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }
    
    [Keyword] public string OfferFrom { get; set; }
    
    [Keyword] public string OfferTo { get; set; }
    
    public decimal Price { get; set; }
    
    public long Quantity { get; set; }

    public long RealQuantity { get; set; }
    
    public DateTime ExpireTime { get; set; }
    [Keyword] public string BizInfoId { get; set; }
    [Keyword] public string BizSymbol { get; set; }
    public TokenInfoIndex PurchaseToken { get; set; }
    public DateTime CreateTime { get; set; }

}