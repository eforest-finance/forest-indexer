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
        CreateMap<DropCreated, NFTDropIndex>()
            .ForMember(destination => destination.Id,
                opt => opt.MapFrom(source => source.DropId.ToString()))
            .ForMember(destination => destination.StartTime,
                opt => opt.MapFrom(source => source.StartTime.ToDateTime()))
            .ForMember(destination => destination.ExpireTime,
                opt => opt.MapFrom(source => source.ExpireTime.ToDateTime()))
            .ForMember(destination => destination.CreateTime,
                opt => opt.MapFrom(source => source.CreateTime.ToDateTime()))
            .ForMember(destination => destination.UpdateTime,
                opt => opt.MapFrom(source => source.UpdateTime.ToDateTime()))
            .Ignore(destination => destination.ClaimPrice);
        CreateMap<NFTDropIndex, NFTDropInfoDto>()
            .ForMember(destination => destination.DropId,
                opt => opt.MapFrom(source => source.Id));
        
        CreateMap<LogEventContext, NFTDropClaimIndex>();
        CreateMap<DropClaimAdded, NFTDropClaimIndex>()
            .ForMember(destination => destination.ClaimLimit,
                opt => opt.MapFrom(source => source.TotalAmount))
            .ForMember(destination => destination.ClaimAmount,
                opt => opt.MapFrom(source => source.CurrentAmount));
        CreateMap<NFTDropClaimIndex, NFTDropClaimDto>();
    }
}