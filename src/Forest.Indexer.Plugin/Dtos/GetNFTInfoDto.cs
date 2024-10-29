using JetBrains.Annotations;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetNFTInfoDto
{
    public string Id { get; set; }
    
    public string? Address { get; set; }
}