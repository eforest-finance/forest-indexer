
using Forest.Indexer.Plugin.GraphQLL;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetNFTCollectionsByAddressListDto : PagedResultQueryDtoBase
{
    public List<string> AddressList { get; set; }
    public List<int>? CollectionType { get; set; } = new List<int>();
    public string? Param { get; set; }
}