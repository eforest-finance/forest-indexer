using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class WhitelistBase : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }
}