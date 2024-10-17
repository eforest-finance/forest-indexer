using AeFinder.Sdk.Processor;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class OfferCanceledByExpireTimeLogEventProcessor : LogEventProcessorBase<OfferCanceledByExpireTime>
{
    private readonly ILogger<OfferCanceledByExpireTimeLogEventProcessor> _logger;
    private readonly IObjectMapper _objectMapper;
    public OfferCanceledByExpireTimeLogEventProcessor(ILogger<OfferCanceledByExpireTimeLogEventProcessor> logger,
        IObjectMapper objectMapper)
    {
        _logger = logger;
        _objectMapper = objectMapper;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetNFTForestContractAddress(chainId);
    }

    public async override Task ProcessAsync(OfferCanceledByExpireTime eventValue, LogEventContext context)
    {
        _logger.LogDebug("OfferCanceledByExpireTimeLogEventProcessor-1 {context}", JsonConvert.SerializeObject(context));
        _logger.LogDebug("OfferCanceledByExpireTimeLogEventProcessor-2 {eventValue}",
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

        // var offerIndexList = await _nftOfferIndexRepository.GetListAsync(ListingFilter);
        //
        // if (offerIndexList.Item1 == 0) return;
        //
        // foreach (var cancelOfferIndex in offerIndexList.Item2)
        // {
        //     await AddNFTActivityRecordAsync(eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
        //         null, cancelOfferIndex.Quantity, cancelOfferIndex.Price,
        //         NFTActivityType.CancelOffer, context, cancelOfferIndex.PurchaseToken, cancelOfferIndex.ExpireTime); 
        // } todo v2
    }
}