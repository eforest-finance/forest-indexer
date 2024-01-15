using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class OfferChangedLogEventProcessor : OfferLogEventProcessorBase<OfferChanged>
{
    private readonly IAElfIndexerClientEntityRepository<OfferInfoIndex, LogEventInfo> _nftOfferIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> _tokenIndexRepository;
    private readonly IUserBalanceProvider _userBalanceProvider;

    public OfferChangedLogEventProcessor(ILogger<OfferChangedLogEventProcessor> logger, IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo> nftActivityIndexRepository,
        IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> nftInfoIndexRepository,
        IAElfIndexerClientEntityRepository<OfferInfoIndex, LogEventInfo> nftOfferIndexRepository,
        IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> tokenIndexRepository,
        IAElfIndexerClientEntityRepository<ProxyAccountIndex, LogEventInfo> proxyAccountIndexRepository,
        INFTInfoProvider infoProvider,
        INFTOfferProvider offerProvider,
        ICollectionProvider collectionProvider,
        ICollectionChangeProvider collectionChangeProvider,
        IUserBalanceProvider userBalanceProvider,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions) : base(logger, objectMapper,
        nftActivityIndexRepository, nftInfoIndexRepository, proxyAccountIndexRepository, infoProvider, offerProvider,collectionProvider,
        collectionChangeProvider,
        contractInfoOptions)
    {
        _nftOfferIndexRepository = nftOfferIndexRepository;
        _tokenIndexRepository = tokenIndexRepository;
        _userBalanceProvider = userBalanceProvider;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).NFTMarketContractAddress;
    }

    protected override async Task HandleEventAsync(OfferChanged eventValue, LogEventContext context)
    {
        var offerIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
            eventValue.OfferTo.ToBase58(), eventValue.ExpireTime.Seconds);
        var offerIndex = await _nftOfferIndexRepository.GetFromBlockStateSetAsync(offerIndexId, context.ChainId);
        if (offerIndex == null) return;

        var tokenIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.Price.Symbol);
        var tokenIndex = await _tokenIndexRepository.GetFromBlockStateSetAsync(tokenIndexId, context.ChainId);
        offerIndex.Price = eventValue.Price.Amount / (decimal)Math.Pow(10, tokenIndex.Decimals);
        offerIndex.Quantity = eventValue.Quantity;
        var userBalanceId =
            IdGenerateHelper.GetUserBalanceId(eventValue.OfferFrom.ToBase58(), context.ChainId, tokenIndexId);
        var userBalance = await _userBalanceProvider.QueryUserBalanceByIdAsync(userBalanceId, context.ChainId);
        var balance = userBalance == null ? 0 : userBalance.Amount;

        offerIndex.RealQuantity = Math.Min(eventValue.Quantity, balance / eventValue.Price.Amount);
        _objectMapper.Map(context, offerIndex);
        await _nftOfferIndexRepository.AddOrUpdateAsync(offerIndex);
        await _collectionChangeProvider.SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);
    }
}