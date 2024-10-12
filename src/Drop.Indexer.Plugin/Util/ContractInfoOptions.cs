namespace Drop.Indexer.Plugin;

public static class ContractInfoHelper
{
    public static string GetNFTDropContractAddress(string chainId)
    {
        return chainId switch
        {
            DropIndexerConstants.AELF => DropIndexerConstants.NFTDropContractAddressAELF,
            DropIndexerConstants.TDVV => DropIndexerConstants.NFTDropContractAddressTDVV,
            DropIndexerConstants.TDVW => DropIndexerConstants.NFTDropContractAddressTDVW,
            _ => ""
        };
    }
}

