using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class UserNFTBalanceChangeIndex : AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public string Symbol { get; set; }

    [Keyword] public string Address { get; set; }

    [Keyword] public string UserBalanceId { get; set; }

    public BalanceType BalanceType { get; set; }

    public DateTime UpdateTime { get; set; }
}