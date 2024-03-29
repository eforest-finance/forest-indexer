using System.Text.RegularExpressions;
using Forest.Indexer.Plugin.enums;

namespace Forest.Indexer.Plugin;

public class TokenHelper
{
    public static TokenType GetTokenType(string symbol)
    {
        var words = symbol.Split(ForestIndexerConstants.SymbolSeparator);
        if (words.Length == 1) return TokenType.FT;
        return TokenType.NFT;
    }

    public static string GetNftSymbol(string ftSymbol)
    {
        return IdGenerateHelper.GetId(ftSymbol, ForestIndexerConstants.NftSubfix);
    }

    public static string GetFtSymbol(string nftSymbol)
    {
        return nftSymbol.Replace("-" + ForestIndexerConstants.NftSubfix, "");
    }

    public static long GetIntegerDivision(long number, int decimals)
    {
        if (decimals == ForestIndexerConstants.IntZero || number == ForestIndexerConstants.IntZero)
        {
            return number;
        }

        var divisor = (long)Math.Pow(ForestIndexerConstants.IntTen, decimals);
        return number / divisor;
    }

    public static bool CheckSymbolIsNFT(string symbol)
    {
        return symbol.Length != ForestIndexerConstants.IntZero &&
               Regex.IsMatch(symbol, ForestIndexerConstants.NFTSymbolPattern);
    }

    public static string GetCollectionSymbol(string nftSymbol)
    {
        if (!CheckSymbolIsNFT(nftSymbol))
        {
            return ForestIndexerConstants.NFTSymbolSeparator.ToString();
        }

        return IdGenerateHelper.GetId(GetSymbolPreFix(nftSymbol), ForestIndexerConstants.IntZero);
    }

    private static string GetSymbolPreFix(string input)
    {
        var delimiterIndex = input.IndexOf(ForestIndexerConstants.NFTSymbolSeparator);

        if (delimiterIndex == ForestIndexerConstants.NegativeIntOne)
        {
            return input;
        }

        return input.Substring(ForestIndexerConstants.IntZero, delimiterIndex);
    }
}