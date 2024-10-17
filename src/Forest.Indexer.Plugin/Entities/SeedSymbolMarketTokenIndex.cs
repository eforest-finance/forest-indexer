using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class SeedSymbolMarketTokenIndex : TokenInfoBase
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string TransactionId { get; set; }
    [Keyword] public HashSet<string> OwnerManagerSet { get; set; }
    [Keyword] public string RandomOwnerManager { get; set; }
    [Keyword] public HashSet<string> IssueManagerSet { get; set; }
    [Keyword] public HashSet<string> IssueToSet { get; set; }
    [Keyword] public string RandomIssueManager { get; set; }
    [Keyword] public string SymbolMarketTokenLogoImage { get; set; }
    
    [Keyword] public string IssueChain { get; set; }
    
    public bool SameChainFlag { get; set; }
    
    [Keyword] public string Owner { get; set; }
    [Keyword] public string Issuer { get; set; }
    
    
}