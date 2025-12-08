namespace Forest.Indexer.Plugin.GraphQL;

public class NFTListingInfoDto
{
    public string Id { get; set; }
    public long Quantity { get; set; }
    public string Symbol { get; set; }
    public string Owner { get; set; }
    public string ChainId { get; set; }
    public decimal Prices { get; set; }
    public decimal? WhitelistPrices { get; set; }
    public string? WhitelistId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime PublicTime { get; set; }
    public DateTime ExpireTime { get; set; }
    public TokenInfoDto PurchaseToken { get; set; }
    public NFTInfoDto NftInfo { get; set; }
    public NFTCollectionDto NftCollectionDto { get; set; }
    public long RealQuantity { get; set; }
    public string BusinessId { get; set; }
}