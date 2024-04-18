using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Contracts.Auction;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class ClaimedProcessor : AElfLogEventProcessorBase<Claimed, LogEventInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> _tsmSeedSymbolIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo> _auctionInfoIndexRepository;
    private readonly ILogger<AElfLogEventProcessorBase<Claimed, LogEventInfo>> _logger;
    private readonly INFTInfoProvider _nftInfoProvider;
    private readonly IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> _tokenIndexRepository;

    public ClaimedProcessor(
        ILogger<AElfLogEventProcessorBase<Claimed, LogEventInfo>> logger,
        IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo> tsmSeedSymbolIndexRepository,
        IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> seedSymbolIndexRepository,
        IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo> symbolAuctionInfoIndexRepository,
        INFTInfoProvider nftInfoProvider,
        IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> tokenIndexRepository,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions) : base(logger)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _tsmSeedSymbolIndexRepository = tsmSeedSymbolIndexRepository;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _auctionInfoIndexRepository = symbolAuctionInfoIndexRepository;
        _nftInfoProvider = nftInfoProvider;
        _tokenIndexRepository = tokenIndexRepository;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos?.FirstOrDefault(c => c?.ChainId == chainId)
            ?.AuctionContractAddress;
    }

    protected override async Task HandleEventAsync(Claimed eventValue, LogEventContext context)
    {
        var symbolAuctionInfoIndex =
            await _auctionInfoIndexRepository.GetFromBlockStateSetAsync(eventValue.AuctionId.ToHex(),
                context.ChainId);
        symbolAuctionInfoIndex.FinishIdentifier = (int)SeedAuctionStatus.Finished;
        symbolAuctionInfoIndex.FinishTime = eventValue.FinishTime.Seconds;
        symbolAuctionInfoIndex.TransactionHash = context.TransactionId;
        _logger.LogInformation("Claimed HandleEventAsync symbolAuctionInfoIndex TransactionHash :{TransactionHash}", 
            symbolAuctionInfoIndex.TransactionHash);

        _objectMapper.Map(context, symbolAuctionInfoIndex);
        await _auctionInfoIndexRepository.AddOrUpdateAsync(symbolAuctionInfoIndex);
        var seedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, symbolAuctionInfoIndex.Symbol);
        var seedSymbolIndex =
            await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolIndexId, context.ChainId);
        if (seedSymbolIndex == null) return;
        _objectMapper.Map(context, seedSymbolIndex);
        seedSymbolIndex.HasAuctionFlag = false;
        seedSymbolIndex.MaxAuctionPrice = 0;
        await _seedSymbolIndexRepository.AddOrUpdateAsync(seedSymbolIndex);

        var tsmSeedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, seedSymbolIndex.SeedOwnedSymbol);

        var tsmSeedSymbolIndex =
            await _tsmSeedSymbolIndexRepository.GetFromBlockStateSetAsync(tsmSeedSymbolIndexId, context.ChainId);
        if (tsmSeedSymbolIndex == null) return;
        
        var fromOwner = tsmSeedSymbolIndex.Owner;
        var toOwner = eventValue.Bidder.ToBase58();

        _objectMapper.Map(context, tsmSeedSymbolIndex);
        tsmSeedSymbolIndex.Status = SeedStatus.UNREGISTERED;
        tsmSeedSymbolIndex.Owner = symbolAuctionInfoIndex.FinishBidder;
        tsmSeedSymbolIndex.TokenPrice = symbolAuctionInfoIndex.FinishPrice;
        tsmSeedSymbolIndex.AuctionStatus = (int)SeedAuctionStatus.Finished;
        await _tsmSeedSymbolIndexRepository.AddOrUpdateAsync(tsmSeedSymbolIndex);
        
        var purchaseTokenId = IdGenerateHelper.GetId(context.ChainId, symbolAuctionInfoIndex.FinishPrice.Symbol);
        
        var tokenIndex = await _tokenIndexRepository.GetFromBlockStateSetAsync(purchaseTokenId, context.ChainId);
        if (tokenIndex == null) 
            throw new UserFriendlyException("ClaimedProcessor purchase token {context.ChainId}-{purchaseTokenId} NOT FOUND",context.ChainId,purchaseTokenId);
        
        var nftActivityIndexId =
            IdGenerateHelper.GetId(context.ChainId, seedSymbolIndex.Symbol, NFTActivityType.PlaceBid.ToString(), context.TransactionId);
        var activitySaved = await _nftInfoProvider.AddNFTActivityAsync(context, new NFTActivityIndex
        {
            Id = nftActivityIndexId,
            Type = NFTActivityType.PlaceBid,
            From = FullAddressHelper.ToFullAddress(fromOwner, context.ChainId),
            To = FullAddressHelper.ToFullAddress(toOwner, context.ChainId),
            Amount = 1,
            Price = DecimalUntil.ConvertToElf(symbolAuctionInfoIndex.FinishPrice.Amount),
            PriceTokenInfo = tokenIndex,
            TransactionHash = context.TransactionId,
            Timestamp = context.BlockTime,
            NftInfoId = seedSymbolIndex.Id
        });
       
    }
}