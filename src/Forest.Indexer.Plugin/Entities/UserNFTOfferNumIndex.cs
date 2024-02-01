using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class UserNFTOfferNumIndex : AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }

    [Keyword] public string Address { get; set; }

    [Keyword] public string NFTInfoId { get; set; }

    public int OfferNum { get; set; }
}