namespace Forest.Indexer.Plugin;

public static class ForestIndexerConstants
{
    public const string UNDERLINE = "_";
    public const string NFTCollectionSymbolPattern = @"^.+-0$";
    public const string NFTSymbolPattern = @"^.+-(?!0+$)[0-9]+$";
    public const string UserBalanceScriptForNft =
        "doc['symbol'].value =~ /.-(?!0+$)[0-9]+/ && !doc['symbol'].value.contains('SEED-')";
    public const string UserBalanceScriptForSeed = "doc['symbol'].value =~ /^SEED-[0-9]+$/";
    public const string Painless =
        "painless";
    public const string  BurnedAllNftScript = "doc['supply'].value == 0 && doc['issued'].value == doc['totalSupply'].value";
    public const string  CreateFailedANftScript = "doc['supply'].value == 0 && doc['issued'].value == 0";

    public const char NFTSymbolSeparator = '-';
    
    public const string SeedCollectionSymbolPattern = @"^SEED-0$";
    
    public const string SeedSymbolPattern = @"^SEED-[0-9]+$";

    public const string SeedIdPattern = @"^[a-zA-Z]{4}-SEED-[0-9]+$";
    
    public const string SeedZeroIdPattern = @"^[a-zA-Z]{4}-SEED-0$";

    public const int MaxCountNumber = 180;

    public const int NFTInfoQueryStatusDefault = 0;
    public const int NFTInfoQueryStatusBuy = 1;
    public const int NFTInfoQueryStatusSelf = 2;
    public const int NFTInfoQueryStatusAuction = 3;
    public const int NFTInfoQueryStatusOffer = 4;
    
    public const string SymbolSeparator = "-";
    public const string MainChain = "AELF";
    public const int MainChainId = 9992731;
    public const string NftSubfix = "0";
    public const string SeedNamePrefix = "SEED";

    public const string PriceSimpleElf = "ELF";
    public const string TokenSimpleElf = "ELF";

    public const string SeedCollectionSymbol = "SEED-0";

    public const string BrifeInfoDescriptionPrice = "Price";
    public const string BrifeInfoDescriptionOffer = "Offer";
    public const string BrifeInfoDescriptionTopBid = "Top Bid";
    public const string SeedImageType = ".svg";
    
    public const string QueryDropListScript = "Date date = new Date();long currentTime = date.getTime();long startTime = doc['startTime'].value.toInstant().toEpochMilli();long expireTime = doc['expireTime'].value.toInstant().toEpochMilli();if(startTime < currentTime && expireTime > currentTime) return startTime; else if(startTime > currentTime) return startTime*2; else return startTime*3;";
}