using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Whitelist;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class WhitelistAddressInfoAddedProcessor : AElfLogEventProcessorBase<WhitelistAddressInfoAdded, LogEventInfo>
{
    private readonly ContractInfoOptions _contractInfoOptions;
    private ILogger<AElfLogEventProcessorBase<WhitelistAddressInfoAdded, LogEventInfo>> _logger;
    private readonly IWhiteListProvider _whitelistProvider;
    private readonly IObjectMapper _objectMapper;

    public WhitelistAddressInfoAddedProcessor(
        ILogger<AElfLogEventProcessorBase<WhitelistAddressInfoAdded, LogEventInfo>> logger,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions, IWhiteListProvider whitelistProvider,
        IObjectMapper objectMapper) : base(logger)
    {
        _logger = logger;
        _contractInfoOptions = contractInfoOptions.Value;
        _whitelistProvider = whitelistProvider;
        _objectMapper = objectMapper;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).WhitelistContractAddress;
    }

    protected override async Task HandleEventAsync(WhitelistAddressInfoAdded eventValue, LogEventContext context)
    {
        var whitelistId = eventValue.WhitelistId.ToHex();
        _logger.Debug("[WhitelistAddressInfoAdded] START: Id={Id}", whitelistId);

        try
        {
            if (eventValue.ExtraInfoIdList == null || eventValue.ExtraInfoIdList.Value.Count <= 0)
                throw new UserFriendlyException("extraInfo empty");
            
            _logger.Debug("[WhitelistAddressInfoAdded] SAVE: Id={Id}", whitelistId);

            await _whitelistProvider.AddWhiteListExtraInfoAsync(
                context,
                eventValue.ExtraInfoIdList.Value.ToList(), context.ChainId,
                whitelistId);
        
            _logger.Debug("[WhitelistAddressInfoAdded] FINISH: Id={Id}", whitelistId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[WhitelistAddressInfoAdded] FAILED: Id={Id}", whitelistId);
            throw;
        }


    }
}