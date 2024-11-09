using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class NFTListingInfoIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }
    public TokenInfoIndex PurchaseToken { get; set; }
    public decimal Prices { get; set; }
    public long Quantity { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime PublicTime { get; set; }
    public DateTime ExpireTime { get; set; }
    public long DurationHours { get; set; }
    [Keyword] public string NftInfoId { get; set; }
    [Keyword] public string WhitelistId { get; set; }
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string Owner { get; set; }

    [Keyword] public string OfferFrom { get; set; }

    // listed NFT changed params
    [Keyword] public string PreviousDuration { get; set; }
    [Keyword] public string CollectionSymbol { get; set; }
    [Keyword]
    public string ChainId { get; set; }

    [Keyword]
    public string BlockHash { get; set; }

    public long BlockHeight { get; set; }

    [Keyword]
    public string PreviousBlockHash { get; set; }

    public bool IsDeleted { get; set; }
    public long RealQuantity { get; set; }
}