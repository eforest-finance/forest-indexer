using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.GraphQLL;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetActivitiesInput : PagedResultQueryDtoBase
{
    public List<string> Address { get; set; }
    public List<SymbolMarketActivityType> Types { get; set; }
}