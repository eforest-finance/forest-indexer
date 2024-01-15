namespace Forest.Indexer.Plugin.GraphQL;

public class NFTInfoPageResultDto
{
    public long TotalRecordCount { get; set; }

    public List<NFTInfoDto> Data { get; set; }
}