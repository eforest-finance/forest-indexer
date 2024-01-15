using Forest.Indexer.Plugin.enums;

namespace Forest.Indexer.Plugin.GraphQL;

public class MySeedDto
{
    public long TotalRecordCount { get; set; }

    public List<SeedListDto> Data { get; set; }
    
   
}

public class SeedListDto
{
    public string ChainId { get; set; }

    public string Id { get; set; }

    public string Symbol { get; set; }

    public string SeedSymbol { get; set; }

    public string SeedName { get; set; }

    public SeedStatus Status { get; set; }

    public long ExpireTime { get; set; }

    public TokenType TokenType { get; set; }

    public string SeedImage { get; set; }
    
    public string IssuerTo { get; set; }
}