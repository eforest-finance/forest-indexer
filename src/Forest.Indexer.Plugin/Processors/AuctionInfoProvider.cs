using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public interface IAuctionInfoProvider
{
    public Task SetSeedSymbolIndexPriceByAuctionInfoAsync(string auctionId,DateTime dateTime, LogEventContext context);
}

public class AuctionInfoProvider : IAuctionInfoProvider, ISingletonDependency
{
    private readonly IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo> _symbolAuctionInfoIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<IAuctionInfoProvider> _logger;

    public AuctionInfoProvider(IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo> symbolAuctionInfoIndexRepository,
    ILogger<IAuctionInfoProvider> logger,
                               IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> seedSymbolIndexRepository, IObjectMapper objectMapper)
    {
        _symbolAuctionInfoIndexRepository = symbolAuctionInfoIndexRepository;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task SetSeedSymbolIndexPriceByAuctionInfoAsync(string auctionId, DateTime dateTime, LogEventContext context)
    {
        _logger.Debug("SetSeedSymbolIndexPriceByAuctionInfoAsync 1 {chainId} {auctionId}",context.ChainId,auctionId);
        var auctionInfoIndex = await _symbolAuctionInfoIndexRepository.GetFromBlockStateSetAsync(auctionId, context.ChainId);
        if (auctionInfoIndex == null)
        {
            _logger.Debug("SetSeedSymbolIndexPriceByAuctionInfoAsync 1-stop {chainId} {symbol}",context.ChainId,auctionInfoIndex.Symbol);
            return;
        }
        _logger.Debug("SetSeedSymbolIndexPriceByAuctionInfoAsync 2 {chainId} {symbol}",context.ChainId,auctionInfoIndex.Symbol);
        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, auctionInfoIndex.Symbol);
        var seedSymbolIndex = await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolId, context.ChainId);
        if (seedSymbolIndex == null)
        {
            _logger.Debug("SetSeedSymbolIndexPriceByAuctionInfoAsync 2-stop {chainId} {symbol}",context.ChainId,auctionInfoIndex.Symbol);
            return;
        }
        _logger.Debug("SetSeedSymbolIndexPriceByAuctionInfoAsync 3 {chainId} {symbol}",context.ChainId,auctionInfoIndex.Symbol);

        if (auctionInfoIndex.FinishPrice != null && auctionInfoIndex.FinishPrice.Amount >= 0)
        {
            seedSymbolIndex.AuctionPriceSymbol = auctionInfoIndex.FinishPrice.Symbol;
            seedSymbolIndex.AuctionPrice = DecimalUntil.ConvertToElf(auctionInfoIndex.FinishPrice.Amount);
        }
        else
        {
            seedSymbolIndex.AuctionPriceSymbol = auctionInfoIndex.StartPrice.Symbol;
            seedSymbolIndex.AuctionPrice = DecimalUntil.ConvertToElf(auctionInfoIndex.StartPrice.Amount);
            
        }
        seedSymbolIndex.MaxAuctionPrice = seedSymbolIndex.AuctionPrice;
        seedSymbolIndex.HasAuctionFlag = true;

        seedSymbolIndex.AuctionDateTime = dateTime;
        seedSymbolIndex.BeginAuctionPrice = DecimalUntil.ConvertToElf(auctionInfoIndex.StartPrice.Amount);
        _logger.Debug("SetSeedSymbolIndexPriceByAuctionInfoAsync 4 {chainId} {symbol}",context.ChainId,auctionInfoIndex.Symbol);
        _objectMapper.Map(context, seedSymbolIndex);
        _logger.Debug("SetSeedSymbolIndexPriceByAuctionInfoAsync 4 {chainId} {symbol} {seedSymbolIndex}",context.ChainId,auctionInfoIndex.Symbol,JsonConvert.SerializeObject(seedSymbolIndex));
        await _seedSymbolIndexRepository.AddOrUpdateAsync(seedSymbolIndex);
    }
}