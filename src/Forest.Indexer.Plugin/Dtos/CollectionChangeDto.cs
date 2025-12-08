namespace Forest.Indexer.Plugin.GraphQL;

public class CollectionChangeDto
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public DateTime UpdateTime { get; set; }
    public long BlockHeight { get; set; }
}

public class CollectionChangePageResultDto
{
    public long TotalRecordCount { get; set; }

    public List<CollectionChangeDto>? Data { get; set; }
}