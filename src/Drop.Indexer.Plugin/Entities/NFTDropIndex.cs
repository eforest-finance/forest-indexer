using AeFinder.Sdk.Entities;
using Nest;
using Forest.Contracts.Drop;

namespace Drop.Indexer.Plugin.Entities;

public class NFTDropIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }
    
    [Keyword] public string CollectionId { get; set; }
    
    public DateTime StartTime { get; set; }
    
    public DateTime ExpireTime { get; set; }
    
    public long ClaimMax { get; set; }
    
    public decimal ClaimPrice { get; set; }
    
    [Keyword]
    public string ClaimSymbol { get; set; }
    
    public DropDetailList DetailList { get; set; }
    
    public long MaxIndex { get; set; }
    
    public long CurrentIndex { get; set; }
    
    public long TotalAmount { get; set; }
    
    public long ClaimAmount { get; set; }
    
    [Keyword] public string Owner { get; set; }
    
    public bool IsBurn { get; set; }
    
    public DropState State { get; set; }
    
    public DateTime CreateTime { get; set; }
    
    public DateTime UpdateTime { get; set; }
    
}

