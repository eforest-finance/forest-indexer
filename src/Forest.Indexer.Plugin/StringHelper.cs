using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Forest.Indexer.Plugin;

public static class StringHelper
{
    
    public static string DefaultIfEmpty([CanBeNull] this string source, string defaultVal)
    {
        return source.IsNullOrEmpty() ? defaultVal : source;
    }
    
    public static bool NotNullOrEmpty([CanBeNull] this string source)
    {
        return !source.IsNullOrEmpty();
    }
    
    
    public static bool Match([CanBeNull] this string source, string pattern)
    {
        return source.IsNullOrEmpty() ? false : Regex.IsMatch(source, pattern);
    }
    
}