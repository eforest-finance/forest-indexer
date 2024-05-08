using Volo.Abp.Application.Dtos;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetNFTCollectionsByAddressListDto : PagedResultRequestDto
{
    public List<string> AddressList { get; set; }
    public List<int> CollectionType { get; set; }
    public string Param { get; set; }
}