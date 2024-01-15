using Volo.Abp.Application.Dtos;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetWhitelistExtraInfoListInput : PagedResultRequestDto
{
    public string ChainId { get; set; }
    public string ProjectId { get; set; }
    public string WhitelistHash { get; set; }
}