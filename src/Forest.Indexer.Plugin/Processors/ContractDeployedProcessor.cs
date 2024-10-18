using AeFinder.Sdk.Processor;
using AElf.Standards.ACS0;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public class ContractDeployedProcessor: LogEventProcessorBase<ContractDeployed>
{
    private readonly IObjectMapper _objectMapper;

    public ContractDeployedProcessor(
        IObjectMapper objectMapper) 
    {
        _objectMapper = objectMapper;
    }
    
    public override string GetContractAddress(string chainId)
    {
        return ContractInfoHelper.GetGenesisContractAddress(chainId);
    }

    public override async Task ProcessAsync(ContractDeployed eventValue, LogEventContext context)
    {
        if (eventValue.Address.ToBase58() != ContractInfoHelper.GetNFTForestContractAddress(context.ChainId)) return;
        
        var tokenInfoList = TokenInfoListConstants.TokenInfoList.Where(n => n.ChainId == context.ChainId).ToList();
        foreach (var tokenInfo in tokenInfoList)
        {
            var tokenInfoIndex = _objectMapper.Map<TokenInfo, TokenInfoIndex>(tokenInfo);
            tokenInfoIndex.Id = IdGenerateHelper.GetId(tokenInfo.ChainId, tokenInfo.Symbol);
            tokenInfoIndex.BlockHash = context.Block.BlockHash;
            tokenInfoIndex.BlockHeight = context.Block.BlockHeight;
            tokenInfoIndex.PreviousBlockHash = context.Block.PreviousBlockHash;
            await SaveEntityAsync(tokenInfoIndex);

        }
    }
}