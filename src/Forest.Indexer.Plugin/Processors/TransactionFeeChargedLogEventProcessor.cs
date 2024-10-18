using AeFinder.Sdk.Processor;
using AElf.Contracts.MultiToken;
using Forest.Indexer.Plugin.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Forest.Indexer.Plugin.Processors;

public class TransactionFeeChargedLogEventProcessor : LogEventProcessorBase<TransactionFeeCharged>
{
    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly INFTOfferProvider _nftOfferProvider;
    private readonly ILogger<TransactionFeeChargedLogEventProcessor> _logger;
    

    public TransactionFeeChargedLogEventProcessor(ILogger<TransactionFeeChargedLogEventProcessor> logger
        , IUserBalanceProvider userBalanceProvider
        , INFTOfferProvider nftOfferProvider)
    {
        _logger = logger;
        _userBalanceProvider = userBalanceProvider;
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
        var userBalance = await _userBalanceProvider.SaveUserBalanceAsync(eventValue.Symbol,
            eventValue.ChargingAddress.ToBase58(),
            -eventValue.Amount, context);
        await _nftOfferProvider.UpdateOfferRealQualityAsync(eventValue.Symbol, userBalance, eventValue.ChargingAddress.ToBase58(), context);
    }
}