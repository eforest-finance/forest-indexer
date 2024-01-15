using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class SpecialSeedAddedLogEventProcessor: AElfLogEventProcessorBase<SpecialSeedAdded, LogEventInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> _tsmSeedSymbolIndexRepository;
    
    public SpecialSeedAddedLogEventProcessor(
        ILogger<AElfLogEventProcessorBase<SpecialSeedAdded, LogEventInfo>> logger, 
        IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> tsmSeedSymbolIndexRepository,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions) : base(logger)
    {
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _tsmSeedSymbolIndexRepository = tsmSeedSymbolIndexRepository;
    }
    
    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos?.FirstOrDefault(c => c?.ChainId == chainId)
            ?.SymbolRegistrarContractAddress;
    }
    
    protected override async Task HandleEventAsync(SpecialSeedAdded eventValue, LogEventContext context)
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
            await _tsmSeedSymbolIndexRepository.AddOrUpdateAsync(seedSymbolIndex);
        }
    }
}