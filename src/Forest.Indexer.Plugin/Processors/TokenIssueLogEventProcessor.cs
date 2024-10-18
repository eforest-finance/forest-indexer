using AeFinder.Sdk.Processor;
using AElf.Contracts.MultiToken;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class TokenIssueLogEventProcessor : LogEventProcessorBase<Issued>
{
    private readonly IObjectMapper _objectMapper;

    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly IProxyAccountProvider _proxyAccountProvider;
    private readonly INFTInfoProvider _nftInfoProvider;
    private readonly ICollectionChangeProvider _collectionChangeProvider;
    private readonly INFTOfferProvider _nftOfferProvider;
    private readonly INFTListingInfoProvider _nftListingInfoProvider;
    private readonly INFTOfferChangeProvider _nftOfferChangeProvider;

    private readonly ILogger<TokenIssueLogEventProcessor> _logger;
    private readonly INFTListingChangeProvider _listingChangeProvider;

    public TokenIssueLogEventProcessor(ILogger<TokenIssueLogEventProcessor> logger
        , IObjectMapper objectMapper
        , IUserBalanceProvider userBalanceProvider
        , IProxyAccountProvider proxyAccountProvider
        , INFTInfoProvider nftInfoProvider
        , ICollectionChangeProvider collectionChangeProvider
        , INFTOfferProvider nftOfferProvider
        , NFTListingInfoProvider nftListingInfoProvider
        , INFTOfferChangeProvider nftOfferChangeProvider
        , INFTListingChangeProvider listingChangeProvider)
    {
        _objectMapper = objectMapper;
        _userBalanceProvider = userBalanceProvider;
        _proxyAccountProvider = proxyAccountProvider;
        _nftInfoProvider = nftInfoProvider;
        _collectionChangeProvider = collectionChangeProvider;
        _nftOfferProvider = nftOfferProvider;
        _nftListingInfoProvider = nftListingInfoProvider;
        _nftOfferChangeProvider = nftOfferChangeProvider;
        _logger = logger;
        _listingChangeProvider = listingChangeProvider;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetTokenContractAddress(chainId);
    }

    public async override Task ProcessAsync(Issued eventValue, LogEventContext context)
    {
        _logger.LogDebug("TokenIssueLogEventProcessor-1"+JsonConvert.SerializeObject(eventValue));
        _logger.LogDebug("TokenIssueLogEventProcessor-2"+JsonConvert.SerializeObject(context));
        if (eventValue == null || context == null) return;
        await _collectionChangeProvider.SaveCollectionChangeIndexAsync(context, eventValue.Symbol);
        var userBalance = await _userBalanceProvider.SaveUserBalanceAsync(eventValue.Symbol, eventValue.To.ToBase58(),
            eventValue.Amount, context);
        await _nftOfferProvider.UpdateOfferRealQualityAsync(eventValue.Symbol, userBalance, eventValue.To.ToBase58(),
            context);
        await _nftListingInfoProvider.UpdateListingInfoRealQualityAsync(eventValue.Symbol, userBalance, eventValue.To.ToBase58(), context);
        await _nftOfferChangeProvider.SaveNFTOfferChangeIndexAsync(context, eventValue.Symbol, EventType.Other);
        await _nftListingInfoProvider.UpdateListingInfoRealQualityAsync(eventValue.Symbol, userBalance, eventValue.To.ToBase58(), context);

        if (SymbolHelper.CheckSymbolIsELF(eventValue.Symbol)) return;
        if (SymbolHelper.CheckSymbolIsSeedCollection(eventValue.Symbol)) return;

        if (SymbolHelper.CheckSymbolIsSeedSymbol(eventValue.Symbol))
        {
            await HandleForSeedSymbolIssueAsync(eventValue, context);
            return;
        }

        if (SymbolHelper.CheckSymbolIsNoMainChainNFT(eventValue.Symbol, context.ChainId))
        {
            await HandleForNFTIssueAsync(eventValue, context);
            return;
        }
        await HandleForSymbolMarketTokenAsync(eventValue, context);
    }

    private async Task HandleForSymbolMarketTokenAsync(Issued eventValue, LogEventContext context)
    {
        _logger.LogDebug("TokenIssueLogEventProcessor-3-HandleForNoMainChainSeedTokenAsync");
        if (eventValue == null || context == null) return;
        
        var symbolMarketTokenIndexId = IdGenerateHelper.GetSymbolMarketTokenId(context.ChainId, eventValue.Symbol);
        var symbolMarketTokenIndex = await GetEntityAsync<SeedSymbolMarketTokenIndex>(symbolMarketTokenIndexId);
        
        if (symbolMarketTokenIndex == null) return;
        symbolMarketTokenIndex.Supply += eventValue.Amount;
        symbolMarketTokenIndex.Issued += eventValue.Amount;
        if (symbolMarketTokenIndex.IssueToSet.IsNullOrEmpty())
        {
            symbolMarketTokenIndex.IssueToSet = new HashSet<string>()
            {
                eventValue.To.ToBase58()
            };
        }
        else if(!symbolMarketTokenIndex.IssueToSet.Contains(eventValue.To.ToBase58()))
        {
            symbolMarketTokenIndex.IssueToSet.Add(eventValue.To.ToBase58());
        }
        
        symbolMarketTokenIndex =
            await _proxyAccountProvider.FillProxyAccountInfoForSymbolMarketTokenIndexIssuerAsync(symbolMarketTokenIndex,
                context.ChainId);
        _logger.LogDebug("TokenIssueLogEventProcessor-31-HandleForNoMainChainSeedTokenAsync"+JsonConvert.SerializeObject(symbolMarketTokenIndex));
        _objectMapper.Map(context, symbolMarketTokenIndex);
        _logger.LogDebug("TokenIssueLogEventProcessor-32-HandleForNoMainChainSeedTokenAsync"+JsonConvert.SerializeObject(symbolMarketTokenIndex));
        await SaveEntityAsync(symbolMarketTokenIndex);
        _logger.LogDebug("TokenIssueLogEventProcessor-33-HandleForNoMainChainSeedTokenAsync");
        await SaveActivityAsync(eventValue, context, symbolMarketTokenIndex.Id, symbolMarketTokenIndex.Decimals);
    }

    private async Task HandleForSeedSymbolIssueAsync(Issued eventValue, LogEventContext context)
    {
        await DoHandleForSeedSymbolIssueAsync(eventValue, context);
    }

    private async Task DoHandleForSeedSymbolIssueAsync(Issued eventValue, LogEventContext context)
    {
        var seedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        var seedSymbolIndex = await GetEntityAsync<SeedSymbolIndex>(seedSymbolIndexId);
        if (seedSymbolIndex == null) return;
        seedSymbolIndex.IssuerTo = eventValue.To.ToBase58();
        seedSymbolIndex.Supply += eventValue.Amount;
        seedSymbolIndex.Issued += eventValue.Amount;
        //add calc minNftListing
        var minNftListing = await _nftInfoProvider.GetMinListingNftAsync(seedSymbolIndexId);
        seedSymbolIndex.OfMinNftListingInfo(minNftListing);
        _objectMapper.Map(context, seedSymbolIndex);
        await SaveEntityAsync(seedSymbolIndex);
        await _listingChangeProvider.SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
        await SaveActivityAsync(eventValue, context, seedSymbolIndex.Id, seedSymbolIndex.Decimals);
    }

    private async Task HandleForNFTIssueAsync(Issued eventValue, LogEventContext context)
    {
        var nftInfoIndexId = IdGenerateHelper.GetNFTInfoId(context.ChainId, eventValue.Symbol);
        var nftInfoIndex = await GetEntityAsync<NFTInfoIndex>(nftInfoIndexId);
        
        if (nftInfoIndex == null) return;

        nftInfoIndex.Supply += eventValue.Amount;
        nftInfoIndex.Issued += eventValue.Amount;
        //add calc minNftListing
        var minNftListing = await _nftInfoProvider.GetMinListingNftAsync(nftInfoIndexId);
        nftInfoIndex.OfMinNftListingInfo(minNftListing);
        _objectMapper.Map(context, nftInfoIndex);

        nftInfoIndex =
            await _proxyAccountProvider.FillProxyAccountInfoForNFTInfoIndexAsync(nftInfoIndex, context.ChainId);
        
        await SaveEntityAsync(nftInfoIndex);
        await _listingChangeProvider.SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);

        await SaveActivityAsync(eventValue, context, nftInfoIndex.Id, nftInfoIndex.Decimals);
    }

    private async Task SaveActivityAsync(Issued eventValue, LogEventContext context, string bizId, int decimals)
    {
        var nftActivityIndexId =
            IdGenerateHelper.GetId(context.ChainId, eventValue.Symbol, NFTActivityType.Issue.ToString(), context.Transaction.TransactionId);
        var activitySaved = await _nftInfoProvider.AddNFTActivityAsync(context, new NFTActivityIndex
        {
            Id = nftActivityIndexId,
            Type = NFTActivityType.Issue,
            To = FullAddressHelper.ToFullAddress(eventValue.To.ToBase58(), context.ChainId),
            Amount = TokenHelper.GetIntegerDivision(eventValue.Amount,decimals),
            TransactionHash = context.Transaction.TransactionId,
            Timestamp = context.Block.BlockTime,
            NftInfoId = bizId
        });
    }

    
}