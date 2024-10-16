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
}