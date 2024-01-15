namespace Forest.Indexer.Plugin;

public class ContractInfoOptions
{
    public List<ContractInfo> ContractInfos { get; set; }
}

public class ContractInfo
{
    public string ChainId { get; set; }
    public string GenesisContractAddress { get; set; }

    public string TokenContractAddress { get; set; }
    public string NFTMarketContractAddress { get; set; }
    public string WhitelistContractAddress { get; set; }

    public string ProxyAccountContractAddress { get; set; }
    public string TokenAdaptorContractAddress { get; set; }
    
    public string AuctionContractAddress { get; set; }
    public string SymbolRegistrarContractAddress { get; set; }
}