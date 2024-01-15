using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class UniqueSeedPriceIndex : AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    
    [Keyword] public string TokenType { get; set; }
    
    public int SymbolLength { get; set; }
    
    public TokenPriceInfo TokenPrice { get; set; }
}
