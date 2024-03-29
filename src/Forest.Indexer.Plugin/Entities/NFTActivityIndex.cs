using AElf.Indexing.Elasticsearch;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class NFTActivityIndex: NFTActivityBase, IIndexBuild
{
    public TokenInfoIndex PriceTokenInfo { get; set; }

    [Keyword] public string Symbol { get; set; }
    
    [Keyword] public string CollectionSymbol { get; set; }
    
    [Keyword] public string CollectionId { get; set; }
    
}