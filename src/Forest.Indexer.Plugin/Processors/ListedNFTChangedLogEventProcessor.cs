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

public class ListedNFTChangedLogEventProcessor : AElfLogEventProcessorBase<ListedNFTChanged, LogEventInfo>
{
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly ILogger<AElfLogEventProcessorBase<ListedNFTChanged, LogEventInfo>> _logger;
    private readonly IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo> _listedNFTIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> _tokenIndexRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly INFTInfoProvider _nftInfoProvider;
    private readonly ICollectionProvider _collectionProvider;
    private readonly ICollectionChangeProvider _collectionChangeProvider;


    public ListedNFTChangedLogEventProcessor(ILogger<AElfLogEventProcessorBase<ListedNFTChanged, LogEventInfo>> logger,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo> listedNftIndexRepository,
        IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> tokenIndexRepository,
        INFTInfoProvider nftInfoProvider,
        ICollectionProvider collectionProvider,
        ICollectionChangeProvider collectionChangeProvider,
        IObjectMapper objectMapper) : base(logger)
    {
        _logger = logger;
        _listedNFTIndexRepository = listedNftIndexRepository;
        _tokenIndexRepository = tokenIndexRepository;
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _nftInfoProvider = nftInfoProvider;
        _collectionProvider = collectionProvider;
        _collectionChangeProvider = collectionChangeProvider;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).NFTMarketContractAddress;
    }

    protected override async Task HandleEventAsync(ListedNFTChanged eventValue, LogEventContext context)
    {
        var listedNftIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.Symbol, eventValue.Owner.ToBase58(),
            eventValue.Duration.StartTime.Seconds);


        _logger.Debug(
            "[ListedNFTChanged] START: ChainId={ChainId}, symbol={Symbol}, Quantity={Quantity}, Id={Id}, Owner={Owner}",
            context.ChainId, eventValue.Symbol, eventValue.Quantity, listedNftIndexId, eventValue.Owner);

        try
        {
            var listedNFTIndex =
                await _listedNFTIndexRepository.GetFromBlockStateSetAsync(listedNftIndexId, context.ChainId);
            if (listedNFTIndex == null)
                throw new UserFriendlyException("nftInfo NOT FOUND");
            
            
            var purchaseTokenId = IdGenerateHelper.GetId(context.ChainId, eventValue.Price.Symbol);
            var tokenIndex = await _tokenIndexRepository.GetFromBlockStateSetAsync(purchaseTokenId, context.ChainId);
            if (tokenIndex == null)
                throw new UserFriendlyException($"Purchase token {context.ChainId}-{eventValue.Price.Symbol} NOT FOUND");
                                
            listedNFTIndex.Prices = eventValue.Price.Amount / (decimal)Math.Pow(10, tokenIndex.Decimals);
            listedNFTIndex.PurchaseToken = tokenIndex;
            listedNFTIndex.Quantity = eventValue.Quantity;
            listedNFTIndex.RealQuantity = Math.Min(eventValue.Quantity, listedNFTIndex.RealQuantity);

            // copy block data
            _objectMapper.Map(context, listedNFTIndex);

            await _nftInfoProvider.UpdateListedInfoCommonAsync(context.ChainId, eventValue.Symbol, context, listedNFTIndex,"");

            _logger.Debug("[ListedNFTChanged] SAVE:, ChainId={ChainId}, symbol={Symbol}, Quantity={Quantity}, Id={Id}",
                context.ChainId, eventValue.Symbol, eventValue.Quantity, listedNftIndexId);

            await _listedNFTIndexRepository.AddOrUpdateAsync(listedNFTIndex);
            await _collectionChangeProvider.SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);

            _logger.Debug("[ListedNFTChanged] FINISH: Id={Id}", listedNftIndexId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ListedNFTChanged error, listedNFTIndexId={Id}", listedNftIndexId);
            throw;
        }
    }
}