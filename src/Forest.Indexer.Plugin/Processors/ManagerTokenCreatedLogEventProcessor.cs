using AElf;
using AElf.Contracts.TokenAdapterContract;
using AElf.Kernel;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class ManagerTokenCreatedLogEventProcessor :
    AElfLogEventProcessorBase<ManagerTokenCreated, LogEventInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;

    private readonly IAElfIndexerClientEntityRepository<SeedSymbolMarketTokenIndex, LogEventInfo>
        _symbolMarketTokenIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo>
        _tsmSeedSymbolIndexRepository;

    private readonly ILogger<AElfLogEventProcessorBase<ManagerTokenCreated, LogEventInfo>> _logger;
    private readonly IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;


    public ManagerTokenCreatedLogEventProcessor(
        IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<SeedSymbolMarketTokenIndex, LogEventInfo> symbolMarketTokenIndexRepository,
        IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo>
            tsmSeedSymbolIndexRepository,
        ILogger<AElfLogEventProcessorBase<ManagerTokenCreated, LogEventInfo>> logger,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions, IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> seedSymbolIndexRepository) : base(logger)
    {
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _symbolMarketTokenIndexRepository = symbolMarketTokenIndexRepository;
        _tsmSeedSymbolIndexRepository = tsmSeedSymbolIndexRepository;
        _logger = logger;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos?.FirstOrDefault(c => c?.ChainId == chainId)?.TokenAdaptorContractAddress;
    }

    protected override async Task HandleEventAsync(ManagerTokenCreated eventValue, LogEventContext context)
    {
        _logger.Debug("1-ManagerTokenCreatedLogEventProcessor.HandleEventAsync.eventValue " + JsonConvert.SerializeObject(eventValue));
        _logger.Debug("2-ManagerTokenCreatedLogEventProcessor.HandleEventAsync.eventValue " + JsonConvert.SerializeObject(context));

        if (eventValue == null || context == null) return;
        var tsmSeedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        var tsmSeedSymbolIndex =
            await _tsmSeedSymbolIndexRepository.GetFromBlockStateSetAsync(tsmSeedSymbolIndexId,
                context.ChainId);
        if (tsmSeedSymbolIndex != null)
        {
            _objectMapper.Map(context, tsmSeedSymbolIndex);
            tsmSeedSymbolIndex.Status = SeedStatus.REGISTERED;
            _logger.Debug("3-ManagerTokenCreatedLogEventProcessor.HandleEventAsync.eventValue tsmSeedSymbolIndex " + JsonConvert.SerializeObject(tsmSeedSymbolIndex));

            await _tsmSeedSymbolIndexRepository.AddOrUpdateAsync(tsmSeedSymbolIndex);

            var seedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, tsmSeedSymbolIndex.SeedSymbol);
            var seedSymbolIndex =
                await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolIndexId,
                    context.ChainId);
            if (seedSymbolIndex != null)
            {
                _objectMapper.Map(context, seedSymbolIndex);
                seedSymbolIndex.SeedStatus = SeedStatus.REGISTERED;
                _logger.Debug("3-ManagerTokenCreatedLogEventProcessor.HandleEventAsync.eventValue seedSymbolIndex" + JsonConvert.SerializeObject(seedSymbolIndex));

                await _seedSymbolIndexRepository.AddOrUpdateAsync(seedSymbolIndex);
            }
        }

        var symbolMarketTokenIndexId = IdGenerateHelper.GetSymbolMarketTokenId(context.ChainId, eventValue.Symbol);
        var symbolMarketTokenIndex =
            await _symbolMarketTokenIndexRepository.GetFromBlockStateSetAsync(symbolMarketTokenIndexId,
                context.ChainId);
        if (symbolMarketTokenIndex != null) return;

        var realOwner = eventValue.RealOwner != null
            ? eventValue.RealOwner.ToBase58()
            : eventValue.OwnerManagerList == null ? "" : eventValue.OwnerManagerList.ToBase58();
        var realIssuer = eventValue.RealIssuer != null
            ? eventValue.RealIssuer.ToBase58()
            : eventValue.IssuerManagerList == null ? "" : eventValue.IssuerManagerList.ToBase58();
        symbolMarketTokenIndex = new SeedSymbolMarketTokenIndex()
        {
            Id = symbolMarketTokenIndexId,
            TokenName = eventValue.TokenName,
            OwnerManagerSet = new HashSet<string> { realOwner },
            RandomOwnerManager = realOwner,
            IssueManagerSet = new HashSet<string> { realIssuer },
            RandomIssueManager = realIssuer,
            Decimals = eventValue.Decimals,
            TotalSupply = eventValue.TotalSupply,
            Supply = eventValue.Amount,
            Issued = eventValue.Amount,
            IssueChainId = eventValue.IssueChainId,
            IssueChain = ChainHelper.ConvertChainIdToBase58(eventValue.IssueChainId),
            SameChainFlag = context.ChainId.Equals(ChainHelper.ConvertChainIdToBase58(eventValue.IssueChainId)),
            IsBurnable = eventValue.IsBurnable,
            Symbol = eventValue.Symbol,
            Owner = eventValue.Owner.ToBase58(),
            Issuer = eventValue.Issuer.ToBase58(),
            ExternalInfoDictionary = eventValue.ExternalInfo.Value
                .Select(entity => new ExternalInfoDictionary
                {
                    Key = entity.Key,
                    Value = entity.Value
                }).ToList(),
            CreateTime = context.BlockTime
        };
        if (eventValue.ExternalInfo.Value.ContainsKey(
                EnumDescriptionHelper.GetEnumDescription(TokenCreatedExternalInfoEnum.NFTLogoImageUrl)))
        {
            symbolMarketTokenIndex.SymbolMarketTokenLogoImage =
                eventValue.ExternalInfo.Value[
                    EnumDescriptionHelper.GetEnumDescription(TokenCreatedExternalInfoEnum.NFTLogoImageUrl)];
        }

        _logger.Debug("9-ManagerTokenCreatedLogEventProcessor.HandleEventAsync.eventValue " + JsonConvert.SerializeObject(symbolMarketTokenIndex));
        _objectMapper.Map(context, symbolMarketTokenIndex);
        _logger.Debug("10-ManagerTokenCreatedLogEventProcessor.HandleEventAsync.eventValue " + JsonConvert.SerializeObject(symbolMarketTokenIndex));
        await _symbolMarketTokenIndexRepository.AddOrUpdateAsync(symbolMarketTokenIndex);
    }
}