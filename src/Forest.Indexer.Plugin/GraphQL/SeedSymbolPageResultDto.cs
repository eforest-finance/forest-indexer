namespace Forest.Indexer.Plugin.GraphQL;

public class SeedSymbolPageResultDto
{
    public long TotalRecordCount { get; set; }

    public List<SeedSymbolDto> Data { get; set; }
}

public class SeedSymbolDto
{
    public string Id { get; set; }
    
    public string Symbol { get; set; }
    
    public string TokenContractAddress { get; set; }
    
    public int Decimals { get; set; }
    
    public long Supply { get; set; }
    
    public long TotalSupply { get; set; }

    public string TokenName { get; set; }

    public string Issuer { get; set; }

    public bool IsBurnable { get; set; }

    public int IssueChainId { get; set; }
    
    public long Issued { get; set; }

    public DateTime CreateTime { get; set; }
    
    public string SeedOwnedSymbol { get; set; }
    
    public long SeedExpTimeSecond { get; set; }

    public DateTime SeedExpTime { get; set; }
}