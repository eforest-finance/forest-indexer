using Forest.Indexer.Plugin.Entities;

namespace Forest.Indexer.Plugin;

public class InitialInfoOptions
{
    public List<TokenInfo> TokenInfoList { get; set; } = new();
}

public class TokenInfo : TokenInfoBase
{
    
}