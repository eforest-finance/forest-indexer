using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class SeedCreatedProcessor : LogEventProcessorBase<SeedCreated>
{
    private readonly IObjectMapper _objectMapper;
    public SeedCreatedProcessor(
        IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }
    
    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetSymbolRegistrarContractAddress(chainId);
    }

    public async override Task ProcessAsync(SeedCreated eventValue, LogEventContext context)
    {
        Logger.LogDebug("SeedCreatedProcessor-1 {A}",JsonConvert.SerializeObject(eventValue));
        Logger.LogDebug("SeedCreatedProcessor-2 {B}",JsonConvert.SerializeObject(context));
        var seedSymbolIndex = await GetSeedSymbolIndexAsync(context.ChainId, eventValue.OwnedSymbol, eventValue.Symbol);
        _objectMapper.Map(context, seedSymbolIndex);
        seedSymbolIndex.SeedSymbol = eventValue.Symbol;
        seedSymbolIndex.Symbol = eventValue.OwnedSymbol;
        seedSymbolIndex.Status = SeedStatus.UNREGISTERED;
        seedSymbolIndex.RegisterTime = DateTimeHelper.ToUnixTimeMilliseconds(context.Block.BlockTime);
        seedSymbolIndex.ExpireTime = eventValue.ExpireTime;
        seedSymbolIndex.OfType(eventValue.SeedType);
        seedSymbolIndex.Owner = eventValue.To.ToBase58();
        seedSymbolIndex.SeedImage = eventValue.ImageUrl;
        if (eventValue.SeedType == SeedType.Disable)
        {
            seedSymbolIndex.Status = SeedStatus.NOTSUPPORT;
        }
        Logger.LogDebug("SeedCreatedProcessor ImageUrl:{ImageUrl}", eventValue.ImageUrl);
        Logger.LogDebug("SeedCreatedProcessor save TsmSeedSymbolIndex:{A}", JsonConvert.SerializeObject(seedSymbolIndex));

        await SaveEntityAsync(seedSymbolIndex);
        if (seedSymbolIndex.IntSeedType != (int)SeedType.Unique)
        {
            Logger.LogDebug("SeedCreatedProcessor-3");
            var seedMainChainChangeIndex = new SeedMainChainChangeIndex
            {
                Symbol = eventValue.Symbol,
                UpdateTime = context.Block.BlockTime,
                TransactionId = context.Transaction.TransactionId,
                Id = IdGenerateHelper.GetSeedMainChainChangeId(context.ChainId, seedSymbolIndex.SeedSymbol)
            };
            _objectMapper.Map(context, seedMainChainChangeIndex);
            await SaveEntityAsync(seedMainChainChangeIndex);
            Logger.LogDebug("SeedCreatedProcessor-4 {A}",JsonConvert.SerializeObject(seedMainChainChangeIndex));
        }
        
        //update the same prefix nft or ft seed symbol status
        var tokenType = TokenHelper.GetTokenType(eventValue.OwnedSymbol);
        if (tokenType==TokenType.FT)
        {
           var nftSymbol= TokenHelper.GetNftSymbol(eventValue.OwnedSymbol);
           // var nftSeedSymbolId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, nftSymbol);
            // var nftSeedSymbolIndex = 
            // await GetEntityAsync<TsmSeedSymbolIndex>(nftSeedSymbolId);
            
            var newId = IdGenerateHelper.GetNewTsmSeedSymbolId(context.ChainId, eventValue.Symbol,
                nftSymbol);
            var oldId = IdGenerateHelper.GetOldTsmSeedSymbolId(context.ChainId, nftSymbol);
            var nftSeedSymbolIndexId = newId;
                
            var nftSeedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(nftSeedSymbolIndexId);
            if (nftSeedSymbolIndex == null)
            {
                Logger.LogDebug("new nftSeedSymbolIndex is null id={A}", nftSeedSymbolIndexId);
                nftSeedSymbolIndexId = oldId;
                nftSeedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(nftSeedSymbolIndexId);
                if (nftSeedSymbolIndex == null)
                {
                    Logger.LogDebug("old nftSeedSymbolIndex is null id={A}", nftSeedSymbolIndexId);
                    return;
                }
                nftSeedSymbolIndex.Id = newId;
            }
            
            _objectMapper.Map(context, nftSeedSymbolIndex);
            nftSeedSymbolIndex.Status = SeedStatus.NOTSUPPORT;
            await SaveEntityAsync(nftSeedSymbolIndex);
        }
        else
        {
            var ftSymbol= TokenHelper.GetFtSymbol(eventValue.OwnedSymbol);
            // var ftSeedSymbolId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, ftSymbol);
            // var ftSeedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(ftSeedSymbolId);
            
            var newId = IdGenerateHelper.GetNewTsmSeedSymbolId(context.ChainId, eventValue.Symbol,
                ftSymbol);
            var oldId = IdGenerateHelper.GetOldTsmSeedSymbolId(context.ChainId, ftSymbol);
            var ftSeedSymbolId = newId;
            
            var ftSeedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(ftSeedSymbolId);

            if (ftSeedSymbolIndex == null)
            {
                Logger.LogDebug("new ftSeedSymbolIndex is null id={A}", ftSeedSymbolId);

                ftSeedSymbolId = oldId;
                ftSeedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(ftSeedSymbolId);
                if (ftSeedSymbolIndex == null)
                {
                    Logger.LogDebug("old nftSeedSymbolIndex is null id={A}", ftSeedSymbolId);
                    return;
                }
                ftSeedSymbolIndex.Id = newId;
            }
            
            _objectMapper.Map(context, ftSeedSymbolIndex);
            ftSeedSymbolIndex.Status = SeedStatus.NOTSUPPORT;
            await SaveEntityAsync(ftSeedSymbolIndex);
        }
    }

    public async Task<TsmSeedSymbolIndex> GetSeedSymbolIndexAsync(string chainId, string seedOwnedSymbol, string seedSymbol)
    {
        var tsmSeedSymbolId = IdGenerateHelper.GetOldTsmSeedSymbolId(chainId, seedOwnedSymbol);
        var seedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(tsmSeedSymbolId);
        
        if (seedSymbolIndex == null)
        {
            Logger.LogDebug("SeedCreatedProcessor old seedSymbolIndex is null id={A}",tsmSeedSymbolId);
            tsmSeedSymbolId = IdGenerateHelper.GetNewTsmSeedSymbolId(chainId, seedSymbol, seedOwnedSymbol);
            seedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(tsmSeedSymbolId);

            if (seedSymbolIndex == null)
            {
                Logger.LogDebug("SeedCreatedProcessor new seedSymbolIndex is null id={A}",tsmSeedSymbolId);
                seedSymbolIndex = new TsmSeedSymbolIndex
                {
                    Id = tsmSeedSymbolId,
                    Symbol = seedOwnedSymbol,
                    SeedName = IdGenerateHelper.GetSeedName(seedOwnedSymbol)
                };
            }

            seedSymbolIndex.OfType(TokenHelper.GetTokenType(seedOwnedSymbol));
        }

        return seedSymbolIndex;
    }
}