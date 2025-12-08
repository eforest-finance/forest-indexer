namespace Forest.Indexer.Plugin.GraphQL;

public class GetSymbolMarketTokenIssuerInput
{
    public int IssueChainId { get; set; }
    public string TokenSymbol { get; set; }
}