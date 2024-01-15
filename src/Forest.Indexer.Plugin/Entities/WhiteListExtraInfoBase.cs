using AElfIndexer.Client;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class WhiteListExtraInfoBase : AElfIndexerClientEntity<string>
{
    [Keyword] public string Address { get; set; }
}