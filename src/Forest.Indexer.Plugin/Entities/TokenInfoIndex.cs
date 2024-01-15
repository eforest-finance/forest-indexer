using AElf.Indexing.Elasticsearch;

namespace Forest.Indexer.Plugin.Entities;

public class TokenInfoIndex : TokenInfoBase, IIndexBuild
{
    public decimal Prices { get; set; }
}