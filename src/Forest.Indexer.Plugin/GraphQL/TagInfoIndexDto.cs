namespace Forest.Indexer.Plugin.GraphQL;

public class TagInfoIndexDto : TagInfoBaseDto
{
    public WhitelistInfoBaseDto WhitelistInfo { get; set; }
    public long AddressCount { get; set; }
    public string WhitelistId { get; set; }
    public string WhitelistInfoId { get; set; }
}