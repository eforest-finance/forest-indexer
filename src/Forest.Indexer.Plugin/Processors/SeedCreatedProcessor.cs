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
        Logger.LogDebug("SeedCreatedProcessor-1"+JsonConvert.SerializeObject(eventValue));
        Logger.LogDebug("SeedCreatedProcessor-2"+JsonConvert.SerializeObject(context));
        var seedSymbolIndex = await GetSeedSymbolIndexAsync(context.ChainId, eventValue.OwnedSymbol);
        Logger.LogDebug("SeedCreatedProcessor ImageUrl:{ImageUrl}", eventValue.ImageUrl);
        _objectMapper.Map(context, seedSymbolIndex);
        seedSymbolIndex.SeedSymbol = eventValue.Symbol;
        seedSymbolIndex.Symbol = eventValue.OwnedSymbol;
        seedSymbolIndex.Status = SeedStatus.UNREGISTERED;
        seedSymbolIndex.RegisterTime = DateTimeHelper.ToUnixTimeMilliseconds(context.Block.BlockTime);
        seedSymbolIndex.ExpireTime = eventValue.ExpireTime;
        seedSymbolIndex.SeedType = eventValue.SeedType;
        seedSymbolIndex.Owner = eventValue.To.ToBase58();
        seedSymbolIndex.SeedImage = eventValue.ImageUrl;
        if (eventValue.SeedType == SeedType.Disable)
        {
            seedSymbolIndex.Status = SeedStatus.NOTSUPPORT;
        }
        await SaveEntityAsync(seedSymbolIndex);
        if (seedSymbolIndex.SeedType != SeedType.Unique)
        {
            Logger.LogDebug("SeedCreatedProcessor-3");
            var seedMainChainChangeIndex = new SeedMainChainChangeIndex
            {
                Symbol = eventValue.Symbol,
                UpdateTime = context.Block.BlockTime,
                TransactionId = context.Transaction.TransactionId,
                Id = IdGenerateHelper.GetSeedMainChainChangeId(context.ChainId, seedSymbolIndex.SeedSymbol)
            };
            Logger.LogDebug("SeedCreatedProcessor-4"+JsonConvert.SerializeObject(seedMainChainChangeIndex));
            _objectMapper.Map(context, seedMainChainChangeIndex);
            Logger.LogDebug("SeedCreatedProcessor-5"+JsonConvert.SerializeObject(seedMainChainChangeIndex));
            await SaveEntityAsync(seedMainChainChangeIndex);
            Logger.LogDebug("SeedCreatedProcessor-6");
        }
        
        //update the same prefix nft or ft seed symbol status
        var tokenType = TokenHelper.GetTokenType(eventValue.OwnedSymbol);
        if (tokenType==TokenType.FT)
        {
            var nftSymbol= TokenHelper.GetNftSymbol(eventValue.OwnedSymbol);
            var nftSeedSymbolId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, nftSymbol);
            var nftSeedSymbolIndex = 
            await GetEntityAsync<TsmSeedSymbolIndex>(nftSeedSymbolId);
            if(nftSeedSymbolIndex==null) {return;}
            
            _objectMapper.Map(context, nftSeedSymbolIndex);
            nftSeedSymbolIndex.Status = SeedStatus.NOTSUPPORT;
            await SaveEntityAsync(nftSeedSymbolIndex);
        }
        else
        {
            var ftSymbol= TokenHelper.GetFtSymbol(eventValue.OwnedSymbol);
            var ftSeedSymbolId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, ftSymbol);
            var ftSeedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(ftSeedSymbolId);
            if(ftSeedSymbolIndex==null) {return;}
            
            _objectMapper.Map(context, ftSeedSymbolIndex);
            ftSeedSymbolIndex.Status = SeedStatus.NOTSUPPORT;
            await SaveEntityAsync(ftSeedSymbolIndex);
        }
    }
    
    public async Task<TsmSeedSymbolIndex> GetSeedSymbolIndexAsync(string chainId, string symbol)
    {
        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, symbol);
        var seedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(seedSymbolId);
        
        if (seedSymbolIndex == null)
        {
            seedSymbolIndex = new TsmSeedSymbolIndex
            {
                Id = seedSymbolId,
                Symbol = symbol,
                SeedName = IdGenerateHelper.GetSeedName(symbol),
                TokenType = TokenHelper.GetTokenType(symbol)
            };
        }

        return seedSymbolIndex;
    }
}