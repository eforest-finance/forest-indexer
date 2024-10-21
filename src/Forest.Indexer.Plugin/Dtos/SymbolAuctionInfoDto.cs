using Forest.Indexer.Plugin.Entities;
using Nest;

namespace Forest.Indexer.Plugin.GraphQL;

public class SymbolAuctionInfoDto
{
    public string Id { get; set; }

    public string Symbol { get; set; }

    public string CollectionSymbol { get; set; }

    public TokenPriceInfo StartPrice { get; set; }

    public long StartTime { get; set; }

    public long EndTime { get; set; }

    public long MaxEndTime { get; set; }

    public long Duration { get; set; }

    public int FinishIdentifier { get; set; }

    public int MinMarkup { get; set; }

    public string FinishBidder { get; set; }

    public long FinishTime { get; set; }

    public TokenPriceInfo FinishPrice { get; set; }

    public string ReceivingAddress { get; set; }

    public string Creator { get; set; }
    
    public long BlockHeight { get; set; }
    
    public string TransactionHash { get; set; }
}