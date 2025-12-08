using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class UserNFTOfferNumIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }

    [Keyword] public string Address { get; set; }

    [Keyword] public string NFTInfoId { get; set; }

    public int OfferNum { get; set; }
}