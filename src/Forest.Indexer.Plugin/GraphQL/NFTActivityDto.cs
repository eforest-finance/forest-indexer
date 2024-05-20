using Forest.Indexer.Plugin.GraphQL;

namespace NFTMarketServer.NFT;

public class NFTActivityDto
{
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
}