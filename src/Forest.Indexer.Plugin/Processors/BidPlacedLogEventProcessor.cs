using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class BidPlacedLogEventProcessor : AElfLogEventProcessorBase<Forest.Contracts.Auction.BidPlaced, LogEventInfo>
{
    private readonly IAElfIndexerClientEntityRepository<SymbolBidInfoIndex, LogEventInfo> _symbolBidInfoIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo> _symbolAuctionInfoIndexRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAuctionInfoProvider _auctionInfoProvider;
    private readonly ISeedProvider _seedProvider;
    private readonly ICollectionChangeProvider _collectionChangeProvider;
    private readonly ITsmSeedSymbolProvider _tsmSeedSymbolProvider;
    private readonly ILogger<AElfLogEventProcessorBase<Forest.Contracts.Auction.BidPlaced, LogEventInfo>> _logger;

    public BidPlacedLogEventProcessor(ILogger<AElfLogEventProcessorBase<Forest.Contracts.Auction.BidPlaced, LogEventInfo>> logger,
                                      IObjectMapper objectMapper,
                                      IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo> symbolAuctionInfoIndexRepository,
                                      IAElfIndexerClientEntityRepository<SymbolBidInfoIndex, LogEventInfo> symbolBidInfoIndexRepository,
                                      ISeedProvider seedProvider,
                                      ICollectionChangeProvider collectionChangeProvider, 
                                      IOptionsSnapshot<ContractInfoOptions> contractInfoOptions, 
                                      IAuctionInfoProvider auctionInfoProvider,
                                      ITsmSeedSymbolProvider tsmSeedSymbolProvider) : base(logger)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _symbolAuctionInfoIndexRepository = symbolAuctionInfoIndexRepository;
        _symbolBidInfoIndexRepository = symbolBidInfoIndexRepository;
        _auctionInfoProvider = auctionInfoProvider;
        _seedProvider = seedProvider;
        _collectionChangeProvider = collectionChangeProvider;
        _tsmSeedSymbolProvider = tsmSeedSymbolProvider;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos?.FirstOrDefault(c => c?.ChainId == chainId)?.AuctionContractAddress;
    }

    protected override async Task HandleEventAsync(Forest.Contracts.Auction.BidPlaced eventValue, LogEventContext context)
    {
        _logger.LogDebug("BidPlaced eventValue AuctionId {AuctionId}", eventValue.AuctionId.ToHex());
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("BidPlaced eventValue  get date start time:{time} amount:{amount} BlockHeight:{BlockHeight}", startTime.ToString(), eventValue.Price.Amount,
            context.BlockHeight);


        if (eventValue == null) return;

        var auctionInfoIndex = await _symbolAuctionInfoIndexRepository.GetFromBlockStateSetAsync(eventValue.AuctionId.ToHex(), context.ChainId);

        var enDTime = DateTime.UtcNow;
        _logger.LogInformation("BidPlaced eventValue  Symbol: {Symbol} amount:{amount} get symbol end time:{time}", auctionInfoIndex.Symbol, eventValue.Price.Amount,
            enDTime.ToString());
        _logger.LogInformation("BidPlaced eventValue  Symbol: {Symbol} amount:{amount} get GetFromBlockStateSetAsync cost  time:{time}ms", auctionInfoIndex.Symbol,
            eventValue.Price.Amount, (enDTime - startTime).TotalMilliseconds);

        var indexerStart = DateTime.UtcNow;
        _logger.LogInformation("BidPlaced eventValue Symbol: {Symbol} amount:{amount} start time:{time} BlockHeight:{BlockHeight}", auctionInfoIndex.Symbol, eventValue.Price.Amount,
            indexerStart.ToString(),context.BlockHeight);
        if (auctionInfoIndex != null)
        {
            auctionInfoIndex.FinishPrice = new TokenPriceInfo
            {
                Amount = eventValue.Price.Amount,
                Symbol = eventValue.Price.Symbol
            };
            auctionInfoIndex.TransactionHash = context.TransactionId;
            auctionInfoIndex.FinishBidder = eventValue.Bidder.ToBase58();
            _objectMapper.Map(context, auctionInfoIndex);
            await _symbolAuctionInfoIndexRepository.AddOrUpdateAsync(auctionInfoIndex);
            var symbolBidInfoIndex = new SymbolBidInfoIndex
            {
                Id = context.TransactionId,
                Symbol = auctionInfoIndex.Symbol,
                Bidder = eventValue.Bidder.ToBase58(),
                PriceAmount = eventValue.Price.Amount,
                PriceSymbol = eventValue.Price.Symbol,
                BidTime = eventValue.BidTime.Seconds,
                AuctionId = eventValue.AuctionId.ToHex(),
                TransactionHash = context.TransactionId
            };
            _objectMapper.Map(context, symbolBidInfoIndex);
            await _symbolBidInfoIndexRepository.AddOrUpdateAsync(symbolBidInfoIndex);
            var indexerEnd = DateTime.UtcNow;
            _logger.LogInformation("BidPlaced eventValue Symbol: {Symbol} amount:{amount} end time:{time} ms BlockHeight:{BlockHeight}", auctionInfoIndex.Symbol,
                eventValue.Price.Amount,
                indexerEnd.ToString(),context.BlockHeight);

            _logger.LogInformation("BidPlaced eventValue Symbol: {Symbol} amount:{amount} update cost time:{time} ms BlockHeight:{BlockHeight} ", auctionInfoIndex.Symbol, eventValue.Price.Amount,
                (indexerEnd - indexerStart).TotalMilliseconds, context.BlockHeight);
            
            await _tsmSeedSymbolProvider.HandleBidPlacedAsync(context, eventValue, symbolBidInfoIndex, auctionInfoIndex.EndTime);
            await _collectionChangeProvider.SaveCollectionPriceChangeIndexAsync(context, auctionInfoIndex.CollectionSymbol);
        }

        await _auctionInfoProvider.SetSeedSymbolIndexPriceByAuctionInfoAsync(eventValue.AuctionId.ToHex(), DateTimeHelper.FromUnixTimeSeconds(eventValue.BidTime.Seconds), context);
    }
}