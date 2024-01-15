using AElf.Contracts.MultiToken;
using AElf.Contracts.ProxyAccountContract;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Processors;
using Forest.Indexer.Plugin.Tests.Helper;
using Nest;
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using Xunit;

namespace Forest.Indexer.Plugin.Tests.Processors;

public class ProxyAccountLogEventProcessorTests : ForestIndexerPluginTestBase
{
    private readonly IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo> _nftInfoIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<CollectionIndex, LogEventInfo> _nftCollectionIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<ProxyAccountIndex, LogEventInfo> _proxyAccountIndexRepository;

    public ProxyAccountLogEventProcessorTests()
    {
        _nftInfoIndexRepository = GetRequiredService<IAElfIndexerClientEntityRepository<NFTInfoIndex, LogEventInfo>>();
        _nftCollectionIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<CollectionIndex, LogEventInfo>>();
        _proxyAccountIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<ProxyAccountIndex, LogEventInfo>>();
    }

    private async Task NFTCollectionAddedAsync()
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
        nftCollectionAddedLogEventProcessor.GetContractAddress("tDVW");
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
            Issuer = Address.FromPublicKey("BBB".HexToByteArray()),
            Owner = Address.FromPublicKey("BBB".HexToByteArray()),
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
        await Task.Delay(0);

        var nftCollectionIndexId = IdGenerateHelper.GetNFTCollectionId(chainId, symbol);
        var nftCollectionIndex = await _nftCollectionIndexRepository.GetFromBlockStateSetAsync(nftCollectionIndexId,chainId);
        nftCollectionIndex.Id.ShouldBe(nftCollectionIndexId);
        
        nftCollectionIndex.BlockHeight.ShouldBe(blockHeight);
        nftCollectionIndex.Decimals.ShouldBe(decimals);
        nftCollectionIndex.Symbol.ShouldBe(symbol);
        nftCollectionIndex.TokenName.ShouldBe(tokenName);
        nftCollectionIndex.TotalSupply.ShouldBe(totalSupply);
        nftCollectionIndex.Decimals.ShouldBe(decimals);
        nftCollectionIndex.IsBurnable.ShouldBe(isBurnable);
        nftCollectionIndex.IssueChainId.ShouldBe(issueChainId);

    }
    private async Task NFTInfoCreatedAsync()
    {
        await NFTCollectionAddedAsync();
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
            Owner = Address.FromPublicKey("BBB".HexToByteArray()),
            Issuer = Address.FromPublicKey("BBB".HexToByteArray()),
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
        var nftInfoIndex =
            await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftInfoId, chainId);
        nftInfoIndex.BlockHeight.ShouldBe(blockHeight);
        nftInfoIndex.Decimals.ShouldBe(decimals);
        nftInfoIndex.Symbol.ShouldBe(symbol);
        nftInfoIndex.TokenName.ShouldBe(tokenName);
        nftInfoIndex.TotalSupply.ShouldBe(totalSupply);
        nftInfoIndex.Decimals.ShouldBe(decimals);
        nftInfoIndex.IsBurnable.ShouldBe(isBurnable);
        nftInfoIndex.IssueChainId.ShouldBe(issueChainId);
        nftInfoIndex.Issuer.ShouldBe(Address.FromPublicKey("BBB".HexToByteArray()).ToBase58());
    }
    
    private async Task NFTInfoIssuedAsync()
    {
        await NFTInfoCreatedAsync();
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
            To = Address.FromPublicKey("AAA".HexToByteArray())
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
    
    private async Task CheckNFTCollectionManageAddressKeepDefault()
    {
        const string chainId = "tDVW";
        const string symbol = "READ-0";
        var nftCollectionIndexId = IdGenerateHelper.GetNFTCollectionId(chainId, symbol);
        var nftCollectionIndex = await _nftCollectionIndexRepository.GetFromBlockStateSetAsync(nftCollectionIndexId,chainId);
        nftCollectionIndex.Id.ShouldBe(nftCollectionIndexId);
        nftCollectionIndex.ShouldNotBeNull();
        nftCollectionIndex.Owner.ShouldBe(Address.FromPublicKey("BBB".HexToByteArray()).ToBase58());
        nftCollectionIndex.OwnerManagerSet.ShouldNotContain(Address.FromPublicKey("AAA".HexToByteArray()).ToBase58());
        nftCollectionIndex.OwnerManagerSet.ShouldNotContain(Address.FromPublicKey("CCC".HexToByteArray()).ToBase58());
        nftCollectionIndex.OwnerManagerSet.ShouldContain(Address.FromPublicKey("BBB".HexToByteArray()).ToBase58());
    }
    
    private async Task CheckNFTInfoManageAddressKeepDefault()
    {
        const string chainId = "tDVW";
        const string symbol = "READ-1";
        var nftInfoIndexId = IdGenerateHelper.GetNFTCollectionId(chainId, symbol);
        var nftInfoIndex = await _nftInfoIndexRepository.GetFromBlockStateSetAsync(nftInfoIndexId,chainId);
        nftInfoIndex.Id.ShouldBe(nftInfoIndexId);
        nftInfoIndex.ShouldNotBeNull();
        nftInfoIndex.Issuer.ShouldBe(Address.FromPublicKey("BBB".HexToByteArray()).ToBase58());
        nftInfoIndex.IssueManagerSet.ShouldNotContain(Address.FromPublicKey("AAA".HexToByteArray()).ToBase58());
        nftInfoIndex.IssueManagerSet.ShouldNotContain(Address.FromPublicKey("CCC".HexToByteArray()).ToBase58());
        nftInfoIndex.IssueManagerSet.ShouldContain(Address.FromPublicKey("BBB".HexToByteArray()).ToBase58());
    }

    private async Task CheckNFTCollectionManageAddressUpdate()
    {
        var proxyAccountAddress = Address.FromPublicKey("BBB".HexToByteArray()).ToBase58();
        var mustQuery = new List<Func<QueryContainerDescriptor<CollectionIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.Owner).Value(proxyAccountAddress)));

        QueryContainer Filter(QueryContainerDescriptor<CollectionIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await _nftCollectionIndexRepository.GetListAsync(Filter, skip: 0, limit: 1);
        if (result == null) return;
        var nftCollectionIndex = result?.Item2?.FirstOrDefault();
        nftCollectionIndex.ShouldNotBeNull();
        nftCollectionIndex.Owner.ShouldBe(Address.FromPublicKey("BBB".HexToByteArray()).ToBase58());
        nftCollectionIndex.OwnerManagerSet.ShouldContain(Address.FromPublicKey("AAA".HexToByteArray()).ToBase58());
        nftCollectionIndex.OwnerManagerSet.ShouldContain(Address.FromPublicKey("CCC".HexToByteArray()).ToBase58());
        nftCollectionIndex.OwnerManagerSet.ShouldNotContain(Address.FromPublicKey("BBB".HexToByteArray()).ToBase58());
        nftCollectionIndex.RandomOwnerManager.ShouldNotContain(Address.FromPublicKey("BBB".HexToByteArray()).ToBase58());
    }
    private async Task CheckNFTInfoManageAddressUpdate()
    {
        var proxyAccount = Address.FromPublicKey("BBB".HexToByteArray()).ToBase58();
        var mustQuery2 = new List<Func<QueryContainerDescriptor<NFTInfoIndex>, QueryContainer>>();
        mustQuery2.Add(q => q.Term(i =>
            i.Field(f => f.Issuer).Value(proxyAccount)));

        QueryContainer Filter2(QueryContainerDescriptor<NFTInfoIndex> f)
            => f.Bool(b => b.Must(mustQuery2));

        var result = await _nftInfoIndexRepository.GetListAsync(Filter2, skip: 0, limit: 1);
        var nftInfoIndex = result?.Item2?.FirstOrDefault();
        nftInfoIndex.ShouldNotBeNull();
        nftInfoIndex.Issuer.ShouldBe(Address.FromPublicKey("BBB".HexToByteArray()).ToBase58());
        nftInfoIndex.IssueManagerSet.ShouldContain(Address.FromPublicKey("AAA".HexToByteArray()).ToBase58());
        nftInfoIndex.IssueManagerSet.ShouldContain(Address.FromPublicKey("CCC".HexToByteArray()).ToBase58());

    }
    
    private async Task ProxyAccountCreatedAsync()
    {
        const string chainId = "AELF";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;

        var proxyAccountCreatedLogEventProcessor = GetRequiredService<ProxyAccountCreatedLogEventProcessor>();
        proxyAccountCreatedLogEventProcessor.GetContractAddress("tDVW");
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
            ProxyAccountAddress = Address.FromPublicKey("BBB".HexToByteArray()),
            ProxyAccountHash = Hash.Empty,
            ManagementAddresses = new ManagementAddressList()
            {
                Value =
                {
                    new ManagementAddress
                    {
                        Address = Address.FromPublicKey("AAA".HexToByteArray())
                    },
                    new ManagementAddress
                    {
                        Address = Address.FromPublicKey("CCC".HexToByteArray())
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
            IdGenerateHelper.GetProxyAccountIndexId(Address.FromPublicKey("BBB".HexToByteArray()).ToBase58());
        var proxyAccountIndex =
            await _proxyAccountIndexRepository.GetFromBlockStateSetAsync(proxyAccountIndexId, chainId);
        proxyAccountIndex.Id.ShouldBe(proxyAccountIndexId);
        proxyAccountIndex.ProxyAccountAddress.ShouldBe(Address.FromPublicKey("BBB".HexToByteArray()).ToBase58());
        proxyAccountIndex.ManagersSet.ShouldContain(Address.FromPublicKey("AAA".HexToByteArray()).ToBase58());
        proxyAccountIndex.ManagersSet.ShouldContain(Address.FromPublicKey("CCC".HexToByteArray()).ToBase58());
    }

    [Fact]
    public async Task ProxyAccountManagementAddressCreated_Before_Update_ManagerAddress()
    {
        await ProxyAccountCreatedAsync();
        await Task.Delay(0);
        await NFTCollectionAddedAsync();
        await CheckNFTCollectionManageAddressUpdate();
        await NFTInfoIssuedAsync();
        await CheckNFTInfoManageAddressUpdate();
    }

    [Fact]
    public async Task ProxyAccountManagementAddressCreated_After_Update_ManagerAddress()
    {
        await NFTCollectionAddedAsync();
        await NFTInfoIssuedAsync();
        await CheckNFTCollectionManageAddressKeepDefault();
        await CheckNFTInfoManageAddressKeepDefault();
        await Task.Delay(0);
        await ProxyAccountCreatedAsync();
        await CheckNFTCollectionManageAddressUpdate();
        await CheckNFTInfoManageAddressUpdate();
    }

    [Fact]
    public async Task ProxyAccountManagementAddressAdded_Success()
    {
        await ProxyAccountManagementAddressCreated_Before_Update_ManagerAddress();
        const string chainId = "AELF";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;

        var proxyAccountManagementAddressAddedLogEventProcessor = GetRequiredService<ProxyAccountManagementAddressAddedLogEventProcessor>();
        proxyAccountManagementAddressAddedLogEventProcessor.GetContractAddress("tDVW");
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);
        var proxyAccountManagementAddressAdded = new ProxyAccountManagementAddressAdded()
        {
            ProxyAccountAddress = Address.FromPublicKey("BBB".HexToByteArray()),
            ProxyAccountHash = Hash.Empty,
            ManagementAddress = new ManagementAddress
            {
                Address = Address.FromPublicKey("DDD".HexToByteArray())
            }
        };

        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(proxyAccountManagementAddressAdded.ToLogEvent());
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

        await proxyAccountManagementAddressAddedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(0);
        var proxyAccountIndexId =
            IdGenerateHelper.GetProxyAccountIndexId(Address.FromPublicKey("BBB".HexToByteArray()).ToBase58());
        var proxyAccountIndex =
            await _proxyAccountIndexRepository.GetFromBlockStateSetAsync(proxyAccountIndexId, chainId);
        proxyAccountIndex.Id.ShouldBe(proxyAccountIndexId);
        proxyAccountIndex.ProxyAccountAddress.ShouldBe(Address.FromPublicKey("BBB".HexToByteArray()).ToBase58());
        proxyAccountIndex.ManagersSet.ShouldContain(Address.FromPublicKey("DDD".HexToByteArray()).ToBase58());
    }
    
    [Fact]
    public async Task ProxyAccountManagementAddressRemoved_Success()
    {
        await ProxyAccountManagementAddressCreated_Before_Update_ManagerAddress();
        const string chainId = "AELF";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;

        var proxyAccountManagementAddressRemovedLogEventProcessor = GetRequiredService<ProxyAccountManagementAddressRemovedLogEventProcessor>();
        proxyAccountManagementAddressRemovedLogEventProcessor.GetContractAddress("tDVW");
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);
        var proxyAccountManagementAddressRemoved = new ProxyAccountManagementAddressRemoved()
        {
            ProxyAccountAddress = Address.FromPublicKey("BBB".HexToByteArray()),
            ProxyAccountHash = Hash.Empty,
            ManagementAddress = new ManagementAddress
            {
                Address = Address.FromPublicKey("CCC".HexToByteArray())
            }
        };

        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(proxyAccountManagementAddressRemoved.ToLogEvent());
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

        await proxyAccountManagementAddressRemovedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(0);
        var proxyAccountIndexId =
            IdGenerateHelper.GetProxyAccountIndexId(Address.FromPublicKey("BBB".HexToByteArray()).ToBase58());
        var proxyAccountIndex =
            await _proxyAccountIndexRepository.GetFromBlockStateSetAsync(proxyAccountIndexId, chainId);
        proxyAccountIndex.Id.ShouldBe(proxyAccountIndexId);
        proxyAccountIndex.ProxyAccountAddress.ShouldBe(Address.FromPublicKey("BBB".HexToByteArray()).ToBase58());
        proxyAccountIndex.ManagersSet.ShouldNotContain(Address.FromPublicKey("CCC".HexToByteArray()).ToBase58());
        proxyAccountIndex.ManagersSet.ShouldContain(Address.FromPublicKey("AAA".HexToByteArray()).ToBase58());
    }
    
    [Fact]
    public async Task ProxyAccountManagementAddressReset_Success()
    {
        await ProxyAccountManagementAddressCreated_Before_Update_ManagerAddress();
        const string chainId = "AELF";
        const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
        const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
        const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
        const long blockHeight = 100;

        var proxyAccountManagementAddressResetLogEventProcessor = GetRequiredService<ProxyAccountManagementAddressResetLogEventProcessor>();
        proxyAccountManagementAddressResetLogEventProcessor.GetContractAddress("tDVW");
        var blockStateSet = new BlockStateSet<LogEventInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash,
        };
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);
        var proxyAccountManagementAddressReset = new ProxyAccountManagementAddressReset()
        {
            ProxyAccountAddress = Address.FromPublicKey("BBB".HexToByteArray()),
            ProxyAccountHash = Hash.Empty,
            ManagementAddresses = new ManagementAddressList()
            {
                Value =
                {
                    new ManagementAddress
                    {
                        Address = Address.FromPublicKey("EEE".HexToByteArray())
                    },
                    new ManagementAddress
                    {
                        Address = Address.FromPublicKey("FFF".HexToByteArray())
                    }
                }
            },
        };

        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(proxyAccountManagementAddressReset.ToLogEvent());
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

        await proxyAccountManagementAddressResetLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);
        await BlockStateSetSaveDataAsync<LogEventInfo>(blockStateSetKey);
        await Task.Delay(0);
        var proxyAccountIndexId =
            IdGenerateHelper.GetProxyAccountIndexId(Address.FromPublicKey("BBB".HexToByteArray()).ToBase58());
        var proxyAccountIndex =
            await _proxyAccountIndexRepository.GetFromBlockStateSetAsync(proxyAccountIndexId, chainId);
        proxyAccountIndex.Id.ShouldBe(proxyAccountIndexId);
        proxyAccountIndex.ProxyAccountAddress.ShouldBe(Address.FromPublicKey("BBB".HexToByteArray()).ToBase58());
        proxyAccountIndex.ManagersSet.ShouldNotContain(Address.FromPublicKey("CCC".HexToByteArray()).ToBase58());
        proxyAccountIndex.ManagersSet.ShouldNotContain(Address.FromPublicKey("AAA".HexToByteArray()).ToBase58());
        proxyAccountIndex.ManagersSet.ShouldContain(Address.FromPublicKey("EEE".HexToByteArray()).ToBase58());
        proxyAccountIndex.ManagersSet.ShouldContain(Address.FromPublicKey("FFF".HexToByteArray()).ToBase58());
    }
}