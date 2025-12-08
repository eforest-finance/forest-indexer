namespace Forest.Indexer.Plugin.Entities;

public class WhitelistInfoBaseDto
{
    public string ChainId { get; set; }
    public string WhitelistHash { get; set; }
    public string ProjectId { get; set; }
    public StrategyType StrategyType { get; set; }
}