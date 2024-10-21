namespace Forest.Indexer.Plugin.GraphQL;

public class NftDealInfoDto
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string NftFrom { get; set; }
    public string NftTo { get; set; }
    public string NftSymbol { get; set; }
    public string NftQuantity { get; set; }
    public string PurchaseSymbol { get; set; }
    public string PurchaseTokenId { get; set; }
    public string NftInfoId { get; set; }
    public long PurchaseAmount { get; set; }
    public DateTime DealTime { get; set; }
    public string CollectionSymbol { get; set; }
}

public class NftDealInfoDtoPageResultDto
{
    public long TotalRecordCount { get; set; }

    public List<NftDealInfoDto> Data { get; set; }
}