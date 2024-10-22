namespace Forest.Indexer.Plugin.GraphQL;

public abstract class PageResult<T>
{
    protected PageResult(long total, List<T> data)
    {
        TotalRecordCount = total;
        Data = data;
    }

    protected PageResult(string msg)
    {
        Message = msg;
    } 
    
    public long TotalRecordCount { get; set; }
    
    public List<T> Data { get; set; }

    public string Message { get; set; }

}