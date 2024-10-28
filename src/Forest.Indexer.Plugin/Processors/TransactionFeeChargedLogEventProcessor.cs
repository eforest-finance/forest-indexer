using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using AElf.Contracts.MultiToken;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class TransactionFeeChargedLogEventProcessor : LogEventProcessorBase<TransactionFeeCharged>
{
    private readonly IObjectMapper _objectMapper;
    private readonly IReadOnlyRepository<OfferInfoIndex> _nftOfferIndexRepository;
    

    public TransactionFeeChargedLogEventProcessor(
        IObjectMapper objectMapper,
        IReadOnlyRepository<OfferInfoIndex> nftOfferIndexRepository)
    {
        _objectMapper = objectMapper;
        _nftOfferIndexRepository = nftOfferIndexRepository;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetTokenContractAddress(chainId);
    }

    public async override Task ProcessAsync(TransactionFeeCharged eventValue, LogEventContext context)
    {
        Logger.LogDebug("TransactionFeeChargedLogEventProcessor-1 {A}",JsonConvert.SerializeObject(eventValue));
        Logger.LogDebug("TransactionFeeChargedLogEventProcessor-2 {A}",JsonConvert.SerializeObject(context));
        if (eventValue == null) return;
        if (context == null) return;
        var needRecordBalance =
            await NeedRecordBalance(eventValue.Symbol, eventValue.ChargingAddress.ToBase58(),
                context.ChainId);
        if (!needRecordBalance)
        {
            return;
        }
        var userBalance = await SaveUserBalanceAsync(eventValue.Symbol,
            eventValue.ChargingAddress.ToBase58(),
            -eventValue.Amount, context);
        await UpdateOfferRealQualityAsync(eventValue.Symbol, userBalance, eventValue.ChargingAddress.ToBase58(), context);
    }
    private async Task<bool> NeedRecordBalance(string symbol, string offerFrom, string chainId)
    {
        if (!SymbolHelper.CheckSymbolIsELF(symbol))
        {
            return true;
        }

        if (ForestIndexerConstants.NeedRecordBalanceOptionsAddressList.Contains(offerFrom))
        {
            return true;
        } 

        var num = 0;
        var offerNumId = IdGenerateHelper.GetOfferNumId(chainId, offerFrom);
        var nftOfferNumIndex =
            await GetEntityAsync<UserNFTOfferNumIndex>(offerNumId);
        if (nftOfferNumIndex == null)
        {
            num = 0;
        }
        else
        {
            num = nftOfferNumIndex.OfferNum;
        }
        
        if (num > 0)
        {
            return true;
        }

        return false;
    }
    private async Task UpdateOfferRealQualityAsync(string symbol, long balance, string offerFrom,
        LogEventContext context)
    {
        if (context.ChainId.Equals(ForestIndexerConstants.MainChain))
        {
            return;
        }
        if (!SymbolHelper.CheckSymbolIsELF(symbol))
        {
            return;
        }
        int skip = 0;
        int limit = 80;
        
        {
            var queryable = await _nftOfferIndexRepository.GetQueryableAsync();
            var utcNow = DateTime.UtcNow;
            
            queryable = queryable.Where(i => i.ExpireTime > utcNow);
            queryable = queryable.Where(i => i.PurchaseToken.Symbol == symbol);
            queryable = queryable.Where(i => i.ChainId == context.ChainId);
            queryable = queryable.Where(i => i.OfferFrom == offerFrom);

            var result = queryable.OrderByDescending(i => i.Price)
                .Skip(skip)
                .Take(limit)
                .ToList();

            if (result.IsNullOrEmpty())
            {
                return;
            }

            var tokenIndexId = IdGenerateHelper.GetId(context.ChainId, symbol);
            var tokenIndex = await GetEntityAsync<TokenInfoIndex>(tokenIndexId);
            if (tokenIndex == null)
            {
                return;
            }

            //update RealQuantity
            foreach (var offerInfoIndex in result)
            {
                if (symbol.Equals(offerInfoIndex!.PurchaseToken.Symbol))
                {
                    var symbolTokenIndexId = IdGenerateHelper.GetId(context.ChainId, offerInfoIndex.BizSymbol);
                    var symbolTokenInfo =
                        await GetEntityAsync<TokenInfoIndex>(symbolTokenIndexId);
                    
                    var canBuyNum = Convert.ToInt64(Math.Floor(Convert.ToDecimal(balance) /
                                                               (offerInfoIndex.Price *
                                                                (decimal)Math.Pow(10,
                                                                    tokenIndex.Decimals))));
                    canBuyNum = (long)(canBuyNum * (decimal)Math.Pow(10, symbolTokenInfo.Decimals));
                    // Logger.LogInformation(
                    //     "UpdateOfferRealQualityAsync  offerInfoIndex.BizSymbol {BizSymbol} canBuyNum {CanBuyNum} Quantity {Quantity} RealQuantity {RealQuantity}",
                    //     offerInfoIndex.BizSymbol, canBuyNum, offerInfoIndex.Quantity, offerInfoIndex.RealQuantity);
                    
                    var realQuantity = Math.Min(offerInfoIndex.Quantity,
                        canBuyNum);
                    if (realQuantity != offerInfoIndex.RealQuantity)
                    {
                        offerInfoIndex.RealQuantity = realQuantity;
                        _objectMapper.Map(context, offerInfoIndex);
                        var research = GetEntityAsync<OfferInfoIndex>(offerInfoIndex.Id);
                        if (research == null)
                        {
                            // Logger.LogInformation(
                            //     "UpdateOfferRealQualityAsync offerInfoIndex.Id is not exist,not update {OfferInfoIndexId}",
                            //     offerInfoIndex.Id);
                            continue;
                        }
                        await SaveEntityAsync(offerInfoIndex);
                    }
                }
            }
        } 
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
}