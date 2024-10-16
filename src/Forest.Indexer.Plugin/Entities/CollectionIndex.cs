using AeFinder.Sdk.Entities;
using Forest.Indexer.Plugin.enums;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class CollectionIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public HashSet<string> OwnerManagerSet { get; set; }

    [Keyword] public string RandomOwnerManager { get; set; }
    [Keyword] public string LogoImage { get; set; }

    [Keyword] public string FeaturedImageLink { get; set; }

    [Keyword] public string Description { get; set; }

    [Keyword] public string CreatorAddress { get; set; }
    public CollectionType CollectionType { get; set; }
    
}