namespace Forest.Indexer.Plugin.GraphQL;

public class NFTCollectionDto
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public string TokenName { get; set; }
    public long TotalSupply { get; set; }
    public bool IsBurnable { get; set; }
    public int IssueChainId { get; set; }
    public string CreatorAddress { get; set; }
    public string ProxyOwnerAddress { get; set; }
    public string ProxyIssuerAddress { get; set; }
    public string? LogoImage { get; set; }
    public string? FeaturedImageLink { get; set; }
    public string? Description { get; set; }
    public bool IsOfficial { get; set; }
    public List<ExternalInfoDictionaryDto> ExternalInfoDictionary { get; set; }
    public DateTime CreateTime { get; set; }
}