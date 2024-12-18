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
    
    public static string GetAuctionContractAddress(string chainId)
    {
        return chainId switch
        {
            ForestIndexerConstants.AELF => ForestIndexerConstants.AuctionContractAddressAELF,
            ForestIndexerConstants.TDVV => ForestIndexerConstants.AuctionContractAddressTDVV,
            ForestIndexerConstants.TDVW => ForestIndexerConstants.AuctionContractAddressTDVW,
            _ => ""
        };
    }
    
    public static string GetSymbolRegistrarContractAddress(string chainId)
    {
        return chainId switch
        {
            ForestIndexerConstants.AELF => ForestIndexerConstants.SymbolRegistrarContractAddressAELF,
            ForestIndexerConstants.TDVV => ForestIndexerConstants.SymbolRegistrarContractAddressTDVV,
            ForestIndexerConstants.TDVW => ForestIndexerConstants.SymbolRegistrarContractAddressTDVW,
            _ => ""
        };
    }
    
    public static string GetProxyAccountContractAddress(string chainId)
    {
        return chainId switch
        {
            ForestIndexerConstants.AELF => ForestIndexerConstants.ProxyAccountContractAddressAELF,
            ForestIndexerConstants.TDVV => ForestIndexerConstants.ProxyAccountContractAddressTDVV,
            ForestIndexerConstants.TDVW => ForestIndexerConstants.ProxyAccountContractAddressTDVW,
            _ => ""
        };
    }
    public static string GetGenesisContractAddress(string chainId)
    {
        return chainId switch
        {
            ForestIndexerConstants.AELF => ForestIndexerConstants.GenesisContractAddressAELF,
            ForestIndexerConstants.TDVV => ForestIndexerConstants.GenesisContractAddressTDVV,
            ForestIndexerConstants.TDVW => ForestIndexerConstants.GenesisContractAddressTDVW,
            _ => ""
        };
    }
}