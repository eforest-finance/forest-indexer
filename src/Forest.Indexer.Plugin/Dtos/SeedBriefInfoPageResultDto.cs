namespace Forest.Indexer.Plugin.GraphQL;

public class SeedBriefInfoPageResultDto
{
    public long TotalRecordCount { get; set; }

    public List<SeedBriefInfoDto> Data { get; set; }
}

public class NFTBriefInfoPageResultDto
{
    public long TotalRecordCount { get; set; }

    public List<NFTBriefInfoDto> Data { get; set; }
}