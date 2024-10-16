using AeFinder.Sdk.Entities;
using Forest.Indexer.Plugin.enums;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class SeedSymbolIndex: AeFinderEntity, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }
    [Wildcard] public string SeedOwnedSymbol { get; set; }
    
    public long SeedExpTimeSecond { get; set; }

    public DateTime SeedExpTime { get; set; }

    public long RegisterTimeSecond { get; set; }

    public DateTime RegisterTime { get; set; }

    [Keyword] public string IssuerTo { get; set; }
    
    public bool IsDeleteFlag { get; set; }
    
    public TokenType TokenType { get; set; }

    public SeedType SeedType { get; set; }
    
    public decimal Price { get; set; }
    
    [Keyword] public string PriceSymbol { get; set; }
    
    public decimal BeginAuctionPrice { get; set; }
    
    public decimal AuctionPrice { get; set; }
    
    public string AuctionPriceSymbol { get; set; }
    
    public DateTime AuctionDateTime { get; set; }
    
    public bool OtherOwnerListingFlag { get; set; }
    [Keyword] public string ListingId { get; set; }
    [Keyword] public string ListingAddress { get; set; }
    public decimal ListingPrice { get; set; }
    public long ListingQuantity { get; set; }
    public DateTime? ListingEndTime { get; set; }
    public DateTime? LatestListingTime { get; set; }
    
    public decimal OfferPrice { get; set; }
    
    public long OfferQuantity { get; set; }
    
    public DateTime OfferExpireTime { get; set; }
    
    public DateTime? LatestOfferTime { get; set; }
    
    public TokenInfoIndex OfferToken { get; set; }
    public TokenInfoIndex ListingToken { get; set; }
    public TokenInfoIndex LatestDealToken { get; set; }

    public SeedStatus? SeedStatus { get; set; }
    
    public bool HasOfferFlag { get; set; }
    public bool HasListingFlag { get; set; }
    public decimal MinListingPrice { get; set; }
    
    public DateTime? MinListingExpireTime { get; set; }

    [Keyword] public string MinListingId { get; set; }
    
    public bool HasAuctionFlag { get; set; }
    public decimal MaxAuctionPrice { get; set; }
    
    public decimal MaxOfferPrice { get; set; }
     
    public DateTime? MaxOfferExpireTime { get; set; }
    
    [Keyword] public string MaxOfferId { get; set; }
    
    [Keyword] public string SeedImage { get; set; }
    
    public void OfMinNftListingInfo(NFTListingInfoIndex minNftListing)
    {
        HasListingFlag = minNftListing != null;
        MinListingPrice = minNftListing?.Prices ?? 0;
        MinListingExpireTime = minNftListing?.ExpireTime;
        MinListingId = minNftListing?.Id;
    }
    
    public void OfMaxOfferInfo(OfferInfoIndex maxOfferInfo)
    {
        HasOfferFlag = maxOfferInfo != null;
        MaxOfferPrice = maxOfferInfo?.Price ?? 0;
        MaxOfferExpireTime = maxOfferInfo?.ExpireTime;
        MaxOfferId = maxOfferInfo?.Id;
    }
}