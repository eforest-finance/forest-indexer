using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class SeedPriceIndex: AeFinderEntity, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string TokenType { get; set; }
    public int SymbolLength { get; set; }
    public TokenPriceInfo TokenPrice { get; set; }
    [Keyword]
    public string ChainId { get; set; }

    [Keyword]
    public string BlockHash { get; set; }

    public long BlockHeight { get; set; }

    [Keyword]
    public string PreviousBlockHash { get; set; }

    public bool IsDeleted { get; set; }
}