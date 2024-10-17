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

    public AuctionTimeUpdatedLogEventProcessor(
        IObjectMapper objectMapper,
        IAElfClientServiceProvider aElfClientServiceProvider
    )
    {
        _objectMapper = objectMapper;
        _aElfClientServiceProvider = aElfClientServiceProvider;
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
        /*var mustQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.ChainId)
                .Value(chainId)),

            q => q.Term(i => i.Field(f => f.SeedSymbol)
                .Value(seedSymbol))
        };

        QueryContainer Filter(QueryContainerDescriptor<TsmSeedSymbolIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var result = await _tsmSeedSymbolIndexRepository.GetListAsync(Filter);
        return result.Item2.IsNullOrEmpty() ? null : result.Item2.FirstOrDefault();*/
        //todo V2 getTsmSeedInfo from Contract.need test by self
        var tokenContractAddress = ContractInfoHelper.GetTokenContractAddress(chainId);
        var tokenInfo =
            await _aElfClientServiceProvider.GetTokenInfoAsync(chainId, tokenContractAddress, seedSymbol);
        if (tokenInfo == null)
        {
            return null;
        }
        var seedOwnedSymbol = EnumDescriptionHelper.GetExtraInfoValue(tokenInfo.ExternalInfo, TokenCreatedExternalInfoEnum.SeedOwnedSymbol);
        if (seedOwnedSymbol == null)
        {
            return null;
        }

        var seedSymbolIndexId = IdGenerateHelper.GetTsmSeedSymbolId(chainId, seedOwnedSymbol);
        return new TsmSeedSymbolIndex()
        {
            Id = seedSymbolIndexId
        };
    }
}