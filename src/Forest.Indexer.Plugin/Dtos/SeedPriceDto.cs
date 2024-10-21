using Forest.Indexer.Plugin.Entities;

namespace Forest.Indexer.Plugin.GraphQL;

public class SeedPriceDto
{
    public  string Id { get; set; }
    
    public string TokenType { get; set; }
    
    public int SymbolLength { get; set; }
    
    public TokenPriceInfo TokenPrice { get; set; }
    
    public long BlockHeight { get; set; }
}