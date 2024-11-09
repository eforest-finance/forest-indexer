using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class TagInfo : AeFinderEntity, IAeFinderEntity
{
    public int AddressCount { get; set; }
    [Keyword] public override string Id { get; set; }
    [Keyword] public string TagHash { get; set; }
    [Keyword] public string Name { get; set; }
    [Keyword] public string Info { get; set; }
}