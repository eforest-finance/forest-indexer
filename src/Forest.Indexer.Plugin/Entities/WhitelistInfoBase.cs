using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class WhitelistInfoBase
{
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string WhitelistHash { get; set; }
    [Keyword] public string ProjectId { get; set; }
    public StrategyType StrategyType { get; set; }
}