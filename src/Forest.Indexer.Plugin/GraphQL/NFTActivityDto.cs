namespace Forest.Indexer.Plugin.GraphQL;

public class NFTActivityDto
{
    public string Id { get; set; }
    public string NftInfoId { get; set; }
    public int Type { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public long Amount { get; set; }
    public TokenInfoDto PriceTokenInfo { get; set; }
    public decimal Price { get; set; }
    public string TransactionHash { get; set; }
    public DateTime Timestamp { get; set; }
    public long BlockHeight { get; set; }
    public string ChainId { get; set; }
}