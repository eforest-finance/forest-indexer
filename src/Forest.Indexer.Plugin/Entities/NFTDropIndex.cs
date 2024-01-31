using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Nest;
using Forest.Contracts.Drop;

namespace Forest.Indexer.Plugin.Entities;

public class NFTDropIndex : AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    
    [Keyword] public string CollectionSymbol { get; set; }
    
    public DateTime StartTime { get; set; }
    
    public DateTime ExpireTime { get; set; }
    
    public long ClaimMax { get; set; }
    
    public decimal ClaimPrice { get; set; }
    
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

