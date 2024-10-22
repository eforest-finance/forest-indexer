using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class OfferAddedLogEventProcessor : LogEventProcessorBase<OfferAdded>
{
    private readonly IObjectMapper _objectMapper;

    public OfferAddedLogEventProcessor(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetNFTForestContractAddress(chainId);
    }

    public async override Task ProcessAsync(OfferAdded eventValue, LogEventContext context)
    {
        Logger.LogDebug("OfferAddedLogEventProcessor-1 {context}",JsonConvert.SerializeObject(context));
        Logger.LogDebug("OfferAddedLogEventProcessor-2 {eventValue}",JsonConvert.SerializeObject(eventValue));
        var offerIndexId = IdGenerateHelper.GetOfferId(context.ChainId, eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
            eventValue.OfferTo.ToBase58(), eventValue.ExpireTime.Seconds,eventValue.Price.Amount);
        var offerIndex = await GetEntityAsync<OfferInfoIndex>(offerIndexId);
        if (offerIndex != null) return;

        offerIndex = _objectMapper.Map<OfferAdded, OfferInfoIndex>(eventValue);
        offerIndex.Id = offerIndexId;
        var tokenIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.Price.Symbol);
        var tokenIndex = await GetEntityAsync<TokenInfoIndex>(tokenIndexId);
        offerIndex.Price = eventValue.Price.Amount / (decimal)Math.Pow(10, tokenIndex.Decimals);
        offerIndex.PurchaseToken = tokenIndex;
        offerIndex.CreateTime = context.Block.BlockTime;

        if (SymbolHelper.CheckSymbolIsSeedSymbol(eventValue.Symbol))
        {
            offerIndex.BizInfoId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        }
        else if (SymbolHelper.CheckSymbolIsNFT(eventValue.Symbol))
        {
            offerIndex.BizInfoId = IdGenerateHelper.GetNFTInfoId(context.ChainId, eventValue.Symbol);
        }
        offerIndex.BizSymbol = eventValue.Symbol;
        offerIndex.RealQuantity = eventValue.Quantity;
        _objectMapper.Map(context, offerIndex);
        await SaveEntityAsync(offerIndex);
        await AddNFTActivityRecordAsync(eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
            eventValue.OfferTo.ToBase58(), eventValue.Quantity, offerIndex.Price, NFTActivityType.MakeOffer,
            context,
            tokenIndex,
            offerIndex.ExpireTime);
        await SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);
        await UpdateOfferNumAsync(eventValue.Symbol, eventValue.OfferFrom.ToBase58(), 1, context);
        await ReCoverUserBalanceAsync(eventValue.OriginBalanceSymbol, eventValue.OfferFrom.ToBase58(), eventValue.OriginBalance, context);
        await SaveNFTOfferChangeIndexAsync(context, eventValue.Symbol, EventType.Add);
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
    
    private async Task SaveNFTOfferChangeIndexAsync(LogEventContext context, string symbol, EventType eventType)
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
    
    private async Task<long> ReCoverUserBalanceAsync(String symbol, String address, long amount, LogEventContext context)
    {
        var nftInfoIndexId = IdGenerateHelper.GetNFTInfoId(context.ChainId, symbol);
        var userBalanceId = IdGenerateHelper.GetUserBalanceId(address, context.ChainId, nftInfoIndexId);
        var userBalanceIndex = await GetEntityAsync<UserBalanceIndex>(userBalanceId);
        
        if (userBalanceIndex == null)
        {
            userBalanceIndex = new UserBalanceIndex()
            {
                Id = userBalanceId,
                ChainId = context.ChainId,
                NFTInfoId = nftInfoIndexId,
                Address = address,
                Amount = amount,
                Symbol = symbol,
                ChangeTime = context.Block.BlockTime
            };
        }
        else
        {
            userBalanceIndex.Amount = amount;
            userBalanceIndex.ChangeTime = context.Block.BlockTime;
        }

        _objectMapper.Map(context, userBalanceIndex);
        await SaveEntityAsync(userBalanceIndex);
        return userBalanceIndex.Amount;
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
}