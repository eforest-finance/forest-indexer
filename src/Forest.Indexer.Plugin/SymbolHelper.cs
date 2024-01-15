using System.Text.RegularExpressions;
using AElf;

namespace Forest.Indexer.Plugin;

public static class SymbolHelper
{
    private static readonly string _mainChain = "AELF";

    /**
     * symbol‘s format is： XXX-{number}
     * when the number is 0, it means that this is nft collection
     */
    public static bool CheckSymbolIsNoMainChainNFTCollection(string symbol, string chainId)
    {
        return symbol.Length != 0 && !CheckChainIdIsMain(chainId) &&
               Regex.IsMatch(symbol, ForestIndexerConstants.NFTCollectionSymbolPattern);
    }
    
    public static bool CheckSymbolIsNFTCollection(string symbol)
    {
        return symbol.Length != 0 &&
               Regex.IsMatch(symbol, ForestIndexerConstants.NFTCollectionSymbolPattern);
    }

    public static bool CheckSymbolIsNoMainChainNFT(string symbol, string chainId)
    {
        return symbol.Length != 0 && !CheckChainIdIsMain(chainId) &&
               CheckSymbolIsNFT(symbol);
    }
    
    public static bool CheckSymbolIsELF(string symbol)
    {
        return symbol.Length != 0 && symbol.Equals(ForestIndexerConstants.TokenSimpleElf);
    }
    
    public static bool CheckSymbolIsNFT(string symbol)
    {
        return symbol.Length != 0 &&
               Regex.IsMatch(symbol, ForestIndexerConstants.NFTSymbolPattern);
    }

    public static bool CheckSymbolIsSeedCollection(String symbol)
    {
        return symbol.Length != 0 && Regex.IsMatch(symbol, ForestIndexerConstants.SeedCollectionSymbolPattern);
    }

    public static bool CheckSymbolIsSeedSymbol(string symbol)
    {
        return symbol.Length != 0 &&
               Regex.IsMatch(symbol, ForestIndexerConstants.SeedSymbolPattern);
    }

    public static string GetNFTCollectionSymbol(string inputSymbol)
    {
        var symbol = inputSymbol;
        var words = symbol.Split(ForestIndexerConstants.NFTSymbolSeparator);
        const int tokenSymbolLength = 1;
        if (words.Length == tokenSymbolLength) return null;
        if (!(words.Length == 2 && words[1].All(IsValidItemIdChar))) return null;
        return symbol == $"{words[0]}-0" ? null : $"{words[0]}-0";
    }

    private static bool IsValidItemIdChar(char character)
    {
        return character >= '0' && character <= '9';
    }

    public static decimal? ToPriceDecimal(this long? amount, int priceDecimal)
    {
        return amount == null ? null : amount / (decimal)Math.Pow(10, priceDecimal);
    }

    public static bool CheckChainIdIsMain(int chainId)
    {
        return ChainHelper.ConvertChainIdToBase58(chainId).Equals(_mainChain);
    }

    public static bool CheckChainIdIsMain(string chainId)
    {
        return chainId.Equals(_mainChain);
    }

    public static string FullAddress(string chainId, string originAddress)
    {
        return ForestIndexerConstants.TokenSimpleElf+ForestIndexerConstants.UNDERLINE + originAddress + ForestIndexerConstants.UNDERLINE + chainId;
    }
}