using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using AElf;
using AElf.Contracts.MultiToken;
using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class TokenCreatedLogEventProcessor : LogEventProcessorBase<TokenCreated>
{
    private readonly IObjectMapper _objectMapper;
    private readonly IAElfClientServiceProvider _aElfClientServiceProvider;

    public TokenCreatedLogEventProcessor(IObjectMapper objectMapper
        ,IAElfClientServiceProvider aElfClientServiceProvider
        )
    {
        _objectMapper = objectMapper;
        _aElfClientServiceProvider = aElfClientServiceProvider;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetTokenContractAddress(chainId);
    }

    public async override Task ProcessAsync(TokenCreated eventValue, LogEventContext context)
    {
        Logger.LogDebug("TokenCreatedLogEventProcessor-1 {A}",JsonConvert.SerializeObject(eventValue));
        Logger.LogDebug("TokenCreatedLogEventProcessor-2 {B}",JsonConvert.SerializeObject(context));
        if (eventValue == null || context == null) return;
        await TokenInfoIndexCreateAsync(eventValue, context);
        if(SymbolHelper.CheckSymbolIsELF(eventValue.Symbol)) return;
        if (SymbolHelper.CheckSymbolIsSeedCollection(eventValue.Symbol))
        {
            Logger.LogDebug("TokenCreatedLogEventProcessor-3");
            await HandleForSeedCollectionCreate(eventValue, context);
            return;
        }

        if (SymbolHelper.CheckSymbolIsSeedSymbol(eventValue.Symbol))
        {   Logger.LogDebug("TokenCreatedLogEventProcessor-4");
            var ownedSymbol = EnumDescriptionHelper.GetExtraInfoValue(eventValue.ExternalInfo,
                TokenCreatedExternalInfoEnum.SeedOwnedSymbol);
            var tsmSeedSymbolIndexId = IdGenerateHelper.GetTsmSeedSymbolId(context.ChainId, ownedSymbol);
            TsmSeedSymbolIndex tsmSeedSymbolIndex = null;
            if (context.ChainId == ForestIndexerConstants.MainChain)
            {
                Logger.LogInformation(
                    "[TokenCreatedLogEventProcessor] Seed Token Create at mainChain: {tsmSeedSymbolIndexId} ",
                    tsmSeedSymbolIndexId);
                tsmSeedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(tsmSeedSymbolIndexId);
                Logger.LogInformation(
                    "[TokenCreatedLogEventProcessor] Seed Token Create at mainChain then search: {tsmSeedSymbolIndexId} build {tsmSeedSymbolIndex}",
                    tsmSeedSymbolIndexId, JsonConvert.SerializeObject(tsmSeedSymbolIndex));
            }
            else
            {
                Logger.LogInformation(
                    "[TokenCreatedLogEventProcessor] Seed Token Create at no mainChain: {tsmSeedSymbolIndexId} ",
                    tsmSeedSymbolIndexId);
                tsmSeedSymbolIndex = await BuildNoMainChainTsmSeedSymbolIndex(context, eventValue.Symbol, ownedSymbol);
                Logger.LogInformation(
                    "[TokenCreatedLogEventProcessor] Seed Token Create at no mainChain then build: {tsmSeedSymbolIndexId} build {tsmSeedSymbolIndex}",
                    tsmSeedSymbolIndexId, JsonConvert.SerializeObject(tsmSeedSymbolIndex));
            }

            if (tsmSeedSymbolIndex == null) return;
            
            await HandleForSeedSymbolCreateAsync(eventValue, context, tsmSeedSymbolIndex);
            return;
        }

        if (SymbolHelper.CheckSymbolIsNoMainChainNFT(eventValue.Symbol, context.ChainId))
        {
            Logger.LogDebug("TokenCreatedLogEventProcessor-5");
            await HandleForNFTCreateAsync(eventValue, context);
            return;
        }

        if (SymbolHelper.CheckSymbolIsNoMainChainNFTCollection(eventValue.Symbol, context.ChainId))
        {
            Logger.LogDebug("TokenCreatedLogEventProcessor-6");
            await HandleForNFTCollectionCreateAsync(eventValue, context);
            return;
        }

        if (!SymbolHelper.CheckChainIdIsMain(context.ChainId))
        {
            Logger.LogDebug("TokenCreatedLogEventProcessor-7");
            await HandleForNoMainChainSymbolMarketTokenAsync(eventValue, context);
            return;
        }
    }

    private async Task TokenInfoIndexCreateAsync(TokenCreated eventValue, LogEventContext context)
    {
        if (eventValue == null || context == null)
        {
            return;
        }

        var tokenInfoIndex = _objectMapper.Map<TokenCreated, TokenInfoIndex>(eventValue);
        tokenInfoIndex.Issuer = eventValue.Issuer.ToBase58();
        if (eventValue.Owner == null)
        {
            tokenInfoIndex.Owner = eventValue.Issuer.ToBase58();
        }
        else
        {
            tokenInfoIndex.Owner = eventValue.Owner.ToBase58();
        }
        
        tokenInfoIndex.Id = IdGenerateHelper.GetTokenInfoId(context.ChainId, eventValue.Symbol);
        tokenInfoIndex.CreateTime = context.Block.BlockTime;
        _objectMapper.Map(context, tokenInfoIndex);
        await SaveEntityAsync(tokenInfoIndex);
    }
    
    private async Task<TsmSeedSymbolIndex> BuildNoMainChainTsmSeedSymbolIndex(LogEventContext context, string seedSymbol,
        String seedOwnedSymbol)
    {
        var tsmSeedSymbolIndex = new TsmSeedSymbolIndex();
        _objectMapper.Map(context, tsmSeedSymbolIndex);

        var address = ContractInfoHelper.GetSymbolRegistrarContractAddress(ForestIndexerConstants.MainChain);

        var specialSeed =
            await _aElfClientServiceProvider.GetSpecialSeedAsync(ForestIndexerConstants.MainChain, seedOwnedSymbol,
                address);

        var imageUrlPrefix = await _aElfClientServiceProvider.GetSeedImageUrlPrefixAsync(ForestIndexerConstants.MainChain,
            address);
        var imageUrl = (imageUrlPrefix!=null && !imageUrlPrefix.Value.IsNullOrEmpty())
            ? imageUrlPrefix.Value + seedSymbol + ForestIndexerConstants.SeedImageType
            : "";
        tsmSeedSymbolIndex.Id = IdGenerateHelper.GetSeedSymbolId(context.ChainId, seedOwnedSymbol);
        tsmSeedSymbolIndex.SeedImage = imageUrl;
        tsmSeedSymbolIndex.Symbol = seedOwnedSymbol;
        tsmSeedSymbolIndex.SeedName = IdGenerateHelper.GetSeedName(seedOwnedSymbol);
        tsmSeedSymbolIndex.SeedSymbol = seedSymbol;
        tsmSeedSymbolIndex.TokenType = TokenHelper.GetTokenType(seedOwnedSymbol);
        tsmSeedSymbolIndex.IsBurned = true;
        tsmSeedSymbolIndex.Status = SeedStatus.UNREGISTERED;
        Logger.LogInformation(
            "[TokenCreatedLogEventProcessor] mainChain TsmSeed is null specialSeed : {specialSeed}",JsonConvert.SerializeObject(specialSeed));

        if (specialSeed != null && specialSeed.SeedType == SeedType.Unique)
        {
            tsmSeedSymbolIndex.SeedType = specialSeed.SeedType;
            tsmSeedSymbolIndex.AuctionType = specialSeed.AuctionType;
            tsmSeedSymbolIndex.TokenPrice = new TokenPriceInfo()
            {
                Symbol = specialSeed.PriceSymbol,
                Amount = specialSeed.PriceAmount
            };
            
            if (specialSeed.SeedType == SeedType.Disable)
            {
                tsmSeedSymbolIndex.Status = SeedStatus.NOTSUPPORT;
            }
        }
        else
        {
            tsmSeedSymbolIndex.SeedType = SeedType.Regular;
            tsmSeedSymbolIndex.AuctionType = AuctionType.None;
            
            var seedsPrice = await _aElfClientServiceProvider.GetSeedsPriceAsync(ForestIndexerConstants.MainChain,
                address);

            PriceItem tokenPrice = null;
            if (tsmSeedSymbolIndex.TokenType == TokenType.NFT)
            {
                Logger.LogInformation(
                    "[TokenCreatedLogEventProcessor] mainChain TsmSeed is null TokenType is NFT : {SeedSymbol}",tsmSeedSymbolIndex.SeedSymbol);
                
                tokenPrice = seedsPrice?.NftPriceList?.Value?.FirstOrDefault(i => i.SymbolLength == seedOwnedSymbol.Length);
            }
            else
            {
                Logger.LogInformation(
                    "[TokenCreatedLogEventProcessor] mainChain TsmSeed is null TokenType is NOT NFT : {SeedSymbol}",tsmSeedSymbolIndex.SeedSymbol);
                tokenPrice = seedsPrice?.FtPriceList?.Value?.FirstOrDefault(i => i.SymbolLength == seedOwnedSymbol.Length);
            }

            Logger.LogInformation(
                "[TokenCreatedLogEventProcessor] mainChain TsmSeed is null tokenPrice is : {tokenPriceSymbol} {tokenSymbolLength}",tokenPrice?.Symbol,tokenPrice?.SymbolLength);
            tokenPrice = seedsPrice?.FtPriceList?.Value?.FirstOrDefault(i => i.SymbolLength == seedOwnedSymbol.Length);
            if (tokenPrice != null)
            {
                tsmSeedSymbolIndex.TokenPrice = new TokenPriceInfo()
                {
                    Symbol = tokenPrice.Symbol,
                    Amount = tokenPrice.Amount
                };
            }
        }
        return tsmSeedSymbolIndex;
    }

    private async Task HandleForNoMainChainSymbolMarketTokenAsync(TokenCreated eventValue, LogEventContext context)
    {
        var noMainTsmSeedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        var noMainTsmSeedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(noMainTsmSeedSymbolIndexId);
        Logger.LogDebug("TokenCreatedLogEventProcessor-10 {A}", noMainTsmSeedSymbolIndexId + " " +
                         JsonConvert.SerializeObject(noMainTsmSeedSymbolIndex));
        if (noMainTsmSeedSymbolIndex != null && noMainTsmSeedSymbolIndex.Status != SeedStatus.REGISTERED)
        {
            _objectMapper.Map(context, noMainTsmSeedSymbolIndex);
            noMainTsmSeedSymbolIndex.Status = SeedStatus.REGISTERED;
            await SaveEntityAsync(noMainTsmSeedSymbolIndex);
            var noMainSeedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, noMainTsmSeedSymbolIndex.SeedSymbol);
            var noMainSeedSymbolIndex = await GetEntityAsync<TsmSeedSymbolIndex>(noMainSeedSymbolIndexId);
            
            if (noMainSeedSymbolIndex == null) return;
            _objectMapper.Map(context, noMainSeedSymbolIndex);
            noMainSeedSymbolIndex.Status = SeedStatus.REGISTERED;
            await SaveEntityAsync(noMainSeedSymbolIndex);
        }
        Logger.LogDebug("TokenCreatedLogEventProcessor-11 {A} {B}",noMainTsmSeedSymbolIndexId,JsonConvert.SerializeObject(noMainTsmSeedSymbolIndex));
        var symbolMarketTokenIndexId = IdGenerateHelper.GetSymbolMarketTokenId(context.ChainId, eventValue.Symbol);
        var symbolMarketTokenIndex = await GetEntityAsync<SeedSymbolMarketTokenIndex>(symbolMarketTokenIndexId);

        Logger.LogDebug("TokenCreatedLogEventProcessor-12 {A} {B}", symbolMarketTokenIndexId,
            JsonConvert.SerializeObject(symbolMarketTokenIndex));
        if (symbolMarketTokenIndex != null) return;

        var ownerContractAddress =
            ContractInfoHelper.GetProxyAccountContractAddress(ForestIndexerConstants.MainChain);
        var issueChainContractAddress =
            ContractInfoHelper.GetProxyAccountContractAddress(ChainHelper.ConvertChainIdToBase58(eventValue.IssueChainId));

        var proxyAccountForOwner =
            await _aElfClientServiceProvider.GetProxyAccountByProxyAccountAddressAsync(ForestIndexerConstants.MainChain,
                ownerContractAddress, eventValue.Owner);
        var ownerManegerList =
            proxyAccountForOwner?.ManagementAddresses?
                .Where(item => item != null)
                .Select(item => item.Address?.ToBase58())
                .Where(address => address != null)
                .ToList() ?? new List<string>();

        var proxyAccountForIssue =
            await _aElfClientServiceProvider.GetProxyAccountByProxyAccountAddressAsync(ChainHelper.ConvertChainIdToBase58(eventValue.IssueChainId),
                issueChainContractAddress, eventValue.Issuer);
        var issueManegerList =
            proxyAccountForIssue?.ManagementAddresses?
                .Where(item => item != null)
                .Select(item => item.Address?.ToBase58())
                .Where(address => address != null)
                .ToList() ?? new List<string>();

        symbolMarketTokenIndex = new SeedSymbolMarketTokenIndex()
        {
            Id = symbolMarketTokenIndexId,
            TotalSupply = eventValue.TotalSupply,
            Supply = 0,
            Issued = 0,
            Symbol = eventValue.Symbol,
            TokenName = eventValue.TokenName,
            IssueChainId = eventValue.IssueChainId,
            IssueChain = ChainHelper.ConvertChainIdToBase58(eventValue.IssueChainId),
            SameChainFlag = context.ChainId.Equals(ChainHelper.ConvertChainIdToBase58(eventValue.IssueChainId)),
            Decimals = eventValue.Decimals,
            IsBurnable = eventValue.IsBurnable,
            Owner = eventValue.Owner.ToBase58(),
            Issuer = eventValue.Issuer.ToBase58(),
            IssueManagerSet = new HashSet<string>(issueManegerList),
            RandomIssueManager = issueManegerList?.FirstOrDefault(),
            OwnerManagerSet = new HashSet<string>(ownerManegerList),
            RandomOwnerManager = ownerManegerList?.FirstOrDefault(),
        };

        if (eventValue.ExternalInfo.Value.ContainsKey(
                EnumDescriptionHelper.GetEnumDescription(TokenCreatedExternalInfoEnum.NFTLogoImageUrl)))
        {
            symbolMarketTokenIndex.SymbolMarketTokenLogoImage =
                eventValue.ExternalInfo.Value[
                    EnumDescriptionHelper.GetEnumDescription(TokenCreatedExternalInfoEnum.NFTLogoImageUrl)];
        }

        _objectMapper.Map(context, symbolMarketTokenIndex);
        Logger.LogDebug("TokenCreatedLogEventProcessor-13 {A}",JsonConvert.SerializeObject(symbolMarketTokenIndex));
        await SaveEntityAsync(symbolMarketTokenIndex);
    }
    
    private async Task HandleForSeedCollectionCreate(TokenCreated eventValue, LogEventContext context)
    {
        var seedCollectionIndexId = IdGenerateHelper.GetSeedCollectionId(context.ChainId, eventValue.Symbol);
        var seedCollectionIndex = await GetEntityAsync<CollectionIndex>(seedCollectionIndexId);
        
        if (seedCollectionIndex != null) return;

        seedCollectionIndex = _objectMapper.Map<TokenCreated, CollectionIndex>(eventValue);
        seedCollectionIndex.ExternalInfoDictionary = eventValue.ExternalInfo.Value
            .Select(entity => new ExternalInfoDictionary
            {
                Key = entity.Key,
                Value = entity.Value
            }).ToList();

        seedCollectionIndex.Issuer = eventValue.Issuer.ToBase58();
        seedCollectionIndex.Owner = (eventValue.Owner == null ? eventValue.Issuer : eventValue.Owner).ToBase58();
        seedCollectionIndex.IsDeleted = false;
        seedCollectionIndex.Id = seedCollectionIndexId;
        seedCollectionIndex.TokenContractAddress = GetContractAddress(context.ChainId);
        seedCollectionIndex.CreateTime = context.Block.BlockTime;
        seedCollectionIndex.CreatorAddress = eventValue.Issuer.ToBase58();
        seedCollectionIndex.CollectionType = CollectionType.SeedCollection;
        seedCollectionIndex.LogoImage = EnumDescriptionHelper.GetExtraInfoValue(eventValue.ExternalInfo,
            TokenCreatedExternalInfoEnum.NFTLogoImageUrl);
        seedCollectionIndex.FeaturedImageLink = EnumDescriptionHelper.GetExtraInfoValue(eventValue.ExternalInfo,
            TokenCreatedExternalInfoEnum.NFTFeaturedImageLink);
        seedCollectionIndex.Description = EnumDescriptionHelper.GetExtraInfoValue(eventValue.ExternalInfo,
            TokenCreatedExternalInfoEnum.NFTDescription);

        _objectMapper.Map(context, seedCollectionIndex);

        seedCollectionIndex = await FillProxyAccountInfoForNFTCollectionIndexAsync
            (seedCollectionIndex, context.ChainId);
        
        await SaveEntityAsync(seedCollectionIndex);
    }
    
    private async Task<CollectionIndex> FillProxyAccountInfoForNFTCollectionIndexAsync(
        CollectionIndex collectionIndex,
        string chainId)
    {
        if (collectionIndex == null || chainId.IsNullOrEmpty())
            return collectionIndex;
        if (collectionIndex.Owner.IsNullOrEmpty())
            collectionIndex.Owner = collectionIndex.Issuer;
        var proxyAccountId = IdGenerateHelper.GetProxyAccountIndexId(collectionIndex.Owner);
        var proxyAccount = await GetEntityAsync<ProxyAccountIndex>(proxyAccountId);

        return FillNFTCollectionIndex(collectionIndex, proxyAccount);
    }
    private CollectionIndex FillNFTCollectionIndex(CollectionIndex collectionIndex,
        ProxyAccountIndex proxyAccountIndex)
    {
        if (collectionIndex == null) return collectionIndex;

        if (proxyAccountIndex != null)
            collectionIndex.OwnerManagerSet = proxyAccountIndex.ManagersSet;
        else
            collectionIndex.OwnerManagerSet = new HashSet<string> { collectionIndex.Owner };

        collectionIndex.RandomOwnerManager = collectionIndex.OwnerManagerSet?.FirstOrDefault("");

        return collectionIndex;
    }


    private async Task HandleForSeedSymbolCreateAsync(TokenCreated eventValue, LogEventContext context,
        TsmSeedSymbolIndex tsmSeedSymbolIndex)
    {
        await DoHandleForSeedSymbolCreateAsync(eventValue, context, tsmSeedSymbolIndex);
    }

    private async Task DoHandleForSeedSymbolCreateAsync(TokenCreated eventValue, LogEventContext context,
        TsmSeedSymbolIndex tsmSeedSymbolIndex)
    {
        bool sameChainIdFlag = context.ChainId == tsmSeedSymbolIndex.ChainId;
        var seedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, eventValue.Symbol);
        var seedSymbolIndex = _objectMapper.Map<TokenCreated, SeedSymbolIndex>(eventValue);
        seedSymbolIndex.ExternalInfoDictionary = eventValue.ExternalInfo.Value
            .Select(entity => new ExternalInfoDictionary
            {
                Key = entity.Key,
                Value = entity.Value
            }).ToList();
        seedSymbolIndex.Issuer = eventValue.Issuer.ToBase58();
        seedSymbolIndex.Owner = (eventValue.Owner == null ? eventValue.Issuer : eventValue.Owner).ToBase58();
        seedSymbolIndex.IsDeleted = false;
        seedSymbolIndex.Id = seedSymbolIndexId;
        seedSymbolIndex.TokenContractAddress = GetContractAddress(context.ChainId);
        seedSymbolIndex.CreateTime = context.Block.BlockTime;
        seedSymbolIndex.IsDeleteFlag = false;
        seedSymbolIndex.SeedType = tsmSeedSymbolIndex.SeedType;
        seedSymbolIndex.TokenType = tsmSeedSymbolIndex.TokenType;
        seedSymbolIndex.SeedImage = tsmSeedSymbolIndex.SeedImage;
        if (tsmSeedSymbolIndex.TokenPrice != null)
        {
            seedSymbolIndex.Price = tsmSeedSymbolIndex.TokenPrice.Amount;
            seedSymbolIndex.PriceSymbol = tsmSeedSymbolIndex.TokenPrice.Symbol;
        }

        seedSymbolIndex.RegisterTime = context.Block.BlockTime;
        seedSymbolIndex.RegisterTimeSecond = context.Block.BlockTime.ToUtcSeconds();
        seedSymbolIndex.SeedOwnedSymbol = EnumDescriptionHelper.GetExtraInfoValue(eventValue.ExternalInfo,
            TokenCreatedExternalInfoEnum.SeedOwnedSymbol);

        var seedExpTime = EnumDescriptionHelper.GetExtraInfoValue(eventValue.ExternalInfo,
            TokenCreatedExternalInfoEnum.SeedExpTime);
        if (long.TryParse(seedExpTime, out var seedExpTimeSecond))
        {
            seedSymbolIndex.SeedExpTimeSecond = seedExpTimeSecond;
            seedSymbolIndex.SeedExpTime = DateTimeHelper.FromUnixTimeSeconds(seedExpTimeSecond);
            tsmSeedSymbolIndex.ExpireTime = seedExpTimeSecond;
        }

        _objectMapper.Map(context, seedSymbolIndex);
        Logger.LogDebug("TokenCreatedLogEventProcessor-41 {A}",JsonConvert.SerializeObject(seedSymbolIndex));
        tsmSeedSymbolIndex.RegisterTime = DateTimeHelper.ToUnixTimeMilliseconds(context.Block.BlockTime);

        if (sameChainIdFlag)
        {
            Logger.LogDebug("TokenCreatedLogEventProcessor-41-1 {A}",JsonConvert.SerializeObject(seedSymbolIndex));
            await SaveEntityAsync(tsmSeedSymbolIndex);
        }
        else if(!sameChainIdFlag && tsmSeedSymbolIndex.ChainId == ForestIndexerConstants.MainChain)
        {
            Logger.LogDebug("TokenCreatedLogEventProcessor-41-2 {A}",JsonConvert.SerializeObject(seedSymbolIndex));
            var tsmSeedSymbolIndexIdNoMainChainId =
                IdGenerateHelper.GetSeedSymbolId(context.ChainId, tsmSeedSymbolIndex.Symbol);
            var tsmSeedSymbolIndexNoMainChain =
                _objectMapper.Map<TsmSeedSymbolIndex, TsmSeedSymbolIndex>(tsmSeedSymbolIndex);
            tsmSeedSymbolIndexNoMainChain.Id = tsmSeedSymbolIndexIdNoMainChainId;
            tsmSeedSymbolIndexNoMainChain.IsBurned = true;
            tsmSeedSymbolIndexNoMainChain.ChainId = context.ChainId;
            _objectMapper.Map(context, tsmSeedSymbolIndexNoMainChain);
            await SaveEntityAsync(tsmSeedSymbolIndexNoMainChain);
        }
        await SaveEntityAsync(seedSymbolIndex);
        await SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
    }

    private async Task SaveNFTListingChangeIndexAsync(LogEventContext context, string symbol)
    {
        if (context.ChainId.Equals(ForestIndexerConstants.MainChain))
        {
            return;
        }

        if (symbol.Equals(ForestIndexerConstants.TokenSimpleElf))
        {
            return;
        }
        var nftListingChangeIndex = new NFTListingChangeIndex
        {
            Symbol = symbol,
            Id = IdGenerateHelper.GetId(context.ChainId, symbol),
            UpdateTime = context.Block.BlockTime
        };
        _objectMapper.Map(context, nftListingChangeIndex);
        await SaveEntityAsync(nftListingChangeIndex);

    }
    
    private async Task HandleForNFTCreateAsync(TokenCreated eventValue, LogEventContext context)
    {
        var nftInfoIndexId = IdGenerateHelper.GetNFTInfoId(context.ChainId, eventValue.Symbol);
        var nftInfoIndex = await GetEntityAsync<NFTInfoIndex>(nftInfoIndexId);
        if (nftInfoIndex != null) return;
        Logger.LogDebug("TokenCreatedLogEventProcessor-6 symbol:{A}", eventValue.Symbol);
        var collectionSymbol = SymbolHelper.GetNFTCollectionSymbol(eventValue.Symbol);
        if (collectionSymbol == null) return;

        var nftCollectionIndexId = IdGenerateHelper.GetNFTCollectionId(context.ChainId, collectionSymbol);
        var nftCollectionIndex = await GetEntityAsync<CollectionIndex>(nftCollectionIndexId);
        if (nftCollectionIndex == null) return;

        nftInfoIndex = _objectMapper.Map<TokenCreated, NFTInfoIndex>(eventValue);
        nftInfoIndex.ExternalInfoDictionary = eventValue.ExternalInfo.Value
            .Where(entity =>
                !entity.Key.Equals(
                    EnumDescriptionHelper.GetEnumDescription(TokenCreatedExternalInfoEnum.InscriptionImage)))
            .Select(entity => new ExternalInfoDictionary
            {
                Key = entity.Key,
                Value = entity.Value
            }).ToList();

        nftInfoIndex.CollectionName = nftCollectionIndex.TokenName;
        nftInfoIndex.CollectionSymbol = collectionSymbol;
        nftInfoIndex.CollectionId = nftCollectionIndexId;

        nftInfoIndex.Id = nftInfoIndexId;
        nftInfoIndex.Issuer = eventValue.Issuer.ToBase58();
        nftInfoIndex.Owner = (eventValue.Owner == null ? eventValue.Issuer : eventValue.Owner).ToBase58();
        nftInfoIndex.CreatorAddress = eventValue.Issuer.ToBase58();
        nftInfoIndex.IsDeleted = false;
        nftInfoIndex.Supply = 0;
        nftInfoIndex.Issued = 0;
        nftInfoIndex.ListingPrice = 0;
        nftInfoIndex.CreateTime = context.Block.BlockTime;
        nftInfoIndex.TokenContractAddress = GetContractAddress(context.ChainId);

        if (eventValue.ExternalInfo.Value.ContainsKey(
                EnumDescriptionHelper.GetEnumDescription(TokenCreatedExternalInfoEnum.NFTImageUrl)))
        {
            nftInfoIndex.ImageUrl = EnumDescriptionHelper.GetExtraInfoValue(eventValue.ExternalInfo,
                TokenCreatedExternalInfoEnum.NFTImageUrl);
        }else if (eventValue.ExternalInfo.Value.ContainsKey(
                      EnumDescriptionHelper.GetEnumDescription(TokenCreatedExternalInfoEnum.SpecialInscriptionImage)))
        {
            nftInfoIndex.ImageUrl = EnumDescriptionHelper.GetExtraInfoValue(eventValue.ExternalInfo,
                TokenCreatedExternalInfoEnum.SpecialInscriptionImage);
        }else if (eventValue.ExternalInfo.Value.ContainsKey(
                      EnumDescriptionHelper.GetEnumDescription(TokenCreatedExternalInfoEnum.NFTImageUri)))
        {
            nftInfoIndex.ImageUrl = EnumDescriptionHelper.GetExtraInfoValue(eventValue.ExternalInfo,
                TokenCreatedExternalInfoEnum.NFTImageUri);
        }
        
        _objectMapper.Map(context, nftInfoIndex);
        await SaveEntityAsync(nftInfoIndex);
        Logger.LogDebug("TokenCreatedLogEventProcessor-7 nftSave Id:{A} Symbol:{B}", nftInfoIndex.Id, nftInfoIndex.Symbol);
        await SaveNFTListingChangeIndexAsync(context, eventValue.Symbol);
    }

    private async Task HandleForNFTCollectionCreateAsync(TokenCreated eventValue, LogEventContext context)
    {
        var nftCollectionIndexId = IdGenerateHelper.GetNFTCollectionId(context.ChainId, eventValue.Symbol);
        var nftCollectionIndex = await GetEntityAsync<CollectionIndex>(nftCollectionIndexId);
        
        if (nftCollectionIndex != null) return;

        nftCollectionIndex = _objectMapper.Map<TokenCreated, CollectionIndex>(eventValue);
        nftCollectionIndex.ExternalInfoDictionary = eventValue.ExternalInfo.Value
            .Select(entity => new ExternalInfoDictionary
            {
                Key = entity.Key,
                Value = entity.Value
            }).ToList();

        nftCollectionIndex.Issuer = eventValue.Issuer.ToBase58();
        nftCollectionIndex.Owner = (eventValue.Owner == null ? eventValue.Issuer : eventValue.Owner).ToBase58();
        nftCollectionIndex.IsDeleted = false;
        nftCollectionIndex.Id = nftCollectionIndexId;
        nftCollectionIndex.TokenContractAddress = GetContractAddress(context.ChainId);
        nftCollectionIndex.CreateTime = context.Block.BlockTime;
        nftCollectionIndex.CreatorAddress = eventValue.Issuer.ToBase58();
        nftCollectionIndex.CollectionType = CollectionType.NftCollection;

        if (eventValue.ExternalInfo.Value.ContainsKey(
                EnumDescriptionHelper.GetEnumDescription(TokenCreatedExternalInfoEnum.NFTLogoImageUrl)))
        {
            nftCollectionIndex.LogoImage = EnumDescriptionHelper.GetExtraInfoValue(eventValue.ExternalInfo,
                TokenCreatedExternalInfoEnum.NFTLogoImageUrl);
        }else if (eventValue.ExternalInfo.Value.ContainsKey(
                      EnumDescriptionHelper.GetEnumDescription(TokenCreatedExternalInfoEnum.SpecialInscriptionImage)))
        {
            nftCollectionIndex.LogoImage = EnumDescriptionHelper.GetExtraInfoValue(eventValue.ExternalInfo,
                TokenCreatedExternalInfoEnum.SpecialInscriptionImage);
        }else if (eventValue.ExternalInfo.Value.ContainsKey(
                      EnumDescriptionHelper.GetEnumDescription(TokenCreatedExternalInfoEnum.NFTImageUri)))
        {
            nftCollectionIndex.LogoImage = EnumDescriptionHelper.GetExtraInfoValue(eventValue.ExternalInfo,
                TokenCreatedExternalInfoEnum.NFTImageUri);
        }
        
        nftCollectionIndex.FeaturedImageLink = EnumDescriptionHelper.GetExtraInfoValue(eventValue.ExternalInfo,
            TokenCreatedExternalInfoEnum.NFTFeaturedImageLink);
        nftCollectionIndex.Description = EnumDescriptionHelper.GetExtraInfoValue(eventValue.ExternalInfo,
            TokenCreatedExternalInfoEnum.NFTDescription);

        _objectMapper.Map(context, nftCollectionIndex);

        var ownerContractAddress = ContractInfoHelper.GetProxyAccountContractAddress(context.ChainId);
        
        var proxyAccountForOwner =
            await _aElfClientServiceProvider.GetProxyAccountByProxyAccountAddressAsync(context.ChainId,
                ownerContractAddress, eventValue.Owner);
        var ownerManegerList =
            proxyAccountForOwner?.ManagementAddresses?
                .Where(item => item != null)
                .Select(item => item.Address?.ToBase58())
                .Where(address => address != null)
                .ToList() ?? new List<string>();

        if (ownerManegerList.IsNullOrEmpty())
        {
            if (nftCollectionIndex.Owner.IsNullOrEmpty())
                nftCollectionIndex.Owner = nftCollectionIndex.Issuer;
            nftCollectionIndex.OwnerManagerSet = new HashSet<string>(new List<string>{nftCollectionIndex.Owner});
            nftCollectionIndex.RandomOwnerManager = nftCollectionIndex.Owner;
        }
        else
        {
            nftCollectionIndex.OwnerManagerSet = new HashSet<string>(ownerManegerList);
            nftCollectionIndex.RandomOwnerManager = ownerManegerList?.FirstOrDefault();
        }
        
        await SaveEntityAsync(nftCollectionIndex);
        
        var collectionChangeIndex = new CollectionChangeIndex();
        collectionChangeIndex.Symbol = eventValue.Symbol;
        collectionChangeIndex.Id = IdGenerateHelper.GetNFTCollectionId(context.ChainId, eventValue.Symbol);
        collectionChangeIndex.UpdateTime = context.Block.BlockTime;
        _objectMapper.Map(context, collectionChangeIndex);
        await SaveEntityAsync(collectionChangeIndex);
    }
}