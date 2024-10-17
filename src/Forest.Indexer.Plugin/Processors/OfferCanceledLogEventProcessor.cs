using AeFinder.Sdk.Processor;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Microsoft.Extensions.Logging;
using Nest;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class OfferCanceledLogEventProcessor : LogEventProcessorBase<OfferCanceled>
{
    private readonly ILogger<OfferCanceledLogEventProcessor> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly INFTInfoProvider _infoProvider;
    private readonly INFTOfferProvider _offerProvider;
    private readonly INFTOfferChangeProvider _nftOfferChangeProvider;
    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly ICollectionChangeProvider _collectionChangeProvider;

    public OfferCanceledLogEventProcessor(
        ILogger<OfferCanceledLogEventProcessor> logger,
        IObjectMapper objectMapper,
        INFTInfoProvider infoProvider,
        INFTOfferProvider offerProvider,
        INFTOfferChangeProvider nftOfferChangeProvider,
        ICollectionProvider collectionProvider,
        ICollectionChangeProvider collectionChangeProvider,
        IUserBalanceProvider userBalanceProvider)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _infoProvider = infoProvider;
        _offerProvider = offerProvider;
        _nftOfferChangeProvider = nftOfferChangeProvider;
        _collectionProvider = collectionProvider;
        _collectionChangeProvider = collectionChangeProvider;
        _userBalanceProvider = userBalanceProvider;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetNFTForestContractAddress(chainId);
    }

    public async override Task ProcessAsync(OfferCanceled eventValue, LogEventContext context)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<OfferInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.ChainId).Value(context.ChainId)));
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.BizSymbol).Value(eventValue.Symbol)));
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.OfferFrom).Value(eventValue.OfferFrom.ToBase58())));

        QueryContainer ListingFilter(QueryContainerDescriptor<OfferInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        // var offerIndex = await _nftOfferIndexRepository.GetListAsync(ListingFilter);
        // if (offerIndex.Item1 == 0) return;
        // // The current number of items in the IndexList is only 1 
        // foreach (var index in eventValue.IndexList.Value)
        // {
        //     try
        //     {
        //         var cancelOfferIndex = offerIndex.Item2[index];
        //         if (cancelOfferIndex == null) return;
        //         var nftInfoId = cancelOfferIndex.BizInfoId;
        //         await AddNFTActivityRecordAsync(eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
        //             null, cancelOfferIndex.Quantity, cancelOfferIndex.Price,
        //             NFTActivityType.CancelOffer, context, cancelOfferIndex.PurchaseToken, cancelOfferIndex.ExpireTime);
        //         await _nftOfferIndexRepository.DeleteAsync(cancelOfferIndex);
        //         var latestNFTOfferDic =
        //             await _offerProvider.QueryLatestNFTOfferByNFTIdsAsync(new List<string> { cancelOfferIndex.BizInfoId },
        //                 cancelOfferIndex.Id);
        //
        //         var latestNFTOffer = latestNFTOfferDic != null && latestNFTOfferDic.ContainsKey(cancelOfferIndex.BizInfoId)
        //             ? latestNFTOfferDic[cancelOfferIndex.BizInfoId]
        //             : new OfferInfoIndex(){
        //                 BizInfoId = nftInfoId
        //             };
        //
        //         await _infoProvider.UpdateOfferCommonAsync(context.ChainId, eventValue.Symbol, context,
        //             latestNFTOffer,
        //             cancelOfferIndex.Id);
        //     }
        //     catch (Exception e)
        //     {
        //         _logger.LogError(e, "[OfferCanceled] ERROR: Symbol={Symbol},index = {Index},size = {Size}",
        //             eventValue.Symbol, index, offerIndex.Item1);
        //         throw;
        //     }
        // }
        //
        // await _offerProvider.UpdateOfferNumAsync(eventValue.Symbol, eventValue.OfferFrom.ToBase58(),
        //     -eventValue.IndexList.Value.Count, context);
        // await _collectionChangeProvider.SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);
        // await _nftOfferChangeProvider.SaveNFTOfferChangeIndexAsync(context, eventValue.Symbol, EventType.Cancel); todo v2
    }
}