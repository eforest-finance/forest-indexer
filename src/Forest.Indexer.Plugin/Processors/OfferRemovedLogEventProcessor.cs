using AeFinder.Sdk;
using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class OfferRemovedLogEventProcessor : LogEventProcessorBase<OfferRemoved>
{
    private readonly IObjectMapper _objectMapper;
    private readonly IReadOnlyRepository<OfferInfoIndex> _offerInfoRepository;

    public OfferRemovedLogEventProcessor( IObjectMapper objectMapper,
        IReadOnlyRepository<OfferInfoIndex> offerInfoRepository)
    {
        _objectMapper = objectMapper;
        _offerInfoRepository = offerInfoRepository;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetNFTForestContractAddress(chainId);
    }

    public async override Task ProcessAsync(OfferRemoved eventValue, LogEventContext context)
    {
        Logger.LogDebug("OfferRemovedLogEventProcessor-1 {context}",JsonConvert.SerializeObject(context));
        Logger.LogDebug("OfferRemovedLogEventProcessor-2 {eventValue}",JsonConvert.SerializeObject(eventValue));

        var offerIndexId = IdGenerateHelper.GetOfferId(context.ChainId, eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
            eventValue.OfferTo.ToBase58(), eventValue.ExpireTime.Seconds,eventValue.Price.Amount);
        var offerIndex = await GetEntityAsync<OfferInfoIndex>(offerIndexId);

        if (offerIndex == null) return;
        var nftInfoId = offerIndex.BizInfoId;
        _objectMapper.Map(context, offerIndex);
        await DeleteEntityAsync<OfferInfoIndex>(offerIndexId);
        
        await UpdateOfferNumAsync(eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
            -1, context);

        await SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);
        await SaveNFTOfferChangeIndexAsync(context, eventValue.Symbol, EventType.Remove);
    }

    private async Task<int> UpdateOfferNumAsync(string symbol, string offerFrom, int change, LogEventContext context)
    {
        var offerNumId = IdGenerateHelper.GetOfferNumId(context.ChainId, offerFrom);
        var nftOfferNumIndex = await GetEntityAsync<UserNFTOfferNumIndex>(offerNumId);
        
        if (nftOfferNumIndex == null)
        {
            nftOfferNumIndex = new UserNFTOfferNumIndex()
            {
                Id = offerNumId,
                Address = offerFrom,
                OfferNum = change
            };
        }
        else
        {
            nftOfferNumIndex.OfferNum += change;
            // deal history data
            if (nftOfferNumIndex.OfferNum < 0)
            {
                Logger.LogWarning(
                    "UpdateOfferNumAsync has history Address {Address} symbol {Symbol} OfferNum {OfferNum}", offerFrom,
                    symbol, nftOfferNumIndex.OfferNum);
                nftOfferNumIndex.OfferNum = 0;
            }
        }
        
        Logger.LogInformation("UpdateOfferNumAsync Address {Address} symbol {Symbol} OfferNum {OfferNum}", offerFrom,
            symbol, nftOfferNumIndex.OfferNum);
        _objectMapper.Map(context, nftOfferNumIndex);
        await SaveEntityAsync(nftOfferNumIndex);
        return nftOfferNumIndex.OfferNum;
        return 0;
    }
    private async Task SaveCollectionPriceChangeIndexAsync(LogEventContext context, string symbol)
    {
        var collectionPriceChangeIndex = new CollectionPriceChangeIndex();
        var nftCollectionSymbol = SymbolHelper.GetNFTCollectionSymbol(symbol);
        if (nftCollectionSymbol == null)
        {
            return;
        }

        collectionPriceChangeIndex.Symbol = nftCollectionSymbol;
        collectionPriceChangeIndex.Id = IdGenerateHelper.GetNFTCollectionId(context.ChainId, nftCollectionSymbol);
        collectionPriceChangeIndex.UpdateTime = context.Block.BlockTime;
        _objectMapper.Map(context, collectionPriceChangeIndex);
        await SaveEntityAsync(collectionPriceChangeIndex);

    }
    
    private async Task SaveNFTOfferChangeIndexAsync(LogEventContext context, string symbol, EventType eventType)
    {
        if (context.ChainId.Equals(ForestIndexerConstants.MainChain))
        {
            return;
        }

        if (symbol.Equals(ForestIndexerConstants.TokenSimpleElf))
        {
            return;
        }

        var nftOfferChangeIndex = new NFTOfferChangeIndex
        {
            Id = IdGenerateHelper.GetId(context.ChainId, symbol, Guid.NewGuid()),
            NftId = IdGenerateHelper.GetNFTInfoId(context.ChainId, symbol),
            EventType = eventType,
            CreateTime = context.Block.BlockTime
        };
        
        _objectMapper.Map(context, nftOfferChangeIndex);
        await SaveEntityAsync(nftOfferChangeIndex);
    }
    
    private async Task<Dictionary<string, OfferInfoIndex>> QueryLatestNFTOfferByNFTIdsAsync(
        List<string> nftInfoIds, string noOfferId)
    {
        if (nftInfoIds == null) return new Dictionary<string, OfferInfoIndex>();
        var queryLatestNFTOfferList = new List<Task<OfferInfoIndex?>>();
        foreach (string nftInfoId in nftInfoIds)
        {
            queryLatestNFTOfferList.Add(QueryLatestNFTOfferByNFTIdAsync(nftInfoId, noOfferId));
        }

        var latestNFTOfferList = await Task.WhenAll(queryLatestNFTOfferList);
        return await TransferToDicAsync(latestNFTOfferList);
    }
    
    private async Task<OfferInfoIndex?> QueryLatestNFTOfferByNFTIdAsync(string nftInfoId, string noListingId)
    {
        var utcNow = DateTime.UtcNow;
        var queryable = await _offerInfoRepository.GetQueryableAsync();
        queryable = queryable.Where(i => i.ExpireTime > utcNow);
        queryable = queryable.Where(i => i.BizInfoId == nftInfoId);
        if (!noListingId.IsNullOrEmpty())
        {
            queryable = queryable.Where(i => i.Id != noListingId);
        }

        return queryable.OrderByDescending(i => i.BlockHeight)
            .Skip(0)
            .Take(1)
            .ToList().FirstOrDefault();
    }
    
    private async Task<Dictionary<string, OfferInfoIndex>> TransferToDicAsync(
        OfferInfoIndex?[] nftOfferIndices)
    {
        if (nftOfferIndices == null || nftOfferIndices.Length == 0)
            return new Dictionary<string, OfferInfoIndex>();

        nftOfferIndices = nftOfferIndices.Where(x => x != null).ToArray();

        return nftOfferIndices == null || nftOfferIndices.Length == 0
            ? new Dictionary<string, OfferInfoIndex>()
            : nftOfferIndices.ToDictionary(item => item.BizInfoId);
    }

}