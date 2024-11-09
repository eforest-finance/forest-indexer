namespace Forest.Indexer.Plugin.GraphQL;

public class NFTListingChangeDto
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public DateTime UpdateTime { get; set; }
    public long BlockHeight { get; set; }
}

public class NFTListingChangeDtoPageResultDto
{
    public long TotalRecordCount { get; set; }

    public List<NFTListingChangeDto> Data { get; set; }
}