using AElfIndexer.Client.Handlers;
using AutoMapper;
using Drop.Indexer.Plugin.GraphQL;
using Drop.Indexer.Plugin.Entities;

namespace Drop.Indexer.Plugin;

public class DropIndexerClientAutoMapperProfile : Profile
{
    public DropIndexerClientAutoMapperProfile()
    {
        CreateMap<LogEventContext, NFTDropIndex>();
        CreateMap<NFTDropIndex, NFTDropInfoDto>()
            .ForMember(destination => destination.DropId,
                opt => opt.MapFrom(source => source.Id));
        
        CreateMap<LogEventContext, NFTDropClaimIndex>();
        CreateMap<NFTDropClaimIndex, NFTDropClaimDto>();
    }
}