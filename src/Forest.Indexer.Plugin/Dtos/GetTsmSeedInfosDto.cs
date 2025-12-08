namespace Forest.Indexer.Plugin.GraphQL;

public class GetTsmSeedInfosDto
{
    public List<string> SeedSymbols { get; set; }
    public string? ChainId { get; set; }

}