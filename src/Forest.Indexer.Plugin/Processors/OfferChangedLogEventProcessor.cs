using AeFinder.Sdk.Processor;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class OfferChangedLogEventProcessor : LogEventProcessorBase<OfferChanged>
{
    private readonly IObjectMapper _objectMapper;

    public OfferChangedLogEventProcessor(
        IObjectMapper objectMapper
    )
    {
        _objectMapper = objectMapper;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetNFTForestContractAddress(chainId);
    }

    public async override Task ProcessAsync(OfferChanged eventValue, LogEventContext context)
    {
        var offerIndexId = IdGenerateHelper.GetOfferId(context.ChainId, eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
            eventValue.OfferTo.ToBase58(), eventValue.ExpireTime.Seconds,eventValue.Price.Amount);
        var offerIndex = await GetEntityAsync<OfferInfoIndex>(offerIndexId);
        if (offerIndex == null) return;

        var tokenIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.Price.Symbol);
        var tokenIndex = await GetEntityAsync<TokenInfoIndex>(tokenIndexId);
        offerIndex.Price = eventValue.Price.Amount / (decimal)Math.Pow(10, tokenIndex.Decimals);
        offerIndex.Quantity = eventValue.Quantity;
        var userBalanceId =
            IdGenerateHelper.GetUserBalanceId(eventValue.OfferFrom.ToBase58(), context.ChainId, tokenIndexId);
        var userBalance = await GetEntityAsync<UserBalanceIndex>(userBalanceId);
        var balance = userBalance == null ? 0 : userBalance.Amount;

        offerIndex.RealQuantity = Math.Min(eventValue.Quantity, balance / eventValue.Price.Amount);
        _objectMapper.Map(context, offerIndex);
        await SaveEntityAsync(offerIndex);
        await SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);
        await SaveNFTOfferChangeIndexAsync(context, eventValue.Symbol, EventType.Modify);
    }
    public async Task SaveCollectionPriceChangeIndexAsync(LogEventContext context, string symbol)
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
    
    public async Task SaveNFTOfferChangeIndexAsync(LogEventContext context, string symbol, EventType eventType)
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
}