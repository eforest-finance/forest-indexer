using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Contracts.Auction;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class AuctionTimeUpdatedLogEventProcessor : AElfLogEventProcessorBase<AuctionTimeUpdated, LogEventInfo>
{
    private readonly IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo> _symbolAuctionInfoIndexRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly ISeedProvider _seedProvider;
    private readonly ITsmSeedSymbolProvider _tsmSeedSymbolProvider;
    private readonly ILogger<AElfLogEventProcessorBase<AuctionTimeUpdated, LogEventInfo>> _logger;
    
    public AuctionTimeUpdatedLogEventProcessor(ILogger<AElfLogEventProcessorBase<AuctionTimeUpdated, LogEventInfo>> logger,
                                               IObjectMapper objectMapper,
                                               IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo> symbolAuctionInfoIndexRepository,
                                               ISeedProvider seedProvider,
                                               ITsmSeedSymbolProvider tsmSeedSymbolProvider,
                                               IOptionsSnapshot<ContractInfoOptions> contractInfoOptions)
        : base(logger)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _symbolAuctionInfoIndexRepository = symbolAuctionInfoIndexRepository;
        _seedProvider = seedProvider;
        _tsmSeedSymbolProvider = tsmSeedSymbolProvider;
    }


    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos?.FirstOrDefault(c => c?.ChainId == chainId)?.AuctionContractAddress;
    }

    protected override async Task HandleEventAsync(AuctionTimeUpdated eventValue, LogEventContext context)
    {
        
        if (eventValue == null) return;
        
        _logger.LogDebug("AuctionTimeUpdated eventValue AuctionId {AuctionId} EndTime {EndTime} MaxEndTime{MaxEndTime}",
            eventValue.AuctionId.ToHex(), eventValue.EndTime.Seconds, eventValue.MaxEndTime.Seconds);
        
        var auctionInfoIndex = await _symbolAuctionInfoIndexRepository.GetFromBlockStateSetAsync(eventValue.AuctionId.ToHex(), context.ChainId);

        if (auctionInfoIndex != null)
        {
            if (eventValue.StartTime != null)
            {
                auctionInfoIndex.StartTime = eventValue.StartTime.Seconds;
            }

            if (eventValue.EndTime != null)
            {
                auctionInfoIndex.EndTime = eventValue.EndTime.Seconds;
            }

            if (eventValue.MaxEndTime != null)
            {
                auctionInfoIndex.MaxEndTime = eventValue.MaxEndTime.Seconds;
            }
            auctionInfoIndex.TransactionHash = context.TransactionId;
            
            _objectMapper.Map(context, auctionInfoIndex);
            await _symbolAuctionInfoIndexRepository.AddOrUpdateAsync(auctionInfoIndex);
            await _tsmSeedSymbolProvider.UpdateAuctionEndTimeAsync(context, auctionInfoIndex.Symbol, eventValue.EndTime.Seconds);
        }
    }
}