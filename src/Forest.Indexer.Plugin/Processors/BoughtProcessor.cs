using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class BoughtProcessor: LogEventProcessorBase<Bought>
{
    private readonly IObjectMapper _objectMapper;
    private readonly IReadOnlyRepository<TsmSeedSymbolIndex> _tsmSeedSymbolIndexRepository;

    public BoughtProcessor(
        IObjectMapper objectMapper, IReadOnlyRepository<TsmSeedSymbolIndex> tsmSeedSymbolIndexRepository)
    {
        _objectMapper = objectMapper;
        _tsmSeedSymbolIndexRepository = tsmSeedSymbolIndexRepository;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetSymbolRegistrarContractAddress(chainId);
    }

    public override async Task ProcessAsync(Bought eventValue, LogEventContext context)
    {
        var seedSymbolIndex = await GetSeedSymbolIndexAsync(context.ChainId, eventValue.Symbol);
        _objectMapper.Map(context, seedSymbolIndex);
        seedSymbolIndex.TokenPrice = new TokenPriceInfo
        {
            Symbol = eventValue.Price.Symbol,
            Amount = eventValue.Price.Amount
        };
        seedSymbolIndex.Status = SeedStatus.UNREGISTERED;
        seedSymbolIndex.Owner = eventValue.Buyer.ToBase58();
        await SaveEntityAsync(seedSymbolIndex);
    }
    public async Task<TsmSeedSymbolIndex> GetSeedSymbolIndexAsync(string chainId, string symbol)
    {
        // var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, symbol);
        // var seedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(seedSymbolId);
        //
        // if (seedSymbolIndex == null)
        // {
        //     seedSymbolIndex = new TsmSeedSymbolIndex
        //     {
        //         Id = seedSymbolId,
        //         Symbol = symbol,
        //         SeedName = IdGenerateHelper.GetSeedName(symbol)
        //     };
        //     seedSymbolIndex.OfType(TokenHelper.GetTokenType(symbol));
        // }
        // return seedSymbolIndex;
        
        var tsmSeedSymbolIndex = await GetTsmSeedAsync(chainId, symbol);

        if (tsmSeedSymbolIndex == null)
        {
            Logger.LogError("BoughtProcessor tsmSeedSymbolIndex is null chainId={A} symbol={B}", chainId, symbol);
            throw new Exception("tsmSeedSymbolIndex is null");
        }
       
        Logger.LogDebug("BoughtProcessor tsmSeedSymbolIndex is null chainId={A} symbol={B} tsmSeedSymbolIndex={C}", chainId, symbol,JsonConvert.SerializeObject(tsmSeedSymbolIndex));
        
        return tsmSeedSymbolIndex;
    }
    
    private async Task<TsmSeedSymbolIndex> GetTsmSeedAsync(string chainId, string seedOwnedSymbol)
    {
        var queryable = await _tsmSeedSymbolIndexRepository.GetQueryableAsync(); 
        queryable = queryable.Where(x=>x.ChainId == chainId && x.Symbol == seedOwnedSymbol);
        List<TsmSeedSymbolIndex> list = queryable.OrderByDescending(i => i.ExpireTime).Skip(0).Take(1).ToList();
        return list.IsNullOrEmpty() ? null : list.FirstOrDefault();
    }
}