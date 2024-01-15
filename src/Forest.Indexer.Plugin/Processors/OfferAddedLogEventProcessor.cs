using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class OfferAddedLogEventProcessor : OfferLogEventProcessorBase<OfferAdded>
{
    private readonly IAElfIndexerClientEntityRepository<OfferInfoIndex, LogEventInfo> _nftOfferIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> _tokenIndexRepository;
    private readonly ILogger<OfferAddedLogEventProcessor> _logger;
    private readonly IUserBalanceProvider _userBalanceProvider;

    public OfferAddedLogEventProcessor(ILogger<OfferAddedLogEventProcessor> logger, IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<OfferInfoIndex, LogEventInfo> nftOfferIndexRepository,
        IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> tokenIndexRepository,
        IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> nftInfoIndexRepository,
        IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo> nftActivityIndexRepository,
        IAElfIndexerClientEntityRepository<ProxyAccountIndex, LogEventInfo> proxyAccountIndexRepository,
        INFTInfoProvider infoProvider,
        INFTOfferProvider offerProvider,
        ICollectionProvider collectionProvider,
        ICollectionChangeProvider collectionChangeProvider,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        IUserBalanceProvider userBalanceProvider) : base(logger, objectMapper,
        nftActivityIndexRepository, nftInfoIndexRepository, proxyAccountIndexRepository, infoProvider, offerProvider,collectionProvider,
        collectionChangeProvider,
        contractInfoOptions)
    {
        _nftOfferIndexRepository = nftOfferIndexRepository;
        _tokenIndexRepository = tokenIndexRepository;
        _logger = logger;
        _userBalanceProvider = userBalanceProvider;

    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).NFTMarketContractAddress;
    }

    protected override async Task HandleEventAsync(OfferAdded eventValue, LogEventContext context)
    {
        _logger.Debug("OfferAddedLogEventProcessor-1 {context}",JsonConvert.SerializeObject(context));
        _logger.Debug("OfferAddedLogEventProcessor-2 {eventValue}",JsonConvert.SerializeObject(eventValue));
        var offerIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
            eventValue.OfferTo.ToBase58(), eventValue.ExpireTime.Seconds);
        var offerIndex = await _nftOfferIndexRepository.GetFromBlockStateSetAsync(offerIndexId, context.ChainId);
        if (offerIndex != null) return;

        offerIndex = _objectMapper.Map<OfferAdded, OfferInfoIndex>(eventValue);
        offerIndex.Id = offerIndexId;
        var tokenIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.Price.Symbol);
        var tokenIndex = await _tokenIndexRepository.GetFromBlockStateSetAsync(tokenIndexId, context.ChainId);
        offerIndex.Price = eventValue.Price.Amount / (decimal)Math.Pow(10, tokenIndex.Decimals);
        offerIndex.PurchaseToken = tokenIndex;
        offerIndex.CreateTime = context.BlockTime;

        if (SymbolHelper.CheckSymbolIsSeedSymbol(eventValue.Symbol))
        {
            offerIndex.BizInfoId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        }
        else if (SymbolHelper.CheckSymbolIsNFT(eventValue.Symbol))
        {
            offerIndex.BizInfoId = IdGenerateHelper.GetNFTInfoId(context.ChainId, eventValue.Symbol);
        }
        offerIndex.BizSymbol = eventValue.Symbol;
        offerIndex.RealQuantity = eventValue.Quantity;
        _objectMapper.Map(context, offerIndex);
        await _nftOfferIndexRepository.AddOrUpdateAsync(offerIndex);
        await _infoProvider.UpdateOfferCommonAsync(context.ChainId, eventValue.Symbol, context, offerIndex, "");
        await AddNFTActivityRecordAsync(eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
            eventValue.OfferTo.ToBase58(), eventValue.Quantity, offerIndex.Price, NFTActivityType.MakeOffer,
            context,
            tokenIndex);
        await _collectionChangeProvider.SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);
        await _userBalanceProvider.ReCoverUserBalanceAsync(eventValue.OriginBalanceSymbol, eventValue.OfferFrom.ToBase58(), eventValue.OriginBalance, context);
    }
}