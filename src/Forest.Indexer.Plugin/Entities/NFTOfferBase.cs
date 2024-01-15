using AElfIndexer.Client;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class NFTOfferBase: AElfIndexerClientEntity<string>
{
    [Keyword] public override string Id { get; set; }
    
    [Keyword] public string OfferFrom { get; set; }
    
    [Keyword] public string OfferTo { get; set; }
    
    public decimal Price { get; set; }
    
    public long Quantity { get; set; }

    public long RealQuantity { get; set; }
    
    public DateTime ExpireTime { get; set; }
}