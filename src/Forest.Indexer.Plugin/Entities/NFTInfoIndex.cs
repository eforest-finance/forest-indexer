using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class NFTInfoIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public HashSet<string> IssueManagerSet { get; set; }

    [Keyword] public string RandomIssueManager { get; set; }
    
    [Keyword] public string CreatorAddress { get; set; }
    [Keyword] public string ImageUrl { get; set; }
    
    [Keyword] public string CollectionSymbol { get; set; }
    [Keyword] public string CollectionName { get; set; }
    [Keyword] public string CollectionId { get; set; }
    
    public bool OtherOwnerListingFlag { get; set; }
    [Keyword] public string ListingId { get; set; }
    [Keyword] public string ListingAddress { get; set; }
    public decimal ListingPrice { get; set; }
    public long ListingQuantity { get; set; }
    public DateTime? ListingEndTime { get; set; }
    public DateTime? LatestListingTime { get; set; }
    public DateTime? LatestOfferTime { get; set; }
    public decimal LatestDealPrice { get; set; }
    public DateTime LatestDealTime { get; set; }
    public decimal OfferPrice { get; set; }
    public long OfferQuantity { get; set; }
    public DateTime OfferExpireTime { get; set; }
    public TokenInfoIndex OfferToken { get; set; }
    public TokenInfoIndex ListingToken { get; set; }
    public TokenInfoIndex LatestDealToken { get; set; }
    public TokenInfoIndex WhitelistPriceToken { get; set; }

    [Keyword] public string PreviewImage { get; set; }
    [Keyword] public string File { get; set; }
    [Keyword] public string FileExtension { get; set; }
    [Keyword] public string Description { get; set; }
    public bool IsOfficial { get; set; }
    
    public bool HasListingFlag { get; set; }
    public decimal MinListingPrice { get; set; }
    
    public DateTime? MinListingExpireTime { get; set; }

    [Keyword] public string MinListingId { get; set; }
    
    public bool HasOfferFlag { get; set; }
    
    public decimal MaxOfferPrice { get; set; }
    
    public DateTime? MaxOfferExpireTime { get; set; }
    
    [Keyword] public string MaxOfferId { get; set; }
    [Keyword]
    public string ChainId { get; set; }

    [Keyword]
    public string BlockHash { get; set; }

    public long BlockHeight { get; set; }

    [Keyword]
    public string PreviousBlockHash { get; set; }

    public bool IsDeleted { get; set; }
    [Keyword] public override string Id { get; set; }
    
    [Keyword] public string Symbol { get; set; }

    /// <summary>
    /// token contract address
    /// </summary>
    [Keyword] public string TokenContractAddress { get; set; }
    
    public int Decimals { get; set; }
    
    public long Supply { get; set; }
    
    public long TotalSupply { get; set; }

    [Keyword] public string TokenName { get; set; }

    [Keyword] public string Owner { get; set; }
    [Keyword] public string Issuer { get; set; }

    public bool IsBurnable { get; set; }

    public int IssueChainId { get; set; }
    
    public long Issued { get; set; }

    public DateTime CreateTime { get; set; }
    

    public List<ExternalInfoDictionary> ExternalInfoDictionary { get; set; }
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