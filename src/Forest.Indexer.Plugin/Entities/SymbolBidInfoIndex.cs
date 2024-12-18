using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class SymbolBidInfoIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string Symbol { get; set; }

    [Keyword] public string Bidder { get; set; }


    public long PriceAmount { get; set; }

    [Keyword] public string PriceSymbol { get; set; }


    public long BidTime { get; set; }

    [Keyword] public string AuctionId { get; set; }

    [Keyword] public string TransactionHash { get; set; }
    [Keyword]
    public string ChainId { get; set; }

    [Keyword]
    public string BlockHash { get; set; }

    public long BlockHeight { get; set; }

    [Keyword]
    public string PreviousBlockHash { get; set; }

    public bool IsDeleted { get; set; }
}