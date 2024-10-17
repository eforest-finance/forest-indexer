using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class OfferInfoIndex : NFTOfferBase, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string BizInfoId { get; set; }
    [Keyword] public string BizSymbol { get; set; }
    public TokenInfoIndex PurchaseToken { get; set; }
    public DateTime CreateTime { get; set; }

}