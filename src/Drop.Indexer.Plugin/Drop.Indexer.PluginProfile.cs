using AeFinder.Sdk.Processor;
using Drop.Indexer.Plugin.Entities;
using Drop.Indexer.Plugin.GraphQL;
using AutoMapper;

namespace Drop.Indexer.Plugin;

public class DropIndexerPluginProfile : Profile
{
    public DropIndexerPluginProfile()
    {
        CreateMap<LogEventContext, NFTDropIndex>();
        CreateMap<NFTDropIndex, NFTDropInfoDto>()
            .ForMember(destination => destination.DropId,
                opt => opt.MapFrom(source => source.Id));
        
        CreateMap<LogEventContext, NFTDropClaimIndex>();
        CreateMap<NFTDropClaimIndex, NFTDropClaimDto>();
    }
}