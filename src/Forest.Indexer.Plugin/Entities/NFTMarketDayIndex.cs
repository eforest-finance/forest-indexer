using AElf.Indexing.Elasticsearch;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class NFTMarketDayIndex : NFTMarketBase, IIndexBuild
{
    [Keyword] public string NFTInfoId { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public DateTime DayBegin { get; set; }
    public DateTime UpdateDate { get; set; }
    public int MarketNumber { get; set; }
}