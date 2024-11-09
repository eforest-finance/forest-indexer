namespace Forest.Indexer.Plugin.GraphQL;

public class UserMatchedNftIds
{ 
    public List<string> NftIds { get; set; }
}

public class UserMatchedNftIdsPage
{ 
    public List<string> NftIds { get; set; }
    public long Count { get; set; }
}