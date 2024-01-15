using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
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

    public AuctionInfoProvider(IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo> symbolAuctionInfoIndexRepository,
                               IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> seedSymbolIndexRepository, IObjectMapper objectMapper)
    {
        _symbolAuctionInfoIndexRepository = symbolAuctionInfoIndexRepository;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _objectMapper = objectMapper;
    }

    public async Task SetSeedSymbolIndexPriceByAuctionInfoAsync(string auctionId, DateTime dateTime, LogEventContext context)
    {
        var auctionInfoIndex = await _symbolAuctionInfoIndexRepository.GetFromBlockStateSetAsync(auctionId, context.ChainId);
        if (auctionInfoIndex == null)
        {
            return;
        }

        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, auctionInfoIndex.Symbol);
        var seedSymbolIndex = await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolId, context.ChainId);
        if (seedSymbolIndex == null)
        {
            return;
        }

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
        _objectMapper.Map(context, seedSymbolIndex);
        await _seedSymbolIndexRepository.AddOrUpdateAsync(seedSymbolIndex);
    }
}