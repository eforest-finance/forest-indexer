using AeFinder.Sdk.Entities;
using Forest.Indexer.Plugin.enums;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class TsmSeedSymbolIndex: AeFinderEntity, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }
    
    [Keyword] public string Symbol { get; set; }
    public int SymbolLength => Symbol.Length;

    [Keyword] public string SeedSymbol { get; set; }

    [Keyword] public string SeedName { get; set; }

    public SeedStatus Status { get; set; }

    public long RegisterTime { get; set; }

    public long ExpireTime { get; set; }

    public TokenType TokenType { get; set; }

    public SeedType SeedType { get; set; }
    
    public AuctionType AuctionType { get; set; }

    [Keyword] public string Owner { get; set; }
    public TokenPriceInfo TokenPrice { get; set; }

    [Keyword] public string SeedImage { get; set; }
    
    public bool IsBurned { get; set; }
    
    public int AuctionStatus { get; set; }
    
    public int BidsCount { get; set; }
    
    public int BiddersCount { get; set; }
    
    public long AuctionEndTime { get; set; }
    
    public TokenPriceInfo TopBidPrice { get; set; }
}