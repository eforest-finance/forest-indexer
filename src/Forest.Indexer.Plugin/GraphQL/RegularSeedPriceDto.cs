using Forest.Indexer.Plugin.Entities;

namespace Forest.Indexer.Plugin.GraphQL;

public class RegularSeedPriceDto
{
    public string TokenType { get; set; }
    public int SymbolLength { get; set; }
    public TokenPriceInfo TokenPrice { get; set; }
}