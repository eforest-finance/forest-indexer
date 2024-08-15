namespace Drop.Indexer.Plugin.Processors;

public class DecimalUntil
{
    private static decimal ConvertToOtherToken(decimal amount, int decimals)
    {
        if (amount > 0)
        {
            return amount / (decimal)Math.Pow(10, decimals);
        }

        return 0;
    }


    public static decimal ConvertToElf(decimal amount)
    {
        return ConvertToOtherToken(amount, 8);
    }
}