using AeFinder.Sdk.Processor;
using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Util;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class  SpecialSeedAddedLogEventProcessor: LogEventProcessorBase<SpecialSeedAdded>
{
    private readonly IObjectMapper _objectMapper;
    
    public SpecialSeedAddedLogEventProcessor(
        IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }
    
    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetSymbolRegistrarContractAddress(chainId);
    }
    
    public async override Task ProcessAsync(SpecialSeedAdded eventValue, LogEventContext context)
    {
        if (eventValue == null) return;
        
        var seedList = eventValue.AddList.Value;
        foreach (var seed in seedList)
        {
            var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, seed.Symbol);

            var seedSymbolIndex = new TsmSeedSymbolIndex
            {
                Id = seedSymbolId,
                ChainId = context.ChainId,
                Symbol = seed.Symbol,
                SeedName = IdGenerateHelper.GetSeedName(seed.Symbol),
                Status = SeedStatus.AVALIABLE,
                AuctionType = seed.AuctionType,
                TokenType = TokenHelper.GetTokenType(seed.Symbol),
                SeedType = seed.SeedType,
                TokenPrice = new TokenPriceInfo()
                {
                    Symbol = seed.PriceSymbol,
                    Amount = seed.PriceAmount
                },
                IsBurned = false
            };
            _objectMapper.Map(context, seedSymbolIndex);
            if (seed.SeedType == SeedType.Disable)
            {
                seedSymbolIndex.Status = SeedStatus.NOTSUPPORT;
            }
            await SaveEntityAsync(seedSymbolIndex);
        }
    }
}