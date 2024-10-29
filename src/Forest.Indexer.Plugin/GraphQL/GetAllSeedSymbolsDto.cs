using Forest.Indexer.Plugin.GraphQLL;
using JetBrains.Annotations;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetAllSeedSymbolsDto : PagedResultRequestDto
{
    public List<string> AddressList { get; set; }
    public List<string>? ChainList { get; set; }
    public string? SeedOwnedSymbol { get; set; }
}