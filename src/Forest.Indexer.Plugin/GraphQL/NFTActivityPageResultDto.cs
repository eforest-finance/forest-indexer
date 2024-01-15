using NFTMarketServer.NFT;

namespace Forest.Indexer.Plugin.GraphQL;

public class NFTActivityPageResultDto
{
    public long TotalRecordCount { get; set; }
    
    public List<NFTActivityDto> Data { get; set; }
}