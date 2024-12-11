namespace Forest.Indexer.Plugin.GraphQL;

public class SymbolMarkerTokenPageResultDto
{
    public long TotalRecordCount { get; set; }

    public List<SymbolMarkerTokenDto> Data { get; set; }
}

public class SymbolMarkerTokenDto
{
    public string? SymbolMarketTokenLogoImage { get; set; }
    public string Symbol { get; set; }
    public string TokenName { get; set; }
    public int Decimals { get; set; }
    public long TotalSupply { get; set; }
    public long Supply { get; set; }
    public List<string> IssueManagerList { get; set; }
    public string Issuer { get; set; }
    public long Issued { get; set; }
    public int IssueChainId { get; set; }
    public string Owner { get; set; }

}