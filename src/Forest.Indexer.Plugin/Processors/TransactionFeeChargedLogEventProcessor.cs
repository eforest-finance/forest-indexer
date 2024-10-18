using AeFinder.Sdk.Processor;
using AElf.Contracts.MultiToken;
using AutoMapper.Internal.Mappers;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Forest.Indexer.Plugin.Processors;

public class TransactionFeeChargedLogEventProcessor : LogEventProcessorBase<TransactionFeeCharged>
{
    private readonly INFTOfferProvider _nftOfferProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<TransactionFeeChargedLogEventProcessor> _logger;
    

    public TransactionFeeChargedLogEventProcessor(ILogger<TransactionFeeChargedLogEventProcessor> logger,
        IObjectMapper objectMapper,
        INFTOfferProvider nftOfferProvider)
    {
        _objectMapper = objectMapper;
        _logger = logger;
        _nftOfferProvider = nftOfferProvider;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetTokenContractAddress(chainId);
    }

    public async override Task ProcessAsync(TransactionFeeCharged eventValue, LogEventContext context)
    {
        _logger.LogDebug("TransactionFeeChargedLogEventProcessor-1"+JsonConvert.SerializeObject(eventValue));
        _logger.LogDebug("TransactionFeeChargedLogEventProcessor-2"+JsonConvert.SerializeObject(context));
        if (eventValue == null) return;
        if (context == null) return;
        var needRecordBalance =
            await _nftOfferProvider.NeedRecordBalance(eventValue.Symbol, eventValue.ChargingAddress.ToBase58(),
                context.ChainId);
        if (!needRecordBalance)
        {
            return;
        }
        var userBalance = await SaveUserBalanceAsync(eventValue.Symbol,
            eventValue.ChargingAddress.ToBase58(),
            -eventValue.Amount, context);
        await _nftOfferProvider.UpdateOfferRealQualityAsync(eventValue.Symbol, userBalance, eventValue.ChargingAddress.ToBase58(), context);
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
        _logger.LogInformation("SaveUserBalanceAsync Address {Address} symbol {Symbol} balance {Balance}", address,
            symbol, userBalanceIndex.Amount);
        await SaveEntityAsync(userBalanceIndex);
        return userBalanceIndex.Amount;
    }
}