using AElf.Indexing.Elasticsearch;

namespace Forest.Indexer.Plugin.Entities;

public class NFTActivityIndex: NFTActivityBase, IIndexBuild
{
    public TokenInfoIndex PriceTokenInfo { get; set; }
}