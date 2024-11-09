using JetBrains.Annotations;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetSyncNFTInfoRecordDto
{
    public string Id { get; set; }
    
    public string ChainId { get; set; }
}

public class GetSyncSeedSymbolRecordDto
{
    public string Id { get; set; }
    
    public string ChainId { get; set; }
}