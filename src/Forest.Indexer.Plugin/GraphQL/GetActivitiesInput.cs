using Forest.Indexer.Plugin.Entities;
using Volo.Abp.Application.Dtos;

namespace Forest.Indexer.Plugin.GraphQL;

public class GetActivitiesInput : PagedResultRequestDto
{
    public List<string> Address { get; set; }
    public List<SymbolMarketActivityType> Types { get; set; }
}