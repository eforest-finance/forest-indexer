using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class WhiteListExtraInfoIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public string WhitelistInfoId { get; set; }
    [Keyword] public string TagInfoId { get; set; }
    [Keyword] public string Address { get; set; }
    [Keyword] public string LastModifyTime { get; set; }
}