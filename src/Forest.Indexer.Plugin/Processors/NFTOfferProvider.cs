using AeFinder.Sdk.Processor;
// using AElfIndexer.Client;
// using AElfIndexer.Client.Handlers;
// using AElfIndexer.Grains.State.Client; todo v2
using Forest.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Nest;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public interface INFTOfferProvider
{
    public Task<Dictionary<string, OfferInfoIndex>> QueryLatestNFTOfferByNFTIdsAsync(List<string> nftInfoIds,
        string noOfferId);

    public Task<OfferInfoIndex> QueryMaxPriceExcludeSpecialOfferIdAsync(string bizId, string excludeOfferId);

    public Task UpdateOfferRealQualityAsync(string symbol, long balance, string offerFrom, LogEventContext context);
    
    public Task<List<OfferInfoIndex>> GetEffectiveNftOfferInfosAsync(string bizId, string excludeOfferId);

    public Task<int> UpdateOfferNumAsync(string symbol, string offerFrom, int change, LogEventContext context);
    public Task<int> GetOfferNumAsync(string offerFrom, string chainId);

    public Task<bool> NeedRecordBalance(string symbol, string offerFrom, string chainId);
}

public class NFTOfferProvider : INFTOfferProvider, ISingletonDependency
{
    private readonly ILogger<NFTOfferProvider> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IReadOnlyRepository<OfferInfoIndex> _nftOfferIndexRepository;
    private readonly IReadOnlyRepository<TokenInfoIndex> _tokenIndexRepository;

    private readonly IReadOnlyRepository<UserNFTOfferNumIndex>
        _userNFTOfferNumIndexRepository;

    public NFTOfferProvider(ILogger<NFTOfferProvider> logger,
        IObjectMapper objectMapper,
        IReadOnlyRepository<OfferInfoIndex> nftOfferIndexRepository,
        IReadOnlyRepository<TokenInfoIndex> tokenIndexRepository,
        IReadOnlyRepository<UserNFTOfferNumIndex> userNFTOfferNumIndexRepository
    )
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _nftOfferIndexRepository = nftOfferIndexRepository;
        _tokenIndexRepository = tokenIndexRepository;
        _userNFTOfferNumIndexRepository = userNFTOfferNumIndexRepository;
    }

    public async Task<Dictionary<string, OfferInfoIndex>> QueryLatestNFTOfferByNFTIdsAsync(
        List<string> nftInfoIds, string noOfferId)
    {
        if (nftInfoIds == null) return new Dictionary<string, OfferInfoIndex>();
        var queryLatestNFTOfferList = new List<Task<OfferInfoIndex>>();
        foreach (string nftInfoId in nftInfoIds)
        {
            queryLatestNFTOfferList.Add(QueryLatestNFTOfferByNFTIdAsync(nftInfoId, noOfferId));
        }

        var latestNFTOfferList = await Task.WhenAll(queryLatestNFTOfferList);
        return await TransferToDicAsync(latestNFTOfferList);
    }

    public async Task<OfferInfoIndex> QueryMaxPriceExcludeSpecialOfferIdAsync(string bizId, string excludeOfferId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<OfferInfoIndex>, QueryContainer>>();

        // mustQuery.Add(q => q.TermRange(i
        //     => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime))
        //         .GreaterThan(DateTime.UtcNow.ToString("O")))); todo v2
        mustQuery.Add(q => q.Term(i => i.Field(index => index.BizInfoId).Value(bizId)));

        var mustNotQuery = new List<Func<QueryContainerDescriptor<OfferInfoIndex>, QueryContainer>>();

        if (!excludeOfferId.IsNullOrEmpty())
        {
            mustNotQuery.Add(q => q.Term(i => i.Field(index => index.Id).Value(excludeOfferId)));
        }

        QueryContainer Filter(QueryContainerDescriptor<OfferInfoIndex> f) => f.Bool(b => b.Must(mustQuery)
            .MustNot(mustNotQuery));

        // var result = await _nftOfferIndexRepository.GetListAsync(Filter, sortExp: k => k.Price,
        //     sortType: SortOrder.Descending, skip: 0, limit: 1);
        // return result?.Item2?.FirstOrDefault(); todo v2
        return null;
    }


    public async Task UpdateOfferRealQualityAsync(string symbol, long balance, string offerFrom,
        LogEventContext context)
    {
        return;
    }
    
    public async Task<List<OfferInfoIndex>> GetEffectiveNftOfferInfosAsync(string bizId, string excludeOfferId)
    {
        // var mustQuery = new List<Func<QueryContainerDescriptor<OfferInfoIndex>, QueryContainer>>();
        //
        // mustQuery.Add(q => q.TermRange(i
        //     => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime))
        //         .GreaterThan(DateTime.UtcNow.ToString("O"))));
        // mustQuery.Add(q => q.Term(i
        //     => i.Field(index => index.BizInfoId).Value(bizId)));
        //
        // mustQuery.Add(q => q.TermRange(i
        //     => i.Field(index => index.RealQuantity).GreaterThan(0.ToString())));
        //
        // var mustNotQuery = new List<Func<QueryContainerDescriptor<OfferInfoIndex>, QueryContainer>>();
        //
        // if (!excludeOfferId.IsNullOrEmpty())
        // {
        //     mustNotQuery.Add(q => q.Term(i => i.Field(index => index.Id).Value(excludeOfferId)));
        // }
        //
        // QueryContainer Filter(QueryContainerDescriptor<OfferInfoIndex> f) => f.Bool(b => b.Must(mustQuery)
        //     .MustNot(mustNotQuery));
        //
        // var result = await _nftOfferIndexRepository.GetListAsync(Filter, sortExp: k => k.Price,
        //     sortType: SortOrder.Descending, skip: 0);
        //
        // return result.Item2 ?? new List<OfferInfoIndex>(); todo v2
        return null;
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

    private async Task<OfferInfoIndex> QueryLatestNFTOfferByNFTIdAsync(string nftInfoId, string noListingId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<OfferInfoIndex>, QueryContainer>>();
        var mustNotQuery = new List<Func<QueryContainerDescriptor<OfferInfoIndex>, QueryContainer>>();
        // mustQuery.Add(q => q.TermRange(i
        //     => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime))
        //         .GreaterThan(DateTime.UtcNow.ToString("O")))); todo v2
        mustQuery.Add(q => q.Term(i => i.Field(index => index.BizInfoId).Value(nftInfoId)));
        if (!noListingId.IsNullOrEmpty())
        {
            mustNotQuery.Add(q => q.Term(i => i.Field(index => index.Id).Value(noListingId)));
        }

        QueryContainer Filter(QueryContainerDescriptor<OfferInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery).MustNot(mustNotQuery));

        // var result = await _nftOfferIndexRepository.GetListAsync(Filter, sortExp: k => k.BlockHeight,
        //     sortType: SortOrder.Descending, skip: 0, limit: 1);
        // return result?.Item2?.FirstOrDefault(); todo v2
        return null;
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