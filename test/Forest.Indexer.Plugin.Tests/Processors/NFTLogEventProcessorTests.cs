using AElf;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ProxyAccountContract;
using AElf.CSharp.Core.Extension;
using AElf.Standards.ACS0;
using AElf.Types;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.GraphQL;
using Forest.Indexer.Plugin.Processors;
using Forest.Indexer.Plugin.Tests.Helper;
using Forest.SymbolRegistrar;
using Google.Protobuf.WellKnownTypes;
using Nest;
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;
using Timestamp = Google.Protobuf.WellKnownTypes.Timestamp;

namespace Forest.Indexer.Plugin.Tests.Processors;

public class NFTLogEventProcessorTests : ForestIndexerPluginTestBase
{
    private const long TimeSeconds = 4100244981;
    
    private readonly IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> _nftInfoIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<CollectionIndex, LogEventInfo> _nftCollectionIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<WhitelistIndex, LogEventInfo> _whitelistIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> _userBalanceIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<OfferInfoIndex, LogEventInfo> _nftOfferIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo> _nftActivityIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> _seedSymbolIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<ProxyAccountIndex, LogEventInfo> _proxyAccountIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo> _seedSymbolRepo;
    private readonly IAElfIndexerClientEntityRepository<CollectionChangeIndex, LogEventInfo> _nftCollectionChangeIndexRepository;

    // private readonly ILogger<NFTOfferIndex> _logger;
    private readonly IObjectMapper _objectMapper;

    public NFTLogEventProcessorTests()
    {
        _nftInfoIndexRepository = GetRequiredService<IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo>>();
        _nftCollectionIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<CollectionIndex, LogEventInfo>>();
        _nftOfferIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<OfferInfoIndex, LogEventInfo>>();
        _nftActivityIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<NFTActivityIndex, LogEventInfo>>();
        _seedSymbolIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo>>();
        _whitelistIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<WhitelistIndex, LogEventInfo>>();
        _userBalanceIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo>>();
        _proxyAccountIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<ProxyAccountIndex, LogEventInfo>>();
        _seedSymbolRepo = GetRequiredService<IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo>>();
        _objectMapper = GetRequiredService<IObjectMapper>();
        _nftCollectionChangeIndexRepository = GetRequiredService<IAElfIndexerClientEntityRepository<CollectionChangeIndex, LogEventInfo>>();
    }

    [Fact]
    public async Task TestTem()
    {
        
    }
    
    [Fact]
    public async Task HandleSeedAddedAsync_Success()
    {
        
        const string chainId = "AELF";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;

        const string symbol = "SEED-1";
        const string seedOwnedSymbol = "TEST_NFT_COLLECTION_SYMBOLE-0";
        //const string seedOwnedSymbol = "TESTAASELE";
        const string tokenName = "READ Token";
        const long totalSupply = 0;
        const int decimals = 8;
        const bool isBurnable = true;
        const int issueChainId = 9992731;

        await SeedAdd(SeedType.Regular, symbol, seedOwnedSymbol, chainId);

        var seedSymbolCreatedLogEventProcessor = GetRequiredService<TokenCreatedLogEventProcessor>();
        seedSymbolCreatedLogEventProcessor.GetContractAddress(chainId);
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
            ExternalInfo = new ExternalInfo()
            {
                Value =
                {
                    { "__seed_owned_symbol", seedOwnedSymbol },
                    {
                        "__seed_exp_time",
                        new DateTimeOffset(DateTime.UtcNow.AddHours(1)).ToUnixTimeSeconds().ToString()
                    }
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
            TransactionId = transactionId,
            BlockTime = DateTime.Now
        };

        await seedSymbolCreatedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);

        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(0);

        var tsmSeedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, seedOwnedSymbol);
        var tsmSeedSymbolIndex = await _tsmSeedSymbolIndexRepository.GetFromBlockStateSetAsync(tsmSeedSymbolId, chainId);
        tsmSeedSymbolIndex.Id.ShouldBe(tsmSeedSymbolId);
        tsmSeedSymbolIndex.Symbol.ShouldBe(seedOwnedSymbol);
        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, symbol);
        var seedSymbolIndex = await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolId, chainId);
        seedSymbolIndex.Id.ShouldBe(seedSymbolId);
        seedSymbolIndex.Symbol.ShouldBe(symbol);
    }

    [Fact]
    public async Task HandleSeedAddedAsync2_Success()
    {
        const string chainId = "AELF";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;

        const string symbol = "SEED-1";
        const string tokenName = "READ Token";
        const long totalSupply = 0;
        const int decimals = 8;
        const bool isBurnable = true;
        const int issueChainId = 9992731;
        const string seedOwnedSymbol = "TEST_NFT_COLLECTION_SYMBOLE-0";

        
        await SeedAdd(SeedType.Regular, symbol, seedOwnedSymbol, chainId);
        
        var seedSymbolCreatedLogEventProcessor = GetRequiredService<TokenCreatedLogEventProcessor>();
        seedSymbolCreatedLogEventProcessor.GetContractAddress(chainId);
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
                    { "__seed_owned_symbol", seedOwnedSymbol },
                    {
                        "__seed_exp_time",
                        new DateTimeOffset(DateTime.UtcNow.AddHours(1)).ToUnixTimeSeconds().ToString()
                    }
                }
            }
        };

        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(tokenCreated.ToLogEvent());
        logEventInfo.BlockHeight = blockHeight;
        logEventInfo.ChainId = chainId;
        logEventInfo.BlockHash = blockHash;
        logEventInfo.TransactionId = transactionId;
        logEventInfo.BlockTime = DateTime.Now;
        var logEventContext = new LogEventContext
        {
            ChainId = chainId,
            BlockHeight = blockHeight,
            BlockHash = blockHash,
            PreviousBlockHash = previousBlockHash,
            TransactionId = transactionId,
            BlockTime = DateTime.Now
        };

        await seedSymbolCreatedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);

        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(0);

        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, symbol);
        var seedSymbolIndex = await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolId, chainId);
        seedSymbolIndex.Id.ShouldBe(seedSymbolId);
        seedSymbolIndex.Symbol.ShouldBe("SEED-1");
    }


    [Fact]
    public async Task HandleSeedAddedAsync_False()
    {
        const string chainId = "tDWV";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;

        const string symbol = "SEED-1";
        const string tokenName = "READ Token";
        const long totalSupply = 0;
        const int decimals = 8;
        const bool isBurnable = true;
        const int issueChainId = 9992731;

        var seedSymbolCreatedLogEventProcessor = GetRequiredService<TokenCreatedLogEventProcessor>();
        seedSymbolCreatedLogEventProcessor.GetContractAddress(chainId);
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
            ExternalInfo = new ExternalInfo()
            {
                Value =
                {
                    { "__seed_owned_symbol", "TEST_NFT_COLLECTION_SYMBOLE-0" },
                    {
                        "__seed_exp_time",
                        new DateTimeOffset(DateTime.UtcNow.AddHours(1)).ToUnixTimeSeconds().ToString()
                    }
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

        await seedSymbolCreatedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);

        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(0);

        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, symbol);
        var nftCollectionIndex = _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolId, chainId);
        nftCollectionIndex.Result.ShouldBeNull();
    }

    [Fact]
    public async Task HandleNFTCollectionAddedAsync_Test()
    {
        const string chainId = "tDVW";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;

        const string symbol = "READ-0";
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
                    { "__nft_metadata", "[]" }
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
        await Task.Delay(0);

        var nftCollectionIndexId = IdGenerateHelper.GetNFTCollectionId(chainId, symbol);
        var nftCollectionIndex =
            await _nftCollectionIndexRepository.GetFromBlockStateSetAsync(nftCollectionIndexId, chainId);
        nftCollectionIndex.Id.ShouldBe(nftCollectionIndexId);
        var nftCollectionChange =
            await _nftCollectionChangeIndexRepository.GetFromBlockStateSetAsync(nftCollectionIndexId, chainId);
        nftCollectionChange.Id.ShouldBe(nftCollectionIndexId);
        var tokenInfoIndexData =
            await _nftCollectionIndexRepository.GetFromBlockStateSetAsync(nftCollectionIndexId, chainId);
        tokenInfoIndexData.BlockHeight.ShouldBe(blockHeight);
        tokenInfoIndexData.Decimals.ShouldBe(decimals);
        tokenInfoIndexData.Symbol.ShouldBe(symbol);
        tokenInfoIndexData.TokenName.ShouldBe(tokenName);
        tokenInfoIndexData.TotalSupply.ShouldBe(totalSupply);
        tokenInfoIndexData.Decimals.ShouldBe(decimals);
        tokenInfoIndexData.IsBurnable.ShouldBe(isBurnable);
        tokenInfoIndexData.IssueChainId.ShouldBe(issueChainId);
    }

    [Fact]
    public async Task HandleNFTCollectionAddedAsync2_Test()
    {
        const string chainId = "tDVW";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;

        const string symbol = "READ-0";
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
            IsBurnable = isBurnable,
            IssueChainId = issueChainId,
            ExternalInfo = new ExternalInfo()
            {
                Value =
                {
                    { "__nft_file_hash", "88902e066e81ff0ca44b3867351acb6" },
                    { "__nft_feature_hash", "88902e066e81ff0ca44b3867351acb6" },
                    { "__nft_payment_tokens", "ELF" },
                    { "__nft_metadata", "[]" }
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
        await Task.Delay(0);

        var nftCollectionIndexId = IdGenerateHelper.GetNFTCollectionId(chainId, symbol);
        var nftCollectionIndex =
            await _nftCollectionIndexRepository.GetFromBlockStateSetAsync(nftCollectionIndexId, chainId);
        nftCollectionIndex.Id.ShouldBe(nftCollectionIndexId);
        var tokenInfoIndexData =
            await _nftCollectionIndexRepository.GetFromBlockStateSetAsync(nftCollectionIndexId, chainId);
        tokenInfoIndexData.BlockHeight.ShouldBe(blockHeight);
        tokenInfoIndexData.Decimals.ShouldBe(decimals);
        tokenInfoIndexData.Symbol.ShouldBe(symbol);
        tokenInfoIndexData.TokenName.ShouldBe(tokenName);
        tokenInfoIndexData.TotalSupply.ShouldBe(totalSupply);
        tokenInfoIndexData.Decimals.ShouldBe(decimals);
        tokenInfoIndexData.IsBurnable.ShouldBe(isBurnable);
        tokenInfoIndexData.IssueChainId.ShouldBe(issueChainId);
    }

    [Fact]
    public async Task HandleNFTCollectionAddedAsync_ShouldBeNull()
    {
        const string chainId = "AELF";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;

        const string symbol = "READTest-0";
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
            IsBurnable = isBurnable,
            IssueChainId = issueChainId,
            ExternalInfo = new ExternalInfo()
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
        await Task.Delay(0);

        var nftCollectionIndexId = IdGenerateHelper.GetNFTCollectionId(chainId, symbol);
        var nftCollectionIndex = _nftCollectionIndexRepository.GetFromBlockStateSetAsync(nftCollectionIndexId, chainId);
        nftCollectionIndex.Result.ShouldBeNull();
    }


    [Fact]
    public async Task HandleNFTCreatedAsync_Test()
    {
        await HandleNFTCollectionAddedAsync_Test();
        const string chainId = "tDVW";

        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;

        const string symbol = "READ-1";
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
        await Task.Delay(0);

        var nftInfoId = IdGenerateHelper.GetNFTInfoId(chainId, symbol);
        var tokenInfoIndexData =
            await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftInfoId, chainId);
        tokenInfoIndexData.BlockHeight.ShouldBe(blockHeight);
        tokenInfoIndexData.Decimals.ShouldBe(decimals);
        tokenInfoIndexData.Symbol.ShouldBe(symbol);
        tokenInfoIndexData.TokenName.ShouldBe(tokenName);
        tokenInfoIndexData.TotalSupply.ShouldBe(totalSupply);
        tokenInfoIndexData.Decimals.ShouldBe(decimals);
        tokenInfoIndexData.IsBurnable.ShouldBe(isBurnable);
        tokenInfoIndexData.IssueChainId.ShouldBe(issueChainId);
    }

    [Fact]
    public async Task HandleNFTCreatedAsync2_Test()
    {
        await HandleNFTCollectionAddedAsync_Test();
        const string chainId = "tDVW";

        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;

        const string symbol = "READ-1";
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
            Owner = Address.FromPublicKey("AAA".HexToByteArray()),
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
        await Task.Delay(0);

        var nftInfoId = IdGenerateHelper.GetNFTInfoId(chainId, symbol);
        var tokenInfoIndexData =
            await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftInfoId, chainId);
        tokenInfoIndexData.BlockHeight.ShouldBe(blockHeight);
        tokenInfoIndexData.Decimals.ShouldBe(decimals);
        tokenInfoIndexData.Symbol.ShouldBe(symbol);
        tokenInfoIndexData.TokenName.ShouldBe(tokenName);
        tokenInfoIndexData.TotalSupply.ShouldBe(totalSupply);
        tokenInfoIndexData.Decimals.ShouldBe(decimals);
        tokenInfoIndexData.IsBurnable.ShouldBe(isBurnable);
        tokenInfoIndexData.IssueChainId.ShouldBe(issueChainId);
    }

    [Fact]
    public async Task HandleNFTCreatedAsync_ShouldBeNull()
    {
        await HandleNFTCollectionAddedAsync_Test();
        const string chainId = "AELF";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;

        const string symbol = "READ-1";
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
        await Task.Delay(0);

        var nftInfoId = IdGenerateHelper.GetNFTCollectionId(chainId, symbol);
        var nftInfoIndex =
            await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftInfoId, chainId);
        nftInfoIndex.ShouldBeNull();
    }


    [Fact]
    public async Task HandleNFTIssueAsync_Test()
    {
        await HandleNFTCreatedAsync_Test();
        const string chainId = "tDVW";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;

        const string symbol = "READ-1";
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
        await Task.Delay(0);

        var nftIndexId = IdGenerateHelper.GetId(chainId, symbol);
        var tokenInfoIndexData = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftIndexId, chainId);
        tokenInfoIndexData.BlockHeight.ShouldBe(blockHeight);
        tokenInfoIndexData.Decimals.ShouldBe(decimals);
        tokenInfoIndexData.Symbol.ShouldBe(symbol);
        tokenInfoIndexData.TokenName.ShouldBe(tokenName);
        tokenInfoIndexData.Decimals.ShouldBe(decimals);
        tokenInfoIndexData.Issued.ShouldBe(1);
        tokenInfoIndexData.Supply.ShouldBe(1);
        tokenInfoIndexData.IsBurnable.ShouldBe(isBurnable);
        tokenInfoIndexData.IssueChainId.ShouldBe(issueChainId);
    }

    [Fact]
    public async Task HandleSeedIssueAsync_Test()
    {
        await HandleSeedAddedAsync_Success();
        const string chainId = "AELF";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;

        const string symbol = "SEED-1";
        const string tokenName = "READ Token";
        const int decimals = 8;
        const bool isBurnable = true;
        const int issueChainId = 9992731;

        var seedSymbolIssueLogEventProcessor = GetRequiredService<TokenIssueLogEventProcessor>();
        seedSymbolIssueLogEventProcessor.GetContractAddress(chainId);
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
            To = Address.FromPublicKey("AAA".HexToByteArray()),
            Memo = "DESC"
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

        await seedSymbolIssueLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);

        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(0);

        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, symbol);
        var seedSymbolInfoIndexData = await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolId, chainId);
        seedSymbolInfoIndexData.IssuerTo.ShouldBe(Address.FromPublicKey("AAA".HexToByteArray()).ToBase58());
        seedSymbolInfoIndexData.SeedOwnedSymbol.ShouldBe("TEST_NFT_COLLECTION_SYMBOLE-0");
    }


    [Fact]
    public async Task HandleNFTBurnedAsync_Test()
    {
        await HandleNFTIssueAsync_Test();
        const string chainId = "tDVW";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;

        const string symbol = "READ-1";
        const string tokenName = "READ Token";
        const int decimals = 8;
        const bool isBurnable = true;
        const int issueChainId = 9992731;

        var nftBurnedLogEventProcessor = GetRequiredService<TokenBurnedLogEventProcessor>();
        nftBurnedLogEventProcessor.GetContractAddress(chainId);

        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);

        var burned = new Burned()
        {
            Symbol = symbol,
            Amount = 1,
            Burner = Address.FromPublicKey("AAA".HexToByteArray())
        };

        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(burned.ToLogEvent());
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

        await nftBurnedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);

        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(0);

        var nftIndexId = IdGenerateHelper.GetId(chainId, symbol);
        var tokenInfoIndexData = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftIndexId, chainId);
        tokenInfoIndexData.BlockHeight.ShouldBe(blockHeight);
        tokenInfoIndexData.Decimals.ShouldBe(decimals);
        tokenInfoIndexData.Symbol.ShouldBe(symbol);
        tokenInfoIndexData.TokenName.ShouldBe(tokenName);
        tokenInfoIndexData.Decimals.ShouldBe(decimals);
        tokenInfoIndexData.Supply.ShouldBe(0);
        tokenInfoIndexData.IsBurnable.ShouldBe(isBurnable);
        tokenInfoIndexData.IssueChainId.ShouldBe(issueChainId);
    }

    [Fact]
    public async Task HandleSeedBurnedAsync_Test()
    {
        await HandleSeedIssueAsync_Test();
        const string chainId = "AELF";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;

        const string symbol = "SEED-1";
        const string tokenName = "READ Token";
        const int decimals = 8;
        const bool isBurnable = true;
        const int issueChainId = 9992731;

        var nftBurnedLogEventProcessor = GetRequiredService<TokenBurnedLogEventProcessor>();
        nftBurnedLogEventProcessor.GetContractAddress(chainId);
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);

        var burned = new Burned()
        {
            Symbol = symbol,
            Amount = 1,
            Burner = Address.FromPublicKey("AAA".HexToByteArray())
        };

        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(burned.ToLogEvent());
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

        await nftBurnedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);

        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(0);

        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, symbol);
        var seedSymbolIndex = await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolId, chainId);
        seedSymbolIndex.IsDeleteFlag.ShouldBeTrue();
    }

    [Fact]
    public async Task Test()
    {
        var a =new DateTime(2099, 11, 10).AddDays(1).ToUniversalTime().ToTimestamp();
        Task.Delay(1000);
        var b =new DateTime(2099, 11, 10).AddDays(1).ToUniversalTime().ToTimestamp();
        Assert.Equal(a,b);
        Assert.Equal(a.Seconds,b.Seconds);
        
    }

    [Fact]
    public async Task HandleOfferAddedLogEventAsync_Test()
    {
        await HandleNFTIssueAsync_Test();
        await CreateTokenInfo();
        await MockCreateNFT();

        const string chainId = "tDVW";
        const string symbol = "SYB-1";
        const string blockHash = "1d29110ef8085744e8bd4ca4ddca9070036d07f4705b79c549b07115ea1f145b";
        const string previousBlockHash = "4d2986852e78f9d84a1f856ffd0d66264edc91e767d463bf08304f91fedb3d9f";
        const string transactionId = "7a4c16a8aa4bb415b1128d060bb3e356ca7bab9ff77be5838a0ce5c4f5b1fe19";
        const long blockHeight = 111;

        //step1: create blockStateSet
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);

        //step2: create logEventInfo
        var offerAdded = new OfferAdded()
        {
            Symbol = "SYB-1",
            ExpireTime = new DateTime(2099, 11, 10).AddDays(1).ToUniversalTime().ToTimestamp(),
            OfferFrom = Address.FromPublicKey("AAA".HexToByteArray()),
            OfferTo = Address.FromPublicKey("BBB".HexToByteArray()),
            Price = new Price()
            {
                Amount = 500,
                Symbol = "SYB"
            },
            Quantity = 1000
        };
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(offerAdded.ToLogEvent());
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

        //step3: handle event and write result to blockStateSet
        var offerAddedLogEventProcessor = GetRequiredService<OfferAddedLogEventProcessor>();
        await offerAddedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        offerAddedLogEventProcessor.GetContractAddress(chainId);

        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);

        //step5: check result
        string id =
            IdGenerateHelper.GetOfferId(chainId, offerAdded.Symbol, offerAdded.OfferFrom.ToBase58(),
                offerAdded.OfferTo.ToBase58(), offerAdded.ExpireTime.Seconds, offerAdded.Price.Amount);
        var offerIndexData = await _nftOfferIndexRepository.GetFromBlockStateSetAsync(id, chainId);
        
        offerIndexData.ShouldNotBeNull();
        offerIndexData.BizSymbol.ShouldBe(offerAdded.Symbol);
        offerIndexData.Quantity.ShouldBe(offerAdded.Quantity);
        offerIndexData.PurchaseToken.Symbol.ShouldBe(offerAdded.Price.Symbol);
        
        //check MaxOfferPrice info
        var nftId = IdGenerateHelper.GetNFTInfoId(chainId, symbol);
        var nftInfo = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftId, chainId);
        nftInfo.MaxOfferId.ShouldBe(offerIndexData.Id);
        Assert.Equal(0.000005m, nftInfo.MaxOfferPrice);
    }

    [Fact]
    public async Task HandleOfferChangedLogEventAsync_Test1()
    {
    }

    [Fact]
    public async Task HandleOfferChangedLogEventAsync_Test()
    {
        await HandleOfferAddedLogEventAsync_Test();

        await CreateTokenInfo();
        await MockCreateNFT();

        const string chainId = "tDVW";
        const string symbol = "SYB-1";
        
        //step1: create blockStateSet
        const string blockHash = "3c7c267341e9f097b0886c8a1661bef73d6bb4c30464ad73be714fdf22b09bdd";
        const string previousBlockHash = "9a6ef475e4c4b6f15c37559033bcfdbed34ca666c67b2ae6be22751a3ae171de";
        const string transactionId = "c09b8c142dd5e07acbc1028e5f59adca5b5be93a0680eb3609b773044a852c43";
        const long blockHeight = 200;
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);

        //step2: create logEventInfo
        var offerChanged = new OfferChanged()
        {
            Symbol = symbol,
            ExpireTime = new DateTime(2099, 11, 10).AddDays(1).ToUniversalTime().ToTimestamp(),//ToTimestamp(TimeSeconds).ToUniversalTime().ToTimestamp(),
            OfferFrom = Address.FromPublicKey("AAA".HexToByteArray()),
            OfferTo = Address.FromPublicKey("BBB".HexToByteArray()),
            Price = new Price()
            {
                Amount = 500,
                Symbol = "SYB"
            },
            Quantity = 500
        };
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(offerChanged.ToLogEvent());
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
        var nftInfoId = IdGenerateHelper.GetNFTInfoId(chainId, offerChanged.Price.Symbol);
        var userBalanceFromId =
            IdGenerateHelper.GetUserBalanceId(offerChanged.OfferFrom.ToBase58(), chainId, nftInfoId);
        UserBalanceIndex userBalanceIndex = new UserBalanceIndex
        {
            ChainId = chainId,
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            PreviousBlockHash = previousBlockHash,
            Id = userBalanceFromId,
            Address = offerChanged.OfferFrom.ToBase58(),
            Amount = 3000,
            NFTInfoId = nftInfoId
        };
        await _userBalanceIndexRepository.AddOrUpdateAsync(userBalanceIndex);
        //step3: handle event and write result to blockStateSet
        var offerChangedLogEventProcessor = GetRequiredService<OfferChangedLogEventProcessor>();
        await offerChangedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        offerChangedLogEventProcessor.GetContractAddress(chainId);

        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(1000);

        //step5: check result
        var offerIndexData = await _nftOfferIndexRepository.GetAsync(
            IdGenerateHelper.GetOfferId(chainId, offerChanged.Symbol, offerChanged.OfferFrom.ToBase58(),
                offerChanged.OfferTo.ToBase58(), offerChanged.ExpireTime.Seconds, offerChanged.Price.Amount));
        
        offerIndexData.ShouldNotBeNull();
        offerIndexData.BizSymbol.ShouldBe(offerChanged.Symbol);
        offerIndexData.Quantity.ShouldBe(offerChanged.Quantity);
        offerIndexData.RealQuantity.ShouldBeGreaterThan(0);
        offerIndexData.PurchaseToken.Symbol.ShouldBe(offerChanged.Price.Symbol);
        
        //check MaxOfferPrice info
        var nftId = IdGenerateHelper.GetNFTInfoId(chainId, symbol);
        var nftInfo = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftId, chainId);
        nftInfo.MaxOfferId.ShouldBe(offerIndexData.Id);
        Assert.Equal(0.000005m, nftInfo.MaxOfferPrice);
    }

    [Fact]
    public async Task HandleOfferRealQualityByTransferProcessorAsync_Test()
    {
        await HandleOfferAddedLogEventAsync_Test();
        const string chainId = "tDVW";
        const string blockHash = "3c7c267341e9f097b0886c8a1661bef73d6bb4c30464ad73be714fdf22b09bdd";
        const string previousBlockHash = "9a6ef475e4c4b6f15c37559033bcfdbed34ca666c67b2ae6be22751a3ae171de";
        const string transactionId = "c09b8c142dd5e07acbc1028e5f59adca5b5be93a0680eb3609b773044a852c43";
        const long blockHeight = 200;
        const string Symbol = "SYB";
        var from = Address.FromPublicKey("AAA".HexToByteArray()).ToBase58();
        var to = Address.FromPublicKey("BBB".HexToByteArray()).ToBase58();
        var transferProcessor = GetRequiredService<TokenTransferProcessor>();
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        //step1: create blockStateSet
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);
        //step2: create nftInfo userBalance logEventInfo
        var nftInfoId = IdGenerateHelper.GetNFTInfoId(chainId, Symbol);
        var nftInfoIndex = new NFTInfoIndex()
        {
            Id = nftInfoId,
            Symbol = Symbol,
            ChainId = chainId
        };
        nftInfoIndex.BlockHeight = blockHeight;
        nftInfoIndex.ChainId = chainId;
        nftInfoIndex.BlockHash = blockHash;
        nftInfoIndex.PreviousBlockHash = previousBlockHash;
        await _nftInfoIndexRepository.AddOrUpdateAsync(nftInfoIndex);

        var userBalanceFromId =
            IdGenerateHelper.GetUserBalanceId(from, chainId, nftInfoId);
        UserBalanceIndex userBalanceIndex = new UserBalanceIndex
        {
            ChainId = chainId,
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            PreviousBlockHash = previousBlockHash,
            Id = userBalanceFromId,
            Address = from,
            Amount = 100,
            NFTInfoId = nftInfoId
        };
        await _userBalanceIndexRepository.AddOrUpdateAsync(userBalanceIndex);
        Transferred transferred = new Transferred()
        {
            Symbol = "ELF",
            Amount = 10,
            From = Address.FromBase58(from),
            To = Address.FromBase58(to)
        };
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(transferred.ToLogEvent());
        logEventInfo.BlockHeight = blockHeight;
        logEventInfo.ChainId = chainId;
        logEventInfo.BlockHash = blockHash;
        logEventInfo.PreviousBlockHash = previousBlockHash;
        logEventInfo.TransactionId = transactionId;
        var logEventContext = new LogEventContext
        {
            ChainId = chainId,
            BlockHeight = blockHeight,
            BlockHash = blockHash,
            PreviousBlockHash = previousBlockHash,
            TransactionId = transactionId,
            BlockTime = DateTime.Now
        };

        //step3: handle event and write result to blockStateSet
        await transferProcessor.HandleEventAsync(logEventInfo, logEventContext);

        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(2000);

        //step5: check result
        var mustQuery = new List<Func<QueryContainerDescriptor<OfferInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.TermRange(i
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime))
                .GreaterThan(DateTime.UtcNow.ToString("O"))));
        mustQuery.Add(q => q.Term(i => i.Field(index => index.PurchaseToken.Symbol).Value(Symbol)));
        mustQuery.Add(q => q.Term(i => i.Field(index => index.ChainId).Value(chainId)));
        mustQuery.Add(q => q.Term(i => i.Field(index => index.OfferFrom).Value(from)));
        QueryContainer Filter(QueryContainerDescriptor<OfferInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        var result = await _nftOfferIndexRepository.GetListAsync(Filter, null, sortExp: k => k.Price,
            sortType: SortOrder.Descending);
        result.Item2.Count.ShouldBeGreaterThan(0);
        result.Item2[0].Quantity.ShouldBe(result.Item2[0].RealQuantity);
    }

    [Fact]
    public async Task HandleOfferCanceledLogEventAsync_Test()
    {
        await HandleOfferAddedLogEventAsync_Test();

        //step1: create blockStateSet
        const string chainId = "tDVW";
        const string blockHash = "3c7c267341e9f097b0886c8a1661bef73d6bb4c30464ad73be714fdf22b09bdd";
        const string previousBlockHash = "9a6ef475e4c4b6f15c37559033bcfdbed34ca666c67b2ae6be22751a3ae171de";
        const string transactionId = "c09b8c142dd5e07acbc1028e5f59adca5b5be93a0680eb3609b773044a852c43";
        const long blockHeight = 200;
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);

        //step2: create logEventInfo
        var offerCanceled = new OfferCanceled()
        {
            Symbol = "SYB-1",
            OfferFrom = Address.FromPublicKey("AAA".HexToByteArray()),
            OfferTo = Address.FromPublicKey("BBB".HexToByteArray()),
            IndexList = new Int32List()
        };
        offerCanceled.IndexList.Value.Add(0);
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(offerCanceled.ToLogEvent());
        logEventInfo.BlockHeight = blockHeight;
        logEventInfo.ChainId = chainId;
        logEventInfo.BlockHash = blockHash;
        logEventInfo.TransactionId = transactionId;
        var logEventContext = new LogEventContext
        {
            ChainId = chainId,
            BlockHeight = blockHeight,
            BlockHash = blockHash,
            BlockTime = DateTime.Now.ToUniversalTime(),
            PreviousBlockHash = previousBlockHash,
            TransactionId = transactionId
        };

        //step3: handle event and write result to blockStateSet
        var offerCanceledLogEventProcessor = GetRequiredService<OfferCanceledLogEventProcessor>();
        await offerCanceledLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        offerCanceledLogEventProcessor.GetContractAddress(chainId);

        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(1000);

        //step5: check result
        var activityIndexData = await _nftActivityIndexRepository.GetAsync(IdGenerateHelper.GetId(chainId,
            offerCanceled.Symbol, offerCanceled.OfferFrom.ToBase58(),
            null, transactionId));
        activityIndexData.ShouldNotBeNull();
        activityIndexData.TransactionHash.ShouldBe(transactionId);
        activityIndexData.Type.ShouldBe(NFTActivityType.CancelOffer);
    }

    [Fact]
    public async Task HandleOfferRemovedLogEventAsync_Test()
    {
        await HandleOfferAddedLogEventAsync_Test();
        
        const string symbol = "SYB-1";

        //step1: create blockStateSet
        const string chainId = "tDVW";
        const string blockHash = "3c7c267341e9f097b0886c8a1661bef73d6bb4c30464ad73be714fdf22b09bdd";
        const string previousBlockHash = "9a6ef475e4c4b6f15c37559033bcfdbed34ca666c67b2ae6be22751a3ae171de";
        const string transactionId = "c09b8c142dd5e07acbc1028e5f59adca5b5be93a0680eb3609b773044a852c43";
        const long blockHeight = 200;
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);

        //step2: create logEventInfo
        var offerRemoved = new OfferRemoved()
        {

            Symbol = "SYB-1",
            ExpireTime = new DateTime(2099, 11, 10).AddDays(1).ToUniversalTime().ToTimestamp(),
            OfferFrom = Address.FromPublicKey("AAA".HexToByteArray()),
            OfferTo = Address.FromPublicKey("BBB".HexToByteArray())
        };
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(offerRemoved.ToLogEvent());
        logEventInfo.BlockHeight = blockHeight;
        logEventInfo.ChainId = chainId;
        logEventInfo.BlockHash = blockHash;
        logEventInfo.TransactionId = transactionId;
        var logEventContext = new LogEventContext
        {
            ChainId = chainId,
            BlockHeight = blockHeight,
            BlockHash = blockHash,
            BlockTime = DateTime.Now.ToUniversalTime(),
            PreviousBlockHash = previousBlockHash,
            TransactionId = transactionId
        };

        //step3: handle event and write result to blockStateSet
        var offerRemovedLogEventProcessor = GetRequiredService<OfferRemovedLogEventProcessor>();
        await offerRemovedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        offerRemovedLogEventProcessor.GetContractAddress(chainId);

        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(1000);

        //step5: check result
        var offerIndexData = await _nftOfferIndexRepository.GetAsync(IdGenerateHelper.GetId(chainId,
            offerRemoved.Symbol,
            offerRemoved.OfferFrom.ToBase58(),
            offerRemoved.OfferTo.ToBase58(), offerRemoved.ExpireTime.Seconds));
        offerIndexData.ShouldBeNull();
        
        //check minListingPrice info
        var nftId = IdGenerateHelper.GetNFTInfoId(chainId, symbol);
        var nftInfo = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftId, chainId);
        nftInfo.MaxOfferId.ShouldBe(null);
        Assert.Equal(0m, nftInfo.MaxOfferPrice);
    }

    [Fact]
    public async Task QueryNFTOffersTest()
    {
        await HandleOfferAddedLogEventAsync_Test();

        var result = await Query.NftOffers(_nftOfferIndexRepository, _objectMapper, new GetNFTOffersDto()
        {
            SkipCount = 0,
            MaxResultCount = 10,
            ChainId = "tDVW",
            NFTInfoId = "tDVW-SYB-1",
            ExpireTimeGt = DateTimeHelper.ToUnixTimeMilliseconds(new DateTime())
        });
        result.TotalRecordCount.ShouldBe(1);
        result.Data.Count.ShouldBe(1);
        result.Data.First().BizSymbol.ShouldBe("SYB-1");
        result.Data.First().BizInfoId.ShouldBe("tDVW-SYB-1");
        result.Data.First().PurchaseToken.Symbol.ShouldBe("SYB");
    }


    private async Task CreateTokenInfo()
    {
        const string chainId = "tDVW";
        const string blockHash = "0f4f79c709ee39c597795689f99be3c1384148dbb1a0b1b0fa21fc91229164e3";
        const string previousBlockHash = "f2316fb0e7646259a4238d8cd4700c9c6451a432e89df48c2368418c55c22b81";
        const string transactionId = "7a4c16a8aa4bb415b1128d060bb3e356ca7bab9ff77be5838a0ce5c4f5b1fe19";
        const long blockHeight = 10;

        //step1: create blockStateSet
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);

        //step2: create logEventInfo
        var contractDeployed = new ContractDeployed
        {
            Address = Address.FromPublicKey("BBB".HexToByteArray())
        };
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(contractDeployed.ToLogEvent());
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

        //step3: handle event and write result to blockStateSet
        var contractDeployedProcessor = GetRequiredService<ContractDeployedProcessor>();
        await contractDeployedProcessor.HandleEventAsync(logEventInfo, logEventContext);

        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(1000);
    }

    [Fact]
    public async Task QuerySeedSymbolsTest()
    {
        await HandleSeedIssueAsync_Test();
        var result = await Query.SeedSymbols(_seedSymbolIndexRepository, _objectMapper, new GetSeedSymbolsDto()
        {
            SkipCount = 0,
            MaxResultCount = 10,
            Address = Address.FromPublicKey("AAA".HexToByteArray()).ToBase58(),
            SeedOwnedSymbol = "TEST_NFT_COLLECTION_SYMBOLE-0"
        });
        result.TotalRecordCount.ShouldBe(1L);
        result.Data.First().SeedOwnedSymbol.ShouldBe("TEST_NFT_COLLECTION_SYMBOLE-0");
        result.Data.First().Symbol.ShouldBe("SEED-1");
    }

    [Fact]
    public async Task QueryNFTCollections()
    {
        const string chainId = "tDVW";
        const string symbol = "READ-0";

        await HandleNFTCollectionAddedAsync_Test();
        var result = await Query.NFTCollections(_nftCollectionIndexRepository, _objectMapper, new GetNFTCollectionsDto()
        {
            SkipCount = 0,
            MaxResultCount = 10,
            CreatorAddress = Address.FromPublicKey("AAA".HexToByteArray()).ToBase58()
        });
        result.TotalRecordCount.ShouldBe(1L);
        result.Data.First().Id.ShouldBe(IdGenerateHelper.GetNFTCollectionId(chainId, symbol));
        var result2 = await Query.NFTCollections(_nftCollectionIndexRepository, _objectMapper,
            new GetNFTCollectionsDto()
            {
                SkipCount = 0,
                MaxResultCount = 10,
                CreatorAddress = ""
            });
        result2.TotalRecordCount.ShouldBe(1L);
        result2.Data.First().Id.ShouldBe(IdGenerateHelper.GetNFTCollectionId(chainId, symbol));
        var result3 = await Query.NFTCollection(_nftCollectionIndexRepository, _objectMapper, new GetNFTCollectionDto()
        {
            Id = IdGenerateHelper.GetNFTCollectionId(chainId, symbol)
        });
        result3.Id.ShouldBe(IdGenerateHelper.GetNFTCollectionId(chainId, symbol));
        var result4 = await Query.NFTCollection(_nftCollectionIndexRepository, _objectMapper, new GetNFTCollectionDto()
        {
            Id = ""
        });
        result4.ShouldBeNull();
    }

    [Fact]
    public async Task QueryNFT()
    {
        await ProxyAccountCreatedAsync();
        await Task.Delay(0);
        const string chainId = "tDVW";
        const string symbol = "READ-1";

        await HandleNFTIssueAsync_Test();

        var result = await Query.NFTInfo(_nftInfoIndexRepository, _seedSymbolRepo,
            _whitelistIndexRepository, _userBalanceIndexRepository, _objectMapper,
            new GetNFTInfoDto()
            {
                Id = IdGenerateHelper.GetNFTInfoId(chainId, symbol),
                Address = ""
            });
        result.ShouldNotBeNull();
        result.OwnerCount.ShouldBe(1);
        result.Id.ShouldBe(IdGenerateHelper.GetNFTInfoId(chainId, symbol));
        result.OtherOwnerListingFlag.ShouldBeFalse();

        var result2 = await Query.NFTInfo(_nftInfoIndexRepository, _seedSymbolRepo,
            _whitelistIndexRepository, _userBalanceIndexRepository, _objectMapper,
            new GetNFTInfoDto()
            {
                Id = IdGenerateHelper.GetNFTInfoId(chainId, symbol),
                Address = Address.FromPublicKey("BBB".HexToByteArray()).ToBase58()
            });
        result2.ShouldNotBeNull();
        result2.Id.ShouldBe(IdGenerateHelper.GetNFTInfoId(chainId, symbol));
        var result3 = await Query.NFTInfo(_nftInfoIndexRepository, _seedSymbolRepo,
            _whitelistIndexRepository, _userBalanceIndexRepository, _objectMapper,
            new GetNFTInfoDto()
            {
                Id = "",
                Address = ""
            });
        result3.ShouldBeNull();
        var result4 = await Query.NFTInfo(_nftInfoIndexRepository, _seedSymbolRepo,
            _whitelistIndexRepository, _userBalanceIndexRepository, _objectMapper,
            new GetNFTInfoDto()
            {
                Id = IdGenerateHelper.GetNFTInfoId(chainId, symbol),
            });
        result4.ShouldNotBeNull();
        result4.Owner.ShouldBe(Address.FromPublicKey("BBB".HexToByteArray()).ToBase58());
    }

    [Fact]
    public async Task QueryNFTs()
    {
        await ProxyAccountCreatedAsync();
        await Task.Delay(0);
        const string chainId = "tDVW";
        const string symbol = "READ-1";

        await HandleNFTIssueAsync_Test();

        var result = await Query.NFTBriefInfos(_nftInfoIndexRepository,
            new GetNFTBriefInfosDto()
            {
                Sorting = "ListingTime DESC"
            });
        result.ShouldNotBeNull();
        result.TotalRecordCount.ShouldBe(1L);

        var result2 = await Query.NFTBriefInfos(_nftInfoIndexRepository,
            new GetNFTBriefInfosDto()
            {
                Sorting = "ListingTime DESC"
            });
        result2.ShouldNotBeNull();
        result2.TotalRecordCount.ShouldBe(1L);

        var result3 = await Query.NFTBriefInfos(_nftInfoIndexRepository,
            new GetNFTBriefInfosDto()
            {
                Sorting = "ListingTime DESC"
            });
        result3.ShouldNotBeNull();
        result3.TotalRecordCount.ShouldBe(1L);
    }
    

    [Fact]
    public async Task TestQueryNFTBriefInfos()
    {
        await MockCreateNFTSymbol("SYB-1", 1);
        await MockNFTIssue(0, "SYB-1", 2);
        await AddSymbol(DateTime.UtcNow.AddMinutes(1), "SYB-1", 3);
        await MockCreateNFTSymbol("SYB-2", 3);
        await MockNFTIssue(1, "SYB-2", 4);
        await AddSymbol(DateTime.UtcNow.AddMinutes(1), "SYB-2", 6);
        await MockCreateNFTSymbol("SYB-3", 5);
        await MockNFTIssue(1, "SYB-3", 7);
        await MockCreateNFTSymbol("SYB-4", 8);
        await MockNFTIssue(2, "SYB-4", 9);
        await MockCreateNFTSymbol("SYB-5", 10);
        await MockNFTIssue(3, "SYB-5", 11);
        var result = await Query.NFTBriefInfos(_nftInfoIndexRepository,
            new GetNFTBriefInfosDto()
            {
                Sorting = "ListingTime DESC"
            });
        result.TotalRecordCount.ShouldBe(4);
        result.Data.Count.ShouldBe(4);

        var result2 = await Query.NFTBriefInfos(_nftInfoIndexRepository,
            new GetNFTBriefInfosDto()
            {
                Sorting = "ListingTime DESC"
            });
        result2.TotalRecordCount.ShouldBe(4);
        result2.Data.Count.ShouldBe(4);
        var result3 = await Query.NFTBriefInfos(_nftInfoIndexRepository,
            new GetNFTBriefInfosDto()
            {
                Sorting = "ListingTime DESC"
            });
        result3.TotalRecordCount.ShouldBe(4);
        result3.Data.Count.ShouldBe(4);
        
        var result4 = await Query.NFTBriefInfos(_nftInfoIndexRepository,
            new GetNFTBriefInfosDto()
            {
                SearchParam = "SYB-1",
                Sorting = "ListingTime DESC"
            });
        result4.TotalRecordCount.ShouldBe(0);
        result4.Data.Count.ShouldBe(0);
        var result5 = await Query.NFTBriefInfos(_nftInfoIndexRepository,
            new GetNFTBriefInfosDto()
            {
                SearchParam = "SYB-2",
                Sorting = "ListingTime DESC"
            });
        result5.TotalRecordCount.ShouldBe(1);
        result5.Data.Count.ShouldBe(1);
    }

    private async Task AddSymbol(DateTime StartTime, string inputSymbol, long blockHeight)
    {
        string symbol = inputSymbol;
        const string tokenSymbol = "SYB-1";
        const long amount = 100;
        const long decimals = 8;
        const long quantity = 1;
        Hash whitelistId = HashHelper.ComputeFrom("test@gmail.com");

        var logEventContext = MockLogEventContext(blockHeight);
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
                StartTime = Timestamp.FromDateTime(StartTime),
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

    private async Task ProxyAccountCreatedAsync()
    {
        const string chainId = "AELF";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;

        var proxyAccountCreatedLogEventProcessor = GetRequiredService<ProxyAccountCreatedLogEventProcessor>();

        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);

        var proxyAccountCreated = new ProxyAccountCreated()
        {
            ProxyAccountAddress = Address.FromPublicKey("AAA".HexToByteArray()),
            ProxyAccountHash = Hash.Empty,
            ManagementAddresses = new ManagementAddressList()
            {
                Value =
                {
                    new ManagementAddress
                    {
                        Address = Address.FromPublicKey("BBB".HexToByteArray())
                    }
                }
            },
            CreateChainId = chainId.GetHashCode()
        };
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(proxyAccountCreated.ToLogEvent());
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

        await proxyAccountCreatedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(0);
        var proxyAccountIndexId =
            IdGenerateHelper.GetProxyAccountIndexId(Address.FromPublicKey("AAA".HexToByteArray()).ToBase58());
        var proxyAccountIndex =
            await _proxyAccountIndexRepository.GetFromBlockStateSetAsync(proxyAccountIndexId, chainId);
        proxyAccountIndex.Id.ShouldBe(proxyAccountIndexId);
        proxyAccountIndex.ProxyAccountAddress.ShouldBe(Address.FromPublicKey("AAA".HexToByteArray()).ToBase58());
        proxyAccountIndex.ManagersSet.ShouldContain(Address.FromPublicKey("BBB".HexToByteArray()).ToBase58());
    }

    [Fact]
    public async Task TestSymbol()
    {
        SymbolHelper.CheckSymbolIsSeedSymbol("SEED-0").ShouldBeTrue();
        SymbolHelper.CheckSymbolIsSeedSymbol("SEED-1").ShouldBeTrue();
        SymbolHelper.CheckSymbolIsNFTCollection("AAA-0").ShouldBeTrue();
        SymbolHelper.CheckSymbolIsNFTCollection("AAA-1").ShouldBeFalse();
        SymbolHelper.CheckSymbolIsNoMainChainNFT("AAA-1","tDVV").ShouldBeTrue();
        SymbolHelper.CheckSymbolIsNoMainChainNFT("AAA-11","tDVV").ShouldBeTrue();
        SymbolHelper.CheckSymbolIsNoMainChainNFT("AAA-1","AELF").ShouldBeFalse();
        SymbolHelper.CheckSymbolIsNoMainChainNFT("AAA-0","tDVV").ShouldBeFalse();
    }
    
    public static DateTime ToTimestamp(long seconds)
    {
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return epoch.AddSeconds(seconds);
    }
}