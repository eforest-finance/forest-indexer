using Forest.Indexer.Plugin.enums;
using JetBrains.Annotations;
using Volo.Abp.Application.Dtos;

namespace Forest.Indexer.Plugin.GraphQL;

public class MySeedInput : PagedResultRequestDto
{
    [CanBeNull] public string ChainId { get; set; }
    public List<string> AddressList { get; set; }
    public TokenType? TokenType { get; set; }
    public SeedStatus? Status { get; set; }
}