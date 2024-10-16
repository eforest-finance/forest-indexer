
namespace Forest.Indexer.Plugin.Entities;

public class UserBalanceIndex: UserBalanceBase
{
    public decimal ListingPrice { get; set; }
    public DateTime? ListingTime { get; set; }
}