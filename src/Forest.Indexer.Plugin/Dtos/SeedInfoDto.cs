using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;

namespace Forest.Indexer.Plugin.GraphQL;

public class SeedInfoDto
{
    public string? Id { get; set; }
    public string ChainId { get; set; }
    public string Symbol { get; set; }

    public string? SeedSymbol { get; set; }

    public string SeedName { get; set; }

    public SeedStatus Status { get; set; }

    public long RegisterTime { get; set; }

    public long ExpireTime { get; set; }

    public string TokenType { get; set; }

    public SeedType SeedType { get; set; }

    public AuctionType AuctionType { get; set; }

    public string? Owner { get; set; }

    public string Memo { get; set; }

    public string BlockHeight { get; set; }
    public TokenPriceInfo? TokenPrice { get; set; }

    public TokenPriceInfo? TopBidPrice { get; set; }

    public long AuctionEndTime { get; set; }

    public SeedStatus? NotSupportSeedStatus { get; set; }
    
    public string? SeedImage { get; set; }
    public string? CurrentChainId { get; set; }
    
    public bool IsBurned { get; set; }
    
    public int AuctionStatus { get; set; }
    
    public int BidsCount { get; set; }
    
    public int BiddersCount { get; set; }
}    