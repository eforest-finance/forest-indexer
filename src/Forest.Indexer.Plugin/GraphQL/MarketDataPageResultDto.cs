namespace Forest.Indexer.Plugin.GraphQL;

public class MarketDataPageResultDto
{
    public long TotalRecordCount { get; set; }
    
    public List<NFTInfoMarketDataDto> Data { get; set; }
}