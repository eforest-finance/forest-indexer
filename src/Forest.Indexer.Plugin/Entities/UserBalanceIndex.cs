using AElf.Indexing.Elasticsearch;

namespace Forest.Indexer.Plugin.Entities;

public class UserBalanceIndex: UserBalanceBase, IIndexBuild
{
    public decimal ListingPrice { get; set; }
    public DateTime? ListingTime { get; set; }
}