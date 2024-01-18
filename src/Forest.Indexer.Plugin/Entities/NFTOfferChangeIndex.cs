using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class NFTOfferChangeIndex: AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public string NftId { get; set; }
    
    public EventType EventType { get; set; }

    public DateTime CreateTime { get; set; }
}

public enum EventType
{
    Add,
    Modify,
    Cancel,
    Remove,
    Other,
}