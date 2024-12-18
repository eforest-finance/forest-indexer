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
        CreateMap<LogEventContext, OfferInfoIndex>()
            .ForMember(destination => destination.BlockHeight,
                opt => opt.MapFrom(source => source.Block.BlockHeight))
            .ForMember(destination => destination.CreateTime,
                opt => opt.MapFrom(source => source.Block.BlockTime));
        CreateMap<LogEventContext, SeedMainChainChangeIndex>()
            .ForMember(destination => destination.BlockHeight,
                opt => opt.MapFrom(source => source.Block.BlockHeight))
            .ForMember(destination => destination.BlockHash,
                opt => opt.MapFrom(source => source.Block.BlockHash));
        CreateMap<LogEventContext, NFTListingChangeIndex>()
            .ForMember(destination => destination.BlockHeight,
                opt => opt.MapFrom(source => source.Block.BlockHeight))
            .ForMember(destination => destination.BlockHash,
                opt => opt.MapFrom(source => source.Block.BlockHash));
        CreateMap<LogEventContext, UserBalanceIndex>()
            .ForMember(destination => destination.BlockHeight,
                opt => opt.MapFrom(source => source.Block.BlockHeight))
            .ForMember(destination => destination.BlockHash,
                opt => opt.MapFrom(source => source.Block.BlockHash));
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
        CreateMap<LogEventContext, NFTActivityIndex>()
            .ForMember(destination => destination.BlockHeight,
                opt => opt.MapFrom(source => source.Block.BlockHeight))
            .ForMember(destination => destination.TransactionHash,
                opt => opt.MapFrom(source => source.Transaction.TransactionId));
        CreateMap<LogEventContext, UserBalanceIndex>()
            .ForMember(destination => destination.BlockHeight,
                opt => opt.MapFrom(source => source.Block.BlockHeight))
            .ForMember(destination => destination.BlockHash,
                opt => opt.MapFrom(source => source.Block.BlockHash));
        CreateMap<SeedMainChainChangeIndex, SeedMainChainChangeDto>();

        CreateMap<OfferInfoIndex, NFTOfferDto>()
            .ForMember(des => des.From, opt
                => opt.MapFrom(source => source.OfferFrom))
            .ForMember(des => des.To, opt
                => opt.MapFrom(source => source.OfferTo))
            .ForMember(des => des.ExpireTime, opt
                => opt.MapFrom(source => source.ExpireTime));
        CreateMap<TokenInfoIndex, TokenInfoDto>();
        
        CreateMap<NFTActivityIndex, NFTActivityDto>().ForMember(destination => destination.NftInfoId,
            opt => opt.MapFrom(source => source.NftInfoId));
        CreateMap<LogEventContext, NFTInfoIndex>()
            .ForMember(destination => destination.BlockHeight,
                opt => opt.MapFrom(source => source.Block.BlockHeight))
            .ForMember(destination => destination.CreateTime,
                opt => opt.MapFrom(source => source.Block.BlockTime))
            .ForMember(destination => destination.BlockHash,
                opt => opt.MapFrom(source => source.Block.BlockHash));
        CreateMap<LogEventContext, CollectionIndex>()
            .ForMember(destination => destination.CreateTime,
                opt => opt.MapFrom(source => source.Block.BlockTime));
        CreateMap<LogEventContext, TokenInfoIndex>()
            .ForMember(destination => destination.BlockHeight,
                opt => opt.MapFrom(source => source.Block.BlockHeight))
            .ForMember(destination => destination.CreateTime,
                opt => opt.MapFrom(source => source.Block.BlockTime))
            .ForMember(destination => destination.BlockHash,
                opt => opt.MapFrom(source => source.Block.BlockHash));
        CreateMap<LogEventContext, NFTMarketInfoIndex>();
        CreateMap<LogEventContext, SoldIndex>()
            .ForMember(destination => destination.BlockHeight,
                opt => opt.MapFrom(source => source.Block.BlockHeight))
            .ForMember(destination => destination.BlockHash,
                opt => opt.MapFrom(source => source.Block.BlockHash));

        CreateMap<LogEventContext, OwnedSymbolRelationIndex>()
            .ForMember(destination => destination.BlockHeight,
                opt => opt.MapFrom(source => source.Block.BlockHeight))
            .ForMember(destination => destination.CreateTime,
                opt => opt.MapFrom(source => source.Block.BlockTime))
            .ForMember(destination => destination.BlockHash,
                opt => opt.MapFrom(source => source.Block.BlockHash));
        CreateMap<LogEventContext, SeedSymbolIndex>()
            .ForMember(destination => destination.BlockHeight,
                opt => opt.MapFrom(source => source.Block.BlockHeight))
            .ForMember(destination => destination.CreateTime,
                opt => opt.MapFrom(source => source.Block.BlockTime))
            .ForMember(destination => destination.BlockHash,
                opt => opt.MapFrom(source => source.Block.BlockHash));
        CreateMap<LogEventContext, WhiteListManagerIndex>();
        CreateMap<LogEventContext, WhiteListExtraInfoIndex>();
        CreateMap<TokenCreated, NFTInfoIndex>()
            .ForMember(destination => destination.Issuer,
                opt => opt.MapFrom(source => source.Issuer.Value.Length != 0 ? source.Issuer.ToBase58() : ""))
            .ForMember(destination => destination.Owner,
                opt => opt.MapFrom(source => source.Owner.Value.Length != 0?source.Owner.ToBase58():""));
        CreateMap<TokenCreated, SeedSymbolIndex>()
            .ForMember(destination => destination.Issuer,
                opt => opt.MapFrom(source => source.Issuer.Value.Length != 0 ? source.Issuer.ToBase58() : ""))
            .ForMember(destination => destination.Owner,
                opt => opt.MapFrom(source => source.Owner.Value.Length != 0?source.Owner.ToBase58():""));
        CreateMap<TokenCreated, CollectionIndex>()
            .ForMember(destination => destination.Issuer,
                opt => opt.MapFrom(source => source.Issuer.Value.Length != 0 ? source.Issuer.ToBase58() : ""))
            .ForMember(destination => destination.Owner,
                opt => opt.MapFrom(source => source.Owner.Value.Length != 0?source.Owner.ToBase58():""));
        CreateMap<TokenCreated, TokenInfoIndex>()
            .ForMember(destination => destination.Issuer,
                opt => opt.MapFrom(source => source.Issuer.Value.Length != 0 ? source.Issuer.ToBase58() : ""))
            .ForMember(destination => destination.Owner,
                opt => opt.MapFrom(source => source.Owner.Value.Length != 0?source.Owner.ToBase58():""));

        CreateMap<LogEventContext, NFTListingInfoIndex>()
            .ForMember(destination => destination.BlockHeight,
                opt => opt.MapFrom(source => source.Block.BlockHeight)) 
            .ForMember(destination => destination.BlockHash,
                opt => opt.MapFrom(source => source.Block.BlockHash));

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
        CreateMap<LogEventContext, ProxyAccountIndex>()
            .ForMember(destination => destination.CreateTime,
                opt => opt.MapFrom(source => source.Block.BlockTime));
        
        CreateMap<LogEventContext, SymbolMarketActivityIndex>()
            .ForMember(destination => destination.TransactionId,
                opt => opt.MapFrom(source => source.Transaction.TransactionId));
        CreateMap<LogEventContext, SeedSymbolMarketTokenIndex>()
            .ForMember(destination => destination.CreateTime,
                opt => opt.MapFrom(source => source.Block.BlockTime))
            .ForMember(destination => destination.TransactionId,
                opt => opt.MapFrom(source => source.Transaction.TransactionId));
        CreateMap<SeedSymbolMarketTokenIndex, SymbolMarkerTokenDto>()
            .ForMember(d => d.IssueManagerList,
                opt => opt.MapFrom(d =>
                    d.IssueManagerSet == null ? new List<string>() : d.IssueManagerSet.ToList()));
        CreateMap<LogEventContext, TsmSeedSymbolIndex>()
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(destination => destination.BlockHeight,
                opt => opt.MapFrom(source => source.Block.BlockHeight))
            .ForMember(destination => destination.BlockHash,
                opt => opt.MapFrom(source => source.Block.BlockHash));
        CreateMap<LogEventContext, SeedPriceIndex>()
            .ForMember(destination => destination.BlockHeight,
                opt => opt.MapFrom(source => source.Block.BlockHeight))
            .ForMember(destination => destination.BlockHash,
                opt => opt.MapFrom(source => source.Block.BlockHash));
        CreateMap<LogEventContext, UniqueSeedPriceIndex>()
            .ForMember(destination => destination.BlockHeight,
                opt => opt.MapFrom(source => source.Block.BlockHeight))
            .ForMember(destination => destination.BlockHash,
                opt => opt.MapFrom(source => source.Block.BlockHash));
        CreateMap<TsmSeedSymbolIndex, SeedInfoDto>();
        CreateMap<LogEventContext, SymbolAuctionInfoIndex>()
            .ForMember(destination => destination.BlockHeight,
                opt => opt.MapFrom(source => source.Block.BlockHeight))
            .ForMember(destination => destination.BlockHash,
                opt => opt.MapFrom(source => source.Block.BlockHash))
            .ForMember(destination => destination.TransactionHash,
                opt => opt.MapFrom(source => source.Transaction.TransactionId));
        CreateMap<LogEventContext, SymbolBidInfoIndex>()
            .ForMember(destination => destination.BlockHeight,
                opt => opt.MapFrom(source => source.Block.BlockHeight))
            .ForMember(destination => destination.BlockHash,
                opt => opt.MapFrom(source => source.Block.BlockHash))
            .ForMember(destination => destination.TransactionHash,
                opt => opt.MapFrom(source => source.Transaction.TransactionId));
        CreateMap<SymbolAuctionInfoIndex, SymbolAuctionInfoDto>();
        CreateMap<SymbolBidInfoIndex, SymbolBidInfoDto>();
        CreateMap<SeedSymbolIndex, NFTInfoDto>()
            .ForMember(d => d.CollectionId, opt => opt.MapFrom(d => d.ChainId + "-SEED-0"))
            .ForMember(d => d.CollectionSymbol, opt => opt.MapFrom(d => "SEED-0"))
            .ForMember(d => d.CollectionName, opt => opt.MapFrom(d => "SEED-0"));
        CreateMap<LogEventContext, CollectionChangeIndex>()
            .ForMember(destination => destination.BlockHeight,
                opt => opt.MapFrom(source => source.Block.BlockHeight))
            .ForMember(destination => destination.BlockHash,
                opt => opt.MapFrom(source => source.Block.BlockHash));
        CreateMap<LogEventContext, CollectionPriceChangeIndex>()
            .ForMember(destination => destination.BlockHeight,
                opt => opt.MapFrom(source => source.Block.BlockHeight))
            .ForMember(destination => destination.BlockHash,
                opt => opt.MapFrom(source => source.Block.BlockHash));
        CreateMap<CollectionChangeIndex, CollectionChangeDto>();
        CreateMap<CollectionPriceChangeIndex, CollectionPriceChangeDto>();
        CreateMap<SeedPriceIndex, SeedPriceDto>();
        CreateMap<UniqueSeedPriceIndex, UniqueSeedPriceDto>();
        CreateMap<NFTInfoIndex, NFTInfoSyncDto>();
        CreateMap<SeedSymbolIndex, SeedSymbolSyncDto>();
        CreateMap<NFTListingInfoIndex, NFTListingInfoResult>();
        CreateMap<SeedSymbolMarketTokenIndex, SymbolMarketTokenExistDto>();
        CreateMap<SoldIndex, NftDealInfoDto>();
        CreateMap<LogEventContext, NFTOfferChangeIndex>()
            .ForMember(destination => destination.BlockHeight,
                opt => opt.MapFrom(source => source.Block.BlockHeight))
            .ForMember(destination => destination.CreateTime,
                opt => opt.MapFrom(source => source.Block.BlockTime))
            .ForMember(destination => destination.BlockHash,
                opt => opt.MapFrom(source => source.Block.BlockHash));
        CreateMap<NFTOfferChangeIndex, NFTOfferChangeDto>();
        CreateMap<UserBalanceIndex, NFTOwnerInfoDto>();
        CreateMap<LogEventContext, UserNFTOfferNumIndex>();
        CreateMap<UserBalanceIndex, UserBalanceDto>();
        CreateMap<TreePointsChangeRecordIndex, TreePointsChangeRecordDto>();
        CreateMap<TreePointsChangeRecordDto, TreePointsChangeRecordIndex>();
        CreateMap<LogEventContext, TreePointsChangeRecordIndex>()
            .ForMember(destination => destination.BlockHeight,
                opt => opt.MapFrom(source => source.Block.BlockHeight))
            .ForMember(destination => destination.BlockHash,
                opt => opt.MapFrom(source => source.Block.BlockHash));

    }
}