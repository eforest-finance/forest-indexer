using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Nest;
using Volo.Abp.DependencyInjection;

namespace Forest.Indexer.Plugin.Processors.Provider;

public interface ICollectionProvider
{
    public Task<decimal> CalcCollectionFloorPriceAsync(string chainId, string symbol, decimal oriFloorPrice);
}

public class CollectionProvider : ICollectionProvider, ISingletonDependency
{
    private readonly IAElfIndexerClientEntityRepository<CollectionIndex, LogEventInfo>
        _collectionIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo>
        _symbolAuctionInfoIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo>
        _nftListingInfoIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<OfferInfoIndex, LogEventInfo>
        _nftOfferIndexRepository;

    public CollectionProvider(
        IAElfIndexerClientEntityRepository<CollectionIndex, LogEventInfo> collectionIndexRepository,
        IAElfIndexerClientEntityRepository<SymbolAuctionInfoIndex, LogEventInfo>
            symbolAuctionInfoIndexRepository,
        IAElfIndexerClientEntityRepository<NFTListingInfoIndex, LogEventInfo>
            nftListingInfoIndexRepository,
        IAElfIndexerClientEntityRepository<OfferInfoIndex, LogEventInfo>
            nftOfferIndexRepository)
    {
        _collectionIndexRepository = collectionIndexRepository;
        _symbolAuctionInfoIndexRepository = symbolAuctionInfoIndexRepository;
        _nftListingInfoIndexRepository = nftListingInfoIndexRepository;
        _nftOfferIndexRepository = nftOfferIndexRepository;
    }
    
    public async Task<decimal> CalcCollectionFloorPriceAsync(string chainId, string symbol, decimal oriFloorPrice)
    {
        //Calculate current FloorPrice
        //symbol is collection symbol.
        var symbolAuctionInfoIndex = await QueryMinPriceForSymbolAuctionInfoIndexAsync(chainId, symbol);
        decimal? auctionMinPrice =  symbolAuctionInfoIndex?.FinishPrice.Amount;
        if (auctionMinPrice !=null && auctionMinPrice.Value > 0)
        {
            auctionMinPrice = DecimalUntil.ConvertToElf(auctionMinPrice.Value);
        }
        var nftListingInfoIndex = await QueryMinPriceForNFTListingInfoIndexAsync(chainId, symbol);
        decimal? listingMinPrice =  nftListingInfoIndex?.Prices;
        if (auctionMinPrice == null && listingMinPrice == null)
        {
            return -1m;
        } 
        return Math.Min(auctionMinPrice ?? decimal.MaxValue, listingMinPrice ?? decimal.MaxValue);
    }
    
    private async Task<SymbolAuctionInfoIndex> QueryMinPriceForSymbolAuctionInfoIndexAsync(string chainId,
        string symbol)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SymbolAuctionInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionSymbol).Value(symbol)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));

        QueryContainer Filter(QueryContainerDescriptor<SymbolAuctionInfoIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await _symbolAuctionInfoIndexRepository.GetListAsync(Filter, sortExp: k => k.FinishPrice.Amount,
            sortType: SortOrder.Ascending, limit: 1);

        
        return result?.Item2?.FirstOrDefault();
    }

    private async Task<NFTListingInfoIndex> QueryMinPriceForNFTListingInfoIndexAsync(string chainId, string symbol)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTListingInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionSymbol).Value(symbol)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
        //Add conditions within effective time
        mustQuery.Add(q => q.TermRange(i 
            => i.Field(index => DateTimeHelper.ToUnixTimeMilliseconds(index.ExpireTime)).GreaterThan(DateTime.UtcNow.ToString("O"))));
        QueryContainer Filter(QueryContainerDescriptor<NFTListingInfoIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await _nftListingInfoIndexRepository.GetListAsync(Filter, sortExp: k => k.Prices,
            sortType: SortOrder.Ascending, limit: 1);

        return result?.Item2?.FirstOrDefault();
    }
}