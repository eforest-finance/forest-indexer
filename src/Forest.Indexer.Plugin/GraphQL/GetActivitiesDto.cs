using Forest.Indexer.Plugin.GraphQLL;
using JetBrains.Annotations;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetActivitiesDto: PagedResultRequestDto
{
    public String NFTInfoId { get; set; }
    public List<int>? Types { get; set; }
    public long? TimestampMin { get; set; }
    public long? TimestampMax { get; set; }
}

public class GetCollectionActivitiesDto: PagedResultRequestDto
{
    public string CollectionId { get; set; }
    public List<string>? BizIdList { get; set; }
    public List<int>? Types { get; set; }
}

public class GetMessageActivitiesDto
{
    public List<int>? Types { get; set; }
    
    public string? ChainId { get; set; }
    
    public long BlockHeight { get; set; }
    public int SkipCount { get; set; }
}