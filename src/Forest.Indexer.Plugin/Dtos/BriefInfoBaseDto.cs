namespace Forest.Indexer.Plugin.GraphQL;

public class BriefInfoBaseDto
{
    public string Id { get; set; }
    public string TokenName { get; set; }
    public string CollectionSymbol { get; set; }
    public string NFTSymbol { get; set; }
    public string PreviewImage { get; set; }
    public string PriceDescription { get; set; }
    public decimal? Price { get; set; }
    public long IssueChainId { get; set; }
    public string IssueChainIdStr { get; set; }
    public long ChainId { get; set; }
    public string ChainIdStr { get; set; }
    
}

public class SeedBriefInfoDto : BriefInfoBaseDto
{
}
public class NFTBriefInfoDto : BriefInfoBaseDto
{
}