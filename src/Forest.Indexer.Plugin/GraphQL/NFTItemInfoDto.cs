namespace Forest.Indexer.Plugin.GraphQL;

public class NFTItemInfoDto
{
    public string Symbol { get; set; }
    
    public string TokenContractAddress { get; set; }

    public int Decimals { get; set; }
    
    public long Supply { get; set; }
    
    public long TotalSupply { get; set; }

    public string TokenName { get; set; }

    public string Issuer { get; set; }

    public bool IsBurnable { get; set; }

    public int IssueChainId { get; set; }
    
    public string ImageUrl { get; set; }
    
    public string CollectionSymbol { get; set; }
    
    public string CollectionName { get; set; }
}