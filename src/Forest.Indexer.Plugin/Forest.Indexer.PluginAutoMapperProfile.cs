using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.GraphQL;
using AutoMapper;

namespace Forest.Indexer.Plugin;

public class ForestIndexerAutoMapperProfile : Profile
{
    public ForestIndexerAutoMapperProfile()
    {
        CreateMap<MyEntity, MyEntityDto>();
        CreateMap<SymbolMarketActivityIndex,SymbolMarkerActivityDto>();
        CreateMap<CollectionIndex,NFTCollectionDto>();


    }
}