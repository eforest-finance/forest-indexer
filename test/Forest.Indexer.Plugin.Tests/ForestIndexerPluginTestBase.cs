using AElf;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenAdapterContract;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Orleans.TestBase;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.GraphQL;
using Forest.Indexer.Plugin.Processors;
using Forest.Indexer.Plugin.Tests.Helper;
using Forest.SymbolRegistrar;
using Google.Protobuf.WellKnownTypes;
using Microsoft.IdentityModel.Tokens;
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace Forest.Indexer.Plugin.Tests;

public abstract class ForestIndexerPluginTestBase : ForestIndexerOrleansTestBase<ForestIndexerPluginTestModule>
{
    private readonly IAElfIndexerClientInfoProvider _indexerClientInfoProvider;
    public IBlockStateSetProvider<LogEventInfo> _blockStateSetLogEventInfoProvider;
    private readonly IBlockStateSetProvider<TransactionInfo> _blockStateSetTransactionInfoProvider;
    private readonly IDAppDataProvider _dAppDataProvider;
    private readonly IDAppDataIndexManagerProvider _dAppDataIndexManagerProvider;

    protected readonly IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo>
        _tsmSeedSymbolIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo>
        _seedSymbolIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<SeedSymbolMarketTokenIndex, LogEventInfo>
        _symbolMarketTokenIndexRepository;
    
    private readonly IObjectMapper _objectMapper;

    public ForestIndexerPluginTestBase()
    {
        _indexerClientInfoProvider = GetRequiredService<IAElfIndexerClientInfoProvider>();
        _blockStateSetLogEventInfoProvider = GetRequiredService<IBlockStateSetProvider<LogEventInfo>>();
        _blockStateSetTransactionInfoProvider = GetRequiredService<IBlockStateSetProvider<TransactionInfo>>();
        _dAppDataProvider = GetRequiredService<IDAppDataProvider>();
        _dAppDataIndexManagerProvider = GetRequiredService<IDAppDataIndexManagerProvider>();
        _tsmSeedSymbolIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<TsmSeedSymbolIndex, LogEventInfo>>();
        _seedSymbolIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<SeedSymbolIndex, LogEventInfo>>();
        _symbolMarketTokenIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<SeedSymbolMarketTokenIndex, LogEventInfo>>();
        _objectMapper = GetRequiredService<IObjectMapper>();
    }

    protected async Task<string> InitializeBlockStateSetAsync(BlockStateSet<LogEventInfo> blockStateSet, string chainId)
    {
        var key = GrainIdHelper.GenerateGrainId("BlockStateSets", _indexerClientInfoProvider.GetClientId(), chainId,
            _indexerClientInfoProvider.GetVersion());

        await _blockStateSetLogEventInfoProvider.SetBlockStateSetAsync(key, blockStateSet);
        await _blockStateSetLogEventInfoProvider.SetCurrentBlockStateSetAsync(key, blockStateSet);
        await _blockStateSetLogEventInfoProvider.SetLongestChainBlockStateSetAsync(key, blockStateSet.BlockHash);

        return key;
    }

    protected async Task<string> InitializeBlockStateSetAsync(BlockStateSet<TransactionInfo> blockStateSet,
        string chainId)
    {
        var key = GrainIdHelper.GenerateGrainId("BlockStateSets", _indexerClientInfoProvider.GetClientId(), chainId,
            _indexerClientInfoProvider.GetVersion());

        await _blockStateSetTransactionInfoProvider.SetBlockStateSetAsync(key, blockStateSet);
        await _blockStateSetTransactionInfoProvider.SetCurrentBlockStateSetAsync(key, blockStateSet);
        await _blockStateSetTransactionInfoProvider.SetLongestChainBlockStateSetAsync(key, blockStateSet.BlockHash);

        return key;
    }

    protected async Task BlockStateSetSaveDataAsync<TSubscribeType>(string key)
    {
        await _dAppDataProvider.SaveDataAsync();
        await _dAppDataIndexManagerProvider.SavaDataAsync();
        if (typeof(TSubscribeType) == typeof(TransactionInfo))
            await _blockStateSetTransactionInfoProvider.SaveDataAsync(key);
        else if (typeof(TSubscribeType) == typeof(LogEventInfo))
            await _blockStateSetLogEventInfoProvider.SaveDataAsync(key);
    }

    protected LogEventContext MockLogEventContext(long inputBlockHeight = 100, string chainId = "tDVW",string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2")
    {
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        var blockHeight = inputBlockHeight;
        return new LogEventContext
        {
            ChainId = chainId,
            BlockHeight = blockHeight,
            BlockHash = blockHash,
            PreviousBlockHash = previousBlockHash,
            TransactionId = transactionId,
            BlockTime = DateTime.UtcNow.FromUnixTimeSeconds(1672502400),
            ExtraProperties = new Dictionary<string, string>
            {
                { "TransactionFee", "{\"ELF\": 10, \"DECIMAL\": 8}" },
                { "ResourceFee", "{\"ELF\": 10, \"DECIMAL\": 15}" }
            }
        };
    }

    protected LogEventInfo MockLogEventInfo(LogEvent logEvent)
    {
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(logEvent);
        var logEventContext = MockLogEventContext(100);
        logEventInfo.BlockHeight = logEventContext.BlockHeight;
        logEventInfo.ChainId = logEventContext.ChainId;
        logEventInfo.BlockHash = logEventContext.BlockHash;
        logEventInfo.TransactionId = logEventContext.TransactionId;
        logEventInfo.BlockTime = DateTime.Now;
        return logEventInfo;
    }

    protected async Task<string> MockBlockState(LogEventContext logEventContext)
    {
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = logEventContext.BlockHash,
            BlockHeight = logEventContext.BlockHeight,
            Confirmed = true,
            PreviousBlockHash = logEventContext.PreviousBlockHash
        };
        return await InitializeBlockStateSetAsync(blockStateSet, logEventContext.ChainId);
    }

    protected async Task<string> MockBlockStateForTransactionInfo(LogEventContext logEventContext)
    {
        var blockStateSet = new BlockStateSet<TransactionInfo>
        {
            BlockHash = logEventContext.BlockHash,
            BlockHeight = logEventContext.BlockHeight,
            Confirmed = true,
            PreviousBlockHash = logEventContext.PreviousBlockHash
        };
        return await InitializeBlockStateSetAsync(blockStateSet, logEventContext.ChainId);
    }

    protected async Task SeedAdd(SeedType seedType, string symbol, string seedOwnedSymbol,string chainIdParam)
    {
        var chainId = chainIdParam.IsNullOrEmpty() ? ForestIndexerConstants.MainChain : chainIdParam;
        var logEventContext = MockLogEventContext(100, chainId);
        var blockStateSetKey = await MockBlockState(logEventContext);

        var seedCreatedProcessor = GetRequiredService<SeedCreatedProcessor>();
        seedCreatedProcessor.GetContractAddress(chainId);

        var SeedCreated = new SeedCreated()
        {
            Symbol = symbol,
            SeedType = seedType,
            OwnedSymbol = seedOwnedSymbol,
            To = Address.FromPublicKey("AAA".HexToByteArray()),
            ExpireTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.Now)
        };
        var logEventInfo = MockLogEventInfo(SeedCreated.ToLogEvent());
        await seedCreatedProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);

        var seedSymbolIndex =
            await _tsmSeedSymbolIndexRepository.GetFromBlockStateSetAsync(
                IdGenerateHelper.GetSeedSymbolId(chainId, seedOwnedSymbol), chainId);
        Assert.NotNull(seedSymbolIndex);
    }

    protected async Task SymbolMarketTokenAdd(string chainId, string symbol, string seedOwnedSymbol)
    {
        const string tokenName = "READ Token";
        const long totalSupply = 1000;
        const int decimals = 2;
        const bool isBurnable = true;
        const int issueChainId = 9992731;

        var logEventContext = MockLogEventContext(100,chainId);
        var blockStateSetKey = await MockBlockStateForTransactionInfo(logEventContext);
        await SeedAdd(SeedType.Regular, symbol, seedOwnedSymbol, null);
        await HandleSeedTokenCreate(chainId, symbol, seedOwnedSymbol);
        var managerTokenCreatedLogEventProcessor = GetRequiredService<ManagerTokenCreatedLogEventProcessor>();
        managerTokenCreatedLogEventProcessor.GetContractAddress(chainId);

        var managerTokenCreated = new ManagerTokenCreated()
        {
            Symbol = seedOwnedSymbol,
            TokenName = tokenName,
            TotalSupply = totalSupply,
            Decimals = decimals,
            Owner = Address.FromPublicKey("AAA1".HexToByteArray()),
            Issuer = Address.FromPublicKey("BBB1".HexToByteArray()),
            IsBurnable = isBurnable,
            IssueChainId = issueChainId,
            RealIssuer = Address.FromPublicKey("AAA".HexToByteArray()),
            RealOwner = Address.FromPublicKey("BBB".HexToByteArray()),
            ExternalInfo = new ExternalInfos()
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

        var logEventInfo = MockLogEventInfo(managerTokenCreated.ToLogEvent());

        await managerTokenCreatedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<TransactionInfo>(blockStateSetKey);

        var seedSymbolId =
            "AELF-seedOwnedSymbol1";
        var result =
            _symbolMarketTokenIndexRepository.GetFromBlockStateSetAsync(seedSymbolId, chainId);
        Assert.True(result.Result.Id.Equals(seedSymbolId));
        Assert.True(result.Result.IssueManagerSet.Contains("2YcGvyn7QPmhvrZ7aaymmb2MDYWhmAks356nV3kUwL8FkGSYeZ"));
        Assert.True(result.Result.OwnerManagerSet.Contains("aLyxCJvWMQH6UEykTyeWAcYss9baPyXkrMQ37BHnUicxD2LL3"));
        Assert.True(result.Result.TotalSupply.Equals(1000));
        Assert.True(result.Result.Decimals.Equals(2));
        Assert.True(result.Result.Supply.Equals(0));
        Assert.True(result.Result.Issued.Equals(0));
        Assert.True(result.Result.IsBurnable.Equals(true));

    }
    
    protected async Task SymbolSeedCrossChain(string chainId, string symbol, string seedOwnedSymbol)
    {
        const string tokenName = "READ Token";
        const long totalSupply = 1000;
        const int decimals = 2;
        const bool isBurnable = true;
        const int issueChainId = 9992731;

        var logEventContext = MockLogEventContext(100,chainId);
        var blockStateSetKey = await MockBlockStateForTransactionInfo(logEventContext);
        await SeedAdd(SeedType.Regular, symbol, seedOwnedSymbol, null);
        await HandleSeedTokenCreate(chainId, symbol, seedOwnedSymbol);
        await HandleSeedIssueAsync(symbol,chainId,"AAA");
        
        
            //crosschain-seed burned
        await SeedBurnedAsync_Test(chainId,symbol);
        
        //crosschain-CrossChainReceivedProcessor
        await MockCrossChain(1, symbol, 100, "AELF", "tDVV", "READ Token", "AAA", "BBB");

    }
    
    protected async Task SeedTokenCrossChain(string chainId,string targetChainId, string symbol, string seedOwnedSymbol,string addressFrom,string addressTo)
    {
        const string tokenName = "READ Token";
        const long totalSupply = 1000;
        const int decimals = 2;
        const bool isBurnable = true;
        const int issueChainId = 9992731;

        var logEventContext = MockLogEventContext(100,chainId);
        var blockStateSetKey = await MockBlockStateForTransactionInfo(logEventContext);
        await SeedAdd(SeedType.Regular, symbol, seedOwnedSymbol, null);
        await HandleSeedTokenCreate(chainId, symbol, seedOwnedSymbol);
        //issue
        await HandleSeedTokenIssueAsync(seedOwnedSymbol,chainId,addressFrom);
        //crosschain-seedToken burned
        await SeedTokenBurnedAsync_Test(chainId,seedOwnedSymbol);
        
        //crosschain-CrossChainReceivedProcessor
        await MockCrossChain(1, seedOwnedSymbol, 100, chainId, targetChainId, "READ Token", addressFrom, addressTo);
        
    }

    
    protected async Task HandleSeedTokenCreate(string chainId,string symbol, string seedOwnedSymbol)
    {
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;
        
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
        seedSymbolIndex.Symbol.ShouldBe(symbol);
    }
    
    protected async Task HandleSeedIssueAsync(string symbol,string chainId,string address)
    {
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;
        
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
            To = Address.FromPublicKey(address.HexToByteArray()),
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
        seedSymbolInfoIndexData.Symbol.ShouldBe(symbol);
    }
    
    protected async Task HandleSeedTokenIssueAsync(string symbol,string chainId,string address)
    {
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;
        
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
            To = Address.FromPublicKey(address.HexToByteArray()),
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
        seedSymbolInfoIndexData.Symbol.ShouldBe(symbol);
    }
    
    protected async Task MockCreateNFT()
    {
        // Create NFT collection
        const string collectionSymbol = "SYB-0";
        const string nftSymbol = "SYB-1";
        const string tokenName = "SYB Token";
        const bool isBurnable = true;
        const long totalSupply = 1;
        const int decimals = 8;
        const int issueChainId = 9992731;
        var logEventContext = MockLogEventContext(100);
        var blockStateSetKey = await MockBlockState(logEventContext);

        var tokenCreated = new TokenCreated()
        {
            Symbol = collectionSymbol,
            TokenName = tokenName,
            TotalSupply = totalSupply,
            Decimals = decimals,
            Issuer = Address.FromPublicKey("AAA".HexToByteArray()),
            IsBurnable = isBurnable,
            IssueChainId = issueChainId,
            ExternalInfo = new ExternalInfo()
        };
        var logEventInfo = MockLogEventInfo(tokenCreated.ToLogEvent());
        var nftCollectionAddedLogEventProcessor = GetRequiredService<TokenCreatedLogEventProcessor>();
        await nftCollectionAddedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);


        // Create NFT
        tokenCreated = new TokenCreated()
        {
            Symbol = nftSymbol,
            TokenName = tokenName,
            TotalSupply = totalSupply,
            Decimals = decimals,
            Issuer = Address.FromPublicKey("AAA".HexToByteArray()),
            IsBurnable = isBurnable,
            IssueChainId = issueChainId,
            ExternalInfo = new ExternalInfo(),
        };
        logEventInfo = MockLogEventInfo(tokenCreated.ToLogEvent());

        var nftCreateLogEventProcessor = GetRequiredService<TokenCreatedLogEventProcessor>();
        await nftCreateLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
    }
    
    protected async Task MockCreateNFT(string nftSymbol, string collectionSymbol, string publicKey, long totalSupply)
    {
        // Create NFT collection
        string tokenName = nftSymbol;
        const bool isBurnable = true; 
        const int decimals = 8;
        const int issueChainId = 9992731;
        var logEventContext = MockLogEventContext(100);
        var blockStateSetKey = await MockBlockState(logEventContext);

        var tokenCreated = new TokenCreated()
        {
            Symbol = collectionSymbol,
            TokenName = tokenName,
            TotalSupply = totalSupply,
            Decimals = decimals,
            Issuer = Address.FromPublicKey(publicKey.HexToByteArray()),
            IsBurnable = isBurnable,
            IssueChainId = issueChainId,
            ExternalInfo = new ExternalInfo()
        };
        var logEventInfo = MockLogEventInfo(tokenCreated.ToLogEvent());
        var nftCollectionAddedLogEventProcessor = GetRequiredService<TokenCreatedLogEventProcessor>();
        await nftCollectionAddedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);


        // Create NFT
        tokenCreated = new TokenCreated()
        {
            Symbol = nftSymbol,
            TokenName = tokenName,
            TotalSupply = totalSupply,
            Decimals = decimals,
            Issuer = Address.FromPublicKey(publicKey.HexToByteArray()),
            IsBurnable = isBurnable,
            IssueChainId = issueChainId,
            ExternalInfo = new ExternalInfo(),
        };
        logEventInfo = MockLogEventInfo(tokenCreated.ToLogEvent());

        var nftCreateLogEventProcessor = GetRequiredService<TokenCreatedLogEventProcessor>();
        await nftCreateLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
    }
    
    protected async Task MockCreateNFTSymbol(string inputNFTSymbol, long blockHeight)
    {
        // Create NFT collection
        const string collectionSymbol = "SYB-0";
        string nftSymbol = inputNFTSymbol;
        const string tokenName = "SYB Token";
        const bool isBurnable = true;
        const long totalSupply = 1;
        const int decimals = 8;
        const int issueChainId = 9992731;
        var logEventContext = MockLogEventContext(blockHeight);
        var blockStateSetKey = await MockBlockState(logEventContext);

        var tokenCreated = new TokenCreated()
        {
            Symbol = collectionSymbol,
            TokenName = tokenName,
            TotalSupply = totalSupply,
            Decimals = decimals,
            Issuer = Address.FromPublicKey("AAA".HexToByteArray()),
            IsBurnable = isBurnable,
            IssueChainId = issueChainId,
            ExternalInfo = new ExternalInfo()
        };
        var logEventInfo = MockLogEventInfo(tokenCreated.ToLogEvent());
        var nftCollectionAddedLogEventProcessor = GetRequiredService<TokenCreatedLogEventProcessor>();
        await nftCollectionAddedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);


        // Create NFT
        tokenCreated = new TokenCreated()
        {
            Symbol = nftSymbol,
            TokenName = tokenName,
            TotalSupply = totalSupply,
            Decimals = decimals,
            Issuer = Address.FromPublicKey("AAA".HexToByteArray()),
            IsBurnable = isBurnable,
            IssueChainId = issueChainId,
            ExternalInfo = new ExternalInfo(),
        };
        logEventInfo = MockLogEventInfo(tokenCreated.ToLogEvent());

        var nftCreateLogEventProcessor = GetRequiredService<TokenCreatedLogEventProcessor>();
        await nftCreateLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
    }

    protected async Task MockNFTIssue(int inputDecimal, string inputSymbol, long height,string chainId = "tDVW",string tokenName = "READ Token",string address = "AAA")
    {
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        long blockHeight = height;

        string symbol = inputSymbol;

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
            Amount = inputDecimal,
            To = Address.FromPublicKey(address.HexToByteArray())
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
    
    protected async Task MockCrossChain(int inputDecimal, string inputSymbol, long height,string fromChainId = "AELF",string toChainId="tDVW",string tokenName = "READ Token",string addressFrom = "AAA",string addressTo = "BBB")
    {
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        long blockHeight = height;

        string symbol = inputSymbol;

        var crossChainReceivedProcessor = GetRequiredService<CrossChainReceivedProcessor>();
        crossChainReceivedProcessor.GetContractAddress(toChainId);
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, toChainId);

        var crossChainReceived = new CrossChainReceived()
        {
            From = Address.FromPublicKey(addressFrom.HexToByteArray()),
            To = Address.FromPublicKey(addressTo.HexToByteArray()),
            Symbol = symbol,
            Amount =1,
            Memo="",
            FromChainId = 9992731,
            IssueChainId = 9992731,
            ParentChainHeight =15455072,
            TransferTransactionId = Hash.LoadFromHex(transactionId)
        };

        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(crossChainReceived.ToLogEvent());
        logEventInfo.BlockHeight = blockHeight;
        logEventInfo.ChainId = fromChainId;
        logEventInfo.BlockHash = blockHash;
        logEventInfo.TransactionId = transactionId;
        var logEventContext = new LogEventContext
        {
            ChainId = toChainId,
            BlockHeight = blockHeight,
            BlockHash = blockHash,
            PreviousBlockHash = previousBlockHash,
            TransactionId = transactionId
        };

        await crossChainReceivedProcessor.HandleEventAsync(logEventInfo, logEventContext);

        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        
        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(toChainId, symbol);
        var seedSymbolIndex = await _seedSymbolIndexRepository.GetFromBlockStateSetAsync(seedSymbolId, toChainId);
        seedSymbolIndex.IsDeleteFlag.ShouldBeFalse();
        
        var tsmSeedSymbolId = IdGenerateHelper.GetSeedSymbolId(toChainId, seedSymbolIndex.SeedOwnedSymbol);
        var tsmSeedSymbolIndex = await _tsmSeedSymbolIndexRepository.GetFromBlockStateSetAsync(tsmSeedSymbolId, toChainId);
        tsmSeedSymbolIndex.IsBurned.ShouldBeFalse();
    }


    protected async Task<Forest.Contracts.Auction.AuctionCreated> MockAddAuctionInfo(string symbol,string chainIdParam)
    {
        var chainId = !chainIdParam.IsNullOrEmpty()? chainIdParam : "tDVW";
        const string from = "2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const string to = "Lmemfcp2nB8kAvQDLxsLtQuHWgpH5gUWVmmcEkpJ2kRY9Jv25";
        const long blockHeight = 100;

        var auctionCreatedProcessor = GetRequiredService<AuctionCreatedLogEventProcessor>();
        auctionCreatedProcessor.GetContractAddress(chainId);
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        //step1: create blockStateSet
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);

        var auctionCreated = new Forest.Contracts.Auction.AuctionCreated
        {
            Symbol = symbol,
            Creator = Address.FromPublicKey("AAA".HexToByteArray()),
            AuctionId = HashHelper.ComputeFrom(symbol),
            StartPrice = new Contracts.Auction.Price
            {
                Amount = 10000,
                Symbol = "ELF"
            },
            AuctionType = Forest.Contracts.Auction.AuctionType.English,
            AuctionConfig = new Forest.Contracts.Auction.AuctionConfig
            {
                Duration = 1000000,
                MinMarkup = 100,
                MaxExtensionTime = 10000000,
                StartImmediately = false
            },
            ReceivingAddress = Address.FromBase58(to),
            MaxEndTime = new DateTime(2099, 11, 10).AddDays(1).ToUniversalTime().ToTimestamp()
        };
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(auctionCreated.ToLogEvent());
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

        await auctionCreatedProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        return auctionCreated;
    }
    
    protected async Task MockBidPlaced(string symbol, string chainId, Forest.Contracts.Auction.AuctionCreated auctionCreated, 
        string to,
        long incr)
    {
        const string from = "2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        //const string to = "Lmemfcp2nB8kAvQDLxsLtQuHWgpH5gUWVmmcEkpJ2kRY9Jv25";
        const long blockHeight = 100;
        
        var bidTime = Timestamp.FromDateTime(DateTime.UtcNow);
        var endTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(10));
        var maxEndTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(100));

        var bidPlacedLogEventProcessor = GetRequiredService<BidPlacedLogEventProcessor>();

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
        var auctionTimeUpdated = new Forest.Contracts.Auction.BidPlaced
        {
            AuctionId = auctionCreated.AuctionId,
            Bidder = Address.FromBase58(to),
            Price = new Forest.Contracts.Auction.Price
            {
                Amount = 20000 + incr,
                Symbol = "USDT"
            },
            BidTime = bidTime,
        };
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(auctionTimeUpdated.ToLogEvent());
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
        await bidPlacedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);

        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
    }
    
    protected async Task SeedBurnedAsync_Test(string chainId,string symbol)
    {
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;
        
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
            Burner = Address.FromPublicKey("CCC".HexToByteArray())
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
        
        var tsmSeedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, seedSymbolIndex.SeedOwnedSymbol);
        var tsmSeedSymbolIndex = await _tsmSeedSymbolIndexRepository.GetFromBlockStateSetAsync(tsmSeedSymbolId, chainId);
        tsmSeedSymbolIndex.IsBurned.ShouldBeTrue();
    }

    protected async Task SeedTokenBurnedAsync_Test(string chainId,string symbol)
    {
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;
        
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
            Burner = Address.FromPublicKey("CCC".HexToByteArray())
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
        
        var tsmSeedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, seedSymbolIndex.SeedOwnedSymbol);
        var tsmSeedSymbolIndex = await _tsmSeedSymbolIndexRepository.GetFromBlockStateSetAsync(tsmSeedSymbolId, chainId);
        tsmSeedSymbolIndex.IsBurned.ShouldBeTrue();
    }
    
    protected async Task MockTsmSeedSymbolInfoIndexAsync(string chainId, string symbol)
    {
        const string from = "2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const string to = "Lmemfcp2nB8kAvQDLxsLtQuHWgpH5gUWVmmcEkpJ2kRY9Jv25";
        const long blockHeight = 100;
        
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var logEventContext = MockLogEventContext();
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);
        var tsmSeedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, symbol);
        var tsmSeedSymbolIndex =
            await _tsmSeedSymbolIndexRepository.GetFromBlockStateSetAsync(tsmSeedSymbolId, chainId);
        tsmSeedSymbolIndex.SeedSymbol = symbol;
        _objectMapper.Map(logEventContext, tsmSeedSymbolIndex);
        await _tsmSeedSymbolIndexRepository.AddOrUpdateAsync(tsmSeedSymbolIndex);
        var seedSymbolId = IdGenerateHelper.GetSeedSymbolId(chainId, tsmSeedSymbolIndex.SeedSymbol);
        SeedSymbolIndex seedSymbolIndex = new SeedSymbolIndex();
        seedSymbolIndex.Id = seedSymbolId;
        seedSymbolIndex.Symbol = tsmSeedSymbolIndex.SeedSymbol;
        seedSymbolIndex.SeedOwnedSymbol = tsmSeedSymbolIndex.Symbol;
        _objectMapper.Map(logEventContext, seedSymbolIndex);
        await _seedSymbolIndexRepository.AddOrUpdateAsync(seedSymbolIndex);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
    }
    
    protected async Task MockSpecialSeedAddedAsync(string symbol, string to)
    {
        var logEventContext = MockLogEventContext();
        var blockStateSetKey = await MockBlockState(logEventContext);
        var specialSeedAdded = new SpecialSeedAdded()
        {
            AddList = new SpecialSeedList
            {
                Value =
                {
                    new SpecialSeed
                    {
                        SeedType = SeedType.Unique,
                        Symbol = symbol,
                        PriceSymbol = ForestIndexerConstants.PriceSimpleElf,
                        PriceAmount = 1_0000_0000,
                        AuctionType = Forest.SymbolRegistrar.AuctionType.English,
                        IssueChain = ForestIndexerConstants.MainChain,
                        IssueChainContractAddress = to,
                    }
                }
            }
        };

        var logEventInfo = MockLogEventInfo(specialSeedAdded.ToLogEvent());
        var seedCreatedProcessor = GetRequiredService<SpecialSeedAddedLogEventProcessor>();
        await seedCreatedProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
    }
    
}