using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Contracts.Auction;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class AuctionCreatedLogEventProcessor : AElfLogEventProcessorBase<AuctionCreated, LogEventInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly ILogger<AElfLogEventProcessorBase<AuctionCreated, LogEventInfo>> _logger;
    private readonly IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo>
        _symbolAuctionInfoIndexRepository;

    private readonly IAuctionInfoProvider _auctionInfoProvider;
    private readonly ICollectionProvider _collectionProvider;
    private readonly ICollectionChangeProvider _collectionChangeProvider;

    public AuctionCreatedLogEventProcessor(ILogger<AElfLogEventProcessorBase<AuctionCreated, LogEventInfo>> logger,
                                           IObjectMapper objectMapper,
                                           IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo> symbolAuctionInfoIndexRepository,
                                           IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
                                           IAuctionInfoProvider auctionInfoProvider,
                                           ICollectionProvider collectionProvider,
                                           ICollectionChangeProvider collectionChangeProvider)
        : base(logger)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _symbolAuctionInfoIndexRepository = symbolAuctionInfoIndexRepository;
        _auctionInfoProvider = auctionInfoProvider;
        _collectionProvider = collectionProvider;
        _collectionChangeProvider = collectionChangeProvider;
    }


    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos?.FirstOrDefault(c => c?.ChainId == chainId)?.AuctionContractAddress;
    }

    protected override async Task HandleEventAsync(AuctionCreated eventValue, LogEventContext context)
    {
        _logger.LogDebug("AuctionCreated eventValue AuctionId {AuctionId} Symbol {Symbol}", eventValue.AuctionId.ToHex(), eventValue.Symbol);
        _logger.LogDebug("AuctionCreated eventValue eventValue {eventValue}", JsonConvert.SerializeObject(eventValue));

        if (eventValue == null) return;

        var fromBlockStateSetAsync = await _symbolAuctionInfoIndexRepository.GetFromBlockStateSetAsync(eventValue.AuctionId.ToHex(), context.ChainId);

        if (fromBlockStateSetAsync != null)
        {
            return;
        }
  
        var symbolAuctionInfoIndex = new SymbolAuctionInfoIndex
        {
            Id = eventValue.AuctionId.ToHex(),
            Symbol = eventValue.Symbol,
            StartPrice = new TokenPriceInfo
            {
                Symbol = eventValue.StartPrice.Symbol,
                Amount = eventValue.StartPrice.Amount
            },
            FinishPrice = new TokenPriceInfo
            {
                Symbol = eventValue.StartPrice.Symbol,
                Amount = eventValue.StartPrice.Amount
            },
            StartTime = eventValue.StartTime != null ? eventValue.StartTime.Seconds : 0,
            EndTime = eventValue.EndTime != null ? eventValue.EndTime.Seconds : 0,
            MaxEndTime = eventValue.MaxEndTime != null ? eventValue.MaxEndTime.Seconds : 0,
            MinMarkup = eventValue.AuctionConfig.MinMarkup,
            Duration = eventValue.AuctionConfig.Duration,
            Creator = eventValue.Creator?.ToBase58(),
            ReceivingAddress = eventValue.ReceivingAddress?.ToBase58(),
            CollectionSymbol = ForestIndexerConstants.SeedCollectionSymbol,
            TransactionHash = context.TransactionId
        };
        _objectMapper.Map(context, symbolAuctionInfoIndex);
        await _symbolAuctionInfoIndexRepository.AddOrUpdateAsync(symbolAuctionInfoIndex);
        await _auctionInfoProvider.SetSeedSymbolIndexPriceByAuctionInfoAsync(eventValue.AuctionId.ToHex(),context.BlockTime, context);
        await _collectionChangeProvider.SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);
    }
}