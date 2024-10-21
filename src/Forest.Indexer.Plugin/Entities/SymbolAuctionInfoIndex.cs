using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class SymbolAuctionInfoIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }

    [Keyword] public string Symbol { get; set; }
    
    [Keyword] public string CollectionSymbol { get; set; }
    
    public TokenPriceInfo StartPrice { get; set; }

    public long StartTime { get; set; }

    public long EndTime { get; set; }

    public long MaxEndTime { get; set; }

    public long Duration { get; set; }
    
    public int FinishIdentifier { get; set; }
    
    public int MinMarkup { get; set; }

    [Keyword] public string FinishBidder { get; set; }

    public long FinishTime { get; set; }
    
    public TokenPriceInfo FinishPrice { get; set; }
        
    [Keyword] public string ReceivingAddress { get; set; }
    
    [Keyword] public string Creator { get; set; }
    
    [Keyword] public string TransactionHash { get; set; }
    [Keyword]
    public string ChainId { get; set; }

    [Keyword]
    public string BlockHash { get; set; }

    public long BlockHeight { get; set; }

    [Keyword]
    public string PreviousBlockHash { get; set; }

    public bool IsDeleted { get; set; }
}