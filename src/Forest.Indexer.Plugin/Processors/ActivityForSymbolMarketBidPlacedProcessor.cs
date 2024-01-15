using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class ActivityForSymbolMarketBidPlacedProcessor : ActivityProcessorBase<Forest.Contracts.Auction.BidPlaced>
{
    private readonly IAElfIndexerClientEntityRepository<SymbolMarketActivityIndex, TransactionInfo>
        _symbolMarketActivityIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo>
        _seedSymbolIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo>
        _symbolAuctionInfoIndexRepository;

    private readonly ILogger<AElfLogEventProcessorBase<Forest.Contracts.Auction.BidPlaced, TransactionInfo>> _logger;
    private readonly ContractInfoOptions _contractInfoOptions;

    public ActivityForSymbolMarketBidPlacedProcessor(
        IAElfIndexerClientEntityRepository<SymbolMarketActivityIndex, TransactionInfo>
            symbolMarketActivityIndexRepository,
        IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo>
            seedSymbolIndexRepository,
        IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo>
            symbolAuctionInfoIndexRepository,
        IObjectMapper objectMapper,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        ILogger<AElfLogEventProcessorBase<Forest.Contracts.Auction.BidPlaced, TransactionInfo>> logger) : base(
        objectMapper, contractInfoOptions,
        logger)
    {
        _symbolMarketActivityIndexRepository = symbolMarketActivityIndexRepository;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _symbolAuctionInfoIndexRepository = symbolAuctionInfoIndexRepository;
        _contractInfoOptions = contractInfoOptions.Value;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).AuctionContractAddress;
    }

    protected override async Task HandleEventAsync(Forest.Contracts.Auction.BidPlaced eventValue,
        LogEventContext context)
    {
        if (eventValue == null || context == null) return;
        var auctionInfoIndex =
            await _symbolAuctionInfoIndexRepository.GetFromBlockStateSetAsync(eventValue.AuctionId.ToHex(),
                context.ChainId);
        if (auctionInfoIndex == null)
        {
            return;
        }

        var seedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId,auctionInfoIndex.Symbol);
        var seedSymbolIndex =
            await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolIndexId,
                context.ChainId);
        if (seedSymbolIndex == null) return;
        var symbolAuctionInfoIndex =
            await _symbolAuctionInfoIndexRepository.GetFromBlockStateSetAsync(eventValue.AuctionId.ToHex(),
                context.ChainId);
        if (symbolAuctionInfoIndex == null) return;
        var symbolMarketActivityId = IdGenerateHelper.GetSymbolMarketActivityId(
            SymbolMarketActivityType.Buy.ToString(), context.ChainId, seedSymbolIndex.SeedOwnedSymbol,
            context.From, context.To, context.TransactionId);
        var symbolMarketActivityIndex =
            await _symbolMarketActivityIndexRepository.GetFromBlockStateSetAsync(symbolMarketActivityId,
                context.ChainId);
        if (symbolMarketActivityIndex != null) return;

        symbolMarketActivityIndex =
            await buildSymbolMarketActivityIndexAsync(symbolMarketActivityId, eventValue, context,
                seedSymbolIndex.SeedType, symbolAuctionInfoIndex.Symbol);
        _objectMapper.Map(context, symbolMarketActivityIndex);
        symbolMarketActivityIndex.SeedSymbol = seedSymbolIndex.Symbol;
        await _symbolMarketActivityIndexRepository.AddOrUpdateAsync(symbolMarketActivityIndex);
    }

    private async Task<SymbolMarketActivityIndex> buildSymbolMarketActivityIndexAsync(string symbolMarketActivityId,
        Forest.Contracts.Auction.BidPlaced eventValue,
        LogEventContext context, SeedType seedType, string symbol)
    {
        return new SymbolMarketActivityIndex
        {
            Id = symbolMarketActivityId,
            Type = SymbolMarketActivityType.Bid,
            Price = eventValue.Price.Amount,
            PriceSymbol = eventValue.Price.Symbol,

            TransactionDateTime = eventValue.BidTime.ToDateTime(),
            Symbol = symbol,
            Address = FullAddressHelper.ToFullAddress(eventValue.Bidder.ToBase58(), context.ChainId),
            SeedType = seedType,
            TransactionFee = GetFeeTypeElfAmount(context.ExtraProperties),
            TransactionFeeSymbol = FeeMapTypeElf,
            TransactionId = context.TransactionId,
        };
    }
}