namespace Forest.Indexer.Plugin.GraphQL;

public class NFTUserBalanceDto
{
    public string Owner { get; set; }
    
    public long OwnerCount { get; set; }
}

public class NFTOwnerInfoDto
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string Address { get; set; }
    public long Amount { get; set; }
    public string NFTInfoId { get; set; }
}

public class NFTOwnersPageResultDto
{
    public long TotalCount { get; set; }

    public List<NFTOwnerInfoDto> Data { get; set; }
}