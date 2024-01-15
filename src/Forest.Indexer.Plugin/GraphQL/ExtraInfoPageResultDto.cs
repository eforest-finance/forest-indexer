
namespace Forest.Indexer.Plugin.GraphQL;

public class ExtraInfoPageResultDto
{
    public long TotalCount { get; set; }

    public List<WhitelistExtraInfoIndexDto> Items { get; set; }
}