using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.enums;

namespace Forest.Indexer.Plugin.GraphQL;

public class SeedDetailDto
{
    public string Id { get; set; }

    public string Symbol { get; set; }

    public string SeedSymbol { get; set; }

    public string SeedName { get; set; }

    public SeedStatus Status { get; set; }

    public long RegisterTime { get; set; }

    public long ExpireTime { get; set; }

    public string TokenType { get; set; }

    public SeedType SeedType { get; set; }

    public string BuyerAddress { get; set; }

    public string Memo { get; set; }

    public TokenDto PriceToken { get; set; }
}