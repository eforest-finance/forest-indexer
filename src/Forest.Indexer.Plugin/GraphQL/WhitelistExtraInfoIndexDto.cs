namespace Forest.Indexer.Plugin.GraphQL;

public class WhitelistExtraInfoIndexDto
{
    public string ChainId { get; set; }
    public string Address { get; set; }
    public string TagInfoId { get; set; }
    public WhitelistInfoBaseDto WhitelistInfo { get; set; }
    public TagInfoBaseDto TagInfo { get; set; }
    public string LastModifyTime { get; set; }
}