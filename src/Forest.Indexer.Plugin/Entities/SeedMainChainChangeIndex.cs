using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class SeedMainChainChangeIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string Symbol { get; set; }
    
    [Keyword] public string TransactionId { get; set; }

    public DateTime UpdateTime { get; set; }
}