namespace Forest.Indexer.Plugin.Util;

public class ContractInfoHelper
{
    public static string GetNFTForestContractAddress(string chainId)
    {
        return chainId switch
        {
            ForestIndexerConstants.AELF => ForestIndexerConstants.NFTForestContractAddressAELF,
            ForestIndexerConstants.TDVV => ForestIndexerConstants.NFTForestContractAddressTDVV,
            ForestIndexerConstants.TDVW => ForestIndexerConstants.NFTForestContractAddressTDVW,
            _ => ""
        };
    }
    public static string GetTokenAdaptorContractAddress(string chainId)
    {
        return chainId switch
        {
            ForestIndexerConstants.AELF => ForestIndexerConstants.TokenAdaptorContractAddressAELF,
            ForestIndexerConstants.TDVV => ForestIndexerConstants.TokenAdaptorContractAddressTDVV,
            ForestIndexerConstants.TDVW => ForestIndexerConstants.TokenAdaptorContractAddressTDVW,
            _ => ""
        };
    }
    public static string GetTokenContractAddress(string chainId)
    {
        return chainId switch
        {
            ForestIndexerConstants.AELF => ForestIndexerConstants.TokenContractAddressAELF,
            ForestIndexerConstants.TDVV => ForestIndexerConstants.TokenContractAddressTDVV,
            ForestIndexerConstants.TDVW => ForestIndexerConstants.TokenContractAddressTDVW,
            _ => ""
        };
    }
}