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
    private readonly CleanDataOptions _cleanDataOptions;

    private readonly IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo> _nftActivityIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> _nftInfoIndexRepository;
    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly INFTListingInfoProvider _listingInfoProvider;
    private readonly IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;
    private readonly ICollectionChangeProvider _collectionChangeProvider;
    private readonly INFTOfferProvider _nftOfferProvider;
    private readonly INFTInfoProvider _nftInfoProvider;
    private readonly ILogger<AElfLogEventProcessorBase<Transferred, LogEventInfo>> _logger;
    private readonly IAElfIndexerClientEntityRepository<SymbolBidInfoIndex, LogEventInfo> _symbolBidInfoIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo> _symbolAuctionInfoIndexRepository;
    private readonly ITsmSeedSymbolProvider _tsmSeedSymbolProvider;
    private readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> _tsmSeedSymbolIndexRepository;
    private readonly IAuctionInfoProvider _auctionInfoProvider;

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
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        IOptionsSnapshot<CleanDataOptions> cleanDataOptions,
        ITsmSeedSymbolProvider tsmSeedSymbolProvider,
        IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo> symbolAuctionInfoIndexRepository,
        IAElfIndexerClientEntityRepository<SymbolBidInfoIndex, LogEventInfo> symbolBidInfoIndexRepository,
        IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> tsmSeedSymbolIndexRepository,
        IAuctionInfoProvider auctionInfoProvider) : base(logger)
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
        _logger = logger;
        _cleanDataOptions = cleanDataOptions.Value;
        _tsmSeedSymbolProvider = tsmSeedSymbolProvider;
        _symbolAuctionInfoIndexRepository = symbolAuctionInfoIndexRepository;
        _symbolBidInfoIndexRepository = symbolBidInfoIndexRepository;
        _tsmSeedSymbolIndexRepository = tsmSeedSymbolIndexRepository;
        _auctionInfoProvider = auctionInfoProvider;

    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).TokenContractAddress;
    }

    protected override async Task HandleEventAsync(Transferred eventValue, LogEventContext context)
    {
        CleanSideChainSeedSymboIndexDataAsync(eventValue, context);
        _logger.Debug("TokenTransferProcessor-1"+JsonConvert.SerializeObject
            (eventValue));
        _logger.Debug("TokenTransferProcessor-2"+JsonConvert.SerializeObject(context));
        if (eventValue == null) return;
        if (context == null) return;
        await UpdateUserBalanceAsync(eventValue, context);
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

    private async Task CleanSideChainSeedSymboIndexDataAsync(Transferred eventValue, LogEventContext context)
    {
        try
        {
            var fromAddress = _cleanDataOptions.FromAddress;
            var toAddress = _cleanDataOptions.ToAddress;
            var seedSymbolList = _cleanDataOptions.SeedSymbolList;
            var mainChainId = ForestIndexerConstants.MainChain;//"AELF";
            _logger.LogInformation(
                "CleanSide--fromAddress: {fromAddress} toAddress: {toAddress}  seedSymbolList: {seedSymbolList},mainChainId:{mainChainId}",
                fromAddress, toAddress, JsonConvert.SerializeObject(seedSymbolList),mainChainId);
            _logger.LogInformation(
                "CleanSide--2fromAddress: {from} toAddress: {to}  ,mainChainId:{ChainId}",
                eventValue.From.ToBase58(), eventValue.To.ToBase58(),context.ChainId);
            if (!eventValue.From.ToBase58().Equals(fromAddress) || !eventValue.To.ToBase58().Equals(toAddress) || context.ChainId.Equals(mainChainId)) return;
            var sideChainId = context.ChainId;
            foreach (var seedSybmol in seedSymbolList)
            {
                //query main chain seed symbol index
                var indexIdMainChain = IdGenerateHelper.GetSeedSymbolId(mainChainId, seedSybmol);
                var indexMainChain = await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(indexIdMainChain, mainChainId);
                _logger.LogInformation(
                    "CleanSide--[CleanData-Main] indexIdMainChain: {indexIdMainChain} mainChainId: {mainChainId}  indexMainChain: {indexMainChain}",
                    indexIdMainChain, mainChainId, JsonConvert.SerializeObject(indexMainChain));
                if (indexMainChain == null) continue;

                //update side chain seed symbol index
                var indexIdSideChainId =
                    IdGenerateHelper.GetSeedSymbolId(sideChainId, seedSybmol);
                var indexSideChain = _objectMapper.Map<SeedSymbolIndex, SeedSymbolIndex>(indexMainChain);
                indexSideChain.Id = indexIdSideChainId;
                indexSideChain.IsDeleteFlag = false;
                indexSideChain.ChainId = sideChainId;
                indexSideChain.IssuerTo = indexMainChain.IssuerTo;
                _objectMapper.Map(context, indexSideChain);
                indexSideChain.Supply = 1;
                //add calc minNftListing
                var minNftListing = await _nftInfoProvider.GetMinListingNftAsync(indexIdSideChainId);
                indexSideChain.OfMinNftListingInfo(minNftListing);
                
                var auctionInfoIndex = await QueryAuctionAsync(sideChainId, seedSybmol);
                _logger.LogInformation(
                    "CleanSide--[CleanData-Side-auctionInfoIndex] auctionInfoIndex: {auctionInfoIndex}",
                    JsonConvert.SerializeObject(auctionInfoIndex));
                indexSideChain.BeginAuctionPrice = auctionInfoIndex?.StartPrice.Amount/ 100000000 ?? 0;
                indexSideChain.AuctionPriceSymbol = auctionInfoIndex?.StartPrice.Symbol ?? string.Empty;
                indexSideChain.AuctionDateTime = context.BlockTime;
                indexSideChain.HasAuctionFlag = auctionInfoIndex != null;
                indexSideChain.MaxAuctionPrice = indexSideChain.BeginAuctionPrice;
                indexSideChain.AuctionPrice = auctionInfoIndex?.StartPrice.Amount / 100000000 ?? 0;
                
                await _seedSymbolIndexRepository.AddOrUpdateAsync(indexSideChain);
                _logger.LogInformation(
                    "CleanSide--[CleanData-Side-SeedSymbol] indexIdSideChainId: {indexIdSideChainId} sideChainId: {sideChainId}  indexSideChain: {indexSideChain}",
                    indexIdSideChainId, sideChainId, JsonConvert.SerializeObject(indexSideChain));
                
                //update side chain symbol auction info index
                var symbolBidInfoIndex = await QueryRecentSymbolBidInfoAsync(sideChainId, seedSybmol);
                _logger.LogInformation(
                    "CleanSide--[CleanData-Side-symbolBidInfoIndex] symbolBidInfoIndex: {symbolBidInfoIndex}",
                    JsonConvert.SerializeObject(symbolBidInfoIndex));
                if (symbolBidInfoIndex == null) continue;
               // auctionInfoIndex = await QueryAuctionAsync(sideChainId, symbolBidInfoIndex);
                Contracts.Auction.BidPlaced eventValueBidPlaced = new BidPlaced()
                {
                    Price = new Contracts.Auction.Price()
                    {
                        Amount = symbolBidInfoIndex.PriceAmount,
                        Symbol = symbolBidInfoIndex.PriceSymbol
                    }
                };
                await _tsmSeedSymbolProvider.HandleBidPlacedAsync(context, eventValueBidPlaced, symbolBidInfoIndex, auctionInfoIndex.EndTime);
                await _collectionChangeProvider.SaveCollectionPriceChangeIndexAsync(context, auctionInfoIndex.CollectionSymbol);
                await _auctionInfoProvider.SetSeedSymbolIndexPriceByAuctionInfoAsync(symbolBidInfoIndex.AuctionId,DateTimeHelper.FromUnixTimeSeconds(symbolBidInfoIndex.BidTime), context);
                if (auctionInfoIndex.FinishIdentifier == (int)SeedAuctionStatus.Finished)
                {
                    var seedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(sideChainId, auctionInfoIndex.Symbol);
                    var seedSymbolIndex =
                        await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolIndexId, sideChainId);
                    if (seedSymbolIndex == null) continue;
                    _objectMapper.Map(context, seedSymbolIndex);
                    seedSymbolIndex.HasAuctionFlag = false;
                    seedSymbolIndex.MaxAuctionPrice = 0;
                    seedSymbolIndex.IssuerTo = auctionInfoIndex.FinishBidder;
                    await _seedSymbolIndexRepository.AddOrUpdateAsync(seedSymbolIndex);

                    var tsmSeedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, seedSymbolIndex.SeedOwnedSymbol);

                    var tsmSeedSymbolIndex =
                        await _tsmSeedSymbolIndexRepository.GetFromBlockStateSetAsync(tsmSeedSymbolIndexId, context.ChainId);
                    if (tsmSeedSymbolIndex == null) continue;
                    
                    _objectMapper.Map(context, tsmSeedSymbolIndex);
                    tsmSeedSymbolIndex.Status = SeedStatus.UNREGISTERED;
                    tsmSeedSymbolIndex.Owner = auctionInfoIndex.FinishBidder;
                    tsmSeedSymbolIndex.TokenPrice = auctionInfoIndex.FinishPrice;
                    tsmSeedSymbolIndex.AuctionStatus = (int)SeedAuctionStatus.Finished;
                    await _tsmSeedSymbolIndexRepository.AddOrUpdateAsync(tsmSeedSymbolIndex);

                }
                _logger.LogInformation(
                    "CleanSide--[CleanData-Side-tsmSeedSymbol] indexIdSideChainId: {indexIdSideChainId} sideChainId: {sideChainId}  indexSideChain: {indexSideChain}",
                    indexIdSideChainId, sideChainId, JsonConvert.SerializeObject(indexSideChain));
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CleanSide--CleanSideChainSeedSymboIndexData");
        }
    }
    private async Task<SymbolBidInfoIndex> QueryRecentSymbolBidInfoAsync(string chainId, string seedSymbol)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SymbolBidInfoIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.ChainId)
                .Value(chainId)),
            q => q.Term(i => i.Field(f => f.Symbol)
                .Value(seedSymbol))
        };
        QueryContainer Filter(QueryContainerDescriptor<SymbolBidInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));
        var result = await _symbolBidInfoIndexRepository.GetListAsync(Filter, skip: 0,limit:1,sortExp:q=>q.BidTime,sortType:SortOrder.Descending);
        return result.Item2.IsNullOrEmpty() ? null : result.Item2.FirstOrDefault();
    }
    private async Task<SymbolAuctionInfoIndex> QueryAuctionAsync(string chainId,SymbolBidInfoIndex symbolBidInfo)
    {
        if (symbolBidInfo == null) return null;
        var auctionInfoIndex = await _symbolAuctionInfoIndexRepository.GetFromBlockStateSetAsync(symbolBidInfo.AuctionId, chainId);
        return auctionInfoIndex;
    }
    
    private async Task<SymbolAuctionInfoIndex> QueryAuctionAsync(string chainId,string symbol)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SymbolAuctionInfoIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.ChainId)
                .Value(chainId)),
            q => q.Term(i => i.Field(f => f.Symbol)
                .Value(symbol))
        };
        QueryContainer Filter(QueryContainerDescriptor<SymbolAuctionInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));
        var result = await _symbolAuctionInfoIndexRepository.GetListAsync(Filter, skip: 0,limit:1);
        return result.Item2.IsNullOrEmpty() ? null : result.Item2.FirstOrDefault();
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

    private async Task UpdateUserBalanceAsync(Transferred eventValue, LogEventContext context)
    {
        var nftInfoIndexId = IdGenerateHelper.GetNFTInfoId(context.ChainId, eventValue.Symbol);
        var fromUserBalance = await _userBalanceProvider.SaveUserBalanceAsync(eventValue.Symbol,
            eventValue.From.ToBase58(),
            -eventValue.Amount, context);
        await _nftOfferProvider.UpdateOfferRealQualityAsync(eventValue.Symbol, fromUserBalance, eventValue.From.ToBase58(),
            context);
        await _listingInfoProvider.UpdateListingInfoRealQualityAsync(eventValue.Symbol, fromUserBalance, eventValue.From.ToBase58(), context);

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
    }

    private bool CheckIssueToIsContractAddress(string issuerToAddress)
    {
        return _contractInfoOptions.ContractInfos.First().TokenContractAddress.Equals(issuerToAddress) ||
               _contractInfoOptions.ContractInfos.First().TokenAdaptorContractAddress.Equals(issuerToAddress) ||
               _contractInfoOptions.ContractInfos.Last().TokenContractAddress.Equals(issuerToAddress)||_contractInfoOptions.ContractInfos.Last().AuctionContractAddress.Equals(issuerToAddress);
    }
}