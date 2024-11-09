using Forest.Indexer.Plugin.GraphQLL;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetUserBalanceDto
{
    public string nftInfoId { get; set; }
}

public class GetNFTOwnersDto: PagedResultRequestDto
{
    public string NftInfoId { get; set; }
    public string ChainId { get; set; }
}

