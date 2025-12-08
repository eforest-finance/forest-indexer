

using Forest.Indexer.Plugin.GraphQLL;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetNFTCollectionsDto : PagedResultQueryDtoBase
{
    public string CreatorAddress { get; set; }
    public List<int> CollectionType { get; set; }
    public string Param { get; set; }
}