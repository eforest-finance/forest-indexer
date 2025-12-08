using Drop.Indexer.Plugin.GraphQL;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetSeedSymbolsDto : PagedResultRequestDto
{
    public string Address { get; set; }
    public string SeedOwnedSymbol { get; set; }
}