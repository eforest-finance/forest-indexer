using AeFinder.Sdk.Processor;
using AElf.Contracts.MultiToken;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class TokenTransferProcessor : LogEventProcessorBase<Transferred>
{
    private readonly IObjectMapper _objectMapper;
    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly INFTListingInfoProvider _listingInfoProvider;
    private readonly ICollectionChangeProvider _collectionChangeProvider;
    private readonly INFTOfferProvider _nftOfferProvider;
    private readonly INFTInfoProvider _nftInfoProvider;
    private readonly INFTOfferChangeProvider _nftOfferChangeProvider;
    private readonly ILogger<TokenTransferProcessor> _logger;
    private readonly INFTListingChangeProvider _listingChangeProvider;

    public TokenTransferProcessor(ILogger<TokenTransferProcessor> logger,
        IObjectMapper objectMapper,
        IUserBalanceProvider userBalanceProvider,
        INFTListingInfoProvider listingInfoProvider,
        ICollectionChangeProvider collectionChangeProvider,
        INFTOfferProvider nftOfferProvider,
        INFTInfoProvider nftInfoProvider,
        INFTOfferChangeProvider nftOfferChangeProvider,
        INFTListingChangeProvider listingChangeProvider)
    {
        _objectMapper = objectMapper;
        _userBalanceProvider = userBalanceProvider;
        _listingInfoProvider = listingInfoProvider;
        _collectionChangeProvider = collectionChangeProvider;
        _nftOfferProvider = nftOfferProvider;
        _nftInfoProvider = nftInfoProvider;
        _nftOfferChangeProvider = nftOfferChangeProvider;
        _logger = logger;
        _listingChangeProvider = listingChangeProvider;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetTokenContractAddress(chainId);
    }

    public async override Task ProcessAsync(Transferred eventValue, LogEventContext context)
    {
        _logger.LogDebug("TokenTransferProcessor-1"+JsonConvert.SerializeObject
            (eventValue));
        _logger.LogDebug("TokenTransferProcessor-2"+JsonConvert.SerializeObject(context));
        if (eventValue == null) return;
        if (context == null) return;
        await UpdateUserFromBalanceAsync(eventValue, context);
        await UpdateUserToBalanceAsync(eventValue, context);
        if(SymbolHelper.CheckSymbolIsELF(eventValue.Symbol)) return;
        await _collectionChangeProvider.SaveCollectionChangeIndexAsync(context, eventValue.Symbol);
        if (SymbolHelper.CheckSymbolIsSeedCollection(eventValue.Symbol)) return;
        if (SymbolHelper.CheckSymbolIsSeedSymbol(eventValue.Symbol))
        {
            _logger.LogDebug("TokenTransferProcessor-3"+JsonConvert.SerializeObject
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
        _logger.LogDebug("TokenTransferProcessor-4"+JsonConvert.SerializeObject
            (eventValue));
        _logger.LogDebug("TokenTransferProcessor-5"+JsonConvert.SerializeObject
            (context));
        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        var seedSymbol =
            await GetEntityAsync<SeedSymbolIndex>(seedSymbolId);

        if (seedSymbol == null) return;
        if (seedSymbol.IsDeleted) return;
        _logger.LogDebug("TokenTransferProcessor-8"+JsonConvert.SerializeObject
            (seedSymbol));
        
        //add calc minNftListing
        var minNftListing = await _nftInfoProvider.GetMinListingNftAsync(seedSymbolId);
        seedSymbol.OfMinNftListingInfo(minNftListing);
        
        _objectMapper.Map(context, seedSymbol);
        await SaveEntityAsync(seedSymbol);
        await _listingChangeProvider.SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
        await SaveNftActivityIndexAsync(eventValue, context, seedSymbol.Id, seedSymbol.Decimals);
    }

    private async Task HandleForNFTTransferAsync(Transferred eventValue, LogEventContext context)
    {
        var nftInfoIndexId = IdGenerateHelper.GetNFTInfoId(context.ChainId, eventValue.Symbol);
        var nftInfoIndex = await GetEntityAsync<NFTInfoIndex>(nftInfoIndexId);
        if (nftInfoIndex == null) return;

        //add calc minNftListing
        var minNftListing = await _nftInfoProvider.GetMinListingNftAsync(nftInfoIndex.Id);
        nftInfoIndex.OfMinNftListingInfo(minNftListing);
        _objectMapper.Map(context, nftInfoIndex);
        await SaveEntityAsync(nftInfoIndex);
        await _listingChangeProvider.SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
        await SaveNftActivityIndexAsync(eventValue, context, nftInfoIndex.Id, nftInfoIndex.Decimals);
    }

    private async Task SaveNftActivityIndexAsync(Transferred eventValue, LogEventContext context, string bizId,
        int decimals)
    {
        var nftActivityIndexId = IdGenerateHelper.GetNftActivityId(context.ChainId, eventValue.Symbol,
            eventValue.From.ToBase58(),
            eventValue.To.ToBase58(), context.Transaction.TransactionId);
        var checkNftActivityIndex = await GetEntityAsync<NFTActivityIndex>(nftActivityIndexId);
        if (checkNftActivityIndex != null) return;
        
        NFTActivityIndex nftActivityIndex = new NFTActivityIndex
        {
            Id = nftActivityIndexId,
            Type = NFTActivityType.Transfer,
            Amount = TokenHelper.GetIntegerDivision(eventValue.Amount,decimals),
            TransactionHash = context.Transaction.TransactionId,
            Timestamp = context.Block.BlockTime,
            NftInfoId = bizId
        };
        _objectMapper.Map(context, nftActivityIndex);
        nftActivityIndex.From =
            FullAddressHelper.ToFullAddress(eventValue.From.ToBase58(), context.ChainId);
         nftActivityIndex.To =
             FullAddressHelper.ToFullAddress(eventValue.To.ToBase58(), context.ChainId);
         await SaveEntityAsync(nftActivityIndex);
    }

    private async Task UpdateUserFromBalanceAsync(Transferred eventValue, LogEventContext context)
    {
        var needRecordBalance =
            await _nftOfferProvider.NeedRecordBalance(eventValue.Symbol, eventValue.From.ToBase58(), context.ChainId);
        if (!needRecordBalance)
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
        var needRecordBalance =
            await _nftOfferProvider.NeedRecordBalance(eventValue.Symbol, eventValue.To.ToBase58(), context.ChainId);
        if (!needRecordBalance)
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
                ChangeTime = context.Block.BlockTime,
                ListingPrice = lastNFTListingInfo.Prices,
                ListingTime = lastNFTListingInfo.StartTime
            };
        }
        else
        {
            userBalanceTo.Amount += eventValue.Amount;
            userBalanceTo.ChangeTime = context.Block.BlockTime;
        }

        _objectMapper.Map(context, userBalanceTo);
        await _userBalanceProvider.UpdateUserBalanceAsync(userBalanceTo);
        await _nftOfferProvider.UpdateOfferRealQualityAsync(eventValue.Symbol, userBalanceTo.Amount,
            eventValue.To.ToBase58(), context);
        await _listingInfoProvider.UpdateListingInfoRealQualityAsync(eventValue.Symbol, userBalanceTo.Amount, eventValue.To.ToBase58(), context);
        await _nftOfferChangeProvider.SaveNFTOfferChangeIndexAsync(context, eventValue.Symbol, EventType.Other);
    }
}