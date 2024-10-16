using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class WhiteListExtraInfoIndex : WhitelistBase
{
    [Keyword] public string WhitelistInfoId { get; set; }
    [Keyword] public string TagInfoId { get; set; }
    [Keyword] public string Address { get; set; }
    [Keyword] public string LastModifyTime { get; set; }
}