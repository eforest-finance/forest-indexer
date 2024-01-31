using AElf.Types;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.enums;
using Nest;

namespace Forest.Indexer.Plugin;

public class IdGenerateHelper
{
    public static string GetProxyAccountIndexId(string proxyAccountAddress)
    {
        return GetId(proxyAccountAddress);
    }
    
    public static string GetMarketDataTodayIndexId(string nftInfoId, long timestamp)
    {
        return GetId(nftInfoId, timestamp.ToString());
    }

    public static string GetMarketDataWeekIndexId(string nftInfoId, long timestamp)
    {
        return GetId(nftInfoId, timestamp.ToString());
    }
    
    public static string GetId(params object[] inputs)
    {
        return inputs.JoinAsString("-");
    }
    
    public static string GetTokenInfoId(string chainId, string symbol)
    {
        return GetId(chainId, symbol);
    }

    public static string GetListingWhitelistPriceId(string chainId, WhiteListExtraInfoIndex extraInfoIndex)
    {
        return GetId(chainId, extraInfoIndex.TagInfoId);
    }

    public static string GetNFTInfoId(string chainId, string symbol)
    {
        return GetId(chainId, symbol);
    }
    
    public static string GetNFTCollectionId(string chainId, string symbol)
    {
        return GetId(chainId, symbol);
    }

    public static string GetSeedCollectionId(string chainId, string symbol)
    {
        return GetId(chainId, symbol);
    }
    
    public static string GetSeedSymbolId(string chainId, string symbol)
    {
        return GetId(chainId, symbol);
    }
    
    public static string GetSeedMainChainChangeId(string chainId, string symbol)
    {
        return GetId(chainId, symbol);
    }

    public static string GetTsmSeedSymbolId(string chainId,string seedOwnedSymbol)
    {
        return GetId(chainId, seedOwnedSymbol);
    }

    public static string GetUserBalanceId(string address, string chainId, string nftInfoId)
    {
        return GetId(address, chainId, nftInfoId);
    }

    public static string GetNftActivityId(string chainId, string symbol, string from, string to, string transactionId)
    {
        return GetId(chainId, symbol, from, to, transactionId);
    }
    
    public static string GetSymbolMarketActivityId(string activityType, string chainId, string symbol, string from,
        string to,
        string transactionId)
    {
        return GetId(activityType, chainId, symbol, from, to, transactionId);
    }

    public static string GetSymbolMarketTokenId(string chainId, string tokenSymbol)
    {
        return GetId(chainId, tokenSymbol);
    }
    

    public static string GetSeedName(string symbol)
    {
        return GetId(ForestIndexerConstants.SeedNamePrefix, symbol);
    }

    public static string GetSeedPriceId(string tokenType, int symbolLength)
    {
        return GetId(tokenType, symbolLength);
    }

    public static string GetOfferId(string chainId, string symbol, string fromAddress,
        string toAddress, long expiredSecond, long price)
    {
        return GetId(chainId, symbol, expiredSecond, price, fromAddress, toAddress);
    }
    
    public static string GetNFTDropClaimId(string dropId, string address)
    {
        return dropId.Substring(0, 20) + address.Substring(20, 10);
    }
}