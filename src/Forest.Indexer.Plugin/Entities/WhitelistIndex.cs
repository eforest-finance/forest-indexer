using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class WhitelistIndex : WhitelistBase
{
    public bool IsAvailable { get; set; }
    public bool IsCloneable { get; set; }
    [Keyword] public string Remark { get; set; }
    [Keyword] public string CloneFrom { get; set; }
    [Keyword] public string Creator { get; set; }
    public List<string> ManagerInfoDictory { get; set; }
    [Keyword] public string ProjectId { get; set; }
    [Keyword] public string LastModifyTime { get; set; }
    public StrategyType StrategyType { get; set; }
}

public enum StrategyType
{
    Basic = 0,
    Price = 1,
    Customize = 2
}