namespace Forest.Indexer.Plugin.GraphQL;

public class PageResultDto<T>
{
    public long TotalRecordCount { get; set; }
    
    public List<T> Data { get; set; }
    
    protected internal PageResultDto()
    {
        TotalRecordCount = 0;
        Data = new List<T>();
    }
    
    public static PageResultDto<T> Initialize()
    {
        return new PageResultDto<T>
        {
            TotalRecordCount = 0,
            Data = new List<T>()
        };
    }
}


