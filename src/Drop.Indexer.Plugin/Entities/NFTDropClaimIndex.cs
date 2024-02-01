using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Nest;

namespace Drop.Indexer.Plugin.Entities;

public class NFTDropClaimIndex : AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    
    [Keyword] public string Address { get; set; }
    
    [Keyword] public string DropId { get; set; }
    
    public long ClaimLimit { get; set; }
    
    public long ClaimAmount { get; set; }
    
    public DateTime CreateTime { get; set; }
    
    public DateTime UpdateTime { get; set; }
}