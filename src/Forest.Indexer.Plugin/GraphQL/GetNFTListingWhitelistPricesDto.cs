using Volo.Abp.Application.Dtos;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetNFTListingWhitelistPricesDto : PagedResultRequestDto
{
    public string Address { get; set; }
    public List<string> NftInfoIds { get; set; }

}