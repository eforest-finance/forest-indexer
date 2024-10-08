using AeFinder.Sdk.Processor;
using Drop.Indexer.Plugin.Entities;
using Drop.Indexer.Plugin.GraphQL;
using AutoMapper;

namespace Drop.Indexer.Plugin;

public class DropIndexerPluginAutoMapperProfile : Profile
{
    public DropIndexerPluginAutoMapperProfile()
    {
        CreateMap<LogEventContext, NFTDropIndex>();
        CreateMap<NFTDropIndex, NFTDropInfoDto>()
            .ForMember(destination => destination.DropId,
                opt => opt.MapFrom(source => source.Id));
        
        CreateMap<LogEventContext, NFTDropClaimIndex>();
        CreateMap<NFTDropClaimIndex, NFTDropClaimDto>();
    }
}