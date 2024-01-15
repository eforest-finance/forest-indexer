namespace Forest.Indexer.Plugin.GraphQL;

public class CollectionPriceChangeDto
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public DateTime UpdateTime { get; set; }
    public long BlockHeight { get; set; }
}

public class CollectionPriceChangePageResultDto
{
    public long TotalRecordCount { get; set; }

    public List<CollectionPriceChangeDto> Data { get; set; }
}