using Volo.Abp.Application.Dtos;

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

