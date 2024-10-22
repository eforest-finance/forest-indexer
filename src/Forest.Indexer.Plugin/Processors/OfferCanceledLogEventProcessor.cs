using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class OfferCanceledLogEventProcessor : LogEventProcessorBase<OfferCanceled>
{
    private readonly IObjectMapper _objectMapper;
    private readonly IReadOnlyRepository<OfferInfoIndex> _nftOfferIndexRepository;
    public OfferCanceledLogEventProcessor(
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

    public async override Task ProcessAsync(OfferCanceled eventValue, LogEventContext context)
    {
        var queryable = await _nftOfferIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(ForestQueryFilters.OfferCanceledFilter(context, eventValue));
        
        var offerIndex = queryable.ToList();

       
        if (offerIndex.IsNullOrEmpty()) return;
        // The current number of items in the IndexList is only 1 
        foreach (var index in eventValue.IndexList.Value)
        {
            try
            {
                if (index >= offerIndex.Count) continue;
                var cancelOfferIndex = offerIndex[index];
                if (cancelOfferIndex == null) return;
                var nftInfoId = cancelOfferIndex.BizInfoId;
                await AddNFTActivityRecordAsync(eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
                    null, cancelOfferIndex.Quantity, cancelOfferIndex.Price,
                    NFTActivityType.CancelOffer, context, cancelOfferIndex.PurchaseToken, cancelOfferIndex.ExpireTime);

                await DeleteEntityAsync<OfferInfoIndex>(cancelOfferIndex.Id);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "[OfferCanceled] ERROR: Symbol={Symbol},index = {Index},size = {Size}",
                    eventValue.Symbol, index, offerIndex.Count);
                throw;
            }
        }
        
        await UpdateOfferNumAsync(eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
            -eventValue.IndexList.Value.Count, context);
        await SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);
        await SaveNFTOfferChangeIndexAsync(context, eventValue.Symbol, EventType.Cancel);
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
            Type = activityType,
            TransactionHash = context.Transaction.TransactionId,
            Timestamp = context.Block.BlockTime,
            NftInfoId = nftInfoIndexId
        };
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
            decimals = seedSymbol.Decimals;
        }
        else if (SymbolHelper.CheckSymbolIsNoMainChainNFT(symbol, chainId))
        {
            var nftIndexId = IdGenerateHelper.GetNFTInfoId(chainId, symbol);
            var nftIndex = await GetEntityAsync<NFTInfoIndex>(nftIndexId);
            decimals = nftIndex.Decimals;
        }

        return decimals;
    }

    
    public async Task SaveNFTOfferChangeIndexAsync(LogEventContext context, string symbol, EventType eventType)
    {
        if (context.ChainId.Equals(ForestIndexerConstants.MainChain))
        {
            return;
        }

        if (symbol.Equals(ForestIndexerConstants.TokenSimpleElf))
        {
            return;
        }

        var nftOfferChangeIndex = new NFTOfferChangeIndex
        {
            Id = IdGenerateHelper.GetId(context.ChainId, symbol, Guid.NewGuid()),
            NftId = IdGenerateHelper.GetNFTInfoId(context.ChainId, symbol),
            EventType = eventType,
            CreateTime = context.Block.BlockTime
        };
        
        _objectMapper.Map(context, nftOfferChangeIndex);
        await SaveEntityAsync(nftOfferChangeIndex);
    }
    
    public async Task<int> UpdateOfferNumAsync(string symbol, string offerFrom, int change, LogEventContext context)
    {
        var offerNumId = IdGenerateHelper.GetOfferNumId(context.ChainId, offerFrom);
        var nftOfferNumIndex = await GetEntityAsync<UserNFTOfferNumIndex>(offerNumId);
        
        if (nftOfferNumIndex == null)
        {
            nftOfferNumIndex = new UserNFTOfferNumIndex()
            {
                Id = offerNumId,
                Address = offerFrom,
                OfferNum = change
            };
        }
        else
        {
            nftOfferNumIndex.OfferNum += change;
            // deal history data
            if (nftOfferNumIndex.OfferNum < 0)
            {
                Logger.LogWarning(
                    "UpdateOfferNumAsync has history Address {Address} symbol {Symbol} OfferNum {OfferNum}", offerFrom,
                    symbol, nftOfferNumIndex.OfferNum);
                nftOfferNumIndex.OfferNum = 0;
            }
        }

        Logger.LogInformation("UpdateOfferNumAsync Address {Address} symbol {Symbol} OfferNum {OfferNum}", offerFrom,
            symbol, nftOfferNumIndex.OfferNum);
        _objectMapper.Map(context, nftOfferNumIndex);
        await SaveEntityAsync(nftOfferNumIndex);
        return nftOfferNumIndex.OfferNum;
    }
    public async Task SaveCollectionPriceChangeIndexAsync(LogEventContext context, string symbol)
    {
        var collectionPriceChangeIndex = new CollectionPriceChangeIndex();
        var nftCollectionSymbol = SymbolHelper.GetNFTCollectionSymbol(symbol);
        if (nftCollectionSymbol == null)
        {
            return;
        }

        collectionPriceChangeIndex.Symbol = nftCollectionSymbol;
        collectionPriceChangeIndex.Id = IdGenerateHelper.GetNFTCollectionId(context.ChainId, nftCollectionSymbol);
        collectionPriceChangeIndex.UpdateTime = context.Block.BlockTime;
        _objectMapper.Map(context, collectionPriceChangeIndex);
        await SaveEntityAsync(collectionPriceChangeIndex);
    }

}