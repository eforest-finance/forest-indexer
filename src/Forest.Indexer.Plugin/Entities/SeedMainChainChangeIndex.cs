using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class SeedMainChainChangeIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string Symbol { get; set; }
    
    [Keyword] public string TransactionId { get; set; }

    public DateTime UpdateTime { get; set; }
    
    [Keyword]
    public string ChainId { get; set; }

    [Keyword]
    public string BlockHash { get; set; }

    public long BlockHeight { get; set; }

    [Keyword]
    public string PreviousBlockHash { get; set; }

    public bool IsDeleted { get; set; }
}