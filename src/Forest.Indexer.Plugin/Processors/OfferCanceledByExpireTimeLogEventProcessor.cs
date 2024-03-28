using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class OfferCanceledByExpireTimeLogEventProcessor : OfferLogEventProcessorBase<OfferCanceledByExpireTime>
{
    private readonly IAElfIndexerClientEntityRepository<OfferInfoIndex, LogEventInfo> _nftOfferIndexRepository;
    private readonly ILogger<AElfLogEventProcessorBase<OfferCanceledByExpireTime, LogEventInfo>> _logger;

    public OfferCanceledByExpireTimeLogEventProcessor(ILogger<OfferCanceledByExpireTimeLogEventProcessor> logger,
        IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo> nftActivityIndexRepository,
        IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> nftInfoIndexRepository,
        IAElfIndexerClientEntityRepository<OfferInfoIndex, LogEventInfo> nftOfferIndexRepository,
        IAElfIndexerClientEntityRepository<ProxyAccountIndex, LogEventInfo> proxyAccountIndexRepository,
        INFTInfoProvider infoProvider,
        INFTOfferProvider offerProvider,
        ICollectionChangeProvider collectionChangeProvider,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        INFTOfferChangeProvider nftOfferChangeProvider) : base(logger, objectMapper,
        nftActivityIndexRepository, nftInfoIndexRepository, proxyAccountIndexRepository, infoProvider, offerProvider,
        collectionChangeProvider,
        contractInfoOptions,
        nftOfferChangeProvider)
    {
        _logger = logger;
        _nftOfferIndexRepository = nftOfferIndexRepository;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).NFTMarketContractAddress;
    }

    protected override async Task HandleEventAsync(OfferCanceledByExpireTime eventValue, LogEventContext context)
    {
        _logger.Debug("OfferCanceledByExpireTimeLogEventProcessor-1 {context}", JsonConvert.SerializeObject(context));
        _logger.Debug("OfferCanceledByExpireTimeLogEventProcessor-2 {eventValue}",
            JsonConvert.SerializeObject(eventValue));

        var mustQuery = new List<Func<QueryContainerDescriptor<OfferInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.ChainId).Value(context.ChainId)));
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.BizSymbol).Value(eventValue.Symbol)));
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.OfferFrom).Value(eventValue.OfferFrom.ToBase58())));
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.OfferTo).Value(eventValue.OfferTo.ToBase58())));
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.ExpireTime).Value(eventValue.ExpireTime.ToDateTime())));

        QueryContainer ListingFilter(QueryContainerDescriptor<OfferInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var offerIndexList = await _nftOfferIndexRepository.GetListAsync(ListingFilter);
        if (offerIndexList.Item1 == 0) return;

        foreach (var cancelOfferIndex in offerIndexList.Item2)
        {
            await AddNFTActivityRecordAsync(eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
                null, cancelOfferIndex.Quantity, cancelOfferIndex.Price,
                NFTActivityType.CancelOffer, context, cancelOfferIndex.PurchaseToken);
        }
    }
}