using AElf.Indexing.Elasticsearch;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class NFTMarketInfoIndex: NFTMarketBase, IIndexBuild
{
    [Keyword] public string NFTInfoId {get; set; }
    public string PurchaseSymbol { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public DateTime Timestamp { get; set; }
}