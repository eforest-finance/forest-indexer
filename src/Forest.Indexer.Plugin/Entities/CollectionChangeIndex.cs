using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class CollectionChangeIndex : AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public string Symbol { get; set; }

    public DateTime UpdateTime { get; set; }
}