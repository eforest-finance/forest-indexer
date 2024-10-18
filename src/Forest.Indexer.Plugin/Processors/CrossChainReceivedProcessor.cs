using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using AElf;
using AElf.Contracts.MultiToken;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;
//todo V2 ,code:doing
namespace Forest.Indexer.Plugin.Processors;

public class CrossChainReceivedProcessor : LogEventProcessorBase<CrossChainReceived>
{
    private readonly IObjectMapper _objectMapper;
    private readonly UserBalanceProvider _balanceProvider;
    private readonly INFTOfferProvider _nftOfferProvider;
    private readonly INFTListingInfoProvider _nftListingInfoProvider;
    private readonly INFTInfoProvider _nftInfoProvider;
    private readonly INFTOfferChangeProvider _nftOfferChangeProvider;

    private readonly ILogger<AElfLogEventProcessorBase<CrossChainReceived, LogEventInfo>> _logger;
    private readonly INFTListingChangeProvider _listingChangeProvider;

    public CrossChainReceivedProcessor(
        IObjectMapper objectMapper,
        UserBalanceProvider balanceProvider,
        INFTOfferProvider nftOfferProvider,
        INFTListingInfoProvider nftListingInfoProvider,
        INFTOfferChangeProvider nftOfferChangeProvider,
        INFTListingChangeProvider listingChangeProvider,
        INFTInfoProvider nftInfoProvider)
    {
        _objectMapper = objectMapper;
        _balanceProvider = balanceProvider;
        _nftOfferProvider = nftOfferProvider;
        _nftListingInfoProvider = nftListingInfoProvider;
        _nftInfoProvider = nftInfoProvider;
        _nftOfferChangeProvider = nftOfferChangeProvider;
        _listingChangeProvider = listingChangeProvider;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetTokenContractAddress(chainId);
    }

    public override async Task ProcessAsync(CrossChainReceived eventValue, LogEventContext context)
    {
        Logger.LogDebug("CrossChainReceived-1-eventValue"+JsonConvert.SerializeObject(eventValue));
        Logger.LogDebug("CrossChainReceived-2-context"+JsonConvert.SerializeObject(context));
        if (eventValue == null || context == null) return;
        var needRecordBalance =
            await _nftOfferProvider.NeedRecordBalance(eventValue.Symbol, eventValue.To.ToBase58(), context.ChainId);
        if (!needRecordBalance)
        {
            return;
        }
        var userBalance = await _balanceProvider.SaveUserBalanceAsync(eventValue.Symbol, eventValue.To.ToBase58(),
            eventValue.Amount,
            context);
        await _nftOfferProvider.UpdateOfferRealQualityAsync(eventValue.Symbol, userBalance, eventValue.To.ToBase58(), context);
        await _nftListingInfoProvider.UpdateListingInfoRealQualityAsync(eventValue.Symbol, userBalance, eventValue.To.ToBase58(), context);
        await _nftOfferChangeProvider.SaveNFTOfferChangeIndexAsync(context, eventValue.Symbol, EventType.Other);

        if (SymbolHelper.CheckSymbolIsSeedSymbol(eventValue.Symbol))
        {
            await HandleEventForSeedAsync(eventValue, context);
        }else if (SymbolHelper.CheckSymbolIsNoMainChainNFT(eventValue.Symbol, context.ChainId))
        {
            await HandleEventForNFTAsync(eventValue, context);
        }
    }
    
    private async Task HandleEventForSeedAsync(CrossChainReceived eventValue, LogEventContext context)
    {
        //Get the seed owned symbol from seed symbol index
        
        var seedSymbolIndexIdToChainId =
            IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        var seedSymbolIndexToChain = await GetEntityAsync<SeedSymbolIndex>(seedSymbolIndexIdToChainId);
       
        Logger.LogDebug("CrossChainReceived-3-seedSymbolIndexIdToChainId"+seedSymbolIndexIdToChainId);
        Logger.LogDebug("CrossChainReceived-4-seedSymbolIndexToChain"+JsonConvert.SerializeObject(seedSymbolIndexToChain));
        if(seedSymbolIndexToChain == null) return;
        
        seedSymbolIndexToChain.IsDeleteFlag = false;
        seedSymbolIndexToChain.ChainId = context.ChainId;
        seedSymbolIndexToChain.IssuerTo = eventValue.To.ToBase58();
        _objectMapper.Map(context, seedSymbolIndexToChain);
        seedSymbolIndexToChain.Supply = eventValue.Amount;
        //add calc minNftListing
        var minNftListing = await _nftInfoProvider.GetMinListingNftAsync(seedSymbolIndexIdToChainId);
        seedSymbolIndexToChain.OfMinNftListingInfo(minNftListing);
        await SaveEntityAsync(seedSymbolIndexToChain);

        //Set the tsm seed symbol index info to the to chain
        var tsmSeedSymbolIndexIdToChainId =
            IdGenerateHelper.GetSeedSymbolId(context.ChainId, seedSymbolIndexToChain.SeedOwnedSymbol);
        var tsmSeedSymbolIndexToChain = await GetEntityAsync<TsmSeedSymbolIndex>(tsmSeedSymbolIndexIdToChainId);
        
        Logger.LogDebug("CrossChainReceived-5-tsmSeedSymbolIndexIdToChainId"+tsmSeedSymbolIndexIdToChainId);
        Logger.LogDebug("CrossChainReceived-6-tsmSeedSymbolIndexToChain"+JsonConvert.SerializeObject(tsmSeedSymbolIndexToChain));
        if(tsmSeedSymbolIndexToChain == null) return;
        tsmSeedSymbolIndexToChain.IsBurned = false;
        tsmSeedSymbolIndexToChain.ChainId = context.ChainId;
        tsmSeedSymbolIndexToChain.Owner = eventValue.To.ToBase58();
        _objectMapper.Map(context, tsmSeedSymbolIndexToChain);
        await SaveEntityAsync(tsmSeedSymbolIndexToChain);
        await _listingChangeProvider.SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
    }
    
    private async Task HandleEventForNFTAsync(CrossChainReceived eventValue, LogEventContext context)
    {
        var nftInfoId =
            IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        var nftInfoIndex = await GetEntityAsync<NFTInfoIndex>(nftInfoId);
        
        Logger.LogDebug("CrossChainReceived-5-nftInfoId"+nftInfoId);
        Logger.LogDebug("CrossChainReceived-6-nftInfo"+JsonConvert.SerializeObject(nftInfoIndex));
        if(nftInfoIndex == null) return;
        var minNftListing = await _nftInfoProvider.GetMinListingNftAsync(nftInfoIndex.Id);
        nftInfoIndex.OfMinNftListingInfo(minNftListing);
        _objectMapper.Map(context, nftInfoIndex);
        nftInfoIndex.Supply += eventValue.Amount;
        await SaveEntityAsync(nftInfoIndex);

        await _listingChangeProvider.SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
    }
}