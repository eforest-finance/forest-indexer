using AeFinder.Sdk;
using AElf;
using Forest.Indexer.Plugin.Entities;
using GraphQL;
using IdentityServer4.Extensions;
using Microsoft.IdentityModel.Tokens;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.GraphQL;

public partial class Query
{
    [Name("symbolMarketTokens")]
    public static async Task<SymbolMarkerTokenPageResultDto> SymbolMarketTokens(
        [FromServices]
        IReadOnlyRepository<SeedSymbolMarketTokenIndex> symbolMarketTokenIndexRepository,
        [FromServices] IObjectMapper objectMapper,
        GetSymbolMarketTokensInput dto)
    {
        if (dto == null || dto.Address.IsNullOrEmpty())
        {
            return new SymbolMarkerTokenPageResultDto()
            {
                TotalRecordCount = 0,
                Data = new List<SymbolMarkerTokenDto>()
            };
        }
        var queryable = await symbolMarketTokenIndexRepository.GetQueryableAsync();
        //var mustQuery = new List<Func<QueryContainerDescriptor<SeedSymbolMarketTokenIndex>, QueryContainer>>();
        //todo V2, need test q.Exists(exists => exists.Field(f => f.Symbol))
        /*mustQuery.Add(q => q.Exists(exists => exists
                               .Field(f => f.Symbol))
                           && q.Term(i => i
                               .Field(f => f.SameChainFlag).Value(true)));*/ 
        //queryable = queryable.Where(f => f.SameChainFlag);
        
        var address1 = dto.Address.Count >= 1 ? dto.Address[0] : "";
        var address2 = dto.Address.Count >= 2 ? dto.Address[1] : "";

        if (string.IsNullOrEmpty(address2))
        {
            queryable = queryable.Where(f =>
                f.IssueToSet.Any(item => item == address1) 
                || f.RandomOwnerManager == address1 
                || f.RandomIssueManager == address1);
        }
        else
        {
            queryable = queryable.Where(f =>
                f.IssueToSet.Any(item => item == address1) 
                || f.RandomOwnerManager == address1 
                || f.RandomIssueManager == address1
                ||f.IssueToSet.Any(item => item == address2) 
                || f.RandomOwnerManager == address2 
                || f.RandomIssueManager == address2);
        }
        

        var result = queryable.Skip(dto.SkipCount).Take(dto.MaxResultCount).OrderByDescending(k => k.CreateTime)
            .ToList();
        if (result.IsNullOrEmpty())
        {
            return new SymbolMarkerTokenPageResultDto()
            {
                TotalRecordCount = 0,
                Data = new List<SymbolMarkerTokenDto>()
            };
        }

        var dataList = objectMapper.Map<List<SeedSymbolMarketTokenIndex>, List<SymbolMarkerTokenDto>>(result);
        var pageResult = new SymbolMarkerTokenPageResultDto
        {
            TotalRecordCount = result.Count,
            Data = dataList
        };
        return pageResult;
    }

    [Name("symbolMarketTokenIssuer")]
    public static async Task<SymbolMarketTokenIssuerDto> SymbolMarketTokenIssuer(
        [FromServices]
        IReadOnlyRepository<SeedSymbolMarketTokenIndex> symbolMarketTokenIndexRepository,
        GetSymbolMarketTokenIssuerInput dto)
    {
        if (dto == null)
            return new SymbolMarketTokenIssuerDto()
            {
                SymbolMarketTokenIssuer = ""
            };
        var queryable = await symbolMarketTokenIndexRepository.GetQueryableAsync();
        var issueChainId = ChainHelper.ConvertChainIdToBase58(dto.IssueChainId);
        var SymbolMarketTokenId = IdGenerateHelper.GetSymbolMarketTokenId(issueChainId, dto.TokenSymbol);
        queryable = queryable.Where(f => f.Id == SymbolMarketTokenId);

        var result = queryable.ToList();
        return new SymbolMarketTokenIssuerDto()
        {
            SymbolMarketTokenIssuer = result.IsNullOrEmpty() ? "" : result.FirstOrDefault().Issuer
        };
    }
    
    [Name("symbolMarketTokenExist")]
    public static async Task<SymbolMarketTokenExistDto> SymbolMarketTokenExist(
        [FromServices]
        IReadOnlyRepository<SeedSymbolMarketTokenIndex> symbolMarketTokenIndexRepository,
        [FromServices] IObjectMapper objectMapper,
        GetSymbolMarketTokenExistInput dto)
    {
        var queryable = await symbolMarketTokenIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(f => f.IssueChain == dto.IssueChainId);
        queryable = queryable.Where(f => f.Symbol == dto.TokenSymbol);
        var result = queryable.OrderByDescending(k => k.CreateTime).FirstOrDefault();

        if (result == null)
        {
            return new SymbolMarketTokenExistDto();
        }
        
        var symbolMarketTokenDto = objectMapper.Map<SeedSymbolMarketTokenIndex, SymbolMarketTokenExistDto>(result);
        return symbolMarketTokenDto;
    }
}