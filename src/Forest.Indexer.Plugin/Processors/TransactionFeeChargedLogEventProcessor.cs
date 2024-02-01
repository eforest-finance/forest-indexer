using AElf.Contracts.MultiToken;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Runtime;

namespace Forest.Indexer.Plugin.Processors;

public class TransactionFeeChargedLogEventProcessor : AElfLogEventProcessorBase<TransactionFeeCharged, LogEventInfo>
{
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly INFTOfferProvider _nftOfferProvider;
    private readonly INFTOfferChangeProvider _nftOfferChangeProvider;
    private readonly ILogger<AElfLogEventProcessorBase<TransactionFeeCharged, LogEventInfo>> _logger;
    

    public TransactionFeeChargedLogEventProcessor(ILogger<AElfLogEventProcessorBase<TransactionFeeCharged, LogEventInfo>> logger
        , IUserBalanceProvider userBalanceProvider
        , INFTOfferProvider nftOfferProvider
        , INFTOfferChangeProvider nftOfferChangeProvider
        , IOptionsSnapshot<ContractInfoOptions> contractInfoOptions) : base(logger)
    {
        _logger = logger;
        _contractInfoOptions = contractInfoOptions.Value;
        _userBalanceProvider = userBalanceProvider;
        _nftOfferProvider = nftOfferProvider;
        _nftOfferChangeProvider = nftOfferChangeProvider;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos?.FirstOrDefault(c => c?.ChainId == chainId)?.TokenContractAddress;
    }

    protected override async Task HandleEventAsync(TransactionFeeCharged eventValue, LogEventContext context)
    {
        _logger.Debug("TransactionFeeChargedLogEventProcessor-1"+JsonConvert.SerializeObject(eventValue));
        _logger.Debug("TransactionFeeChargedLogEventProcessor-2"+JsonConvert.SerializeObject(context));
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