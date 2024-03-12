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
}