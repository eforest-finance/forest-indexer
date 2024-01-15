using Forest.Indexer.Plugin.Entities;

namespace Forest.Indexer.Plugin.GraphQL;

public class WhitelistInfoIndexDto
{
    public string ChainId { get; set; }
    public string WhitelistHash { get; set; }
    public string ProjectId { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsCloneable { get; set; }
    public string Remark { get; set; }
    public string Creator { get; set; }
    public StrategyType StrategyType { get; set; }
    public string LastModifyTime { get; set; }
}