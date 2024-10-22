using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class SeedSymbolMarketTokenIndex : AeFinderEntity, IAeFinderEntity
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
    
    [Keyword] public string Symbol { get; set; }

    /// <summary>
    /// token contract address
    /// </summary>
    [Keyword] public string TokenContractAddress { get; set; }
    
    public int Decimals { get; set; }
    
    public long Supply { get; set; }
    
    public long TotalSupply { get; set; }

    [Keyword] public string TokenName { get; set; }

    public bool IsBurnable { get; set; }

    public int IssueChainId { get; set; }
    
    public long Issued { get; set; }

    public DateTime CreateTime { get; set; }
    
    [Keyword]
    public string ChainId { get; set; }
    
    public bool IsDeleted { get; set; }

    public List<ExternalInfoDictionary> ExternalInfoDictionary { get; set; }
    
}