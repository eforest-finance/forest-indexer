using AElfIndexer.Client;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class NFTListingBase : AElfIndexerClientEntity<string>
{
    [Keyword] public override string Id { get; set; }
}