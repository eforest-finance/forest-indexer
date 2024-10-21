namespace Forest.Indexer.Plugin.GraphQL;

public class GetNFTSoldDataDto
{
    public long TotalTransCount { get; set; }
    
    public long TotalTransAmount { get; set; }
    
    public long TotalAddressCount{ get; set; }
    
    public long TotalNftAmount { get; set; }
}