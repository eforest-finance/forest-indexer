using Forest.Contracts.Drop;

namespace Drop.Indexer.Plugin.GraphQL;

public class NFTDropInfoDto
{
    public string DropId { get; set; }
    
    public string CollectionId { get; set; }
    
    public DateTime StartTime { get; set; }
    
    public DateTime ExpireTime { get; set; }
    
    public long ClaimMax { get; set; }
    
    public decimal ClaimPrice { get; set; }
    
    public long MaxIndex { get; set; }
    
    public long TotalAmount { get; set; }
    
    public long ClaimAmount { get; set; }
    
    public string Owner { get; set; }
    
    public bool IsBurn { get; set; }
    
    public DropState State { get; set; }
    
    public DateTime CreateTime { get; set; }
    
    public DateTime UpdateTime { get; set; }
}


public class NFTDropPageResultDto
{
    public long TotalRecordCount { get; set; }

    public List<NFTDropInfoDto> Data { get; set; }
}