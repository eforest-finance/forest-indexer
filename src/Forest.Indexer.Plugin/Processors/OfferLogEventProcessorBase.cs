using AElf.CSharp.Core;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public abstract class OfferLogEventProcessorBase<TEvent>: AElfLogEventProcessorBase<TEvent, LogEventInfo> where TEvent : IEvent<TEvent>, new()
{
    protected readonly IObjectMapper _objectMapper;
    protected readonly ContractInfoOptions _contractInfoOptions;
    protected readonly IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo> _nftActivityIndexRepository;
    protected readonly IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> _nftInfoIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<ProxyAccountIndex, LogEventInfo> _proxyAccountIndexRepository;
    protected readonly INFTInfoProvider _infoProvider;
    protected readonly INFTOfferProvider _offerProvider;
    protected readonly ICollectionProvider _collectionProvider;
    protected readonly ICollectionChangeProvider _collectionChangeProvider;
    protected readonly INFTOfferChangeProvider _nftOfferChangeProvider;

    
    public OfferLogEventProcessorBase(ILogger<OfferLogEventProcessorBase<TEvent>> logger, IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo> nftActivityIndexRepository,
        IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> nftInfoIndexRepository,
        IAElfIndexerClientEntityRepository<ProxyAccountIndex, LogEventInfo> proxyAccountIndexRepository,
        INFTInfoProvider infoProvider,
        INFTOfferProvider offerProvider,
        ICollectionProvider collectionProvider,
        ICollectionChangeProvider collectionChangeProvider,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        INFTOfferChangeProvider nftOfferChangeProvider) : base(logger)
    {
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _nftActivityIndexRepository = nftActivityIndexRepository;
        _nftInfoIndexRepository = nftInfoIndexRepository;
        _proxyAccountIndexRepository = proxyAccountIndexRepository;
        _offerProvider = offerProvider;
        _infoProvider = infoProvider;
        _collectionProvider = collectionProvider;
        _collectionChangeProvider = collectionChangeProvider;
        _nftOfferChangeProvider = nftOfferChangeProvider;
    }

    protected async Task AddNFTActivityRecordAsync(string symbol, string offerFrom, string offerTo,
        long quantity, decimal price, NFTActivityType activityType, LogEventContext context,
        TokenInfoIndex tokenInfoIndex)
    {
        var nftActivityIndexId = IdGenerateHelper.GetId(context.ChainId, symbol, offerFrom,
            offerTo, context.TransactionId);
        var nftActivityIndex =
            await _nftActivityIndexRepository.GetFromBlockStateSetAsync(nftActivityIndexId, context.ChainId);
        if (nftActivityIndex != null) return;

        var nftInfoIndexId = IdGenerateHelper.GetId(context.ChainId, symbol);
        nftActivityIndex = new NFTActivityIndex
        {
            Id = nftActivityIndexId,
            Type = activityType,
            TransactionHash = context.TransactionId,
            Timestamp = context.BlockTime,
            NftInfoId = nftInfoIndexId
        };
        _objectMapper.Map(context, nftActivityIndex);
        nftActivityIndex.From = offerFrom;
        nftActivityIndex.To = await TransferAddress(offerTo);

        nftActivityIndex.Amount = quantity;
        nftActivityIndex.Price = price;
        nftActivityIndex.PriceTokenInfo = tokenInfoIndex;

        await _nftActivityIndexRepository.AddOrUpdateAsync(nftActivityIndex);
    }

    private async Task<string> TransferAddress(string offerToAddress)
    {
        if (offerToAddress.IsNullOrWhiteSpace()) return offerToAddress;
        var proxyAccount = await _proxyAccountIndexRepository.GetAsync(offerToAddress);
        if (proxyAccount == null || proxyAccount.ManagersSet == null)
        {
            return offerToAddress;
        }
        return proxyAccount.ManagersSet.FirstOrDefault(offerToAddress);
    }
}

