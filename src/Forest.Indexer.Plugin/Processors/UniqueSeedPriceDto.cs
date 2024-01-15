using Forest.Indexer.Plugin.Entities;

namespace Forest.Indexer.Plugin.Processors;

public class UniqueSeedPriceDto
{
    public  string Id { get; set; }
    
    public string TokenType { get; set; }
    
    public int SymbolLength { get; set; }
    
    public TokenPriceInfo TokenPrice { get; set; }
    
    
    public long BlockHeight { get; set; }
}