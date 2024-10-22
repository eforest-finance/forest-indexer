using AeFinder.Sdk;
using AeFinder.Sdk.Processor;
using Forest.Indexer.Plugin.Entities;
using Nest;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public interface INFTOfferProvider
{
    public Task UpdateOfferRealQualityAsync(string symbol, long balance, string offerFrom, LogEventContext context);
    
    public Task<List<OfferInfoIndex>> GetEffectiveNftOfferInfosAsync(string bizId, string excludeOfferId);

    public Task<int> UpdateOfferNumAsync(string symbol, string offerFrom, int change, LogEventContext context);
    public Task<int> GetOfferNumAsync(string offerFrom, string chainId);

    public Task<bool> NeedRecordBalance(string symbol, string offerFrom, string chainId);
}

public class NFTOfferProvider : INFTOfferProvider, ISingletonDependency
{
    private readonly IObjectMapper _objectMapper;
    private readonly IReadOnlyRepository<OfferInfoIndex> _nftOfferIndexRepository;
    private readonly IReadOnlyRepository<TokenInfoIndex> _tokenIndexRepository;

    private readonly IReadOnlyRepository<UserNFTOfferNumIndex>
        _userNFTOfferNumIndexRepository;

    public NFTOfferProvider(
        IObjectMapper objectMapper,
        IReadOnlyRepository<OfferInfoIndex> nftOfferIndexRepository,
        IReadOnlyRepository<TokenInfoIndex> tokenIndexRepository,
        IReadOnlyRepository<UserNFTOfferNumIndex> userNFTOfferNumIndexRepository
    )
    {
        _objectMapper = objectMapper;
        _nftOfferIndexRepository = nftOfferIndexRepository;
        _tokenIndexRepository = tokenIndexRepository;
        _userNFTOfferNumIndexRepository = userNFTOfferNumIndexRepository;
    }


    public async Task UpdateOfferRealQualityAsync(string symbol, long balance, string offerFrom,
        LogEventContext context)
    {
        return;
    }
    
    public async Task<List<OfferInfoIndex>> GetEffectiveNftOfferInfosAsync(string bizId, string excludeOfferId)
    {
        var queryable = await _nftOfferIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime) > long.Parse(DateTime.UtcNow.ToString("O") ));
        queryable = queryable.Where(index => index.BizInfoId == bizId);
        queryable = queryable.Where(index => index.RealQuantity > 0);

        if (!excludeOfferId.IsNullOrEmpty())
        {
            queryable = queryable.Where(index => index.Id != excludeOfferId);
        }

        var result = queryable.Skip(0).OrderByDescending(k => k.Price).ToList();
        return result ?? new List<OfferInfoIndex>();
    }

    public async Task<int> UpdateOfferNumAsync(string symbol, string offerFrom, int change, LogEventContext context)
    {
        // var offerNumId = IdGenerateHelper.GetOfferNumId(context.ChainId, offerFrom);
        // var nftOfferNumIndex =
        //     await _userNFTOfferNumIndexRepository.GetFromBlockStateSetAsync(offerNumId, context.ChainId);
        // if (nftOfferNumIndex == null)
        // {
        //     nftOfferNumIndex = new UserNFTOfferNumIndex()
        //     {
        //         Id = offerNumId,
        //         Address = offerFrom,
        //         OfferNum = change
        //     };
        // }
        // else
        // {
        //     nftOfferNumIndex.OfferNum += change;
        //     // deal history data
        //     if (nftOfferNumIndex.OfferNum < 0)
        //     {
        //         _logger.LogWarning(
        //             "UpdateOfferNumAsync has history Address {Address} symbol {Symbol} OfferNum {OfferNum}", offerFrom,
        //             symbol, nftOfferNumIndex.OfferNum);
        //         nftOfferNumIndex.OfferNum = 0;
        //     }
        // }
        //
        // _logger.LogInformation("UpdateOfferNumAsync Address {Address} symbol {Symbol} OfferNum {OfferNum}", offerFrom,
        //     symbol, nftOfferNumIndex.OfferNum);
        // _objectMapper.Map(context, nftOfferNumIndex);
        // await _userNFTOfferNumIndexRepository.AddOrUpdateAsync(nftOfferNumIndex);
        // return nftOfferNumIndex.OfferNum; todo v2
        return 0;
    }

    public async Task<int> GetOfferNumAsync(string offerFrom, string chainId)
    {
        var offerNumId = IdGenerateHelper.GetOfferNumId(chainId, offerFrom);
        // var nftOfferNumIndex =
        //     await _userNFTOfferNumIndexRepository.GetFromBlockStateSetAsync(offerNumId, chainId); todo v2
        var nftOfferNumIndex = new UserNFTOfferNumIndex();// delete todo v2
        if (nftOfferNumIndex == null)
        {
            return 0;
        }

        return nftOfferNumIndex.OfferNum;
    }

    public async Task<bool> NeedRecordBalance(string symbol, string offerFrom, string chainId)
    {
        if (!SymbolHelper.CheckSymbolIsELF(symbol))
        {
            return true;
        }

        // if (_needRecordBalanceOptions.AddressList.Contains(offerFrom))
        // {
        //     return true;
        // } //todo v2

        var num = await GetOfferNumAsync(offerFrom, chainId);
        if (num > 0)
        {
            return true;
        }

        return false;
    }

    private async Task<Dictionary<string, OfferInfoIndex>> TransferToDicAsync(
        OfferInfoIndex[] nftOfferIndices)
    {
        if (nftOfferIndices == null || nftOfferIndices.Length == 0)
            return new Dictionary<string, OfferInfoIndex>();

        nftOfferIndices = nftOfferIndices.Where(x => x != null).ToArray();

        return nftOfferIndices == null || nftOfferIndices.Length == 0
            ? new Dictionary<string, OfferInfoIndex>()
            : nftOfferIndices.ToDictionary(item => item.BizInfoId);
    }
    
    
}