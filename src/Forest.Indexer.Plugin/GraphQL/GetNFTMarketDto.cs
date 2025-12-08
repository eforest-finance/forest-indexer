
using Forest.Indexer.Plugin.GraphQLL;

namespace Forest.Indexer.Plugin.GraphQL
{
    public class GetNFTMarketDto: PagedResultRequestDto
    {
        public String NFTInfoId { get; set; }
        public long TimestampMin { get; set; }
        public long TimestampMax { get; set; }
    }
}