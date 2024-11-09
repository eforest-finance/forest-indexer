using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Forest.Contracts.Auction;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class AuctionCreatedLogEventProcessor : LogEventProcessorBase<AuctionCreated>
{
    private readonly IObjectMapper _objectMapper;

    public AuctionCreatedLogEventProcessor(
        IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetAuctionContractAddress(chainId);
    }

    public override async Task ProcessAsync(AuctionCreated eventValue, LogEventContext context)
    {
        Logger.LogDebug("AuctionCreated eventValue AuctionId {AuctionId} Symbol {Symbol}", eventValue.AuctionId.ToHex(), eventValue.Symbol);
        Logger.LogDebug("AuctionCreated eventValue eventValue {eventValue}", JsonConvert.SerializeObject(eventValue));

        if (eventValue == null) return;

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
            TransactionHash = context.Transaction.TransactionId
        };
        _objectMapper.Map(context, symbolAuctionInfoIndex);
        await SaveEntityAsync(symbolAuctionInfoIndex);
        await SetSeedSymbolIndexPriceByAuctionInfoAsync(eventValue.AuctionId.ToHex(),context.Block.BlockTime, context);
        await SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);
    }
    public async Task SetSeedSymbolIndexPriceByAuctionInfoAsync(string auctionId, DateTime dateTime, LogEventContext context)
    {
        Logger.LogDebug("SetSeedSymbolIndexPriceByAuctionInfoAsync 1 {chainId} {auctionId}",context.ChainId,auctionId);
        var auctionInfoIndex = await GetEntityAsync<SymbolAuctionInfoIndex>(auctionId);
        if (auctionInfoIndex == null)
        {
            Logger.LogDebug("SetSeedSymbolIndexPriceByAuctionInfoAsync 1-stop {chainId} {symbol}",context.ChainId,auctionInfoIndex.Symbol);
            return;
        }
        Logger.LogDebug("SetSeedSymbolIndexPriceByAuctionInfoAsync 2 {chainId} {symbol}",context.ChainId,auctionInfoIndex.Symbol);
        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, auctionInfoIndex.Symbol);
        var seedSymbolIndex = await GetEntityAsync<SeedSymbolIndex>(seedSymbolId);
        if (seedSymbolIndex == null)
        {
            Logger.LogDebug("SetSeedSymbolIndexPriceByAuctionInfoAsync 2-stop {chainId} {symbol}",context.ChainId,auctionInfoIndex.Symbol);
            return;
        }
        Logger.LogDebug("SetSeedSymbolIndexPriceByAuctionInfoAsync 3 {chainId} {symbol}",context.ChainId,auctionInfoIndex.Symbol);

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
        Logger.LogDebug("SetSeedSymbolIndexPriceByAuctionInfoAsync 4 {chainId} {symbol}",context.ChainId,auctionInfoIndex.Symbol);
        _objectMapper.Map(context, seedSymbolIndex);
        Logger.LogDebug("SetSeedSymbolIndexPriceByAuctionInfoAsync 4 {chainId} {symbol} {seedSymbolIndex}",context.ChainId,auctionInfoIndex.Symbol,JsonConvert.SerializeObject(seedSymbolIndex));
        await SaveEntityAsync(seedSymbolIndex);
    }
    public async Task SaveCollectionPriceChangeIndexAsync(LogEventContext context, string symbol)
    {
        var collectionPriceChangeIndex = new CollectionPriceChangeIndex();
        var nftCollectionSymbol = SymbolHelper.GetNFTCollectionSymbol(symbol);
        if (nftCollectionSymbol == null)
        {
            return;
        }

        collectionPriceChangeIndex.Symbol = nftCollectionSymbol;
        collectionPriceChangeIndex.Id = IdGenerateHelper.GetNFTCollectionId(context.ChainId, nftCollectionSymbol);
        collectionPriceChangeIndex.UpdateTime = context.Block.BlockTime;
        _objectMapper.Map(context, collectionPriceChangeIndex);
        await SaveEntityAsync(collectionPriceChangeIndex);

    }
}