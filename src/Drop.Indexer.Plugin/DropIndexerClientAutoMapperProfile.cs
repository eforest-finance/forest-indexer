using AElfIndexer.Client.Handlers;
using AutoMapper;
using Drop.Indexer.Plugin.GraphQL;
using Forest.Contracts.Drop;
using Drop.Indexer.Plugin.Entities;
using Volo.Abp.AutoMapper;

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