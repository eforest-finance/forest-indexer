namespace Forest.Indexer.Plugin.GraphQLL;

public class PagedResultRequestDto : PagedResultQueryDtoBase
{

}

public class PagedResultQueryDtoBase
{
    private const int MaxMaxResultCount = 10000;

    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 100;

    public void Validate()
    {
        if (MaxResultCount > MaxMaxResultCount)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxResultCount),
                $"Max allowed value for {nameof(MaxResultCount)} is {MaxMaxResultCount}.");
        }
    }
}