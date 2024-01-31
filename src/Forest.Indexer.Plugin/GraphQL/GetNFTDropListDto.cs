using Volo.Abp.Application.Dtos;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetNFTDropListDto : PagedResultRequestDto
{
    public SearchType Type { get; set; }
}

public enum SearchType
{
    All,
    Ongoing,
    YetToBegin,
    Finished
}