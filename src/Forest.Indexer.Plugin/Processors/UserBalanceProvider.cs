

using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Forest.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Nest;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

public interface IUserBalanceProvider
{
    public Task UpdateUserBalanceAsync(UserBalanceIndex input);
    public Task<UserBalanceIndex> QueryUserBalanceByIdAsync(string userBalanceId, string chainId);

    public Task UpdateUserBanlanceBynftInfoIdAsync(NFTInfoIndex nftInfoIndex, LogEventContext context,
        long beginBlockHeight);

    public Task<long> SaveUserBalanceAsync(String symbol, String address, long amount, LogEventContext context);
    
    public Task<long> ReCoverUserBalanceAsync(String symbol, String address, long amount, LogEventContext context);
    
    public Task<Dictionary<string, UserBalanceIndex>> QueryUserBalanceByIdsAsync(List<string> userBalanceIds);
}

public class UserBalanceProvider : IUserBalanceProvider, ISingletonDependency
{
    private readonly IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> _userBalanceIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> _tokenInfoIndexRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<IUserBalanceProvider> _logger;
    
    public UserBalanceProvider(
        IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> userBalanceIndexRepository,
        IAElfIndexerClientEntityRepository<TokenInfoIndex, LogEventInfo> tokenInfoIndexRepository,
        IObjectMapper objectMapper,
        ILogger<UserBalanceProvider> logger)
    {
        _userBalanceIndexRepository = userBalanceIndexRepository;
        _objectMapper = objectMapper;
        _logger = logger;
        _tokenInfoIndexRepository = tokenInfoIndexRepository;
    }

    public async Task UpdateUserBalanceAsync(UserBalanceIndex input)
    {
        if (input != null)
        {
            await _userBalanceIndexRepository.AddOrUpdateAsync(input);
        }
    }

    public async Task<long> ReCoverUserBalanceAsync(String symbol, String address, long amount, LogEventContext context)
    {
        var nftInfoIndexId = IdGenerateHelper.GetNFTInfoId(context.ChainId, symbol);
        var userBalanceId = IdGenerateHelper.GetUserBalanceId(address, context.ChainId, nftInfoIndexId);
        var userBalanceIndex =
            await _userBalanceIndexRepository.GetFromBlockStateSetAsync(userBalanceId, context.ChainId);
        var tokenInfoId = IdGenerateHelper.GetTokenInfoId(context.ChainId, symbol);
        var tokenInfo = await _tokenInfoIndexRepository.GetFromBlockStateSetAsync(tokenInfoId, context.ChainId);

        if (userBalanceIndex == null)
        {
            userBalanceIndex = new UserBalanceIndex()
            {
                Id = userBalanceId,
                ChainId = context.ChainId,
                NFTInfoId = nftInfoIndexId,
                Address = address,
                Amount = amount,
                Symbol = symbol,
                ChangeTime = context.BlockTime,
                Decimals = tokenInfo?.Decimals ?? ForestIndexerConstants.IntZero
            };
        }
        else
        {
            userBalanceIndex.Amount = amount;
            userBalanceIndex.ChangeTime = context.BlockTime;
        }

        _objectMapper.Map(context, userBalanceIndex);
        await _userBalanceIndexRepository.AddOrUpdateAsync(userBalanceIndex);
        return userBalanceIndex.Amount;
    }
    public async Task<long> SaveUserBalanceAsync(String symbol, String address, long amount, LogEventContext context)
    {
        var nftInfoIndexId = IdGenerateHelper.GetNFTInfoId(context.ChainId, symbol);
        var userBalanceId = IdGenerateHelper.GetUserBalanceId(address, context.ChainId, nftInfoIndexId);
        var userBalanceIndex =
            await _userBalanceIndexRepository.GetFromBlockStateSetAsync(userBalanceId, context.ChainId);
        var tokenInfoId = IdGenerateHelper.GetTokenInfoId(context.ChainId, symbol);
        var tokenInfo = await _tokenInfoIndexRepository.GetFromBlockStateSetAsync(tokenInfoId, context.ChainId);
        
        if (userBalanceIndex == null)
        {
            userBalanceIndex = new UserBalanceIndex()
            {
                Id = userBalanceId,
                ChainId = context.ChainId,
                NFTInfoId = nftInfoIndexId,
                Address = address,
                Amount = amount,
                Symbol = symbol,
                ChangeTime = context.BlockTime,
                Decimals = tokenInfo?.Decimals ?? ForestIndexerConstants.IntZero
            };
        }
        else
        {
            userBalanceIndex.Amount += amount;
            userBalanceIndex.ChangeTime = context.BlockTime;
        }

        _objectMapper.Map(context, userBalanceIndex);
        _logger.LogInformation("SaveUserBalanceAsync Address {Address} symbol {Symbol} balance {Balance}", address,
            symbol, userBalanceIndex.Amount);
        await _userBalanceIndexRepository.AddOrUpdateAsync(userBalanceIndex);
        return userBalanceIndex.Amount;
    }

    public async Task<Dictionary<string, UserBalanceIndex>> QueryUserBalanceByIdsAsync(List<string> userBalanceIds)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Terms(i => i.Field(index => index.Id).Terms(userBalanceIds)));

        QueryContainer FilterForUserBalance(QueryContainerDescriptor<UserBalanceIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await _userBalanceIndexRepository.GetListAsync(FilterForUserBalance);

        return result.Item2.IsNullOrEmpty() ? new Dictionary<string, UserBalanceIndex>() : result.Item2.ToDictionary(item => item.Id);
    }

    public async Task<UserBalanceIndex> QueryUserBalanceByIdAsync(string userBalanceId, string chainId)
    {
        if (userBalanceId.IsNullOrWhiteSpace() ||
            chainId.IsNullOrWhiteSpace())
        {
            return null;
        }
        return await _userBalanceIndexRepository.GetFromBlockStateSetAsync(userBalanceId,chainId);
    }

    public async Task UpdateUserBanlanceBynftInfoIdAsync(NFTInfoIndex nftInfoIndex, LogEventContext context,
        long beginBlockHeight)
    {
        if (nftInfoIndex == null || context == null || nftInfoIndex.Id.IsNullOrWhiteSpace() || beginBlockHeight < 0) return;

        var result = await QueryAndUpdateUserBanlanceBynftInfoId(nftInfoIndex, beginBlockHeight, context.BlockHeight);
        if (result != null && result.Item1 > 0 && result.Item2 != null)
        {
            beginBlockHeight = result.Item2.Last().BlockHeight;
            foreach (var userBalanceIndex in result.Item2)
            {
                userBalanceIndex.ListingPrice = nftInfoIndex.ListingPrice;
                userBalanceIndex.ListingTime = nftInfoIndex.LatestListingTime;
                _objectMapper.Map(context, userBalanceIndex);
                await _userBalanceIndexRepository.AddOrUpdateAsync(userBalanceIndex);
            }

            await UpdateUserBanlanceBynftInfoIdAsync(nftInfoIndex, context, beginBlockHeight);
        }
    }

    private async Task<Tuple<long,List<UserBalanceIndex>>> QueryAndUpdateUserBanlanceBynftInfoId(NFTInfoIndex nftInfoIndex, long blockHeight,long temMaxBlockHeight)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Range(i => i.Field(index => index.BlockHeight).GreaterThan(blockHeight)));
        mustQuery.Add(q => q.Range(i => i.Field(index => index.BlockHeight).LessThan(temMaxBlockHeight)));
        mustQuery.Add(q => q.Term(i => i.Field(index => index.NFTInfoId).Value(nftInfoIndex.Id)));

        QueryContainer FilterForUserBalance(QueryContainerDescriptor<UserBalanceIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var resultUserBalanceIndex = await _userBalanceIndexRepository.GetListAsync(FilterForUserBalance,
            sortType: SortOrder.Ascending,
            sortExp: o => o.BlockHeight, skip: 0, limit: 100);
        return resultUserBalanceIndex;
    }
}