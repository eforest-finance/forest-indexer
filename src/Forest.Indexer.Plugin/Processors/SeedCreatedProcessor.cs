using System.Diagnostics;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Processors.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class SeedCreatedProcessor: AElfLogEventProcessorBase<SeedCreated, LogEventInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> _tsmSeedSymbolIndexRepository;
    private readonly ISeedProvider _seedProvider;
    private readonly ILogger<SeedCreatedProcessor> _loggerProcessor;
    private readonly IAElfIndexerClientEntityRepository<SeedMainChainChangeIndex, LogEventInfo>
        _seedMainChainChangeIndexRepository;
    private readonly ILogger<AElfLogEventProcessorBase<SeedCreated, LogEventInfo>> _logger;

    public SeedCreatedProcessor(
        ILogger<AElfLogEventProcessorBase<SeedCreated, LogEventInfo>> logger,
        IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> tsmSeedSymbolIndexRepository,
        ISeedProvider seedProvider,
        IAElfIndexerClientEntityRepository<SeedMainChainChangeIndex, LogEventInfo>
            seedMainChainChangeIndexRepository,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,ILogger<SeedCreatedProcessor> loggerProcessor) : base(logger)
    {
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _tsmSeedSymbolIndexRepository = tsmSeedSymbolIndexRepository;
        _seedProvider = seedProvider;
        _loggerProcessor = loggerProcessor;
        _seedMainChainChangeIndexRepository = seedMainChainChangeIndexRepository;
        _logger = logger;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos?.FirstOrDefault(c => c?.ChainId == chainId)
            ?.SymbolRegistrarContractAddress;
    }

    protected override async Task HandleEventAsync(SeedCreated eventValue, LogEventContext context)
    {
        _logger.Debug("SeedCreatedProcessor-1"+JsonConvert.SerializeObject(eventValue));
        _logger.Debug("SeedCreatedProcessor-2"+JsonConvert.SerializeObject(context));
        var seedSymbolIndex = await _seedProvider.GetSeedSymbolIndexAsync(context.ChainId, eventValue.OwnedSymbol);
        _loggerProcessor.LogDebug("SeedCreatedProcessor ImageUrl:{ImageUrl}", eventValue.ImageUrl);
        _objectMapper.Map(context, seedSymbolIndex);
        seedSymbolIndex.SeedSymbol = eventValue.Symbol;
        seedSymbolIndex.Symbol = eventValue.OwnedSymbol;
        seedSymbolIndex.Status = SeedStatus.UNREGISTERED;
        seedSymbolIndex.RegisterTime = DateTimeHelper.ToUnixTimeMilliseconds(context.BlockTime);
        seedSymbolIndex.ExpireTime = eventValue.ExpireTime;
        seedSymbolIndex.SeedType = eventValue.SeedType;
        seedSymbolIndex.Owner = eventValue.To.ToBase58();
        seedSymbolIndex.SeedImage = eventValue.ImageUrl;
        if (eventValue.SeedType == SeedType.Disable)
        {
            seedSymbolIndex.Status = SeedStatus.NOTSUPPORT;
        }
        await _tsmSeedSymbolIndexRepository.AddOrUpdateAsync(seedSymbolIndex);

        if (seedSymbolIndex.SeedType != SeedType.Unique)
        {
            _logger.Debug("SeedCreatedProcessor-3");
            var seedMainChainChangeIndex = new SeedMainChainChangeIndex
            {
                Symbol = eventValue.Symbol,
                UpdateTime = context.BlockTime,
                TransactionId = context.TransactionId,
                Id = IdGenerateHelper.GetSeedMainChainChangeId(context.ChainId, seedSymbolIndex.SeedSymbol)
            };
            _logger.Debug("SeedCreatedProcessor-4"+JsonConvert.SerializeObject(seedMainChainChangeIndex));
            _objectMapper.Map(context, seedMainChainChangeIndex);
            _logger.Debug("SeedCreatedProcessor-5"+JsonConvert.SerializeObject(seedMainChainChangeIndex));
            await _seedMainChainChangeIndexRepository.AddOrUpdateAsync(seedMainChainChangeIndex);
            _logger.Debug("SeedCreatedProcessor-6");
        }
        
        //update the same prefix nft or ft seed symbol status
        var tokenType = TokenHelper.GetTokenType(eventValue.OwnedSymbol);
        if (tokenType==TokenType.FT)
        {
            var nftSymbol= TokenHelper.GetNftSymbol(eventValue.OwnedSymbol);
            var nftSeedSymbolId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, nftSymbol);
            var nftSeedSymbolIndex = await _tsmSeedSymbolIndexRepository.GetFromBlockStateSetAsync(nftSeedSymbolId, context.ChainId);
            if(nftSeedSymbolIndex==null) {return;}
            
            _objectMapper.Map(context, nftSeedSymbolIndex);
            nftSeedSymbolIndex.Status = SeedStatus.NOTSUPPORT;
            await _tsmSeedSymbolIndexRepository.AddOrUpdateAsync(nftSeedSymbolIndex);
        }
        else
        {
            var ftSymbol= TokenHelper.GetFtSymbol(eventValue.OwnedSymbol);
            var ftSeedSymbolId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, ftSymbol);
            var ftSeedSymbolIndex = await _tsmSeedSymbolIndexRepository.GetFromBlockStateSetAsync(ftSeedSymbolId, context.ChainId);
            if(ftSeedSymbolIndex==null) {return;}
            
            _objectMapper.Map(context, ftSeedSymbolIndex);
            ftSeedSymbolIndex.Status = SeedStatus.NOTSUPPORT;
            await _tsmSeedSymbolIndexRepository.AddOrUpdateAsync(ftSeedSymbolIndex);
        }
    }
}