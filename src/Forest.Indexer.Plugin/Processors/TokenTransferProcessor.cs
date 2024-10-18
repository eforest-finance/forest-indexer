using AeFinder.Sdk.Logging;
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
    private readonly ILogger<TokenTransferProcessor> _logger;
    private readonly NFTOfferProvider _nftOfferProvider;

    public TokenTransferProcessor(ILogger<TokenTransferProcessor> logger,
        IObjectMapper objectMapper,
        NFTOfferProvider nftOfferProvider)
    {
        _objectMapper = objectMapper;
        _logger = logger;
        _nftOfferProvider = nftOfferProvider;
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
        await SaveCollectionChangeIndexAsync(context, eventValue.Symbol);
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

        _objectMapper.Map(context, seedSymbol);
        await SaveEntityAsync(seedSymbol);
        await SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
        await SaveNftActivityIndexAsync(eventValue, context, seedSymbol.Id, seedSymbol.Decimals);
    }
    private async Task SaveNFTListingChangeIndexAsync(LogEventContext context, string symbol)
    {
        if (context.ChainId.Equals(ForestIndexerConstants.MainChain))
        {
            return;
        }

        if (symbol.Equals(ForestIndexerConstants.TokenSimpleElf))
        {
            return;
        }
        var nftListingChangeIndex = new NFTListingChangeIndex
        {
            Symbol = symbol,
            Id = IdGenerateHelper.GetId(context.ChainId, symbol),
            UpdateTime = context.Block.BlockTime
        };
        _objectMapper.Map(context, nftListingChangeIndex);
        await SaveEntityAsync(nftListingChangeIndex);

    }
    private async Task SaveCollectionChangeIndexAsync(LogEventContext context, string symbol)
    {
        var collectionChangeIndex = new CollectionChangeIndex();
        var nftCollectionSymbol = SymbolHelper.GetNFTCollectionSymbol(symbol);
        if (nftCollectionSymbol == null)
        {
            return;
        }

        collectionChangeIndex.Symbol = nftCollectionSymbol;
        collectionChangeIndex.Id = IdGenerateHelper.GetNFTCollectionId(context.ChainId, nftCollectionSymbol);
        collectionChangeIndex.UpdateTime = context.Block.BlockTime;
        _objectMapper.Map(context, collectionChangeIndex);
        await SaveEntityAsync(collectionChangeIndex);
    }
    
    private async Task HandleForNFTTransferAsync(Transferred eventValue, LogEventContext context)
    {
        var nftInfoIndexId = IdGenerateHelper.GetNFTInfoId(context.ChainId, eventValue.Symbol);
        var nftInfoIndex = await GetEntityAsync<NFTInfoIndex>(nftInfoIndexId);
        if (nftInfoIndex == null) return;
        
        _objectMapper.Map(context, nftInfoIndex);
        await SaveEntityAsync(nftInfoIndex);
        await SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
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

        var fromUserBalance = await SaveUserBalanceAsync(eventValue.Symbol,
            eventValue.From.ToBase58(),
            -eventValue.Amount, context);
        // await _nftOfferProvider.UpdateOfferRealQualityAsync(eventValue.Symbol, fromUserBalance,
        //     eventValue.From.ToBase58(),
        //     context); todo v2
        // await _listingInfoProvider.UpdateListingInfoRealQualityAsync(eventValue.Symbol, fromUserBalance,
        //     eventValue.From.ToBase58(), context);todp v2
    }
    
    public async Task<long> SaveUserBalanceAsync(String symbol, String address, long amount, LogEventContext context)
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
            userBalanceIndex.Amount += amount;
            userBalanceIndex.ChangeTime = context.Block.BlockTime;
        }

        _objectMapper.Map(context, userBalanceIndex);
        Logger.LogInformation("SaveUserBalanceAsync Address {Address} symbol {Symbol} balance {Balance}", address,
            symbol, userBalanceIndex.Amount);
        await SaveEntityAsync(userBalanceIndex);
        return userBalanceIndex.Amount;
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
        var userBalanceTo = await QueryUserBalanceByIdAsync(userBalanceToId, context.ChainId);
        if (userBalanceTo == null)
        {
            // var lastNFTListingInfoDic =
            //     await _listingInfoProvider.QueryLatestNFTListingInfoByNFTIdsAsync(new List<string> { nftInfoIndexId },
            //         ""); todo v2
            var lastNFTListingInfoDic = new Dictionary<string, NFTListingInfoIndex>();//todo v2
            
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
        await SaveEntityAsync(userBalanceTo);
        await _nftOfferProvider.UpdateOfferRealQualityAsync(eventValue.Symbol, userBalanceTo.Amount,
            eventValue.To.ToBase58(), context);
        // await _listingInfoProvider.UpdateListingInfoRealQualityAsync(eventValue.Symbol, userBalanceTo.Amount, eventValue.To.ToBase58(), context); todo v2
        await SaveNFTOfferChangeIndexAsync(context, eventValue.Symbol, EventType.Other);
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
    
    private async Task<UserBalanceIndex> QueryUserBalanceByIdAsync(string userBalanceId, string chainId)
    {
        if (userBalanceId.IsNullOrWhiteSpace() ||
            chainId.IsNullOrWhiteSpace())
        {
            return null;
        }
        return await GetEntityAsync<UserBalanceIndex>(userBalanceId);
    }
}