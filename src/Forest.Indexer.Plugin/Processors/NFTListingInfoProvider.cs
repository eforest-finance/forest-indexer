using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Nest;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using DateTime = System.DateTime;

namespace Forest.Indexer.Plugin.Processors;

public interface INFTListingInfoProvider
{
    public Task<Dictionary<string, NFTListingInfoIndex>> QueryLatestNFTListingInfoByNFTIdsAsync(List<string> nftInfoIds,
        string noListingId);

    public Task<Dictionary<string, NFTListingInfoIndex>> QueryOtherAddressNFTListingInfoByNFTIdsAsync(
        List<string> nftInfoIds, string noListingOwner, string noListingId);

    public Task<NFTListingInfoIndex> QueryMinPriceExcludeSpecialListingIdAsync(string bizId, string excludeListingId);

    public Task UpdateListingInfoRealQualityAsync(string symbol, long balance, string ownerAddress,
        LogEventContext context);
    
    Task<List<NFTListingInfoIndex>> GetEffectiveNftListingInfos(string nftInfoId, HashSet<string> excludeListingIds);
}

public class NFTListingInfoProvider : INFTListingInfoProvider, ISingletonDependency
{
    private readonly ILogger<NFTListingInfoIndex> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo> _listedNFTIndexRepository;
    private const int MaxQuerySize = 10000;
    private const int MaxQueryCount = 5;


    public NFTListingInfoProvider(ILogger<NFTListingInfoIndex> logger,
        IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo> listedNFTIndexRepository)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _listedNFTIndexRepository = listedNFTIndexRepository;
    }

    public async Task<Dictionary<string, NFTListingInfoIndex>> QueryLatestNFTListingInfoByNFTIdsAsync(
        List<string> nftInfoIds, string noListingId)
    {
        if (nftInfoIds == null) return new Dictionary<string, NFTListingInfoIndex>();
        var queryLatestList = new List<Task<NFTListingInfoIndex>>();
        foreach (string nftInfoId in nftInfoIds)
        {
            queryLatestList.Add(QueryLatestWhiteListByNFTIdAsync(nftInfoId,noListingId));
        }

        var latestList = await Task.WhenAll(queryLatestList);
        return await TransferToDicAsync(latestList);
    }

    public async Task<Dictionary<string, NFTListingInfoIndex>> QueryOtherAddressNFTListingInfoByNFTIdsAsync(
        List<string> nftInfoIds, string noListingOwner, string noListingId)
    {
        if (nftInfoIds == null) return new Dictionary<string, NFTListingInfoIndex>();

        var queryOtherList = new List<Task<NFTListingInfoIndex>>();
        foreach (string nftInfoId in nftInfoIds)
        {
            queryOtherList.Add(QueryOtherWhiteListExistAsync(nftInfoId, noListingOwner, noListingId));
        }

        var otherList = await Task.WhenAll(queryOtherList);
        return await TransferToDicAsync(otherList);
    }

    public async Task<NFTListingInfoIndex> QueryMinPriceExcludeSpecialListingIdAsync(string bizId, string excludeListingId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();

        mustQuery.Add(q => q.TermRange(i
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime)).GreaterThan(DateTime.UtcNow.ToString("O"))));
        mustQuery.Add(q=>q.Term(i=>i.Field(index=>index.NftInfoId).Value(bizId)));
        
        var mustNotQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();

        if (!excludeListingId.IsNullOrEmpty())
        {
            mustNotQuery.Add(q=>q.Term(i=>i.Field(index=>index.Id).Value(excludeListingId)));
        }
       
        QueryContainer Filter(QueryContainerDescriptor<NFTListingInfoIndex> f) => f.Bool(b => b.Must(mustQuery)
            .MustNot(mustNotQuery));
        var result = await _listedNFTIndexRepository.GetListAsync(Filter, sortExp: k => k.Prices,
            sortType: SortOrder.Ascending, skip: 0, limit: 1);
        return result?.Item2?.FirstOrDefault();
    }
    private async Task<NFTListingInfoIndex> QueryLatestWhiteListByNFTIdAsync(string nftInfoId, string noListingId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();
        var mustNotQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.TermRange(i
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime)).GreaterThan(DateTime.UtcNow.ToString("O"))));
        mustQuery.Add(q=>q.Term(i=>i.Field(index=>index.NftInfoId).Value(nftInfoId)));
        if (!noListingId.IsNullOrEmpty())
        {
            mustNotQuery.Add(q=>q.Term(i=>i.Field(index=>index.Id).Value(noListingId)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<NFTListingInfoIndex> f) => f.Bool(b => b.Must(mustQuery).MustNot(mustNotQuery));
        var result = await _listedNFTIndexRepository.GetListAsync(Filter, sortExp: k => k.BlockHeight,
            sortType: SortOrder.Descending, skip: 0, limit: 1);
        return result?.Item2?.FirstOrDefault();
    }

    public async Task<List<NFTListingInfoIndex>> GetEffectiveNftListingInfos(string nftInfoId, HashSet<string> excludeListingIds)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();

        mustQuery.Add(q => q.TermRange(i
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime))
                .GreaterThan(DateTime.UtcNow.ToString("O"))));
        mustQuery.Add(q => q.Term(i => i.Field(index => index.NftInfoId).Value(nftInfoId)));

        var mustNotQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();

        if (!excludeListingIds.IsNullOrEmpty())
        {
            mustNotQuery.Add(q => q.Terms(i => i.Field(index => index.Id).Terms(excludeListingIds)));
        }

        QueryContainer Filter(QueryContainerDescriptor<NFTListingInfoIndex> f) => f.Bool(b => b.Must(mustQuery)
            .MustNot(mustNotQuery));

        var result = await _listedNFTIndexRepository.GetListAsync(Filter, sortExp: k => k.Prices,
            sortType: SortOrder.Ascending, skip: 0);
        return result.Item2??new List<NFTListingInfoIndex>();
    }
    
    private async Task<NFTListingInfoIndex> QueryOtherWhiteListExistAsync(string nftInfoId,string noListingOwner ,string noListingId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();

        mustQuery.Add(q => q.TermRange(i
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime)).GreaterThan(DateTime.UtcNow.ToString("O"))));
        mustQuery.Add(q=>q.Term(i=>i.Field(index=>index.NftInfoId).Value(nftInfoId)));
        
        var mustNotQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();
        if (!noListingOwner.IsNullOrEmpty())
        {
            mustNotQuery.Add(q=>q.Term(i=>i.Field(index=>index.Owner).Value(noListingOwner)));

        }

        if (!noListingId.IsNullOrEmpty())
        {
            mustNotQuery.Add(q=>q.Term(i=>i.Field(index=>index.Id).Value(noListingId)));
        }
       
        QueryContainer Filter(QueryContainerDescriptor<NFTListingInfoIndex> f) => f.Bool(b => b.Must(mustQuery)
            .MustNot(mustNotQuery));
        var result = await _listedNFTIndexRepository.GetListAsync(Filter, sortExp: k => k.BlockHeight,
            sortType: SortOrder.Descending, skip: 0, limit: 1);
        return result?.Item2?.FirstOrDefault();
    }

    private async Task<Dictionary<string, NFTListingInfoIndex>> TransferToDicAsync(
        NFTListingInfoIndex[] nftListingInfoIndices)
    {
        if (nftListingInfoIndices == null || nftListingInfoIndices.Length == 0)
            return new Dictionary<string, NFTListingInfoIndex>();

        nftListingInfoIndices = nftListingInfoIndices.Where(x => x != null).ToArray();

        return nftListingInfoIndices == null || nftListingInfoIndices.Length == 0
            ? new Dictionary<string, NFTListingInfoIndex>()
            :
            nftListingInfoIndices.ToDictionary(item => item.NftInfoId);
    }
    
    public async Task UpdateListingInfoRealQualityAsync(string symbol, long balance, string ownerAddress, LogEventContext context)
    {
        if (SymbolHelper.CheckSymbolIsELF(symbol))
        {
            return;
        }
        var nftId = IdGenerateHelper.GetSeedSymbolId(context.ChainId, symbol);
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.TermRange(i
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime))
                .GreaterThan(DateTime.UtcNow.ToString("O"))));
        mustQuery.Add(q => q.Term(i => i.Field(index => index.NftInfoId).Value(nftId)));
        mustQuery.Add(q => q.Term(i => i.Field(index => index.Owner).Value(ownerAddress)));
        QueryContainer Filter(QueryContainerDescriptor<NFTListingInfoIndex> f) => f.Bool(b => b.Must(mustQuery));

        int skip = 0;
        var nftListings = new List<NFTListingInfoIndex>();
        int queryCount = 0;
        while (queryCount < MaxQueryCount)
        {
            var result = await _listedNFTIndexRepository.GetListAsync(Filter, skip: skip, limit: MaxQuerySize);
            if (result.Item2.IsNullOrEmpty())
            {
                break;
            }
            if(result.Item2.Count < MaxQuerySize)
            {
                nftListings.AddRange(result.Item2);
                break;
            }
            skip += MaxQuerySize;
            queryCount++;
        }

        //update RealQuantity
        foreach (var nftListingInfoIndex in nftListings)
        {
            var realNftListingInfoIndex =
                await _listedNFTIndexRepository.GetFromBlockStateSetAsync(nftListingInfoIndex.Id, context.ChainId);
            if (realNftListingInfoIndex == null) continue;
            var realQuantity = Math.Min(realNftListingInfoIndex.Quantity, balance);
            if (realQuantity != realNftListingInfoIndex.RealQuantity)
            {
                realNftListingInfoIndex.RealQuantity = realQuantity;
                _objectMapper.Map(context, realNftListingInfoIndex);
                await _listedNFTIndexRepository.AddOrUpdateAsync(realNftListingInfoIndex);
            }
        }
    }
}