using AElfIndexer.Client;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class WhitelistBase : AElfIndexerClientEntity<string>
{
    [Keyword] public override string Id { get; set; }
}