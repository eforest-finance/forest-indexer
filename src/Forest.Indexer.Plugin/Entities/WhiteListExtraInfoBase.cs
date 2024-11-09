using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class WhiteListExtraInfoBase 
{
   // [Keyword] public override string Id { get; set; }
    [Keyword] public string Address { get; set; }
}