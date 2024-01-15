namespace Forest.Indexer.Plugin.GraphQL;

public class NFTCollectionPageResultDto
{
    public long TotalRecordCount { get; set; }

    public List<NFTCollectionDto> Data { get; set; }
}