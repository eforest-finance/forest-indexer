

using JetBrains.Annotations;

namespace Forest.Indexer.Plugin.GraphQL;

public class NFTInfoDto
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public string Issuer { get; set; }
    public string ProxyIssuerAddress { get; set; }
    public string Owner { get; set; }
    public long OwnerCount { get; set; }
    public int IssueChainId { get; set; }
    public string CreatorAddress { get; set; }
    public string ImageUrl { get; set; }
    public string TokenName { get; set; }
    public long TotalSupply { get; set; }
    public string? WhitelistId { get; set; }

    public string CollectionSymbol { get; set; }
    public string CollectionName { get; set; }
    public string CollectionId { get; set; }

    public bool OtherOwnerListingFlag { get; set; }
    public string ListingId { get; set; }
    public string ListingAddress { get; set; }
    public decimal ListingPrice { get; set; }
    public long ListingQuantity { get; set; }
    public DateTime? ListingEndTime { get; set; }
    public DateTime? LatestListingTime { get; set; }

    public decimal LatestDealPrice { get; set; }
    public DateTime LatestDealTime { get; set; }

    public TokenInfoDto ListingToken { get; set; }
    public TokenInfoDto LatestDealToken { get; set; }

    public string PreviewImage { get; set; }
    public string File { get; set; }
    public string FileExtension { get; set; }
    public string Description { get; set; }
    public bool IsOfficial { get; set; }
    public List<ExternalInfoDictionaryDto> ExternalInfoDictionary { get; set; }
    public long Supply { get; set; }
    public long Issued { get; set; }

    // seed only
    public string SeedOwnedSymbol { get; set; }
    public string SeedType { get; set; }
    public string TokenType { get; set; }
    public long? RegisterTimeSecond { get; set; }
    public long? SeedExpTimeSecond { get; set; }
    public bool HasListingFlag { get; set; }
    public decimal MinListingPrice { get; set; }

}