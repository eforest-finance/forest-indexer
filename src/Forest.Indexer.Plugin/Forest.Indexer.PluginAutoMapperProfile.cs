using AeFinder.Sdk.Processor;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ProxyAccountContract;
using Forest.Indexer.Plugin.Entities;
using AutoMapper;
using Forest.Indexer.Plugin.GraphQL;
using Forest.Indexer.Plugin.Processors;
using Volo.Abp.AutoMapper;

namespace Forest.Indexer.Plugin;

public class ForestIndexerAutoMapperProfile : Profile
{
    public ForestIndexerAutoMapperProfile()
    {
        CreateMap<MyEntity, MyEntityDto>();
        CreateMap<SymbolMarketActivityIndex,SymbolMarkerActivityDto>();
        CreateMap<CollectionIndex,NFTCollectionDto>();
        
        CreateMap<TokenInfo, TokenInfoIndex>();
        CreateMap<SymbolMarketActivityIndex,SymbolMarkerActivityDto>();
        CreateMap<NFTListingChangeIndex,NFTListingChangeDto>();
        CreateMap<SeedSymbolIndex, SeedSymbolIndex>();
        CreateMap<LogEventContext, OfferInfoIndex>();
        CreateMap<LogEventContext, SeedMainChainChangeIndex>();
        CreateMap<LogEventContext, NFTListingChangeIndex>();
        CreateMap<LogEventContext, UserBalanceIndex>();
        CreateMap<LogEventContext, NFTMarketDayIndex>();
        CreateMap<LogEventContext, NFTMarketWeekIndex>();

        CreateMap<OfferAdded, OfferInfoIndex>()
            .ForMember(destination => destination.OfferFrom,
                opt => opt.MapFrom(source => source.OfferFrom.ToBase58()))
            .ForMember(destination => destination.OfferTo,
                opt => opt.MapFrom(source => source.OfferTo.ToBase58()))
            .ForMember(destination => destination.ExpireTime,
                opt => opt.MapFrom(source => source.ExpireTime.ToDateTime()))
            .Ignore(o => o.Price);

        CreateMap<OfferChanged, OfferInfoIndex>()
            .ForMember(destination => destination.OfferFrom,
                opt => opt.MapFrom(source => source.OfferFrom.ToBase58()))
            .ForMember(destination => destination.OfferTo,
                opt => opt.MapFrom(source => source.OfferTo.ToBase58()))
            .ForMember(destination => destination.ExpireTime,
                opt => opt.MapFrom(source => source.ExpireTime.ToDateTime()))
            .Ignore(o => o.Price);

        CreateMap<LogEventContext, WhitelistIndex>();
        CreateMap<LogEventContext, NFTActivityIndex>();
        CreateMap<LogEventContext, UserBalanceIndex>();
        CreateMap<SeedMainChainChangeIndex,SeedMainChainChangeDto>();

        CreateMap<OfferInfoIndex, NFTOfferDto>()
            .ForMember(des => des.From, opt
                => opt.MapFrom(source => source.OfferFrom))
            .ForMember(des => des.To, opt
                => opt.MapFrom(source => source.OfferTo))
            .ForMember(des => des.ExpireTime, opt
                => opt.MapFrom(source => source.ExpireTime));
        CreateMap<TokenInfoIndex, TokenInfoDto>();

        CreateMap<LogEventContext, NFTActivityIndex>();
        CreateMap<NFTActivityIndex, NFTActivityDto>().ForMember(destination => destination.NftInfoId,
            opt => opt.MapFrom(source => source.NftInfoId));
        CreateMap<LogEventContext, NFTInfoIndex>();
        CreateMap<LogEventContext, CollectionIndex>();
        CreateMap<LogEventContext, TokenInfoIndex>();
        CreateMap<LogEventContext, NFTMarketInfoIndex>();
        CreateMap<LogEventContext, SoldIndex>();

        CreateMap<LogEventContext, SeedSymbolIndex>();
        CreateMap<LogEventContext, WhiteListManagerIndex>();
        CreateMap<LogEventContext, WhiteListExtraInfoIndex>();
        CreateMap<TokenCreated, NFTInfoIndex>();
        CreateMap<TokenCreated, SeedSymbolIndex>();
        CreateMap<TokenCreated, CollectionIndex>();
        CreateMap<TokenCreated, TokenInfoIndex>();

        CreateMap<LogEventContext, NFTListingInfoIndex>();

        CreateMap<ListedNFTAdded, NFTListingInfoIndex>()
            .ForMember(des => des.Owner, opt
                => opt.MapFrom(source => source.Owner.ToBase58()))
            .ForMember(des => des.WhitelistId, opt
                => opt.MapFrom(source => source.WhitelistId.ToHex()));
        CreateMap<ListedNFTRemoved, NFTListingInfoIndex>()
            .ForMember(des => des.Owner, opt
                => opt.MapFrom(source => source.Owner.ToBase58()));

        CreateMap<ListedNFTChanged, NFTListingInfoIndex>()
            .ForMember(des => des.Owner, opt
                => opt.MapFrom(source => source.Owner.ToBase58()));
        
        CreateMap<NFTInfoIndex, NFTInfoDto>().ForMember(
                destination => destination.CreatorAddress,
                opt => opt.MapFrom(source => source.RandomIssueManager))
            .ForMember(
                destination => destination.Issuer,
                opt => opt.MapFrom(source => source.RandomIssueManager))
            .ForMember(
            destination => destination.ProxyIssuerAddress,
            opt => opt.MapFrom(source => source.Issuer));
        
        CreateMap<SeedSymbolIndex, SeedSymbolDto>();
        CreateMap<SeedSymbolIndex, SeedInfoProfileDto>();

        CreateMap<CollectionIndex, NFTCollectionDto>().ForMember(
            destination => destination.CreatorAddress,
            opt => opt.MapFrom(source => source.RandomOwnerManager)).ForMember(
            destination => destination.ProxyOwnerAddress,
            opt => opt.MapFrom(source => source.Owner)).ForMember(
            destination => destination.ProxyIssuerAddress,
            opt => opt.MapFrom(source => source.Issuer));
        
        CreateMap<ExternalInfoDictionary, ExternalInfoDictionaryDto>();
        
        CreateMap<NFTMarketInfoIndex, NFTInfoMarketDataDto>().ForMember(
            destination => destination.Timestamp,
            opt => opt.MapFrom(source =>
                DateTimeHelper.ToUnixTimeMilliseconds(source.Timestamp)));
        CreateMap<NFTMarketDayIndex, NFTInfoMarketDataDto>().ForMember(
            destination => destination.Timestamp,
            opt => opt.MapFrom(source =>
                DateTimeHelper.ToUnixTimeMilliseconds(source.DayBegin)))
            .ForMember(
                destination => destination.Price,
                opt => opt.MapFrom(source =>
                    source.AveragePrice));

        CreateMap<NFTListingInfoIndex, NFTListingInfoDto>()
            .ForMember(des => des.BusinessId, opt
                => opt.MapFrom(source => source.NftInfoId))
            ;
        CreateMap<Sold, SoldIndex>()
            .ForMember(des => des.NftFrom, opt
                => opt.MapFrom(source => source.NftFrom.ToBase58()))
            .ForMember(des => des.NftTo, opt
                => opt.MapFrom(source => source.NftTo.ToBase58()));

        CreateMap<NFTListingInfoIndex, NFTInfoIndex>()
            .Ignore(destination => destination.Id)
            .ForMember(destination => destination.ListingId, opt
                => opt.MapFrom(source => source.Id))
            .ForMember(destination => destination.ListingAddress, opt
                => opt.MapFrom(source => source.Owner))
            .ForMember(destination => destination.ListingPrice, opt
                => opt.MapFrom(source => source.Prices))
            .ForMember(destination => destination.ListingEndTime, opt
                => opt.MapFrom(source => source.ExpireTime))
            .ForMember(destination => destination.ListingQuantity, opt
                => opt.MapFrom(source => source.Quantity)
            );

        
        // white list query

        // agent
        CreateMap<LogEventContext, ProxyAccountIndex>();
        
        CreateMap<LogEventContext, SymbolMarketActivityIndex>();
        CreateMap<LogEventContext, SeedSymbolMarketTokenIndex>()
            .ForMember(d=>d.CreateTime,opt=>opt.MapFrom(o=>o.Block.BlockTime));
        CreateMap<SeedSymbolMarketTokenIndex, SymbolMarkerTokenDto>()
            .ForMember(d => d.IssueManagerList,
                opt => opt.MapFrom(d =>
                    d.IssueManagerSet == null ? new List<string>() : d.IssueManagerSet.ToList()));
        CreateMap<LogEventContext, TsmSeedSymbolIndex>()
            .ForMember(dest => dest.Status, opt => opt.Ignore());
        CreateMap<LogEventContext, SeedPriceIndex>();
        CreateMap<LogEventContext, UniqueSeedPriceIndex>();
        CreateMap<TsmSeedSymbolIndex, SeedInfoDto>();
        CreateMap<LogEventContext, SymbolAuctionInfoIndex>();
        CreateMap<LogEventContext, SymbolBidInfoIndex>();
        CreateMap<SymbolAuctionInfoIndex, SymbolAuctionInfoDto>();
        CreateMap<SymbolBidInfoIndex, SymbolBidInfoDto>();
        CreateMap<SeedSymbolIndex, NFTInfoDto>()
            .ForMember(d => d.CollectionId, opt => opt.MapFrom(d => d.ChainId + "-SEED-0"))
            .ForMember(d => d.CollectionSymbol, opt => opt.MapFrom(d => "SEED-0"))
            .ForMember(d => d.CollectionName, opt => opt.MapFrom(d => "SEED-0"));
        CreateMap<LogEventContext, CollectionChangeIndex>();
        CreateMap<LogEventContext, CollectionPriceChangeIndex>();
        CreateMap<CollectionChangeIndex, CollectionChangeDto>();
        CreateMap<CollectionPriceChangeIndex, CollectionPriceChangeDto>();
        CreateMap<SeedPriceIndex, SeedPriceDto>();
        CreateMap<UniqueSeedPriceIndex, UniqueSeedPriceDto>();
        CreateMap<NFTInfoIndex, NFTInfoSyncDto>();
        CreateMap<SeedSymbolIndex, SeedSymbolSyncDto>();
        CreateMap<NFTListingInfoIndex, NFTListingInfoResult>();
        CreateMap<SeedSymbolMarketTokenIndex, SymbolMarketTokenExistDto>();
        CreateMap<SoldIndex, NftDealInfoDto>();
        CreateMap<LogEventContext, NFTOfferChangeIndex>();
        CreateMap<NFTOfferChangeIndex, NFTOfferChangeDto>();
        CreateMap<UserBalanceIndex, NFTOwnerInfoDto>();
        CreateMap<LogEventContext, UserNFTOfferNumIndex>();
        CreateMap<UserBalanceIndex, UserBalanceDto>();
        CreateMap<TreePointsChangeRecordIndex, TreePointsChangeRecordDto>();
        CreateMap<TreePointsChangeRecordDto, TreePointsChangeRecordIndex>();
    }
}