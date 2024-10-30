namespace Forest.Indexer.Plugin.GraphQL;

public class NftListingPageResultDto : PageResult<NFTListingInfoDto>
{
    public NftListingPageResultDto(long total, List<NFTListingInfoDto> data, string msg) : base(total, data, msg)
    {
    }
    
    public NftListingPageResultDto(long total, List<NFTListingInfoDto> data) : base(total, data)
    {
    }

    public NftListingPageResultDto(string msg) : base(msg)
    {
    }
}