namespace Forest.Indexer.Plugin.GraphQL;

public class WhitelistManagerIndexDto
{
    public string ChainId { get; set; }
    public string Manager { get; set; }
    public WhitelistInfoBaseDto WhitelistInfo { get; set; }
}