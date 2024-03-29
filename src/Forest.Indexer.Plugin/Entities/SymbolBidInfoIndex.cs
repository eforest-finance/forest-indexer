using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class SymbolBidInfoIndex: AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string Symbol { get; set; }

    [Keyword] public string Bidder { get; set; }


    public long PriceAmount { get; set; }

    [Keyword] public string PriceSymbol { get; set; }


    public long BidTime { get; set; }
    
    [Keyword] public string AuctionId { get; set; }

    [Keyword] public string TransactionHash { get; set; }
}