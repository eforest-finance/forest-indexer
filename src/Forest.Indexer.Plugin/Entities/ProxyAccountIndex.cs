using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class ProxyAccountIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }
    public string ProxyAccountAddress { get; set; }
    public HashSet<string> ManagersSet { get; set; }
    
    public DateTime CreateTime { get; set; }
}