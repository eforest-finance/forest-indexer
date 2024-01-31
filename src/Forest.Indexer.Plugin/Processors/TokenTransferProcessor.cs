using AElf.Contracts.MultiToken;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Contracts.Auction;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Processors.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class TokenTransferProcessor : AElfLogEventProcessorBase<Transferred, LogEventInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;

    private readonly IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo> _nftActivityIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> _nftInfoIndexRepository;
    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly INFTListingInfoProvider _listingInfoProvider;
    private readonly IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;
    private readonly ICollectionChangeProvider _collectionChangeProvider;
    private readonly INFTOfferProvider _nftOfferProvider;
    private readonly INFTInfoProvider _nftInfoProvider;
    private readonly INFTOfferChangeProvider _nftOfferChangeProvider;
    private readonly ILogger<AElfLogEventProcessorBase<Transferred, LogEventInfo>> _logger;
    private readonly INFTListingChangeProvider _listingChangeProvider;

    public TokenTransferProcessor(ILogger<AElfLogEventProcessorBase<Transferred, LogEventInfo>> logger,
        IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo> nftActivityIndexRepository,
        IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> nftInfoIndexRepository,
        IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> seedSymbolIndexRepository,
        IUserBalanceProvider userBalanceProvider,
        INFTListingInfoProvider listingInfoProvider,
        ICollectionChangeProvider collectionChangeProvider,
        INFTOfferProvider nftOfferProvider,
        INFTInfoProvider nftInfoProvider,
        INFTOfferChangeProvider nftOfferChangeProvider,
        INFTListingChangeProvider listingChangeProvider,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions) : base(logger)
    {
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _nftActivityIndexRepository = nftActivityIndexRepository;
        _nftInfoIndexRepository = nftInfoIndexRepository;
        _userBalanceProvider = userBalanceProvider;
        _listingInfoProvider = listingInfoProvider;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _collectionChangeProvider = collectionChangeProvider;
        _nftOfferProvider = nftOfferProvider;
        _nftInfoProvider = nftInfoProvider;
        _nftOfferChangeProvider = nftOfferChangeProvider;
        _logger = logger;
        _listingChangeProvider = listingChangeProvider;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).TokenContractAddress;
    }

    protected override async Task HandleEventAsync(Transferred eventValue, LogEventContext context)
    {
        _logger.Debug("TokenTransferProcessor-1"+JsonConvert.SerializeObject
            (eventValue));
        _logger.Debug("TokenTransferProcessor-2"+JsonConvert.SerializeObject(context));
        if (eventValue == null) return;
        if (context == null) return;
        await UpdateUserFromBalanceAsync(eventValue, context);
        await UpdateUserToBalanceAsync(eventValue, context);
        if(SymbolHelper.CheckSymbolIsELF(eventValue.Symbol)) return;
        await _collectionChangeProvider.SaveCollectionChangeIndexAsync(context, eventValue.Symbol);
        if (SymbolHelper.CheckSymbolIsSeedCollection(eventValue.Symbol)) return;
        if (SymbolHelper.CheckSymbolIsSeedSymbol(eventValue.Symbol))
        {
            _logger.Debug("TokenTransferProcessor-3"+JsonConvert.SerializeObject
                (eventValue));
            await HandleForSeedSymbolTransferAsync(eventValue, context);
            return;
        }

        if (SymbolHelper.CheckSymbolIsNoMainChainNFT(eventValue.Symbol, context.ChainId))
        {
            await HandleForNFTTransferAsync(eventValue, context);
        }
    }
    private async Task HandleForSeedSymbolTransferAsync(Transferred eventValue, LogEventContext context)
    {
        await DoHandleForSeedSymbolTransferAsync(eventValue, context);
    }

    private async Task DoHandleForSeedSymbolTransferAsync(Transferred eventValue, LogEventContext context)
    {
        _logger.Debug("TokenTransferProcessor-4"+JsonConvert.SerializeObject
            (eventValue));
        _logger.Debug("TokenTransferProcessor-5"+JsonConvert.SerializeObject
            (context));
        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        var seedSymbol = await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolId, context.ChainId);
        
        if (seedSymbol == null) return;
        if (seedSymbol.IsDeleted) return;
        _logger.Debug("TokenTransferProcessor-8"+JsonConvert.SerializeObject
            (seedSymbol));
        
        if(!CheckIssueToIsContractAddress(eventValue.To.ToBase58()))
        {
            _logger.Debug("TokenTransferProcessor-9");
            seedSymbol.IssuerTo = eventValue.To.ToBase58();
            _logger.Debug("TokenTransferProcessor-10");
        }
        //add calc minNftListing
        var minNftListing = await _nftInfoProvider.GetMinListingNftAsync(seedSymbolId);
        seedSymbol.OfMinNftListingInfo(minNftListing);
        
        _objectMapper.Map(context, seedSymbol);
        await _seedSymbolIndexRepository.AddOrUpdateAsync(seedSymbol);
        await _listingChangeProvider.SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
        await SaveNftActivityIndexAsync(eventValue, context, seedSymbol.Id);
    }

    private async Task HandleForNFTTransferAsync(Transferred eventValue, LogEventContext context)
    {
        var nftInfoIndexId = IdGenerateHelper.GetNFTInfoId(context.ChainId, eventValue.Symbol);
        var nftInfoIndex = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftInfoIndexId, context.ChainId);
        if (nftInfoIndex == null) return;

        //add calc minNftListing
        var minNftListing = await _nftInfoProvider.GetMinListingNftAsync(nftInfoIndex.Id);
        nftInfoIndex.OfMinNftListingInfo(minNftListing);
        _objectMapper.Map(context, nftInfoIndex);
        await _nftInfoIndexRepository.AddOrUpdateAsync(nftInfoIndex);
        await _listingChangeProvider.SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
        await SaveNftActivityIndexAsync(eventValue, context, nftInfoIndex.Id);
    }

    private async Task SaveNftActivityIndexAsync(Transferred eventValue, LogEventContext context, string bizId)
    {
        var nftActivityIndexId = IdGenerateHelper.GetNftActivityId(context.ChainId, eventValue.Symbol,
            eventValue.From.ToBase58(),
            eventValue.To.ToBase58(), context.TransactionId);
        var checkNftActivityIndex =
            await _nftActivityIndexRepository.GetFromBlockStateSetAsync(nftActivityIndexId, context.ChainId);
        if (checkNftActivityIndex != null) return;
        
        NFTActivityIndex nftActivityIndex = new NFTActivityIndex
        {
            Id = nftActivityIndexId,
            Type = NFTActivityType.Transfer,
            Amount = eventValue.Amount,
            TransactionHash = context.TransactionId,
            Timestamp = context.BlockTime,
            NftInfoId = bizId
        };
        _objectMapper.Map(context, nftActivityIndex);
        nftActivityIndex.From =
            CheckIssueToIsContractAddress(eventValue.From.ToBase58())
                ? SymbolHelper.FullAddress(context.ChainId, eventValue.From.ToBase58())
                : eventValue.From.ToBase58();
        nftActivityIndex.To = CheckIssueToIsContractAddress(eventValue.To.ToBase58())
            ? SymbolHelper.FullAddress(context.ChainId, eventValue.To.ToBase58())
            : eventValue.To.ToBase58();
        await _nftActivityIndexRepository.AddOrUpdateAsync(nftActivityIndex);
    }

    private async Task UpdateUserFromBalanceAsync(Transferred eventValue, LogEventContext context)
    {
        var offerNum =
            await _nftOfferProvider.GetOfferNumAsync(eventValue.Symbol, eventValue.From.ToBase58(), context.ChainId);
        if (offerNum == 0 && SymbolHelper.CheckSymbolIsELF(eventValue.Symbol))
        {
            return;
        }

        var fromUserBalance = await _userBalanceProvider.SaveUserBalanceAsync(eventValue.Symbol,
            eventValue.From.ToBase58(),
            -eventValue.Amount, context);
        await _nftOfferProvider.UpdateOfferRealQualityAsync(eventValue.Symbol, fromUserBalance,
            eventValue.From.ToBase58(),
            context);
        await _listingInfoProvider.UpdateListingInfoRealQualityAsync(eventValue.Symbol, fromUserBalance,
            eventValue.From.ToBase58(), context);
    }

    private async Task UpdateUserToBalanceAsync(Transferred eventValue, LogEventContext context)
    {
        var offerNum =
            await _nftOfferProvider.GetOfferNumAsync(eventValue.Symbol, eventValue.To.ToBase58(), context.ChainId);
        if (offerNum == 0 && SymbolHelper.CheckSymbolIsELF(eventValue.Symbol))
        {
            return;
        }
        var nftInfoIndexId = IdGenerateHelper.GetNFTInfoId(context.ChainId, eventValue.Symbol);
        var userBalanceToId =
            IdGenerateHelper.GetUserBalanceId(eventValue.To.ToBase58(), context.ChainId, nftInfoIndexId);
        var userBalanceTo = await _userBalanceProvider.QueryUserBalanceByIdAsync(userBalanceToId, context.ChainId);
        if (userBalanceTo == null)
        {
            var lastNFTListingInfoDic =
                await _listingInfoProvider.QueryLatestNFTListingInfoByNFTIdsAsync(new List<string> { nftInfoIndexId },
                    "");
            var lastNFTListingInfo = lastNFTListingInfoDic != null && lastNFTListingInfoDic.ContainsKey(nftInfoIndexId)
                ? lastNFTListingInfoDic[nftInfoIndexId]
                : new NFTListingInfoIndex();
            userBalanceTo = new UserBalanceIndex
            {
                Id = userBalanceToId,
                ChainId = context.ChainId,
                NFTInfoId = nftInfoIndexId,
                Symbol = eventValue.Symbol,
                Address = eventValue.To.ToBase58(),
                Amount = eventValue.Amount,
                ChangeTime = context.BlockTime,
                ListingPrice = lastNFTListingInfo.Prices,
                ListingTime = lastNFTListingInfo.StartTime
            };
        }
        else
        {
            userBalanceTo.Amount += eventValue.Amount;
            userBalanceTo.ChangeTime = context.BlockTime;
        }

        _objectMapper.Map(context, userBalanceTo);
        await _userBalanceProvider.UpdateUserBalanceAsync(userBalanceTo);
        await _nftOfferProvider.UpdateOfferRealQualityAsync(eventValue.Symbol, userBalanceTo.Amount,
            eventValue.To.ToBase58(), context);
        await _listingInfoProvider.UpdateListingInfoRealQualityAsync(eventValue.Symbol, userBalanceTo.Amount, eventValue.To.ToBase58(), context);
        await _nftOfferChangeProvider.SaveNFTOfferChangeIndexAsync(context, eventValue.Symbol, EventType.Other);
    }

    private bool CheckIssueToIsContractAddress(string issuerToAddress)
    {
        return _contractInfoOptions.ContractInfos.First().TokenContractAddress.Equals(issuerToAddress) ||
               _contractInfoOptions.ContractInfos.First().TokenAdaptorContractAddress.Equals(issuerToAddress) ||
               _contractInfoOptions.ContractInfos.Last().TokenContractAddress.Equals(issuerToAddress)||_contractInfoOptions.ContractInfos.Last().AuctionContractAddress.Equals(issuerToAddress);
    }
}