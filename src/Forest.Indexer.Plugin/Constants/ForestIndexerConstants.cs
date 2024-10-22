namespace Forest.Indexer.Plugin;

public static partial class ForestIndexerConstants
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
    public const string IssuedLessThenOneANftScript = "(doc['supply'].value / Math.pow(10, doc['decimals'].value)) < 1";
    
    public const string SymbolIsSGR = "doc['symbol'].value.contains('SGR-')";
    public const string SymbolAmountLessThanOneSGR = "((doc['amount'].value / Math.pow(10, 8)) < 1)";


    public const char NFTSymbolSeparator = '-';
    
    public const string SeedCollectionSymbolPattern = @"^SEED-0$";
    
    public const string SeedSymbolPattern = @"^SEED-[0-9]+$";

    public const string SeedIdPattern = @"^[a-zA-Z]{4}-SEED-[0-9]+$";
    
    public const string SeedZeroIdPattern = @"^[a-zA-Z]{4}-SEED-0$";

    public const int MaxCountNumber = 180;
    public const int DefaultMaxCountNumber = 1000;

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
    public const long EsLimitTotalNumber = 10000;
    public const int IntZero = 0;
    public const int IntOne = 0;
    public const int IntTen = 10;
    public const string SGRCollection = "SGR-0";

    public const int SGRDecimal = 8;
    public const int QueryUserBalanceListDefaultSize = 3000;
    
    public const string AELF = "AELF";
    public const string TDVV = "tDVV";
    public const string TDVW = "tDVW";

    public const string NFTForestContractAddressAELF = "";//not have
    public const string NFTForestContractAddressTDVV = "2cGT3RZZy6UJJ3eJPZdWMmuoH2TZBihvMtAtKvLJUaBnvskK2x";
    public const string NFTForestContractAddressTDVW = "zv7YnQ2dLM45ssfifN1dpwqBwdxH13pqGm9GDH6peRdH8F3hD";
    //test
    public const string TokenAdaptorContractAddressAELF = "gjGmHom31GWr5VPWf11de3mJGHVdaDFsR4zgrqjrbijYXv6TW";
    //prod todo V2
    //public const string TokenAdaptorContractAddressAELF = "ZYNkxNAzswRC8UeHc6bYMdRmbmLqYDPqZv7sE5d9WuJ5rRQEi";

    public const string TokenAdaptorContractAddressTDVV = "";//not have
    public const string TokenAdaptorContractAddressTDVW = "";//not have
    
    //test
    public const string TokenContractAddressAELF = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE";
    //prod
   // public const string TokenContractAddressAELF = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE";
    public const string TokenContractAddressTDVV = "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX";
    public const string TokenContractAddressTDVW = "ASh2Wt7nSEmYqnGxPPzp4pnVDU4uhj1XW9Se5VeZcX2UDdyjx";
    
    public const string AuctionContractAddressAELF = "";
    public const string AuctionContractAddressTDVV = "";
    public const string AuctionContractAddressTDVW = "1EFmvua5WQiv15N3xF4egEUvkvLGNWHdoYLMcbXdaXxzrGmA";
    //test
    public const string SymbolRegistrarContractAddressAELF = "SRVEHfZoiifcHYfnTagJvtW3QtkGnVo1rEEssKk8hirHX8xed";
    //prod
    //public const string SymbolRegistrarContractAddressAELF = "";

    public const string SymbolRegistrarContractAddressTDVV = "";
    public const string SymbolRegistrarContractAddressTDVW = "";
    //test
    public const string ProxyAccountContractAddressAELF = "2QbymcZLDgQErCixb3r87tLChDhpAueDK6tit9et9B5kvZfrqG";
    //prod
    //public const string ProxyAccountContractAddressAELF = "2UM9eusxdRyCztbmMZadGXzwgwKfFdk8pF4ckw58D769ehaPSR";
    public const string ProxyAccountContractAddressTDVV = "hg7hFigUZ6W3gLreo1bGnpAQTQpGsidueYBScVpzPAi81A2AA";
    public const string ProxyAccountContractAddressTDVW = "C6fn7Cb1QJbgw8FjYHQSoUBbnsii6uKqD5eneQUsDPWpr62kJ";
    
    public const string GenesisContractAddressAELF = "pykr77ft9UUKJZLVq15wCH8PinBSjVRQ12sD1Ayq92mKFsJ1i";
    public const string GenesisContractAddressTDVV = "2dtnkWDyJJXeDRcREhKSZHrYdDGMbn3eus5KYpXonfoTygFHZm";
    public const string GenesisContractAddressTDVW = "2dtnkWDyJJXeDRcREhKSZHrYdDGMbn3eus5KYpXonfoTygFHZm";
    public const int MaxWriteDBRecord = 80;
    public const int MaxQueryCount = 5;
    public const int MaxQuerySize = 10000;
    public static List<string> NeedRecordBalanceOptionsAddressList = new ();
}