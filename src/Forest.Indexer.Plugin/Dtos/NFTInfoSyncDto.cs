
namespace Forest.Indexer.Plugin.GraphQL;

public class NFTInfoSyncDto
{
    public string Id { get; set; }

    public string ChainId { get; set; }

    public string BlockHash { get; set; }

    public long BlockHeight { get; set; }

    public string Symbol { get; set; }

    public string TokenContractAddress { get; set; }

    public int Decimals { get; set; }

    public long Supply { get; set; }

    public long TotalSupply { get; set; }

    public string TokenName { get; set; }

    public string Owner { get; set; }
    public string Issuer { get; set; }

    public bool IsBurnable { get; set; }

    public int IssueChainId { get; set; }

    public long Issued { get; set; }

    public DateTime CreateTime { get; set; }

    public List<ExternalInfoDictionaryDto> ExternalInfoDictionary { get; set; }


    public HashSet<string> IssueManagerSet { get; set; }

    public string? RandomIssueManager { get; set; }

    public string CreatorAddress { get; set; }
    public string ImageUrl { get; set; }

    public string CollectionSymbol { get; set; }
    public string CollectionName { get; set; }
    public string CollectionId { get; set; }

    public bool OtherOwnerListingFlag { get; set; }
    public string? ListingId { get; set; }
    public string? ListingAddress { get; set; }
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
    public TokenInfoDto? OfferToken { get; set; }
    public TokenInfoDto? ListingToken { get; set; }
    public TokenInfoDto? LatestDealToken { get; set; }
    public TokenInfoDto? WhitelistPriceToken { get; set; }

    public string? PreviewImage { get; set; }
    public string? File { get; set; }
    public string? FileExtension { get; set; }
    public string? Description { get; set; }
    public bool IsOfficial { get; set; }

    public bool HasListingFlag { get; set; }
    public decimal MinListingPrice { get; set; }

    public DateTime? MinListingExpireTime { get; set; }

    public string? MinListingId { get; set; }

    public bool HasOfferFlag { get; set; }

    public decimal MaxOfferPrice { get; set; }

    public DateTime? MaxOfferExpireTime { get; set; }

    public string? MaxOfferId { get; set; }
}