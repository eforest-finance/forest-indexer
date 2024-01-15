using Volo.Abp.Application.Dtos;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetNFTCollectionsDto : PagedResultRequestDto
{
    public string CreatorAddress { get; set; }
    public List<int> CollectionType { get; set; }
    public string Param { get; set; }
}