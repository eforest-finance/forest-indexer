using Forest.Indexer.Plugin.GraphQLL;
using JetBrains.Annotations;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetAllSeedSymbolsDto : PagedResultRequestDto
{
    public List<string> AddressList { get; set; }
    [CanBeNull] public List<string> ChainList { get; set; }
    [CanBeNull] public string SeedOwnedSymbol { get; set; }
}