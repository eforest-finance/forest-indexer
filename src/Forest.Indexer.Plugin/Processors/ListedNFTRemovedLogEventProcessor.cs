using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class ListedNFTRemovedLogEventProcessor : AElfLogEventProcessorBase<ListedNFTRemoved, LogEventInfo>
{
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo> _listedNFTIndexRepository;
    private readonly ILogger<AElfLogEventProcessorBase<ListedNFTRemoved, LogEventInfo>> _logger;
    private readonly IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> _tokenIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> _nftInfoIndexRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly INFTInfoProvider _nftInfoProvider;
    private readonly INFTListingInfoProvider _listingInfoProvider;
    private readonly ICollectionProvider _collectionProvider;
    private readonly ICollectionChangeProvider _collectionChangeProvider;
    private readonly INFTListingChangeProvider _listingChangeProvider;


    public ListedNFTRemovedLogEventProcessor(ILogger<AElfLogEventProcessorBase<ListedNFTRemoved, LogEventInfo>> logger,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> tokenIndexRepository,
        IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> nftInfoIndexRepository,
        IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo> listedNftIndexRepository,
        INFTListingInfoProvider listingInfoProvider,
        ICollectionProvider collectionProvider,
        ICollectionChangeProvider collectionChangeProvider,
        INFTListingChangeProvider listingChangeProvider,
        IObjectMapper objectMapper, INFTInfoProvider nftInfoProvider) : base(logger)
    {
        _logger = logger;
        _tokenIndexRepository = tokenIndexRepository;
        _nftInfoIndexRepository = nftInfoIndexRepository;
        _listedNFTIndexRepository = listedNftIndexRepository;
        _objectMapper = objectMapper;
        _nftInfoProvider = nftInfoProvider;
        _listingInfoProvider = listingInfoProvider;
        _contractInfoOptions = contractInfoOptions.Value;
        _collectionProvider = collectionProvider;
        _collectionChangeProvider = collectionChangeProvider;
        _listingChangeProvider = listingChangeProvider;
        
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).NFTMarketContractAddress;
    }

    protected override async Task HandleEventAsync(ListedNFTRemoved eventValue, LogEventContext context)
    {
        var listedNftIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.Symbol, eventValue.Owner.ToBase58(),
            eventValue.Duration.StartTime.Seconds);
        _logger.Debug("[ListedNFTRemoved] START: ChainId={ChainId}, symbol={Symbol}, id={Id}",
            context.ChainId, eventValue.Symbol, listedNftIndexId);
        
        try
        {
            var nftListingInfoIndex = await _listedNFTIndexRepository.GetFromBlockStateSetAsync(listedNftIndexId, context.ChainId);
            if (nftListingInfoIndex == null)
                throw new UserFriendlyException("listing info NOT FOUND");

            var purchaseTokenId = IdGenerateHelper.GetId(context.ChainId, eventValue.Price.Symbol);
            var tokenIndex = await _tokenIndexRepository.GetFromBlockStateSetAsync(purchaseTokenId, context.ChainId);
            if (tokenIndex == null)
                throw new UserFriendlyException($"purchase token {context.ChainId}-{purchaseTokenId} NOT FOUND");

            _objectMapper.Map(context, nftListingInfoIndex);
            await _listedNFTIndexRepository.DeleteAsync(nftListingInfoIndex);
            
            var nffInfoId = IdGenerateHelper.GetNFTInfoId(context.ChainId, eventValue.Symbol);
            var latestNFTListingInfoDic = await _listingInfoProvider.QueryLatestNFTListingInfoByNFTIdsAsync(new List<string>{nffInfoId},nftListingInfoIndex.Id);

            var latestNFTListingInfo = latestNFTListingInfoDic != null && latestNFTListingInfoDic.ContainsKey(nffInfoId)
                ? latestNFTListingInfoDic[nffInfoId]
                : new NFTListingInfoIndex();

            await _nftInfoProvider.UpdateListedInfoCommonAsync(context.ChainId, eventValue.Symbol, context,
                latestNFTListingInfo, nftListingInfoIndex.Id);
            

            // NFT activity
            var nftActivityIndexId =
                IdGenerateHelper.GetId(context.ChainId, eventValue.Symbol, "DELIST", context.TransactionId, Guid.NewGuid());
            var activitySaved = await _nftInfoProvider.AddNFTActivityAsync(context, new NFTActivityIndex
            {
                Id = nftActivityIndexId,
                Type = NFTActivityType.DeList,
                From = eventValue.Owner.ToBase58(),
                Amount = nftListingInfoIndex.Quantity,
                Price = nftListingInfoIndex.Prices,
                PriceTokenInfo = tokenIndex,
                TransactionHash = context.TransactionId,
                Timestamp = context.BlockTime,
                NftInfoId = nffInfoId
            });
            if (!activitySaved) throw new UserFriendlyException("Activity SAVE FAILED");
            await _collectionChangeProvider.SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);
            await _listingChangeProvider.SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[ListedNFTRemoved] error, listedNFTIndexId={Id}", listedNftIndexId);
            throw;
        }
    }
}