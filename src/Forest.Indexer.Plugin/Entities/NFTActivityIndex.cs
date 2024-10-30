using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class NFTActivityIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }
    
    [Keyword] public string NftInfoId { get; set; }
    
    public NFTActivityType Type { get; set; }
    public int IntType { get; set; }
    
    [Keyword] public string From { get; set; }
    
    [Keyword] public string To { get; set; }
    
    public long Amount { get; set; }
    
    public decimal Price { get; set; }
    
    [Keyword] public string TransactionHash { get; set; }
    
    public DateTime Timestamp { get; set; }
    public TokenInfoIndex PriceTokenInfo { get; set; }
    
    [Keyword]
    public string ChainId { get; set; }

    public long BlockHeight { get; set; }

    public void OfType(NFTActivityType nftActivityType)
    {
        Type = nftActivityType;
        IntType = (int)nftActivityType;
    }
}


public enum NFTActivityType
{
    Issue,
    Burn,
    Transfer,
    Sale,
    ListWithFixedPrice,
    DeList,
    MakeOffer,
    CancelOffer,
    PlaceBid,
}

