namespace Forest.Indexer.Plugin.GraphQL;

public class SeedInfoProfileDto
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public string TokenContractAddress { get; set; }
    public int Decimals { get; set; }
    public long Supply { get; set; }
    public long TotalSupply { get; set; }
    public string TokenName { get; set; }
    public string Issuer { get; set; }
    public bool IsBurnable { get; set; }
    public int IssueChainId { get; set; }
    public long Issued { get; set; }
    public DateTime CreateTime { get; set; }
    public string SeedOwnedSymbol { get; set; }
    public DateTime SeedExpTime { get; set; }
    public string ListingId { get; set; }
    public string ListingAddress { get; set; }
    public decimal ListingPrice { get; set; }
    public long ListingQuantity { get; set; }
    public DateTime? ListingEndTime { get; set; }
    public DateTime? LatestListingTime { get; set; }
    public string SeedImage { get; set; }
    public string Owner { get; set; }
    public bool OtherOwnerListingFlag { get; set; }
    public string SeedType { get; set; }
    public string TokenType { get; set; }
    public long? RegisterTimeSecond { get; set; }
    public long? SeedExpTimeSecond { get; set; }
    public bool HasOfferFlag { get; set; }
    public bool HasListingFlag { get; set; }
    public decimal MinListingPrice { get; set; }
    public decimal MaxAuctionPrice { get; set; }
    public decimal MaxOfferPrice { get; set; }
    public List<ExternalInfoDictionaryDto> ExternalInfoDictionary { get; set; }
    public TokenInfoDto ListingToken { get; set; }
    public TokenInfoDto LatestDealToken { get; set; }
}