using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Forest.Contracts.Auction;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class AuctionCreatedLogEventProcessor : LogEventProcessorBase<AuctionCreated>
{
    private readonly IObjectMapper _objectMapper;

    private readonly IAuctionInfoProvider _auctionInfoProvider;
    private readonly ICollectionProvider _collectionProvider;
    private readonly ICollectionChangeProvider _collectionChangeProvider;

    public AuctionCreatedLogEventProcessor(
        IObjectMapper objectMapper,
        IAuctionInfoProvider auctionInfoProvider,
        ICollectionProvider collectionProvider,
        ICollectionChangeProvider collectionChangeProvider)
    {
        _objectMapper = objectMapper;
        _auctionInfoProvider = auctionInfoProvider;
        _collectionProvider = collectionProvider;
        _collectionChangeProvider = collectionChangeProvider;
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

        var fromBlockStateSetAsync = await GetEntityAsync<SymbolAuctionInfoIndex>(eventValue.AuctionId.ToHex());

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
            TransactionHash = context.Transaction.TransactionId
        };
        _objectMapper.Map(context, symbolAuctionInfoIndex);
        await SaveEntityAsync(symbolAuctionInfoIndex);
        await _auctionInfoProvider.SetSeedSymbolIndexPriceByAuctionInfoAsync(eventValue.AuctionId.ToHex(),context.Block.BlockTime, context);
        await _collectionChangeProvider.SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);
    }
}