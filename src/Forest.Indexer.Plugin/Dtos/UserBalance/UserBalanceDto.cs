namespace Forest.Indexer.Plugin.GraphQL;

public class UserBalanceDto
{
    public string Id { get; set; }
    
    public string ChainId { get; set; }
    
    public long BlockHeight { get; set; }

    public string Address { get; set; }
    
    public long Amount { get; set; }
    
    public string NFTInfoId { get; set; }

    public string Symbol { get; set; }

    public DateTime ChangeTime { get; set; }

    public decimal ListingPrice { get; set; }
    
    public DateTime? ListingTime { get; set; }

}

public class UserBalancePageResultDto
{
    public long TotalCount { get; set; }

    public List<UserBalanceDto> Data { get; set; }
}

public class GetUserBalancesDto
{
    public long BlockHeight { get; set; }
    public int SkipCount { get; set; }
}