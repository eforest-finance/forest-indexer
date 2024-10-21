using Forest.Indexer.Plugin.GraphQLL;
using JetBrains.Annotations;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetActivitiesConditionDto: PagedResultRequestDto
{
    public string NFTInfoId { get; set; }
    [CanBeNull] public List<int> Types { get; set; }
    public long? TimestampMin { get; set; }
    public long? TimestampMax { get; set; }
    public string SortType { get; set; }
    public double AbovePrice { get; set; }
    
    public string FilterSymbol { get; set; }

   
}
