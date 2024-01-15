using AElf.Indexing.Elasticsearch;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class OfferInfoIndex : NFTOfferBase, IIndexBuild
{
    [Keyword] public string BizInfoId { get; set; }
    [Keyword] public string BizSymbol { get; set; }
    public TokenInfoIndex PurchaseToken { get; set; }
    public DateTime CreateTime { get; set; }

}