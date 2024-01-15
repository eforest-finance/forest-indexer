using Forest.Indexer.Plugin.enums;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetRegularSeedPriceInput
{
    public string Symbol { get; set; }
    public TokenType TokenType { get; set; }
}