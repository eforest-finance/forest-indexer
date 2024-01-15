using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class SeedMainChainChangeIndex : AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public string Symbol { get; set; }
    
    [Keyword] public string TransactionId { get; set; }

    public DateTime UpdateTime { get; set; }
}