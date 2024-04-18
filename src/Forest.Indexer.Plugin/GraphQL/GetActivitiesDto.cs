using JetBrains.Annotations;
using Volo.Abp.Application.Dtos;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetActivitiesDto: PagedResultRequestDto
{
    public String NFTInfoId { get; set; }
    [CanBeNull] public List<int> Types { get; set; }
    public long? TimestampMin { get; set; }
    public long? TimestampMax { get; set; }
}

public class GetCollectionActivitiesDto: PagedResultRequestDto
{
    public string collectionId { get; set; }
    public List<string> BizIdList { get; set; }
    [CanBeNull] public List<int> Types { get; set; }
}