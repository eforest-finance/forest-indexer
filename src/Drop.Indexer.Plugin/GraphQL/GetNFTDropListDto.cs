
namespace Drop.Indexer.Plugin.GraphQL;

public class GetNFTDropListDto
{
    public SearchType Type { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; }

}

public enum SearchType
{
    All,
    Ongoing,
    YetToBegin,
    Finished
}