namespace Forest.Indexer.Plugin.GraphQL;

public class NftOfferPageResultDto
{
    public long TotalRecordCount { get; set; }

    public List<NFTOfferDto> Data { get; set; }
}