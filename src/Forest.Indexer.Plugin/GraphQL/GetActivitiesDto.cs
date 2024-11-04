using Forest.Indexer.Plugin.GraphQLL;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetActivitiesDto: PagedResultRequestDto
{
    public string? NFTInfoId { get; set; }
    
    public List<int>? Types { get; set; } = new List<int>();
    public long? TimestampMin { get; set; }
    public long? TimestampMax { get; set; }
}

public class GetCollectionActivitiesDto: PagedResultRequestDto
{
    public string CollectionId { get; set; }
    public List<string>? BizIdList { get; set; } = new List<string>();
    public List<int>? Types { get; set; }
}

public class GetMessageActivitiesDto
{
    public List<int>? Types { get; set; } = new List<int>();
    
    public string? ChainId { get; set; }
    
    public long BlockHeight { get; set; }
    public int SkipCount { get; set; }
}