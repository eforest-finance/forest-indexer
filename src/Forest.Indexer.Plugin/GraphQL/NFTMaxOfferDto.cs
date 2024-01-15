namespace Forest.Indexer.Plugin.GraphQL;

public class ExpiredNftMaxOfferInfo
{
    public string Id  { get; set; }
    public DateTime ExpireTime { get; set; }
    public decimal Prices  { get; set; }
}

public class ExpiredNftMaxOfferDto
{
    public string Key  { get; set; }
    public ExpiredNftMaxOfferInfo Value { get; set; }
}