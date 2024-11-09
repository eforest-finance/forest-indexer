using AeFinder.Sdk.Entities;
using Forest.Contracts.SymbolRegistrar;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class SymbolMarketActivityIndex: AeFinderEntity, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }
    public DateTime TransactionDateTime { get; set; }
    [Keyword] public string Symbol { get; set; }
    
    [Keyword] public string SeedSymbol { get; set; }
    [Keyword] public string Address { get; set; }
    private SeedType SeedType { get; set; }
    public int IntSeedType { get; set; }
    public decimal TransactionFee { get; set; }
    [Keyword] public string TransactionFeeSymbol { get; set; }
    [Keyword] public string TransactionId { get; set; }
    private SymbolMarketActivityType Type { get; set; }
    public int IntType { get; set; }
    public decimal Price { get; set; }
    public string PriceSymbol { get; set; }
    
    public void OfType(SymbolMarketActivityType type)
    {
        Type = type;
        IntType = (int)type;
    }
    public void OfType(SeedType seedType)
    {
        SeedType = seedType;
        IntSeedType = (int)seedType;
    }
}

public enum SymbolMarketActivityType
{
    //Seed in the chain by the current user address of competitive bidding the Bid for auction after a successful transaction
    // SymbolMarkerContract.BidPlaced
    Bid,

    //In the chain by the current user address of Seed transactions
    // SymbolMarkerContract.Dealt
    Buy,

    //The current user address created in the chain by the FT NFT
    // TokenContract.create & SeedSymbolInfo TokenType in [FT,NFT]
    Create,

    //In the chain by the current user address issued token
    // TokenContract.Issue & SeedSymbolInfo TokenType in [FT]
    Issue
}