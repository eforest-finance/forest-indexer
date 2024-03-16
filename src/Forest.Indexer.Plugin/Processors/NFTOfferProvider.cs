using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp.DependencyInjection;
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
    private readonly IAElfIndexerClientEntityRepository<OfferInfoIndex, LogEventInfo> _nftOfferIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> _tokenIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<UserNFTOfferNumIndex, LogEventInfo>
        _userNFTOfferNumIndexRepository;

    private readonly NeedRecordBalanceOptions _needRecordBalanceOptions;

    public NFTOfferProvider(ILogger<NFTOfferProvider> logger,
        IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<OfferInfoIndex, LogEventInfo> nftOfferIndexRepository,
        IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> tokenIndexRepository,
        IAElfIndexerClientEntityRepository<UserNFTOfferNumIndex, LogEventInfo> userNFTOfferNumIndexRepository,
        IOptionsSnapshot<NeedRecordBalanceOptions> needRecordBalanceOptions
    )
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _nftOfferIndexRepository = nftOfferIndexRepository;
        _tokenIndexRepository = tokenIndexRepository;
        _userNFTOfferNumIndexRepository = userNFTOfferNumIndexRepository;
        _needRecordBalanceOptions = needRecordBalanceOptions.Value;
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

        mustQuery.Add(q => q.TermRange(i
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime))
                .GreaterThan(DateTime.UtcNow.ToString("O"))));
        mustQuery.Add(q => q.Term(i => i.Field(index => index.BizInfoId).Value(bizId)));

        var mustNotQuery = new List<Func<QueryContainerDescriptor<OfferInfoIndex>, QueryContainer>>();

        if (!excludeOfferId.IsNullOrEmpty())
        {
            mustNotQuery.Add(q => q.Term(i => i.Field(index => index.Id).Value(excludeOfferId)));
        }

        QueryContainer Filter(QueryContainerDescriptor<OfferInfoIndex> f) => f.Bool(b => b.Must(mustQuery)
            .MustNot(mustNotQuery));

        var result = await _nftOfferIndexRepository.GetListAsync(Filter, sortExp: k => k.Price,
            sortType: SortOrder.Descending, skip: 0, limit: 1);
        return result?.Item2?.FirstOrDefault();
    }


    public async Task UpdateOfferRealQualityAsync(string symbol, long balance, string offerFrom,
        LogEventContext context)
    {
        if (context.ChainId.Equals(ForestIndexerConstants.MainChain))
        {
            return;
        }
        if (!SymbolHelper.CheckSymbolIsELF(symbol))
        {
            return;
        }
        int skip = 0;
        int queryCount;
        int limit = 1000;
        do
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<OfferInfoIndex>, QueryContainer>>();
            mustQuery.Add(q => q.TermRange(i
                => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime))
                    .GreaterThan(DateTime.UtcNow.ToString("O"))));
            mustQuery.Add(q => q.Term(i => i.Field(index => index.PurchaseToken.Symbol).Value(symbol)));
            mustQuery.Add(q => q.Term(i => i.Field(index => index.ChainId).Value(context.ChainId)));
            mustQuery.Add(q => q.Term(i => i.Field(index => index.OfferFrom).Value(offerFrom)));
            QueryContainer Filter(QueryContainerDescriptor<OfferInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
            var result = await _nftOfferIndexRepository.GetListAsync(Filter, null, sortExp: k => k.Price,
                sortType: SortOrder.Descending, limit, skip);
            if (result.Item2.IsNullOrEmpty())
            {
                break;
            }

            var tokenIndexId = IdGenerateHelper.GetId(context.ChainId, symbol);
            var tokenIndex = await _tokenIndexRepository.GetFromBlockStateSetAsync(tokenIndexId, context.ChainId);
            if (tokenIndex == null)
            {
                return;
            }

            //update RealQuantity
            foreach (var offerInfoIndex in result.Item2)
            {
                if (symbol.Equals(offerInfoIndex!.PurchaseToken.Symbol))
                {
                    var canBuyNum = Convert.ToInt64(Math.Floor(Convert.ToDecimal(balance) /
                                                               (offerInfoIndex.Price *
                                                                (decimal)Math.Pow(10,
                                                                    tokenIndex.Decimals))));
                    _logger.LogInformation(
                        "UpdateOfferRealQualityAsync  offerInfoIndex.BizSymbol {BizSymbol} canBuyNum {CanBuyNum} Quantity {Quantity} RealQuantity {RealQuantity}",
                        offerInfoIndex.BizSymbol, canBuyNum, offerInfoIndex.Quantity, offerInfoIndex.RealQuantity);
                    var realQuantity = Math.Min(offerInfoIndex.Quantity,
                        canBuyNum);
                    if (realQuantity != offerInfoIndex.RealQuantity)
                    {
                        offerInfoIndex.RealQuantity = realQuantity;
                        _objectMapper.Map(context, offerInfoIndex);
                        var research = await _nftOfferIndexRepository.GetFromBlockStateSetAsync(offerInfoIndex.Id,context.ChainId);
                        if (research == null)
                        {
                            _logger.LogInformation(
                                "UpdateOfferRealQualityAsync offerInfoIndex.Id is not exist,not update {OfferInfoIndexId}",
                                offerInfoIndex.Id);
                            continue;
                        }
                        await _nftOfferIndexRepository.AddOrUpdateAsync(offerInfoIndex);
                    }
                }
            }

            queryCount = result.Item2.Count;
            skip += limit;
        } while (queryCount == limit);
    }
    
    public async Task<List<OfferInfoIndex>> GetEffectiveNftOfferInfosAsync(string bizId, string excludeOfferId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<OfferInfoIndex>, QueryContainer>>();

        mustQuery.Add(q => q.TermRange(i
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime))
                .GreaterThan(DateTime.UtcNow.ToString("O"))));
        mustQuery.Add(q => q.Term(i
            => i.Field(index => index.BizInfoId).Value(bizId)));

        mustQuery.Add(q => q.TermRange(i
            => i.Field(index => index.RealQuantity).GreaterThan(0.ToString())));

        var mustNotQuery = new List<Func<QueryContainerDescriptor<OfferInfoIndex>, QueryContainer>>();

        if (!excludeOfferId.IsNullOrEmpty())
        {
            mustNotQuery.Add(q => q.Term(i => i.Field(index => index.Id).Value(excludeOfferId)));
        }

        QueryContainer Filter(QueryContainerDescriptor<OfferInfoIndex> f) => f.Bool(b => b.Must(mustQuery)
            .MustNot(mustNotQuery));

        var result = await _nftOfferIndexRepository.GetListAsync(Filter, sortExp: k => k.Price,
            sortType: SortOrder.Descending, skip: 0);

        return result.Item2 ?? new List<OfferInfoIndex>();
    }

    public async Task<int> UpdateOfferNumAsync(string symbol, string offerFrom, int change, LogEventContext context)
    {
        var offerNumId = IdGenerateHelper.GetOfferNumId(context.ChainId, offerFrom);
        var nftOfferNumIndex =
            await _userNFTOfferNumIndexRepository.GetFromBlockStateSetAsync(offerNumId, context.ChainId);
        if (nftOfferNumIndex == null)
        {
            nftOfferNumIndex = new UserNFTOfferNumIndex()
            {
                Id = offerNumId,
                Address = offerFrom,
                OfferNum = change
            };
        }
        else
        {
            nftOfferNumIndex.OfferNum += change;
            // deal history data
            if (nftOfferNumIndex.OfferNum < 0)
            {
                _logger.LogWarning(
                    "UpdateOfferNumAsync has history Address {Address} symbol {Symbol} OfferNum {OfferNum}", offerFrom,
                    symbol, nftOfferNumIndex.OfferNum);
                nftOfferNumIndex.OfferNum = 0;
            }
        }

        _logger.LogInformation("UpdateOfferNumAsync Address {Address} symbol {Symbol} OfferNum {OfferNum}", offerFrom,
            symbol, nftOfferNumIndex.OfferNum);
        _objectMapper.Map(context, nftOfferNumIndex);
        await _userNFTOfferNumIndexRepository.AddOrUpdateAsync(nftOfferNumIndex);
        return nftOfferNumIndex.OfferNum;
    }

    public async Task<int> GetOfferNumAsync(string offerFrom, string chainId)
    {
        var offerNumId = IdGenerateHelper.GetOfferNumId(chainId, offerFrom);
        var nftOfferNumIndex =
            await _userNFTOfferNumIndexRepository.GetFromBlockStateSetAsync(offerNumId, chainId);
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

        if (_needRecordBalanceOptions.AddressList.Contains(offerFrom))
        {
            return true;
        }

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
        mustQuery.Add(q => q.TermRange(i
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime))
                .GreaterThan(DateTime.UtcNow.ToString("O"))));
        mustQuery.Add(q => q.Term(i => i.Field(index => index.BizInfoId).Value(nftInfoId)));
        if (!noListingId.IsNullOrEmpty())
        {
            mustNotQuery.Add(q => q.Term(i => i.Field(index => index.Id).Value(noListingId)));
        }

        QueryContainer Filter(QueryContainerDescriptor<OfferInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery).MustNot(mustNotQuery));

        var result = await _nftOfferIndexRepository.GetListAsync(Filter, sortExp: k => k.BlockHeight,
            sortType: SortOrder.Descending, skip: 0, limit: 1);
        return result?.Item2?.FirstOrDefault();
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