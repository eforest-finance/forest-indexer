using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class WhiteListManagerIndex : WhiteListManagerBase
{
    [Keyword] public string WhitelistInfoId { get; set; }
    [Keyword] public string Manager { get; set; }
}