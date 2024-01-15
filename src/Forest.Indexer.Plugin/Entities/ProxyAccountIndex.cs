using AElf.Indexing.Elasticsearch;

namespace Forest.Indexer.Plugin.Entities;

public class ProxyAccountIndex : TokenInfoBase, IIndexBuild
{
    public string ProxyAccountAddress { get; set; }
    public HashSet<string> ManagersSet { get; set; }
}