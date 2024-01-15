using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class TokenPriceInfo
{
    [Keyword] public string Symbol { get; set; }
    public decimal Amount { get; set; }
}