using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Forest.Contracts.Auction;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Util;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class ClaimedProcessor : LogEventProcessorBase<Claimed>
{
    private readonly IObjectMapper _objectMapper;

    public ClaimedProcessor(
        IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetAuctionContractAddress(chainId);
    }

    public override async Task ProcessAsync(Claimed eventValue, LogEventContext context)
    {
        var symbolAuctionInfoIndex =
            await GetEntityAsync<SymbolAuctionInfoIndex>(eventValue.AuctionId.ToHex());
        symbolAuctionInfoIndex.FinishIdentifier = (int)SeedAuctionStatus.Finished;
        symbolAuctionInfoIndex.FinishTime = eventValue.FinishTime.Seconds;
        symbolAuctionInfoIndex.TransactionHash = context.Transaction.TransactionId;
        Logger.LogInformation("Claimed HandleEventAsync symbolAuctionInfoIndex TransactionHash :{TransactionHash}", 
            symbolAuctionInfoIndex.TransactionHash);

        _objectMapper.Map(context, symbolAuctionInfoIndex);
        await SaveEntityAsync(symbolAuctionInfoIndex);

        var seedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, symbolAuctionInfoIndex.Symbol);
        var seedSymbolIndex = await GetEntityAsync<SeedSymbolIndex>(seedSymbolIndexId);
        
        if (seedSymbolIndex == null) return;
        _objectMapper.Map(context, seedSymbolIndex);
        seedSymbolIndex.HasAuctionFlag = false;
        seedSymbolIndex.MaxAuctionPrice = 0;
        await SaveEntityAsync(seedSymbolIndex);

        var tsmSeedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, seedSymbolIndex.SeedOwnedSymbol);

        var tsmSeedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(tsmSeedSymbolIndexId);
        if (tsmSeedSymbolIndex == null) return;
        
        var fromOwner = tsmSeedSymbolIndex.Owner;
        var toOwner = eventValue.Bidder.ToBase58();

        _objectMapper.Map(context, tsmSeedSymbolIndex);
        tsmSeedSymbolIndex.Status = SeedStatus.UNREGISTERED;
        tsmSeedSymbolIndex.Owner = symbolAuctionInfoIndex.FinishBidder;
        tsmSeedSymbolIndex.TokenPrice = symbolAuctionInfoIndex.FinishPrice;
        tsmSeedSymbolIndex.AuctionStatus = (int)SeedAuctionStatus.Finished;
        await SaveEntityAsync(tsmSeedSymbolIndex);

        var purchaseTokenId = IdGenerateHelper.GetId(context.ChainId, symbolAuctionInfoIndex.FinishPrice.Symbol);
        
        var tokenIndex = await GetEntityAsync<TokenInfoIndex>(purchaseTokenId);
        if (tokenIndex == null)
        {
            Logger.LogError("ClaimedProcessor purchase token {context.ChainId}-{purchaseTokenId} NOT FOUND",context.ChainId,purchaseTokenId);
            return;
        }

        var nftActivityIndexId =
            IdGenerateHelper.GetId(context.ChainId, seedSymbolIndex.Symbol, NFTActivityType.PlaceBid.ToString(), context.Transaction.TransactionId);
        var activitySaved = await AddNFTActivityAsync(context, new NFTActivityIndex
        {
            Id = nftActivityIndexId,
            Type = NFTActivityType.PlaceBid,
            From = FullAddressHelper.ToFullAddress(fromOwner, context.ChainId),
            To = FullAddressHelper.ToFullAddress(toOwner, context.ChainId),
            Amount = 1,
            Price = DecimalUntil.ConvertToElf(symbolAuctionInfoIndex.FinishPrice.Amount),
            PriceTokenInfo = tokenIndex,
            TransactionHash = context.Transaction.TransactionId,
            Timestamp = context.Block.BlockTime,
            NftInfoId = seedSymbolIndex.Id
        });
       
    }
    public async Task<bool> AddNFTActivityAsync(LogEventContext context, NFTActivityIndex nftActivityIndex)
    {
        // NFT activity
        var nftActivityIndexExists = await GetEntityAsync<NFTActivityIndex>(nftActivityIndex.Id);
        if (nftActivityIndexExists != null)
        {
            Logger.LogDebug("[AddNFTActivityAsync] FAIL: activity EXISTS, nftActivityIndexId={Id}", nftActivityIndex.Id);
            return false;
        }

        var from = nftActivityIndex.From;
        var to = nftActivityIndex.To;
        _objectMapper.Map(context, nftActivityIndex);
        nftActivityIndex.From = FullAddressHelper.ToFullAddress(from, context.ChainId);
        nftActivityIndex.To = FullAddressHelper.ToFullAddress(to, context.ChainId);

        Logger.LogDebug("[AddNFTActivityAsync] SAVE: activity SAVE, nftActivityIndexId={Id}", nftActivityIndex.Id);
        await SaveEntityAsync(nftActivityIndex);

        Logger.LogDebug("[AddNFTActivityAsync] SAVE: activity FINISH, nftActivityIndexId={Id}", nftActivityIndex.Id);
        return true;
    }
}