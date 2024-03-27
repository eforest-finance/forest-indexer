using AElf.Indexing.Elasticsearch;
using Forest.Indexer.Plugin.enums;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class CollectionIndex : TokenInfoBase, IIndexBuild
{
    [Keyword] public HashSet<string> OwnerManagerSet { get; set; }

    [Keyword] public string RandomOwnerManager { get; set; }
    [Text(Index = false)] public string LogoImage { get; set; }

    [Text(Index = false)] public string FeaturedImageLink { get; set; }

    [Text(Index = false)] public string Description { get; set; }

    [Keyword] public string CreatorAddress { get; set; }
    public CollectionType CollectionType { get; set; }
    
}