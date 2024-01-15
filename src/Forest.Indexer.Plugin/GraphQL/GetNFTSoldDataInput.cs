using Volo.Abp.Application.Dtos;

namespace Forest.Indexer.Plugin.GraphQL
{
    public class GetNFTSoldDataInput
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}