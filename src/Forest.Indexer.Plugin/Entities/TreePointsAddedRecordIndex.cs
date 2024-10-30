using AeFinder.Sdk.Entities;
using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.enums;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class TreePointsAddedRecordIndex : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }
    
    [Keyword] public string Address { get; set; }

    [Keyword] public long TotalPoints { get; set; }
    
    [Keyword] public long Points { get; set; }
    
    [Keyword] public int PointsType { get; set; }

    public long OpTime { get; set; }
}