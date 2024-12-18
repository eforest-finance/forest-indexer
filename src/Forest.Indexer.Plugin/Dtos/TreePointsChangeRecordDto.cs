
using Forest.Indexer.Plugin.Entities;

namespace Forest.Indexer.Plugin.GraphQL;


public class TreePointsChangeRecordPageResultDto
{
    public long TotalRecordCount { get; set; }
    
    public List<TreePointsChangeRecordDto> Data { get; set; }
}
public class TreePointsChangeRecordDto
{
    public string Id { get; set; }
    
    public string Address { get; set; }

    public long TotalPoints { get; set; }
    
    public long Points { get; set; }
    
    public OpType OpType { get; set; }//0:Added 1:updateTree 2:claim activity
    public long OpTime { get; set; }
    
    //extend fields
    public PointsType PointsType { get; set; } //optype=Added, 0:normalone 1:normaltwo 2:invite
    public string ActivityId { get; set; } // optype = claim activity
    public string TreeLevel { get; set; } // optype = updateTree
    
    public string ChainId { get; set; }

    public string BlockHash { get; set; }

    public long BlockHeight { get; set; }

    public string PreviousBlockHash { get; set; }

    public bool IsDeleted { get; set; }
}