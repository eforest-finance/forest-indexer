using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Forest.Contracts.Auction;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class ClaimedProcessor : LogEventProcessorBase<Claimed>
{
    private readonly IObjectMapper _objectMapper;
    private readonly IAElfClientServiceProvider _aElfClientServiceProvider;

    public ClaimedProcessor(
        IObjectMapper objectMapper,AElfClientServiceProvider aElfClientServiceProvider)
    {
        _objectMapper = objectMapper;
        _aElfClientServiceProvider = aElfClientServiceProvider;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetAuctionContractAddress(chainId);
    }

    public override async Task ProcessAsync(Claimed eventValue, LogEventContext context)
    {
        Logger.LogInformation("Claimed HandleEventAsync eventValue :{A}", 
            JsonConvert.SerializeObject(eventValue));
        if (eventValue.AuctionId == null || eventValue.AuctionId.Value.Length == 0
                                         || eventValue.Bidder == null || eventValue.Bidder.Value.Length == 0)
        {
            Logger.LogError("Claimed HandleEventAsync error AuctionId or Bidder is null:{A}",
                JsonConvert.SerializeObject(eventValue)
            );
            return;
        }
       
        
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

        if (seedSymbolIndex == null)
        {
            Logger.LogInformation("Claimed HandleEventAsync seedSymbolIndex is null seedSymbolIndexId :{seedSymbolIndexId}", 
                seedSymbolIndexId);
            return;
        }

        _objectMapper.Map(context, seedSymbolIndex);
        seedSymbolIndex.HasAuctionFlag = false;
        seedSymbolIndex.MaxAuctionPrice = 0;
        seedSymbolIndex.IssuerTo = eventValue.Bidder.ToBase58();
        if (!seedSymbolIndex.IssuerTo.IsNullOrEmpty())
        {
            Logger.LogDebug("ClaimedProcessor Update SeedExpTime {symbol}",seedSymbolIndex.Symbol);
            var mainChainSeedToken = await _aElfClientServiceProvider.GetTokenInfoAsync(ForestIndexerConstants.MainChain,
                ContractInfoHelper.GetTokenContractAddress(ForestIndexerConstants.MainChain), seedSymbolIndex.Symbol);
            if (mainChainSeedToken != null)
            {
                var seedExpTime = EnumDescriptionHelper.GetExtraInfoValue(mainChainSeedToken.ExternalInfo,
                    TokenCreatedExternalInfoEnum.SeedExpTime);
                if (long.TryParse(seedExpTime, out var seedExpTimeSecond))
                {
                    Logger.LogDebug("ClaimedProcessor Update SeedExpTime symbol {A} old {B} ", seedSymbolIndex.Symbol,
                        seedSymbolIndex.SeedExpTimeSecond);
                    seedSymbolIndex.SeedExpTimeSecond = seedExpTimeSecond;
                    seedSymbolIndex.SeedExpTime = DateTimeHelper.FromUnixTimeSeconds(seedExpTimeSecond);

                    Logger.LogDebug("ClaimedProcessor Update SeedExpTime symbol {A} new {B} ", seedSymbolIndex.Symbol,
                        seedSymbolIndex.SeedExpTimeSecond);
                }
            }
            else
            {
                Logger.LogDebug("ClaimedProcessor Update SeedExpTime default, symbol {A} old {B} ", seedSymbolIndex.Symbol,
                    seedSymbolIndex.SeedExpTimeSecond);
                seedSymbolIndex.SeedExpTimeSecond = eventValue.FinishTime.Seconds + ForestIndexerConstants.SeedExpireSecond;
                seedSymbolIndex.SeedExpTime = DateTimeHelper.FromUnixTimeSeconds(seedSymbolIndex.SeedExpTimeSecond);
                Logger.LogDebug("ClaimedProcessor Update SeedExpTime default, symbol {A} new {B} ", seedSymbolIndex.Symbol,
                    seedSymbolIndex.SeedExpTimeSecond);
            }
            
        }

        await SaveEntityAsync(seedSymbolIndex);

        var tsmSeedSymbolIndexId = IdGenerateHelper.GetNewTsmSeedSymbolId(context.ChainId,
            symbolAuctionInfoIndex.Symbol, seedSymbolIndex.SeedOwnedSymbol);

        var tsmSeedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(tsmSeedSymbolIndexId);
        if (tsmSeedSymbolIndex == null)
        {
            Logger.LogDebug("new tsmSeedSymbolIndex is null id={A}",tsmSeedSymbolIndexId);
            tsmSeedSymbolIndexId = IdGenerateHelper.GetOldTsmSeedSymbolId(context.ChainId, seedSymbolIndex.SeedOwnedSymbol);
            
            tsmSeedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(tsmSeedSymbolIndexId);
            if (tsmSeedSymbolIndex == null)
            {
                Logger.LogDebug("old tsmSeedSymbolIndex is null id={A}",tsmSeedSymbolIndexId);
                return;
            }
        }
        
        var fromOwner = tsmSeedSymbolIndex.Owner;
        var toOwner = eventValue.Bidder.ToBase58();

        _objectMapper.Map(context, tsmSeedSymbolIndex);
        tsmSeedSymbolIndex.Status = SeedStatus.UNREGISTERED;
        tsmSeedSymbolIndex.Owner = symbolAuctionInfoIndex.FinishBidder;
        tsmSeedSymbolIndex.TokenPrice = symbolAuctionInfoIndex.FinishPrice;
        tsmSeedSymbolIndex.AuctionStatus = (int)SeedAuctionStatus.Finished;
        if (!symbolAuctionInfoIndex.FinishBidder.IsNullOrEmpty())
        {
            tsmSeedSymbolIndex.ExpireTime = seedSymbolIndex.SeedExpTimeSecond;
        }

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
        var activity = new NFTActivityIndex
        {
            Id = nftActivityIndexId,
            From = FullAddressHelper.ToFullAddress(fromOwner, context.ChainId),
            To = FullAddressHelper.ToFullAddress(toOwner, context.ChainId),
            Amount = 1,
            Price = DecimalUntil.ConvertToElf(symbolAuctionInfoIndex.FinishPrice.Amount),
            PriceTokenInfo = tokenIndex,
            TransactionHash = context.Transaction.TransactionId,
            Timestamp = context.Block.BlockTime,
            NftInfoId = seedSymbolIndex.Id
        };
        activity.OfType(NFTActivityType.PlaceBid);
        var activitySaved = await AddNFTActivityAsync(context, activity);
       
    }
    public async Task<bool> AddNFTActivityAsync(LogEventContext context, NFTActivityIndex nftActivityIndex)
    {
        // NFT activity

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