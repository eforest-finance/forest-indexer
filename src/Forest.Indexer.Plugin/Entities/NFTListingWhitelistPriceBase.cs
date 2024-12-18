using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class NFTListingWhitelistPriceBase : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string NFTInfoId { get; set; }
    [Keyword] public string WhitelistHash { get; set; }
    [Keyword] public string WhitelistCreatorAddress { get; set; }
    [Keyword] public string Address { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
}