namespace Drop.Indexer.Plugin.GraphQL;

public class NFTDropClaimDto
{
    
    public string Address { get; set; }
    
    public string DropId { get; set; }
    
    public long ClaimTotal { get; set; }
    
    public long ClaimAmount { get; set; }
    
}