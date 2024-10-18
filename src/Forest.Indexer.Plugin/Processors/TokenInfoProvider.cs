using AeFinder.Sdk.Processor;
using AElf.Contracts.MultiToken;
using Forest.Indexer.Plugin.Entities;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public interface ITokenInfoProvider
{
    public Task TokenInfoIndexCreateAsync(TokenCreated eventValue, LogEventContext context);
}

public class TokenInfoProvider : ITokenInfoProvider, ISingletonDependency
{
    private readonly IObjectMapper _objectMapper;
    
    public TokenInfoProvider(
        IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }
    
    public async Task TokenInfoIndexCreateAsync(TokenCreated eventValue, LogEventContext context)
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
        // await _tokenIndexRepository.AddOrUpdateAsync(tokenInfoIndex); todo v2
    }
}