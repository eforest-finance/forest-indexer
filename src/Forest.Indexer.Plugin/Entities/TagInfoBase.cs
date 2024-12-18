using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class TagInfoBase 
{
    [Keyword] public string TagHash { get; set; }
    [Keyword] public string Name { get; set; }
    [Keyword] public string Info { get; set; }
}
public class PriceTagInfo
{
    [Keyword] public string Symbol { get; set; }
    public decimal Price { get; set; }
}