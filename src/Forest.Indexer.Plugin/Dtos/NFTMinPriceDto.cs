namespace Forest.Indexer.Plugin.GraphQL;

public class ExpiredNftMinPriceInfo
{
    public string Id  { get; set; }
    public DateTime ExpireTime { get; set; }
    public decimal Prices  { get; set; }
    public string Symbol  { get; set; }
}

public class ExpiredNftMinPriceDto
{
    public string Key  { get; set; }
    public ExpiredNftMinPriceInfo Value { get; set; }
}


