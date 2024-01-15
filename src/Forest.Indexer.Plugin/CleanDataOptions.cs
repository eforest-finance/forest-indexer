using Forest.Indexer.Plugin.Entities;

namespace Forest.Indexer.Plugin;

public class CleanDataOptions
{
    public List<string> SeedSymbolList { get; set; } /*= 
        new List<string>()
        {
            "SEED-4","SEED-7","SEED-10","SEED-8","SEED-19","SEED-41","SEED-39","SEED-37","SEED-48","SEED-57","SEED-33","SEED-24","SEED-26","SEED-47","SEED-42","SEED-11","SEED-28","SEED-45","SEED-43","SEED-69","SEED-6","SEED-14","SEED-3","SEED-21","SEED-23","SEED-44","SEED-38","SEED-53","SEED-52","SEED-5","SEED-12","SEED-16","SEED-20","SEED-36","SEED-46"
        };*/
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
}
