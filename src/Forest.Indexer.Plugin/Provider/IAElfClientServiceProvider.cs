using AeFinder.Sdk;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ProxyAccountContract;
using AElf.Types;
using Forest.Contracts.SymbolRegistrar;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace Forest.Indexer.Plugin.Processors;

public interface IAElfClientServiceProvider
{
    Task<SpecialSeed> GetSpecialSeedAsync(string chainId, string symbol, string contractAddress);

    Task<StringValue> GetSeedImageUrlPrefixAsync(string chainId, string contractAddress);
    
    Task<GetSeedsPriceOutput> GetSeedsPriceAsync(string chainId, string contractAddress);

    Task<AElf.Contracts.MultiToken.TokenInfo> GetTokenInfoAsync(string chainId, string contractAddress, string tokenSymbol);

    Task<ProxyAccount> GetProxyAccountByProxyAccountAddressAsync(string chainId, string contractAddress,
        Address proxyAccountAddress);
}

public class AElfClientServiceProvider : IAElfClientServiceProvider, ISingletonDependency
{
    private readonly IBlockChainService _blockChainService;
    private const string MethodGetSpecialSeed = "GetSpecialSeed";
    private const string MethodGetSeedImageUrlPrefix = "GetSeedImageUrlPrefix";
    private const string MethodGetSeedsPrice = "GetSeedsPrice";
    private const string MethodGetTokenInfo = "GetTokenInfo";
    private const string MethodGetProxyAccountByProxyAccountAddress = "GetProxyAccountByProxyAccountAddress";
    
    public AElfClientServiceProvider(IBlockChainService blockChainService)
    {
        _blockChainService = blockChainService;
    }

    public async Task<SpecialSeed> GetSpecialSeedAsync(string chainId, string symbol, string contractAddress)
    {
        var paramGetSpecialSeed = new StringValue()
        {
            Value = symbol
        };
        return await _blockChainService.ViewContractAsync<SpecialSeed>(chainId, contractAddress, MethodGetSpecialSeed,
            paramGetSpecialSeed);
    }

    public async Task<StringValue> GetSeedImageUrlPrefixAsync(string chainId, string contractAddress)
    {
        return await _blockChainService.ViewContractAsync<StringValue>(chainId, contractAddress, MethodGetSeedImageUrlPrefix,
            new Empty());
    }

    public async Task<GetSeedsPriceOutput> GetSeedsPriceAsync(string chainId, string contractAddress)
    {
        return await _blockChainService.ViewContractAsync<GetSeedsPriceOutput>(chainId, contractAddress, MethodGetSeedsPrice,
            new Empty());
    }

    public async Task<AElf.Contracts.MultiToken.TokenInfo> GetTokenInfoAsync(string chainId, string contractAddress,
        string tokenSymbol)
    {
        var getTokenInfoInput = new GetTokenInfoInput()
        {
            Symbol = tokenSymbol
        };
        return await _blockChainService.ViewContractAsync<AElf.Contracts.MultiToken.TokenInfo>(chainId, contractAddress,
            MethodGetTokenInfo,
            getTokenInfoInput);
    }

    public async Task<ProxyAccount> GetProxyAccountByProxyAccountAddressAsync(string chainId, string contractAddress,
        Address proxyAccountAddress)
    {
        return await _blockChainService.ViewContractAsync<ProxyAccount>(chainId, contractAddress,
            MethodGetProxyAccountByProxyAccountAddress,
            proxyAccountAddress );
    }
}