using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class WhiteListManagerIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public string WhitelistInfoId { get; set; }
    [Keyword] public string Manager { get; set; }
}