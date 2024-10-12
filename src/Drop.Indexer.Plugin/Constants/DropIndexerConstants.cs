namespace Drop.Indexer.Plugin;

public static class DropIndexerConstants
{
    public const string QueryDropListScript = "Date date = new Date();long currentTime = date.getTime();long startTime = doc['startTime'].value.toInstant().toEpochMilli();long expireTime = doc['expireTime'].value.toInstant().toEpochMilli();if(startTime < currentTime && expireTime > currentTime) return startTime; else if(startTime > currentTime) return startTime*2; else return startTime*3;";
    
    public const string AELF = "AELF";
    public const string TDVV = "tDVV";
    public const string TDVW = "tDVW";
    
    public const string NFTDropContractAddressAELF = "";
    public const string NFTDropContractAddressTDVV = "";
    public const string NFTDropContractAddressTDVW = "yEMwBeheRq6iiw6VN9TgUt2eASBNcgxsEUUwFFsgXedySgnp2";
    
    
    
    
    
}