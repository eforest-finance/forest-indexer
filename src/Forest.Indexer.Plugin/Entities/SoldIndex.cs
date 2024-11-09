using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class SoldIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public string NftFrom { get; set; }
    [Keyword] public string NftTo { get; set; }
    [Keyword] public string NftSymbol { get; set; }
    [Keyword] public string NftQuantity { get; set; }
    [Keyword] public string PurchaseSymbol { get; set; }
    [Keyword] public string PurchaseTokenId { get; set; }
    [Keyword] public string NftInfoId { get; set; }
    public long PurchaseAmount { get; set; }
    public DateTime DealTime { get; set; }
    [Keyword] public string CollectionSymbol { get; set; }
    [Keyword]
    public string ChainId { get; set; }

    [Keyword]
    public string BlockHash { get; set; }

    public long BlockHeight { get; set; }

    [Keyword]
    public string PreviousBlockHash { get; set; }

    public bool IsDeleted { get; set; }
}