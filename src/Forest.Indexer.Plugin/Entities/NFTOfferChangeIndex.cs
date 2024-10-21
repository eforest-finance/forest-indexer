using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class NFTOfferChangeIndex: AeFinderEntity, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string NftId { get; set; }
    
    public EventType EventType { get; set; }

    public DateTime CreateTime { get; set; }
    [Keyword]
    public string ChainId { get; set; }

    [Keyword]
    public string BlockHash { get; set; }

    public long BlockHeight { get; set; }

    [Keyword]
    public string PreviousBlockHash { get; set; }

    public bool IsDeleted { get; set; }
}

public enum EventType
{
    Add,
    Modify,
    Cancel,
    Remove,
    Other,
}