using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.GraphQLL;
using JetBrains.Annotations;

namespace Forest.Indexer.Plugin.GraphQL;

public class MySeedInput : PagedResultRequestDto
{
    public string? ChainId { get; set; }
    public List<string> AddressList { get; set; }
    public TokenType? TokenType { get; set; }
    public SeedStatus? Status { get; set; }
}