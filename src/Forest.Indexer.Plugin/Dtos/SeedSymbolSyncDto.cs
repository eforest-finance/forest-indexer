using Forest.Indexer.Plugin.enums;

namespace Forest.Indexer.Plugin.GraphQL;

public class SeedSymbolSyncDto
{
    public string? Id { get; set; }

    public string? ChainId { get; set; }

    public string? BlockHash { get; set; }

    public long BlockHeight { get; set; }
    
    public string? Symbol { get; set; }
    
    public string? TokenContractAddress { get; set; }

    public int Decimals { get; set; }

    public long Supply { get; set; }

    public long TotalSupply { get; set; }

    public string? TokenName { get; set; }

    public string? Owner { get; set; }
    
    public string? Issuer { get; set; }

    public bool IsBurnable { get; set; }

    public int IssueChainId { get; set; }

    public long Issued { get; set; }

    public DateTime CreateTime { get; set; }

    public List<ExternalInfoDictionaryDto> ExternalInfoDictionary { get; set; }

    public string? SeedOwnedSymbol { get; set; }

    public long SeedExpTimeSecond { get; set; }

    public DateTime SeedExpTime { get; set; }

    public long RegisterTimeSecond { get; set; }

    public DateTime RegisterTime { get; set; }

    public string? IssuerTo { get; set; }

    public bool IsDeleteFlag { get; set; }

    public string? TokenType { get; set; }

    public string? SeedType { get; set; }

    public decimal Price { get; set; }

    public string? PriceSymbol { get; set; }

    public decimal BeginAuctionPrice { get; set; }

    public decimal AuctionPrice { get; set; }

    public string? AuctionPriceSymbol { get; set; }

    public DateTime AuctionDateTime { get; set; }

    public bool OtherOwnerListingFlag { get; set; }

    public string? ListingId { get; set; }
    public string? ListingAddress { get; set; }
    public decimal ListingPrice { get; set; }
    public long ListingQuantity { get; set; }
    public DateTime? ListingEndTime { get; set; }
    public DateTime? LatestListingTime { get; set; }

    public decimal OfferPrice { get; set; }

    public long OfferQuantity { get; set; }

    public DateTime OfferExpireTime { get; set; }

    public DateTime? LatestOfferTime { get; set; }

    public TokenInfoDto? OfferToken { get; set; }
    public TokenInfoDto? ListingToken { get; set; }
    public TokenInfoDto? LatestDealToken { get; set; }

    public SeedStatus? SeedStatus { get; set; }

    public bool HasOfferFlag { get; set; }
    public bool HasListingFlag { get; set; }
    public decimal MinListingPrice { get; set; }

    public DateTime? MinListingExpireTime { get; set; }

    public string? MinListingId { get; set; }

    public bool HasAuctionFlag { get; set; }
    
    public decimal MaxAuctionPrice { get; set; }

    public decimal MaxOfferPrice { get; set; }

    public DateTime? MaxOfferExpireTime { get; set; }

    public string? MaxOfferId { get; set; }

    public string? SeedImage { get; set; }
}