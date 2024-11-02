using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using AElf;
using AElf.Contracts.TokenAdapterContract;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class ManagerTokenCreatedLogEventProcessor : LogEventProcessorBase<ManagerTokenCreated>
{
    private readonly IObjectMapper _objectMapper;

    public ManagerTokenCreatedLogEventProcessor(
        IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetTokenAdaptorContractAddress(chainId);
    }

    public override async Task ProcessAsync(ManagerTokenCreated eventValue, LogEventContext context)
    {
        Logger.LogDebug("1-ManagerTokenCreatedLogEventProcessor.HandleEventAsync.eventValue {A}",
                        JsonConvert.SerializeObject(eventValue));
        // Logger.LogDebug("2-ManagerTokenCreatedLogEventProcessor.HandleEventAsync.eventValue " +
        //                 JsonConvert.SerializeObject(context));

        if (eventValue == null || context == null) return;
        var tsmSeedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        var tsmSeedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(tsmSeedSymbolIndexId);

        if (tsmSeedSymbolIndex != null)
        {
            _objectMapper.Map(context, tsmSeedSymbolIndex);
            tsmSeedSymbolIndex.Status = SeedStatus.REGISTERED;
            // Logger.LogDebug("3-ManagerTokenCreatedLogEventProcessor.HandleEventAsync.eventValue tsmSeedSymbolIndex {A}", JsonConvert.SerializeObject(tsmSeedSymbolIndex));

            await SaveEntityAsync(tsmSeedSymbolIndex);

            var seedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, tsmSeedSymbolIndex.SeedSymbol);
            var seedSymbolIndex = await GetEntityAsync<SeedSymbolIndex>(seedSymbolIndexId);

            if (seedSymbolIndex != null)
            {
                _objectMapper.Map(context, seedSymbolIndex);
                seedSymbolIndex.SeedStatus = SeedStatus.REGISTERED;
                // Logger.LogDebug("3-ManagerTokenCreatedLogEventProcessor.HandleEventAsync.eventValue seedSymbolIndex {A}",
                //                 JsonConvert.SerializeObject(seedSymbolIndex));
                await SaveEntityAsync(seedSymbolIndex);
            }
        }

        var symbolMarketTokenIndexId = IdGenerateHelper.GetSymbolMarketTokenId(context.ChainId, eventValue.Symbol);
        var symbolMarketTokenIndex = await GetEntityAsync<SeedSymbolMarketTokenIndex>(symbolMarketTokenIndexId);

        if (symbolMarketTokenIndex != null) return;

        var realOwner = eventValue.RealOwner != null
            ? eventValue.RealOwner.ToBase58()
            : eventValue.OwnerManagerList.ToBase58();
        var realManager = eventValue.RealIssuer != null
            ? eventValue.RealIssuer.ToBase58()
            : eventValue.IssuerManagerList.ToBase58();
        symbolMarketTokenIndex = new SeedSymbolMarketTokenIndex()
        {
            Id = symbolMarketTokenIndexId,
            TokenName = eventValue.TokenName,
            OwnerManagerSet = new HashSet<string> { realOwner },
            RandomOwnerManager = realOwner,
            IssueManagerSet = new HashSet<string> { realManager },
            RandomIssueManager = realManager,
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
            CreateTime = context.Block.BlockTime
        };

        if (eventValue.ExternalInfo.Value.ContainsKey(
                EnumDescriptionHelper.GetEnumDescription(TokenCreatedExternalInfoEnum.NFTLogoImageUrl)))
        {
            symbolMarketTokenIndex.SymbolMarketTokenLogoImage =
                eventValue.ExternalInfo.Value[
                    EnumDescriptionHelper.GetEnumDescription(TokenCreatedExternalInfoEnum.NFTLogoImageUrl)];
        }

        // Logger.LogDebug("9-ManagerTokenCreatedLogEventProcessor");
        _objectMapper.Map(context, symbolMarketTokenIndex);
        Logger.LogDebug("10-ManagerTokenCreatedLogEventProcessor {A}",symbolMarketTokenIndex.Id);
        await SaveEntityAsync(symbolMarketTokenIndex);
    }
}