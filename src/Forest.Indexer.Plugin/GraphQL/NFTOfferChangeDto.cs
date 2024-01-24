namespace Forest.Indexer.Plugin.GraphQL;

public class NFTOfferChangeDto
{
    public string NftId { get; set; }
    public string ChainId { get; set; }
    public long BlockHeight { get; set; }
}