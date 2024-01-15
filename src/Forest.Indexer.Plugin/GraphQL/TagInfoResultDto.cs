

namespace Forest.Indexer.Plugin.GraphQL;

public class TagInfoResultDto
{
    public long TotalCount { get; set; }

    public List<TagInfoIndexDto> Items { get; set; }
}