using Volo.Abp.Application.Dtos;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetTokensInput: PagedResultRequestDto
{
    public HashSet<string> Address { get; set; }
    public List<int> Types { get; set; }
}