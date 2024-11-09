
using Forest.Indexer.Plugin.GraphQLL;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetSymbolMarketTokensInput : PagedResultRequestDto
{
    public List<string> Address { get; set; }
}