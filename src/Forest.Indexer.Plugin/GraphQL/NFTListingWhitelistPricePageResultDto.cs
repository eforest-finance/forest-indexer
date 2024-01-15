namespace Forest.Indexer.Plugin.GraphQL;

public class NFTListingWhitelistPricePageResultDto : PageResult<NFTListingWhitelistPriceDto>
{
    public NFTListingWhitelistPricePageResultDto(long total, List<NFTListingWhitelistPriceDto> data) : base(total, data)
    {
    }

    public NFTListingWhitelistPricePageResultDto(string msg) : base(msg)
    {
    }
}

public class NFTListingWhitelistPriceDto
{
    public string ListingId { get; set; }
    public long Quantity { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime PublicTime { get; set; }
    public DateTime ExpireTime { get; set; }
    public long DurationHours { get; set; }
    public string OfferFrom { get; set; }
    public string NftInfoId { get; set; }
    public string Owner { get; set; }
    public decimal Prices { get; set; }
    public decimal? WhiteListPrice { get; set; }
    public string WhitelistId { get; set; }
    public TokenInfoDto PurchaseToken { get; set; }
}