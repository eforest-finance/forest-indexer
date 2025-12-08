using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.GraphQLL;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetSpecialSeedsInput: PagedResultRequestDto
{
    public bool IsApplyNow { get; set; }
    public bool LiveAuction { get; set; }
    public List<string> ChainIds { get; set; }
    public int SymbolLengthMin { get; set; }
    public int SymbolLengthMax { get; set; }
    public long PriceMin { get; set; }
    public long PriceMax { get; set; }
    public List<TokenType> TokenTypes { get; set; }
    public List<SeedType> SeedTypes { get; set; }
}