using AeFinder.Sdk.Entities;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class TreePointsChangeRecordIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }
    
    [Keyword] public string Address { get; set; }

    public long TotalPoints { get; set; }
    
    public long Points { get; set; }
    
    public OpType OpType { get; set; }//0:Added 1:updateTree 2:claim activity
    public long OpTime { get; set; }
    
    //extend fields
    public PointsType PointsType { get; set; } //optype=Added, 0:normalone 1:normaltwo 2:invite
    public string ActivityId { get; set; } // optype = claim activity
    public string TreeLevel { get; set; } // optype = updateTree
    
    [Keyword]
    public string ChainId { get; set; }

    [Keyword]
    public string BlockHash { get; set; }

    public long BlockHeight { get; set; }

    [Keyword]
    public string PreviousBlockHash { get; set; }

    public bool IsDeleted { get; set; }
}

public enum OpType
{
    Added = 0,
    UpdateTree = 1,
    Claim = 2
}

public enum PointsType
{
    NormalOne = 0,
    NormalTwo = 1,
    Invite = 2,
    Default = 3
}

