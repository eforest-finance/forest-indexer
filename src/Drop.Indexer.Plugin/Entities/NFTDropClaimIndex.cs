using AeFinder.Sdk.Entities;
using Nest;

namespace Drop.Indexer.Plugin.Entities;

public class NFTDropClaimIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }

    [Keyword] public string Address { get; set; }

    [Keyword] public string DropId { get; set; }

    public long ClaimTotal { get; set; }

    public long ClaimAmount { get; set; }

    public DateTime CreateTime { get; set; }

    public DateTime UpdateTime { get; set; }
}