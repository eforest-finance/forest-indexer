using AElf.CSharp.Core.Extension;
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

public class ListedNFTAddedLogEventProcessor : AElfLogEventProcessorBase<ListedNFTAdded, LogEventInfo>
{
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly ILogger<AElfLogEventProcessorBase<ListedNFTAdded, LogEventInfo>> _logger;
    private readonly IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo> _listedNFTIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> _tokenIndexRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly INFTInfoProvider _nftInfoProvider;
    private readonly ICollectionProvider _collectionProvider;
    private readonly ICollectionChangeProvider _collectionChangeProvider;
    private readonly INFTListingChangeProvider _listingChangeProvider;
    private readonly IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> _nftIndexRepository;


    public ListedNFTAddedLogEventProcessor(ILogger<AElfLogEventProcessorBase<ListedNFTAdded, LogEventInfo>> logger,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo> listedNftIndexRepository,
        IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> tokenIndexRepository,
        INFTInfoProvider nftInfoProvider,
        ICollectionProvider collectionProvider,
        ICollectionChangeProvider collectionChangeProvider,
        INFTListingChangeProvider listingChangeProvider,
        IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> seedSymbolIndexRepository,
        IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> nftIndexRepository,
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
        _listingChangeProvider = listingChangeProvider;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _nftIndexRepository = nftIndexRepository;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).NFTMarketContractAddress;
    }

    protected override async Task HandleEventAsync(ListedNFTAdded eventValue, LogEventContext context)
    {
        var purchaseTokenId = IdGenerateHelper.GetId(context.ChainId, eventValue.Price.Symbol);

        var listedNftIndexId = IdGenerateHelper.GetId(context.ChainId, eventValue.Symbol, eventValue.Owner.ToBase58(),
            eventValue.Duration.StartTime.Seconds);
        _logger.Debug(
            "[ListedNFTAdded] START: ChainId={ChainId}, symbol={Symbol}, Quantity={Quantity}, Id={Id}, Owner={owner}",
            context.ChainId, eventValue.Symbol, eventValue.Quantity, listedNftIndexId, eventValue.Owner);

        try
        {
            var listingNftInfoIndex =
                await _listedNFTIndexRepository.GetFromBlockStateSetAsync(listedNftIndexId, context.ChainId);
            if (listingNftInfoIndex != null)
            {
                _logger.LogInformation("listingInfo EXISTS");
                return;
            }

            var tokenIndex = await _tokenIndexRepository.GetFromBlockStateSetAsync(purchaseTokenId, context.ChainId);
            if (tokenIndex == null)
            {
                _logger.LogInformation("purchase token {A}-{B} NOT FOUND",context.ChainId,purchaseTokenId);
                return;
            }
            
            listingNftInfoIndex = _objectMapper.Map<ListedNFTAdded, NFTListingInfoIndex>(eventValue);
            listingNftInfoIndex.Id = listedNftIndexId;
            listingNftInfoIndex.Prices = eventValue.Price.Amount / (decimal)Math.Pow(10, tokenIndex.Decimals);
            listingNftInfoIndex.RealQuantity = eventValue.Quantity;
            listingNftInfoIndex.PurchaseToken = tokenIndex;
            listingNftInfoIndex.StartTime = eventValue.Duration.StartTime.ToDateTime();
            listingNftInfoIndex.PublicTime = eventValue.Duration.PublicTime.ToDateTime();
            listingNftInfoIndex.DurationHours = eventValue.Duration.DurationHours;
            listingNftInfoIndex.ExpireTime =
                eventValue.Duration.StartTime.AddHours(eventValue.Duration.DurationHours).AddMinutes(eventValue.Duration.DurationMinutes).ToDateTime();
            listingNftInfoIndex.CollectionSymbol = SymbolHelper.GetNFTCollectionSymbol(eventValue.Symbol);

            // copy block data
            _objectMapper.Map(context, listingNftInfoIndex);

            var updateListedInfoResponse = await _nftInfoProvider.UpdateListedInfoCommonAsync(context.ChainId,
                eventValue.Symbol, context, listingNftInfoIndex,"");
            if (updateListedInfoResponse == null) return;
            listingNftInfoIndex.NftInfoId = updateListedInfoResponse.NftInfoId;
            
            _logger.Debug("[ListedNFTAdded] SAVE: ChainId={ChainId}, symbol={Symbol}, Quantity={Quantity}, Id={Id}",
                context.ChainId, eventValue.Symbol, eventValue.Quantity, listedNftIndexId);

            await _listedNFTIndexRepository.AddOrUpdateAsync(listingNftInfoIndex);

            _logger.Debug("[ListedNFTAdded] FINISH: ChainId={ChainId}, symbol={Symbol}, Quantity={Quantity}, Id={Id}",
                context.ChainId, eventValue.Symbol, eventValue.Quantity, listedNftIndexId);
            
            await _collectionChangeProvider.SaveCollectionPriceChangeIndexAsync(context, eventValue.Symbol);
            await _listingChangeProvider.SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
            
            // NFT activity
            var nftActivityIndexId =
                IdGenerateHelper.GetId(context.ChainId, eventValue.Symbol, "LISTED", context.TransactionId);
            
            var decimals = await _nftInfoProvider.QueryDecimal(context.ChainId, eventValue.Symbol);
            
            var collectionSymbol = TokenHelper.GetCollectionSymbol(eventValue.Symbol);
            var activitySaved = await _nftInfoProvider.AddNFTActivityAsync(context, new NFTActivityIndex
            {
                Id = nftActivityIndexId,
                Type = NFTActivityType.ListWithFixedPrice,
                From = eventValue.Owner.ToBase58(),
                Amount = TokenHelper.GetIntegerDivision(updateListedInfoResponse.ListingQuantity, decimals),
                Price = updateListedInfoResponse.ListingPrice,
                PriceTokenInfo = tokenIndex,
                TransactionHash = context.TransactionId,
                Timestamp = context.BlockTime,
                NftInfoId = updateListedInfoResponse.NftInfoId,
                Symbol = eventValue.Symbol,
                CollectionSymbol = collectionSymbol,
                CollectionId = IdGenerateHelper.GetNFTCollectionId(context.ChainId, collectionSymbol)
            });
            
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[ListedNFTAdded] ERROR: listedNFTIndexId={Id}", listedNftIndexId);
        }
    }
}