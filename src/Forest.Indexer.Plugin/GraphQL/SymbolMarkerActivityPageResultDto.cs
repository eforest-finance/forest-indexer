using Forest.Indexer.Plugin.Entities;

namespace Forest.Indexer.Plugin.GraphQL;

public class SymbolMarkerActivityPageResultDto
{
    public long TotalRecordCount { get; set; }

    public List<SymbolMarkerActivityDto> Data { get; set; }
}

public class SymbolMarkerActivityDto
{
    public DateTime TransactionDateTime { get; set; }
    public string Symbol { get; set; }
    public SymbolMarketActivityType Type { get; set; }
    public decimal Price { get; set; }
    public string PriceSymbol { get; set; }
    public decimal TransactionFee { get; set; }
    public string TransactionFeeSymbol { get; set; }
    public string TransactionId { get; set; }
}