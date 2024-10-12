namespace Drop.Indexer.Plugin;

public static class DateTimeHelper
{
    public static DateTime GainBeginDateTime(DateTime target)
    {
        return new DateTime(target.Year, target.Month, target.Day, 0, 0, 0);
    }

    public static long ToUnixTimeMilliseconds(DateTime value)
    {
        var span = value - DateTime.UnixEpoch;
        return (long)span.TotalMilliseconds;
    }

    public static string GetTimeStampInMilliseconds()
    {
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds().ToString();
    }

    public static DateTime FromUnixTimeSeconds(long value)
    {
        return DateTime.UnixEpoch.AddMilliseconds(value * 1000);
    }
    
    public static DateTime FromUnixTimeMilliseconds(long value)
    {
        return  DateTimeOffset.FromUnixTimeMilliseconds(value).DateTime;
        
    }
    
    public static DateTime FromUnixTimeSeconds(this DateTime dateTime, long timeStamp)
    {
        return FromUnixTimeSeconds(timeStamp);
    }
    
    public static string ToUtcString(this DateTime dateTime)
    {
        return dateTime.ToString("o");
    }
    
    public static long ToUtcMilliSeconds(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
    }
    
    public static long ToUtcSeconds(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
    }

}