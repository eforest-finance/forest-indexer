namespace Forest.Indexer.Plugin.GraphQL;

public class SeedMainChainChangeDto
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public string TransactionId { get; set; }
    public DateTime UpdateTime { get; set; }
    public long BlockHeight { get; set; }
}

public class SeedMainChainChangePageResultDto
{
    public long TotalRecordCount { get; set; }

    public List<SeedMainChainChangeDto> Data { get; set; }
}