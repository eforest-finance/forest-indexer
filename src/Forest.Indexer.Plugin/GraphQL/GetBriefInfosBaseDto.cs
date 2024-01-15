using Forest.Indexer.Plugin.enums;
using JetBrains.Annotations;
using Volo.Abp.Application.Dtos;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetBriefInfosBaseDto : PagedResultRequestDto
{
    public string Sorting { get; set; }
    public bool HasListingFlag { get; set; }
    public bool HasAuctionFlag { get; set; }
    public bool HasOfferFlag { get; set; }
    [CanBeNull] public string SearchParam { get; set; }
    public List<string> ChainList { get; set; }
    public List<TokenType> SymbolTypeList { get; set; }
    public decimal? PriceLow { get; set; }
    public decimal? PriceHigh { get; set; }
}

public class GetSeedBriefInfosDto : GetBriefInfosBaseDto
{
}
public class GetNFTBriefInfosDto : GetBriefInfosBaseDto
{
    public string CollectionId { get; set; }
}