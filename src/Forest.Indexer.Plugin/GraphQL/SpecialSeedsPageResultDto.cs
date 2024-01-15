namespace Forest.Indexer.Plugin.GraphQL;

public class SpecialSeedsPageResultDto
{
    public long TotalRecordCount { get; set; }

    public List<SeedInfoDto> Data { get; set; }
}