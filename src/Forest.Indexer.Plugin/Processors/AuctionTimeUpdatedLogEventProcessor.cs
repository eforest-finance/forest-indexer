using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Forest.Contracts.Auction;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Util;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class AuctionTimeUpdatedLogEventProcessor : LogEventProcessorBase<AuctionTimeUpdated>
{
    private readonly IObjectMapper _objectMapper;
    private readonly IAElfClientServiceProvider _aElfClientServiceProvider;
    private readonly IReadOnlyRepository<TsmSeedSymbolIndex> _tsmSeedSymbolIndexRepository;

    public AuctionTimeUpdatedLogEventProcessor(
        IObjectMapper objectMapper,
        IAElfClientServiceProvider aElfClientServiceProvider,
        IReadOnlyRepository<TsmSeedSymbolIndex> tsmSeedSymbolIndexRepository
    )
    {
        _objectMapper = objectMapper;
        _aElfClientServiceProvider = aElfClientServiceProvider;
        _tsmSeedSymbolIndexRepository = tsmSeedSymbolIndexRepository;
    }


    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetAuctionContractAddress(chainId);
    }

    public override async Task ProcessAsync(AuctionTimeUpdated eventValue, LogEventContext context)
    {
        
        if (eventValue == null) return;
        
        Logger.LogDebug("AuctionTimeUpdated eventValue AuctionId {AuctionId} EndTime {EndTime} MaxEndTime{MaxEndTime}",
            eventValue.AuctionId.ToHex(), eventValue.EndTime.Seconds, eventValue.MaxEndTime.Seconds);
        
        var auctionInfoIndex = await GetEntityAsync<SymbolAuctionInfoIndex>(eventValue.AuctionId.ToHex());

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
            auctionInfoIndex.TransactionHash = context.Transaction.TransactionId;
            _objectMapper.Map(context, auctionInfoIndex);
            await SaveEntityAsync(auctionInfoIndex);
            await UpdateAuctionEndTimeAsync(context, auctionInfoIndex.Symbol, eventValue.EndTime.Seconds);
        }
    }
    public async Task UpdateAuctionEndTimeAsync(LogEventContext context, string symbol, long auctionEndTime)
    {
        var tsmSeed = await GetTsmSeedAsync(context.ChainId, symbol);
        if (tsmSeed == null)
        {
            Logger.LogInformation("UpdateAuctionEndTimeAsync tsmSeed is null, chainId:{chainId} symbol:{symbol}",
                context.ChainId, symbol);
            return;
        }

        var seedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(tsmSeed.Id);
        if (seedSymbolIndex == null)
        {
            return;
        }

        _objectMapper.Map(context, seedSymbolIndex);
        seedSymbolIndex.AuctionEndTime = auctionEndTime;
        Logger.LogInformation(
            "UpdateAuctionEndTimeAsync tsmSeedSymbolId {tsmSeedSymbolId} auctionEndTime:{auctionEndTime}",
            tsmSeed.Id, auctionEndTime);
        await SaveEntityAsync(seedSymbolIndex);

    }
    private async Task<TsmSeedSymbolIndex> GetTsmSeedAsync(string chainId, string seedSymbol)
    {
        //todo V2 GetTsmSeedAsync //code: done, need test

        var queryable = await _tsmSeedSymbolIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(x=>x.ChainId == chainId && x.SeedSymbol == seedSymbol);
        var list = queryable.OrderByDescending(i => i.ExpireTime).Skip(0).Take(1).ToList();
        return list.IsNullOrEmpty() ? null : list.FirstOrDefault();
    }
}