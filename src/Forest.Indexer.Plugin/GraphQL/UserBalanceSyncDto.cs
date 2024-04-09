
using Forest.Indexer.Plugin.Entities;

namespace Forest.Indexer.Plugin.GraphQL;

public class UserBalanceSyncDto
{
    public string Id { get; set; }
    
    //userAccount Address
    public string Address { get; set; }
    
    public long Amount { get; set; }
    
    public int Decimals { get; set; }
    
    public string NFTInfoId { get; set; }

    public string Symbol { get; set; }

    public DateTime ChangeTime { get; set; }
    
    public decimal ListingPrice { get; set; }
    public DateTime? ListingTime { get; set; }

    public BalanceType BalanceType { get; set; } = BalanceType.Other;
    
    public string BlockHash { get; set; }

    public long BlockHeight { get; set; }
}