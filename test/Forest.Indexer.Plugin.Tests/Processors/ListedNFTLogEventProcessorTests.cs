using AElf;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors;
using Forest.Indexer.Plugin.Tests.Helper;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using Xunit;

namespace Forest.Indexer.Plugin.Tests.Processors;

public sealed class ListedNFTLogEventProcessorTests : ForestIndexerPluginTestBase
{
    private readonly IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo>
        _NFTListingIndexRepository;

    private readonly INFTListingInfoProvider _listingInfoProvider;
    private readonly IAElfIndexerClientInfoProvider _indexerClientInfoProvider;
    private readonly IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> _nftInfoIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<NFTListingChangeIndex, LogEventInfo> _nftListingChangeIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> _userBalanceIndexRepository;

    private const string ChainId = "tDVW";
    
    public ListedNFTLogEventProcessorTests()
    {
        _NFTListingIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo>>();
        _listingInfoProvider =
            GetRequiredService<INFTListingInfoProvider>();
        _indexerClientInfoProvider = GetRequiredService<IAElfIndexerClientInfoProvider>();
        _blockStateSetLogEventInfoProvider = GetRequiredService<IBlockStateSetProvider<LogEventInfo>>();
        _nftInfoIndexRepository = GetRequiredService<IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo>>();
        _userBalanceIndexRepository = GetRequiredService<IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo>>();
        _nftListingChangeIndexRepository = 
            GetRequiredService<IAElfIndexerClientEntityRepository<NFTListingChangeIndex, LogEventInfo>>();

    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(BuildUserBalanceProvider());
    }

    private static IUserBalanceProvider BuildUserBalanceProvider()
    {
        var mockUserBalance = new Mock<IUserBalanceProvider>();
        
        mockUserBalance.Setup(service => service.QueryUserBalanceByIdAsync(IdGenerateHelper.GetUserBalanceId(
                "2YcGvyn7QPmhvrZ7aaymmb2MDYWhmAks356nV3kUwL8FkGSYeZ", ChainId,
                IdGenerateHelper.GetNFTInfoId(ChainId, "SYB-1")), ChainId))
            .ReturnsAsync(new UserBalanceIndex
            {
                Amount = 999
            });
        mockUserBalance.Setup(service => service.QueryUserBalanceByIdAsync(IdGenerateHelper.GetUserBalanceId(
                "aLyxCJvWMQH6UEykTyeWAcYss9baPyXkrMQ37BHnUicxD2LL3", ChainId,
                IdGenerateHelper.GetNFTInfoId(ChainId, "SYB-1")), ChainId))
            .ReturnsAsync(new UserBalanceIndex
            {
                Amount = 100
            });

        return mockUserBalance.Object;
    }
    

    [Fact]
    public async Task<NFTListingInfoIndex> HandleListedNFTAddedAsync_Test()
    {
        const string symbol = "SYB-1";
        const long amount = 100; // price
        const long durationHours = 1;
        const string ownerPublicKey = "AAA";
        return await HandleListedNftAddedAsync(symbol, amount, ownerPublicKey, durationHours, DateTime.UtcNow);
    }

    [Fact]
    public async Task HandleListedNFTChangedAsync_Test()
    {
        var listingInfoIndex = await HandleListedNFTAddedAsync_Test();
        const string symbol = "SYB-1";
        const string tokenSymbol = "SYB-1";
        const long amount = 500;
        const long decimals = 8;
        const long quantity = 2;
        Hash whitelistId = HashHelper.ComputeFrom("test@gmail.com");

        var logEventContext = MockLogEventContext(100);
        var blockStateSetKey = await MockBlockState(logEventContext);
        var listedNFTChanged = new ListedNFTChanged()
        {
            WhitelistId = whitelistId,
            Symbol = symbol,
            Quantity = quantity,
            Owner = Address.FromPublicKey("AAA".HexToByteArray()),
            Duration = new ListDuration()
            {
                DurationHours = 1,
                StartTime = Timestamp.FromDateTime(listingInfoIndex.StartTime),
                PublicTime = Timestamp.FromDateTime(DateTime.UtcNow),
            },
            Price = new Price()
            {
                Amount = amount,
                Symbol = tokenSymbol,
            }
        };
        var logEventInfo = MockLogEventInfo(listedNFTChanged.ToLogEvent());
        var listedNftChangedLogEventProcessor = GetRequiredService<ListedNFTChangedLogEventProcessor>();
        listedNftChangedLogEventProcessor.GetContractAddress(logEventContext.ChainId);
        await listedNftChangedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        var nftListingIndexData = await _NFTListingIndexRepository.GetFromBlockStateSetAsync(listingInfoIndex.Id, logEventContext.ChainId);
        nftListingIndexData.ShouldNotBe(null);
        nftListingIndexData.BlockHeight.ShouldBe(logEventContext.BlockHeight);
        nftListingIndexData.Symbol.ShouldBe(symbol);
        nftListingIndexData.Quantity.ShouldBe(quantity);
        nftListingIndexData.Prices.ShouldBe(amount / (decimal)Math.Pow(10, decimals));


        // PurchaseToken NOT FOUND,
        listedNFTChanged.Price.Symbol = "NOTFOUND";
        logEventInfo = MockLogEventInfo(listedNFTChanged.ToLogEvent());
        await listedNftChangedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        
        nftListingIndexData = await _NFTListingIndexRepository.GetFromBlockStateSetAsync(listingInfoIndex.Id, logEventContext.ChainId);
        nftListingIndexData.ShouldNotBe(null);
        nftListingIndexData.BlockHeight.ShouldBe(logEventContext.BlockHeight);
        nftListingIndexData.Symbol.ShouldBe(symbol);
        nftListingIndexData.Quantity.ShouldBe(quantity);
        nftListingIndexData.Prices.ShouldBe(amount / (decimal)Math.Pow(10, decimals));
        
        // ListedNFT NOT FOUND,
        listedNFTChanged.Duration.StartTime = Timestamp.FromDateTime(DateTime.UtcNow);
        logEventInfo = MockLogEventInfo(listedNFTChanged.ToLogEvent());
        await listedNftChangedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        
        nftListingIndexData = await _NFTListingIndexRepository.GetFromBlockStateSetAsync(listingInfoIndex.Id, logEventContext.ChainId);
        nftListingIndexData.ShouldNotBe(null);
        nftListingIndexData.BlockHeight.ShouldBe(logEventContext.BlockHeight);
        nftListingIndexData.Symbol.ShouldBe(symbol);
        nftListingIndexData.Quantity.ShouldBe(quantity);
        nftListingIndexData.Prices.ShouldBe(amount / (decimal)Math.Pow(10, decimals));
        nftListingIndexData.RealQuantity.ShouldBe(Math.Min(nftListingIndexData.RealQuantity,quantity));

        //check minListingPrice info
        var nftId = IdGenerateHelper.GetNFTInfoId(ChainId, symbol);
        var nftInfo = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftId, ChainId);
        nftInfo.MinListingId.ShouldBe(nftListingIndexData.Id);
        Assert.Equal(0.000005m, nftInfo.MinListingPrice);
    }
    
    [Fact]
    public async Task HandleListedNFTRemovedAsync_Test()
    {
        const string symbol = "SYB-1";
        const string chainId = "tDVW";
        const long amount = 100; // price
        const long durationHours = 1;
        const string ownerPublicKey = "AAA";
        var dateNow = DateTime.UtcNow;
        var listingInfoIndex = await HandleListedNftAddedAsync(symbol, amount, ownerPublicKey, durationHours, dateNow);

        var logEventContext = MockLogEventContext(100);
        var blockStateSetKey = await MockBlockState(logEventContext);

        var listedNftRemoved = new ListedNFTRemoved
        {
            Symbol = symbol,
            Owner = Address.FromPublicKey("AAA".HexToByteArray()),
            Price = new Price
            {
                Amount = 0,
                Symbol = symbol,
                //TokenId = 1
            },
            Duration = new ListDuration()
            {
                DurationHours = 1,
                StartTime = Timestamp.FromDateTime(listingInfoIndex.StartTime),
                PublicTime = Timestamp.FromDateTime(dateNow),
            }
        };
        
        var listedNftRemovedLogEventProcessor = GetRequiredService<ListedNFTRemovedLogEventProcessor>();
        listedNftRemovedLogEventProcessor.GetContractAddress(logEventContext.ChainId);
        
        // startTime does not match, record will not be deleted.
        listedNftRemoved.Duration.StartTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(10));
        var logEventInfo = MockLogEventInfo(listedNftRemoved.ToLogEvent());
        await listedNftRemovedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        var nftListingIndexData = await _NFTListingIndexRepository.GetFromBlockStateSetAsync(listingInfoIndex.Id, chainId);
        nftListingIndexData.ShouldNotBeNull();
        
        // startTime matches, record will be deleted
        listedNftRemoved.Duration.StartTime = Timestamp.FromDateTime(listingInfoIndex.StartTime);
        logEventInfo = MockLogEventInfo(listedNftRemoved.ToLogEvent());
        await listedNftRemovedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        nftListingIndexData = await _NFTListingIndexRepository.GetFromBlockStateSetAsync(listingInfoIndex.Id, chainId);
        nftListingIndexData.ShouldBe(null);
        
        //check minListingPrice info
        var nftId = IdGenerateHelper.GetNFTInfoId(ChainId, symbol);
        var nftInfo = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftId, ChainId);
        nftInfo.MinListingId.ShouldBe(null);
        Assert.Equal(0L, nftInfo.MinListingPrice);

        var nftListingChangeInfo = await _nftListingChangeIndexRepository.GetFromBlockStateSetAsync(nftId,ChainId);
        nftListingChangeInfo.ShouldNotBeNull();
        nftListingChangeInfo.Symbol.ShouldBe(symbol);
    }

    [Fact]
    public async Task TestQueryLatestNFTListingInfoByNFTIdsAsync()
    {
        const string nftSymbol = "SYB-1";
        const string chainId = "tDVW";
        var address1 = Address.FromPublicKey("AAA".HexToByteArray()).ToBase58();
        var address2 = Address.FromPublicKey("BBB".HexToByteArray()).ToBase58();
        var nftId = IdGenerateHelper.GetNFTInfoId(chainId, nftSymbol);
        
        await MockCreateNFT();
        var firstTime = DateTime.UtcNow;
        await Add1(firstTime);
        var result1 = _listingInfoProvider.QueryLatestNFTListingInfoByNFTIdsAsync(new List<string> { nftId },"");
        
        result1.ShouldNotBeNull();
        result1.Result.ShouldNotBeNull();
        result1.Result.Keys.ShouldNotBeNull();
        result1.Result.ShouldContainKey(nftId);
        result1.Result[nftId].Owner.ShouldBe(address1);
        result1.Result[nftId].Prices.ShouldBe(new decimal(0.0000010));
        result1.Result[nftId].PurchaseToken.ShouldNotBeNull();

        var secondTime = DateTime.UtcNow.AddMinutes(1);
        await Add2(secondTime);
       
        var result2 = _listingInfoProvider.QueryLatestNFTListingInfoByNFTIdsAsync(new List<string> { nftId },"");
        
        result2.ShouldNotBeNull();
        result2.Result.ShouldNotBeNull();
        result2.Result.Keys.ShouldNotBeNull();
        result2.Result.ShouldContainKey(nftId);
        result2.Result[nftId].Owner.ShouldBe(address2);
        result2.Result[nftId].Prices.ShouldBe(new decimal(0.0000020));
        result2.Result[nftId].PurchaseToken.ShouldNotBeNull();

        var key = GrainIdHelper.GenerateGrainId("BlockStateSets", _indexerClientInfoProvider.GetClientId(), chainId,
            _indexerClientInfoProvider.GetVersion());

        var blockHash =  _blockStateSetLogEventInfoProvider.GetBlockStateSetsAsync(key).Result.Values.First().BlockHash;
        blockHash.ShouldNotBeNull();
    }
    
    [Fact]
    public async Task TestNFTAndListingInfoAsync()
    {
        const string nftSymbol = "SYB-1";
        const string chainId = "tDVW";
        var address1 = Address.FromPublicKey("AAA".HexToByteArray()).ToBase58();
        var address2 = Address.FromPublicKey("BBB".HexToByteArray()).ToBase58();
        var originAddress1 = "AAA";
        var originAddress2 = "BBB";
        
        var nftId = IdGenerateHelper.GetNFTInfoId(chainId, nftSymbol);
        //await MockCreateNFT();
        await HandleNFTIssueAsync_Test();
        var nftInfo0 = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftId,chainId);
        nftInfo0.ShouldNotBeNull();
        nftInfo0.ListingPrice.ShouldBe(new decimal(0));
        nftInfo0.ListingToken.ShouldBeNull();
        nftInfo0.Symbol.ShouldNotBeNull();
        nftInfo0.OtherOwnerListingFlag.ShouldBeFalse();

        var firstTime = DateTime.UtcNow.AddMinutes(1);
        var secondTime = DateTime.UtcNow.AddMinutes(2);
        await Add1(firstTime);
        
        var nftInfo1 = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftId,chainId);
        nftInfo1.ShouldNotBeNull();
        nftInfo1.Symbol.ShouldNotBeNull();
        nftInfo1.ListingAddress.ShouldBe(address1);
        nftInfo1.ListingPrice.ShouldBe(new decimal(0.0000010));
        nftInfo1.ListingToken.ShouldNotBeNull();
        nftInfo1.OtherOwnerListingFlag.ShouldBeFalse();
        
        await Add2(secondTime);
        var nftInfo2 = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftId,chainId);
        nftInfo2.ShouldNotBeNull();
        nftInfo2.ListingAddress.ShouldBe(address2);
        nftInfo2.Symbol.ShouldNotBeNull();
        nftInfo2.ListingPrice.ShouldBe(new decimal(0.0000020));
        nftInfo2.ListingToken.ShouldNotBeNull();
        nftInfo2.OtherOwnerListingFlag.ShouldBeTrue();

        await Modify1(firstTime);
        var nftInfo3 = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftId,chainId);
        nftInfo3.ShouldNotBeNull();
        nftInfo3.ListingAddress.ShouldBe(address1);
        nftInfo3.Symbol.ShouldNotBeNull();
        nftInfo3.ListingPrice.ShouldBe(new decimal(0.0000050));
        nftInfo3.ListingToken.ShouldNotBeNull();
        nftInfo3.OtherOwnerListingFlag.ShouldBeTrue();

        await Remove1(originAddress1,firstTime,chainId,"aaa");
        var nftInfo4 = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftId,chainId);
        nftInfo4.ShouldNotBeNull();
        nftInfo4.ListingAddress.ShouldBe(address2);
        nftInfo4.Symbol.ShouldNotBeNull();
        nftInfo4.ListingPrice.ShouldBe(new decimal(0.0000020));
        nftInfo4.ListingToken.ShouldNotBeNull();
        nftInfo4.OtherOwnerListingFlag.ShouldBeFalse();
        
        await Remove1(originAddress2,secondTime,chainId,"ccc");
        var nftInfo5 = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftId,chainId);
        nftInfo5.ShouldNotBeNull();
        nftInfo5.ListingAddress.ShouldBeNull();
        nftInfo5.Symbol.ShouldNotBeNull();
        nftInfo5.ListingPrice.ShouldBe(new decimal());
        nftInfo5.ListingToken.ShouldBeNull();
        nftInfo5.OtherOwnerListingFlag.ShouldBeFalse();

    }

    [Fact]
    public async Task Test1()
    {
    }

    [Fact]
    public async Task TestQueryOtherAddressNFTListingInfoByNFTIdsAsync()
    {
        await MockCreateNFT();
        var firstTime = DateTime.UtcNow;
        var secondTime = DateTime.UtcNow.AddMinutes(1);
        await Add1(firstTime);
        await Task.Delay(0);
        await Add2(secondTime);
        await Task.Delay(0);
        const string nftSymbol = "SYB-1";
        const string chainId = "tDVW";
        var address = Address.FromPublicKey("AAA".HexToByteArray()).ToBase58();
        var nftId = IdGenerateHelper.GetNFTInfoId(chainId, nftSymbol);
        
        var result2 = _listingInfoProvider.QueryOtherAddressNFTListingInfoByNFTIdsAsync(new List<string> { nftId },address,"");
        result2.ShouldNotBeNull();
        result2.Result.ShouldNotBeNull();
        result2.Result.Keys.ShouldNotBeNull();
        result2.Result.ShouldContainKey(nftId);
        result2.Result[nftId].Owner.ShouldNotBe(address);
        result2.Result[nftId].Prices.ShouldBe(new decimal(0.0000020));
    }

    private async Task Add1(DateTime startTime)
    {
        const string symbol = "SYB-1";
        const string tokenSymbol = "SYB-1";
        const long amount = 100;
        const long decimals = 8;
        const long quantity = 1;
        Hash whitelistId = HashHelper.ComputeFrom("test@gmail.com");

        var logEventContext = MockLogEventContext(100);
        var blockStateSetKey = await MockBlockState(logEventContext);
        
        var listedNftAdded = new ListedNFTAdded()
        {
            WhitelistId = whitelistId,
            Symbol = symbol,
            Quantity = quantity,
            Owner = Address.FromPublicKey("AAA".HexToByteArray()),
            Duration = new ListDuration()
            {
                DurationHours = 1,
                StartTime = Timestamp.FromDateTime(startTime),
                PublicTime = Timestamp.FromDateTime(DateTime.UtcNow),
            },
            Price = new Price()
            {
                Amount = amount,
                Symbol = tokenSymbol,
            }
        };
        var listedNftAddedLogEventProcessor = GetRequiredService<ListedNFTAddedLogEventProcessor>();
        var logEventInfo = MockLogEventInfo(listedNftAdded.ToLogEvent());
        await listedNftAddedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
    }
    private async Task HandleNFTIssueAsync_Test()
    {
        await HandleNFTCreatedAsync_Test();
        const string chainId = "tDVW";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 10;

        const string symbol = "SYB-1";
        const string tokenName = "READ Token";
        const int decimals = 8;
        const bool isBurnable = true;
        const int issueChainId = 9992731;

        var nftIssueLogEventProcessor = GetRequiredService<TokenIssueLogEventProcessor>();
        nftIssueLogEventProcessor.GetContractAddress(chainId);
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);

        var issued = new Issued()
        {
            Symbol = symbol,
            Amount = 1,
            To = Address.FromPublicKey("BBB".HexToByteArray())
        };

        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(issued.ToLogEvent());
        logEventInfo.BlockHeight = blockHeight;
        logEventInfo.ChainId = chainId;
        logEventInfo.BlockHash = blockHash;
        logEventInfo.TransactionId = transactionId;
        var logEventContext = new LogEventContext
        {
            ChainId = chainId,
            BlockHeight = blockHeight,
            BlockHash = blockHash,
            PreviousBlockHash = previousBlockHash,
            TransactionId = transactionId
        };

        await nftIssueLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);

        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
    }
    
    private async Task HandleNFTCreatedAsync_Test()
    {
        await HandleNFTCollectionAddedAsync_Test();
        const string chainId = "tDVW";

        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 5;

        const string symbol = "SYB-1";
        const string tokenName = "READ Token";
        const long totalSupply = 8;
        const int decimals = 8;
        const bool isBurnable = true;
        const int issueChainId = 9992731;

        var nftCreateLogEventProcessor = GetRequiredService<TokenCreatedLogEventProcessor>();
        nftCreateLogEventProcessor.GetContractAddress(chainId);
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);

        var tokenCreated = new TokenCreated()
        {
            Symbol = symbol,
            TokenName = tokenName,
            TotalSupply = totalSupply,
            Decimals = decimals,
            Issuer = Address.FromPublicKey("AAA".HexToByteArray()),
            IsBurnable = isBurnable,
            IssueChainId = issueChainId,
            ExternalInfo = new ExternalInfo(),
        };

        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(tokenCreated.ToLogEvent());
        logEventInfo.BlockHeight = blockHeight;
        logEventInfo.ChainId = chainId;
        logEventInfo.BlockHash = blockHash;
        logEventInfo.TransactionId = transactionId;
        var logEventContext = new LogEventContext
        {
            ChainId = chainId,
            BlockHeight = blockHeight,
            BlockHash = blockHash,
            PreviousBlockHash = previousBlockHash,
            TransactionId = transactionId
        };

        await nftCreateLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);

        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
    }
    
    private async Task HandleNFTCollectionAddedAsync_Test()
    {
        const string chainId = "tDVW";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 1;

        const string symbol = "SYB-0";
        const string tokenName = "READ Token";
        const long totalSupply = 0;
        const int decimals = 8;
        const bool isBurnable = true;
        const int issueChainId = 9992731;

        var nftCollectionAddedLogEventProcessor = GetRequiredService<TokenCreatedLogEventProcessor>();
        nftCollectionAddedLogEventProcessor.GetContractAddress(chainId);
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);

        var tokenCreated = new TokenCreated()
        {
            Symbol = symbol,
            TokenName = tokenName,
            TotalSupply = totalSupply,
            Decimals = decimals,
            Issuer = Address.FromPublicKey("AAA".HexToByteArray()),
            Owner = Address.FromPublicKey("AAA".HexToByteArray()),
            IsBurnable = isBurnable,
            IssueChainId = issueChainId,
            ExternalInfo = new ExternalInfo()
            {
                Value =
                {
                    { "__nft_file_hash", "88902e066e81ff0ca44b3867351acb6" },
                    { "__nft_feature_hash", "88902e066e81ff0ca44b3867351acb6" },
                    { "__nft_payment_tokens", "ELF" },
                    {"__nft_metadata", "[]" }
                }
            }
        };

        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(tokenCreated.ToLogEvent());
        logEventInfo.BlockHeight = blockHeight;
        logEventInfo.ChainId = chainId;
        logEventInfo.BlockHash = blockHash;
        logEventInfo.TransactionId = transactionId;
        var logEventContext = new LogEventContext
        {
            ChainId = chainId,
            BlockHeight = blockHeight,
            BlockHash = blockHash,
            PreviousBlockHash = previousBlockHash,
            TransactionId = transactionId
        };

        await nftCollectionAddedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);

        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
    }
    
    private async Task Add2(DateTime startTime)
    {
        const string symbol = "SYB-1";
        const string tokenSymbol = "SYB-1";
        const long amount = 200;
        const long decimals = 8;
        const long quantity = 1;
        Hash whitelistId = HashHelper.ComputeFrom("test@gmail.com");

        var logEventContext = MockLogEventContext(200,"tDVW","e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e22");
        var blockStateSetKey = await MockBlockState(logEventContext);
        
        var listedNftAdded = new ListedNFTAdded()
        {
            WhitelistId = whitelistId,
            Symbol = symbol,
            Quantity = quantity,
            Owner = Address.FromPublicKey("BBB".HexToByteArray()),
            Duration = new ListDuration()
            {
                DurationHours = 1,
                StartTime = Timestamp.FromDateTime(startTime),
                PublicTime = Timestamp.FromDateTime(DateTime.UtcNow),
            },
            Price = new Price()
            {
                Amount = amount,
                Symbol = tokenSymbol,
            }
        };
        
        var listedNftAddedLogEventProcessor = GetRequiredService<ListedNFTAddedLogEventProcessor>();
        var logEventInfo = MockLogEventInfo(listedNftAdded.ToLogEvent());
        await listedNftAddedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
    }

    public async Task Modify1(DateTime startTime)
    {
        const string symbol = "SYB-1";
        const string tokenSymbol = "SYB-1";
        const long amount = 500;
        const long decimals = 8;
        const long quantity = 2;
        Hash whitelistId = HashHelper.ComputeFrom("test@gmail.com");

        var logEventContext = MockLogEventContext(300);
        var blockStateSetKey = await MockBlockState(logEventContext);
        var listedNFTChanged = new ListedNFTChanged()
        {
            WhitelistId = whitelistId,
            Symbol = symbol,
            Quantity = quantity,
            Owner = Address.FromPublicKey("AAA".HexToByteArray()),
            Duration = new ListDuration()
            {
                DurationHours = 1,
                StartTime = Timestamp.FromDateTime(startTime),
                PublicTime = Timestamp.FromDateTime(DateTime.UtcNow),
            },
            Price = new Price()
            {
                Amount = amount,
                Symbol = tokenSymbol,
            }
        };
        var logEventInfo = MockLogEventInfo(listedNFTChanged.ToLogEvent());
        var listedNftChangedLogEventProcessor = GetRequiredService<ListedNFTChangedLogEventProcessor>();
        await listedNftChangedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);

    }

    private async Task Remove1(string address,DateTime startTime,string chainId,string transactionId)
    {
        const string symbol = "SYB-1";

        var logEventContext = MockLogEventContext(600,chainId,transactionId);
        var blockStateSetKey = await MockBlockState(logEventContext);

        var listedNftRemoved = new ListedNFTRemoved
        {
            Symbol = symbol,
            Owner = Address.FromPublicKey(address.HexToByteArray()),
            Price = new Price
            {
                Amount = 0,
                Symbol = symbol,
                //TokenId = 1
            },
            Duration = new ListDuration
            {
                DurationHours = 1,
                StartTime = Timestamp.FromDateTime(startTime),
                PublicTime = Timestamp.FromDateTime(DateTime.UtcNow),
            }
        };
        
        var listedNftRemovedLogEventProcessor = GetRequiredService<ListedNFTRemovedLogEventProcessor>();
        
        var logEventInfo = MockLogEventInfo(listedNftRemoved.ToLogEvent());
        await listedNftRemovedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
    }
    
    public async Task<NFTListingInfoIndex> HandleListedNftAddedAsync(string symbol, long amount, string ownerPublicKey, long durationHours, DateTime dateTime)
    {
        await MockCreateNFT();
        string tokenSymbol = symbol;
        const long decimals = 8;
        const long quantity = 1;
        Hash whitelistId = HashHelper.ComputeFrom("test@gmail.com");

        var logEventContext = MockLogEventContext(100);
        var blockStateSetKey = await MockBlockState(logEventContext);
        
        var listedNftAdded = new ListedNFTAdded()
        {
            WhitelistId = whitelistId,
            Symbol = symbol,
            Quantity = quantity,
            Owner = Address.FromPublicKey(ownerPublicKey.HexToByteArray()),
            Duration = new ListDuration()
            {
                DurationHours = durationHours,
                StartTime = Timestamp.FromDateTime(dateTime),
                PublicTime = Timestamp.FromDateTime(DateTime.UtcNow),
            },
            Price = new Price()
            {
                Amount = amount,
                Symbol = tokenSymbol,
            }
        };
        var listedNftIndexId = IdGenerateHelper.GetId(logEventContext.ChainId, listedNftAdded.Symbol, listedNftAdded.Owner.ToBase58(),
            listedNftAdded.Duration.StartTime.Seconds);
        var listedNftAddedLogEventProcessor = GetRequiredService<ListedNFTAddedLogEventProcessor>();
        listedNftAddedLogEventProcessor.GetContractAddress(logEventContext.ChainId);

        listedNftAdded.Price.Symbol = "NOTFOUND";
        var logEventInfo = MockLogEventInfo(listedNftAdded.ToLogEvent());
        await listedNftAddedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        var nftListingIndexData = await _NFTListingIndexRepository.GetFromBlockStateSetAsync(listedNftIndexId, logEventContext.ChainId);
        nftListingIndexData.ShouldBeNull();
        
        listedNftAdded.Price.Symbol = symbol;
        logEventInfo = MockLogEventInfo(listedNftAdded.ToLogEvent());
        await listedNftAddedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        nftListingIndexData = await _NFTListingIndexRepository.GetFromBlockStateSetAsync(listedNftIndexId, logEventContext.ChainId);
        nftListingIndexData.ShouldNotBe(null);
        
        nftListingIndexData.BlockHeight.ShouldBe(logEventContext.BlockHeight);
        nftListingIndexData.Symbol.ShouldBe(listedNftAdded.Symbol);
        nftListingIndexData.Prices.ShouldBe(listedNftAdded.Price.Amount / (decimal)Math.Pow(10, decimals));
        nftListingIndexData.RealQuantity.ShouldBe(nftListingIndexData.Quantity);
        return nftListingIndexData;
    }
    
    
    [Fact]
    public async Task HandleMinListedNFTAsync_Test()
    {
        const string symbol = "SYB-1";
        const long amount = 100000000; // price
        const long durationHours = 1;
        const string ownerPublicKey = "AAA";
        var nftId = IdGenerateHelper.GetNFTInfoId(ChainId, symbol);

        var listedNftAdded0 = await HandleListedNftAddedAsync(symbol, amount, ownerPublicKey, durationHours, DateTime.UtcNow);
        var nftInfo0 = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftId, ChainId);
        
        nftInfo0.MinListingId.ShouldBe(listedNftAdded0.Id);
        Assert.Equal(1m, nftInfo0.MinListingPrice);
        
        const long amount2 = 99000000; // price
        const long durationHours2 = 2;
        const string ownerPublicKey2 = "BBB";
        
        var listedNftAdded1= await HandleListedNftAddedAsync(symbol, amount2, ownerPublicKey2, durationHours2, DateTime.UtcNow);
        var nftInfo2 = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftId, ChainId);

        nftInfo2.MinListingId.ShouldBe(listedNftAdded1.Id);
        Assert.Equal(0.99m, nftInfo2.MinListingPrice);
    }
    
    [Fact]
    public async Task HandleMinListedSeedAsync_Test()
    {
        const string symbol = "SYB-1";
        const long amount = 100000000; // price
        const long durationHours = 1;
        const string ownerPublicKey = "AAA";
        var nftId = IdGenerateHelper.GetNFTInfoId(ChainId, symbol);
        
        //listing one
        var listedNftAdded0 = await HandleListedNftAddedAsync(symbol, amount, ownerPublicKey, durationHours, DateTime.UtcNow);

        var nftInfo0 = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftId, ChainId);
        
        nftInfo0.MinListingId.ShouldBe(listedNftAdded0.Id);
        Assert.Equal(1m, nftInfo0.MinListingPrice);
        
        const long amount2 = 99000000; // price
        const long durationHours2 = 2;
        const string ownerPublicKey2 = "BBB";

        //listing two
        var listedNftAdded1= await HandleListedNftAddedAsync(symbol, amount2, ownerPublicKey2, durationHours2, DateTime.UtcNow);
        var nftInfo2 = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftId, ChainId);

        nftInfo2.MinListingId.ShouldBe(listedNftAdded1.Id);
        Assert.Equal(0.99m, nftInfo2.MinListingPrice);
    }
    
}