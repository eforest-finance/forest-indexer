namespace Forest.Indexer.Plugin.GraphQL;

public class WhitelistManagerResultDto
{
    public long TotalCount { get; set; }

    public List<WhitelistManagerIndexDto> Items { get; set; }
}