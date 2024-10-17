using AElf.Standards.ACS0;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class ContractDeployedProcessor: AElfLogEventProcessorBase<ContractDeployed,LogEventInfo>
{
    private readonly IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> _tokenInfoIndexRepository;
    private readonly InitialInfoOptions _initialInfoOptions;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IObjectMapper _objectMapper;

    public ContractDeployedProcessor(ILogger<ContractDeployedProcessor> logger,
        IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> tokenInfoIndexRepository,
        IOptionsSnapshot<InitialInfoOptions> initialInfoOptions, IObjectMapper objectMapper,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions) :
        base(logger)
    {
        _tokenInfoIndexRepository = tokenInfoIndexRepository;
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _initialInfoOptions = initialInfoOptions.Value;
    }
    
    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c=>c.ChainId == chainId).GenesisContractAddress;
    }

    protected override async Task HandleEventAsync(ContractDeployed eventValue, LogEventContext context)
    {
        if (eventValue.Address.ToBase58() != _contractInfoOptions.ContractInfos.First(c=>c.ChainId == context.ChainId).NFTMarketContractAddress) return;
        
        var tokenInfoList = _initialInfoOptions.TokenInfoList.Where(n => n.ChainId == context.ChainId).ToList();
        foreach (var tokenInfo in tokenInfoList)
        {
            var tokenInfoIndex = _objectMapper.Map<TokenInfo, TokenInfoIndex>(tokenInfo);
            tokenInfoIndex.Id = IdGenerateHelper.GetId(tokenInfo.ChainId, tokenInfo.Symbol);
            tokenInfoIndex.BlockHash = context.BlockHash;
            tokenInfoIndex.BlockHeight = context.BlockHeight;
            tokenInfoIndex.PreviousBlockHash = context.PreviousBlockHash;
            await _tokenInfoIndexRepository.AddOrUpdateAsync(tokenInfoIndex);
        }
    }
}