using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class OfferCanceledByExpireTimeLogEventProcessor : LogEventProcessorBase<OfferCanceledByExpireTime>
{
    private readonly IObjectMapper _objectMapper;
    private readonly IReadOnlyRepository<OfferInfoIndex> _nftOfferIndexRepository;
    public OfferCanceledByExpireTimeLogEventProcessor(
        IObjectMapper objectMapper,
        IReadOnlyRepository<OfferInfoIndex> nftOfferIndexRepository)
    {
        _objectMapper = objectMapper;
        _nftOfferIndexRepository = nftOfferIndexRepository;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetNFTForestContractAddress(chainId);
    }

    public async override Task ProcessAsync(OfferCanceledByExpireTime eventValue, LogEventContext context)
    {
        Logger.LogDebug("OfferCanceledByExpireTimeLogEventProcessor-1 {context}", JsonConvert.SerializeObject(context));
        Logger.LogDebug("OfferCanceledByExpireTimeLogEventProcessor-2 {eventValue}",
            JsonConvert.SerializeObject(eventValue));

        if (eventValue.OfferFrom == null || eventValue.OfferFrom.Value.Length == 0)
        {
            Logger.LogError("OfferFrom is null");
            return;
        }
        if (eventValue.OfferTo == null || eventValue.OfferTo.Value.Length == 0)
        {
            Logger.LogError("OfferTo is null");
            return;
        }
        
        var queryable = await _nftOfferIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(ForestQueryFilters.OfferCanceledByExpireTimeFilter(context, eventValue));
        var offerIndexList = queryable.ToList().Where(i => i != null).ToList();
        if (offerIndexList.IsNullOrEmpty()) return;
        for (var i = 0; i < offerIndexList.Count; i++)
        {
            if (offerIndexList[i] == null) continue;
            var cancelOfferIndex = offerIndexList[i];
            await AddNFTActivityRecordAsync(eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
                null, cancelOfferIndex.Quantity, cancelOfferIndex.Price,
                NFTActivityType.CancelOffer, context, cancelOfferIndex.PurchaseToken, cancelOfferIndex.ExpireTime);
        }
    }
    
    private async Task AddNFTActivityRecordAsync(string symbol, string offerFrom, string offerTo,
        long quantity, decimal price, NFTActivityType activityType, LogEventContext context,
        TokenInfoIndex tokenInfoIndex, DateTime expireTime)
    {
        var nftActivityIndexId = IdGenerateHelper.GetId(context.ChainId, symbol, offerFrom,
            offerTo, context.Transaction.TransactionId, expireTime);
        var nftActivityIndex = await GetEntityAsync<NFTActivityIndex>(nftActivityIndexId);
        if (nftActivityIndex != null) return;

        var nftInfoIndexId = IdGenerateHelper.GetId(context.ChainId, symbol);
        
        var decimals = await QueryDecimal(context.ChainId, symbol);
        
        nftActivityIndex = new NFTActivityIndex
        {
            Id = nftActivityIndexId,
            TransactionHash = context.Transaction.TransactionId,
            Timestamp = context.Block.BlockTime,
            NftInfoId = nftInfoIndexId
        };
        nftActivityIndex.OfType(activityType);
        _objectMapper.Map(context, nftActivityIndex);
        nftActivityIndex.From = FullAddressHelper.ToFullAddress(offerFrom, context.ChainId);
        nftActivityIndex.To = FullAddressHelper.ToFullAddress(await TransferAddress(offerTo), context.ChainId);

        nftActivityIndex.Amount = TokenHelper.GetIntegerDivision(quantity, decimals);
        nftActivityIndex.Price = price;
        nftActivityIndex.PriceTokenInfo = tokenInfoIndex;
        
        await SaveEntityAsync(nftActivityIndex);
    }
    
    private async Task<string> TransferAddress(string offerToAddress)
    {
        if (offerToAddress.IsNullOrWhiteSpace()) return offerToAddress;
        var proxyAccount = await GetEntityAsync<ProxyAccountIndex>(offerToAddress);
        if (proxyAccount == null || proxyAccount.ManagersSet == null)
        {
            return offerToAddress;
        }
        return proxyAccount.ManagersSet.FirstOrDefault(offerToAddress);
    }
    
    public async Task<int> QueryDecimal(string chainId,string symbol)
    {
        var decimals = 0;
        if (SymbolHelper.CheckSymbolIsSeedSymbol(symbol))
        {
            var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, symbol);
            var seedSymbol = await GetEntityAsync<SeedSymbolIndex>(seedSymbolId);
            if (seedSymbol == null) return decimals;
            decimals = seedSymbol.Decimals;
        }
        else if (SymbolHelper.CheckSymbolIsNoMainChainNFT(symbol, chainId))
        {
            var nftIndexId = IdGenerateHelper.GetNFTInfoId(chainId, symbol);
            var nftIndex = await GetEntityAsync<NFTInfoIndex>(nftIndexId);
            if (nftIndex == null) return decimals;
            decimals = nftIndex.Decimals;
        }

        return decimals;
    }
}