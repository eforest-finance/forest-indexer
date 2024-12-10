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
    //public const string TokenAdaptorContractAddressAELF = "gjGmHom31GWr5VPWf11de3mJGHVdaDFsR4zgrqjrbijYXv6TW";
    //prod
    public const string TokenAdaptorContractAddressAELF = "ZYNkxNAzswRC8UeHc6bYMdRmbmLqYDPqZv7sE5d9WuJ5rRQEi";

    public const string TokenAdaptorContractAddressTDVV = "";//not have
    public const string TokenAdaptorContractAddressTDVW = "";//not have
    
    //test prod
    public const string TokenContractAddressAELF = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE";
    public const string TokenContractAddressTDVV = "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX";
    public const string TokenContractAddressTDVW = "ASh2Wt7nSEmYqnGxPPzp4pnVDU4uhj1XW9Se5VeZcX2UDdyjx";
    
    public const string AuctionContractAddressAELF = "";
    public const string AuctionContractAddressTDVV = "mhgUyGhd27YaoG8wgXTbwtbAiYx7E59n5GXEkmkTFKKQTvGnB";
    public const string AuctionContractAddressTDVW = "1EFmvua5WQiv15N3xF4egEUvkvLGNWHdoYLMcbXdaXxzrGmA";
    //test
   // public const string SymbolRegistrarContractAddressAELF = "SRVEHfZoiifcHYfnTagJvtW3QtkGnVo1rEEssKk8hirHX8xed";
    //prod
    public const string SymbolRegistrarContractAddressAELF = "iupiTuL2cshxB9UNauXNXe9iyCcqka7jCotodcEHGpNXeLzqG";

    public const string SymbolRegistrarContractAddressTDVV = "";
    public const string SymbolRegistrarContractAddressTDVW = "";
    //test
    //public const string ProxyAccountContractAddressAELF = "2QbymcZLDgQErCixb3r87tLChDhpAueDK6tit9et9B5kvZfrqG";
   // prod
    public const string ProxyAccountContractAddressAELF = "2UM9eusxdRyCztbmMZadGXzwgwKfFdk8pF4ckw58D769ehaPSR";
    public const string ProxyAccountContractAddressTDVV = "hg7hFigUZ6W3gLreo1bGnpAQTQpGsidueYBScVpzPAi81A2AA";
    public const string ProxyAccountContractAddressTDVW = "C6fn7Cb1QJbgw8FjYHQSoUBbnsii6uKqD5eneQUsDPWpr62kJ";
    
    public const string GenesisContractAddressAELF = "pykr77ft9UUKJZLVq15wCH8PinBSjVRQ12sD1Ayq92mKFsJ1i";
    public const string GenesisContractAddressTDVV = "2dtnkWDyJJXeDRcREhKSZHrYdDGMbn3eus5KYpXonfoTygFHZm";
    public const string GenesisContractAddressTDVW = "2dtnkWDyJJXeDRcREhKSZHrYdDGMbn3eus5KYpXonfoTygFHZm";
    public const int MaxWriteDBRecord = 80;
    public const int MaxQueryCount = 5;
    public const int MaxQuerySize = 10000;
    public const int SeedExpireSecond = 1200;

    public static List<string> NeedRecordBalanceOptionsAddressList = new List<string>()
    {
        "9UJLJVLB4Dg9Hhnnkj14XKUBd9Rh1oVzmbTUZ4XMXTCxw3gHw",
        "S8TdjTwQPzRmXJGfDwA9WjUu2ATTdh4EtCu1GjStUWrXAFVwn",
        "9oYxSRqn42jSmpXeapPzJwwUNQR4jx8wnFDvtbCw3Rx1Nem4f",
        "9vSNEe2onMWNCjU53Z2wPqnJDAiRrw4fJ5coTP8yjLUK1zKvi",
        "KSMuhTcdiiFbhRg2SRW1fHBVRMPv7xM1x17cHhgg8g8F3BMyP",
        "49Zr78VPo3SAowsuuEjAN3wZhj7Z7CSiFpeE6APXECsMC5vu5",
        "2ePwE69s5zhtGT3D7QxNCKnkubQdSvgSVgxgMbJ3un5BrvEPac",
        "21ATPh4RLP68b8D152tcxsVimmTWSG7Y2pDac1V87Hdaotk6sW"
    };

    public static List<TokenInfo> TokenInfoList = new List<TokenInfo>()
    {
        new TokenInfo()
        {
            ChainId= "AELF",
            Symbol= "ELF",
            BlockHash = "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash = "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight = 1,
            TokenContractAddress= "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
            Decimals = 8,
            TotalSupply = 100000000000000000,
            TokenName = "AElf Token",
            Issuer = "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv9wFEvQp",
            IsBurnable = true,
            IssueChainId = 9992731
        },
        new TokenInfo()
        {
            ChainId= "AELF",
            Symbol= "ETH",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
            Decimals= 8,
            TotalSupply= 11934405100000000,
            TokenName= "Ethereum",
            Issuer= "2Fo6mvHWqhc5w1vBdai2YLdKrTEkdjFFLxXHy9XD8cxxxtfz73",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "AELF",
            Symbol= "BNB",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
            Decimals= 8,
            TotalSupply= 15586517900000000,
            TokenName= "BNB",
            Issuer= "2Fo6mvHWqhc5w1vBdai2YLdKrTEkdjFFLxXHy9XD8cxxxtfz73",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "AELF",
            Symbol= "USDC",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
            Decimals= 6,
            TotalSupply= 30044395970599550,
            TokenName= "USD Coin",
            Issuer= "2Fo6mvHWqhc5w1vBdai2YLdKrTEkdjFFLxXHy9XD8cxxxtfz73",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "AELF",
            Symbol= "USDT",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
            Decimals= 6,
            TotalSupply= 81061272679788560,
            TokenName= "Tether USD",
            Issuer= "2Fo6mvHWqhc5w1vBdai2YLdKrTEkdjFFLxXHy9XD8cxxxtfz73",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "AELF",
            Symbol= "DAI",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
            Decimals= 8,
            TotalSupply= 505871838980337100,
            TokenName= "Dai Stablecoin",
            Issuer= "2Fo6mvHWqhc5w1vBdai2YLdKrTEkdjFFLxXHy9XD8cxxxtfz73",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "AELF",
            Symbol= "PORT",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
            Decimals= 0,
            TotalSupply= 10000000000000000,
            TokenName= "Port All Project Token",
            Issuer= "aeXhTqNwLWxCG6AzxwnYKrPMWRrzZBskW3HWVD9YREMx1rJxG",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "AELF",
            Symbol= "VOTE",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
            Decimals= 8,
            TotalSupply= 100000000000000000,
            TokenName= "VOTE Token",
            Issuer= "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv9wFEvQp",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "AELF",
            Symbol= "SHARE",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
            Decimals= 8,
            TotalSupply= 100000000000000000,
            TokenName= "SHARE Token",
            Issuer= "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv9wFEvQp",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "AELF",
            Symbol= "WRITE",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
            Decimals= 8,
            TotalSupply= 50000000000000000,
            TokenName= "WRITE Token",
            Issuer= "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv9wFEvQp",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "AELF",
            Symbol= "RAM",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
            Decimals= 8,
            TotalSupply= 50000000000000000,
            TokenName= "RAM Token",
            Issuer= "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv9wFEvQp",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "AELF",
            Symbol= "CPU",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
            Decimals= 8,
            TotalSupply= 50000000000000000,
            TokenName= "CPU Token",
            Issuer= "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv10wFEvQp",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "AELF",
            Symbol= "LOT",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
            Decimals= 8,
            TotalSupply= 100000000000000000,
            TokenName= "aelf Lottery Token",
            Issuer= "2vB6223CorAU79ZMtFpva4LC8DrYuyiSndxvZLCKc61CFvjGbP",
            IsBurnable= true,
            IssueChainId= 1866392
        },
        new TokenInfo()
        {
            ChainId= "AELF",
            Symbol= "READ",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
            Decimals= 8,
            TotalSupply= 50000000000000000,
            TokenName= "READ Token",
            Issuer= "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv10wFEvQp",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "AELF",
            Symbol= "STORAGE",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
            Decimals= 8,
            TotalSupply= 50000000000000000,
            TokenName= "STORAGE Token",
            Issuer= "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv11wFEvQp",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "AELF",
            Symbol= "TRAFFIC",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
            Decimals= 8,
            TotalSupply= 50000000000000000,
            TokenName= "TRAFFIC Token",
            Issuer= "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv12wFEvQp",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "AELF",
            Symbol= "NET",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
            Decimals= 8,
            TotalSupply= 50000000000000000,
            TokenName= "NET Token",
            Issuer= "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv13wFEvQp",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "AELF",
            Symbol= "DISK",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
            Decimals= 8,
            TotalSupply= 50000000000000000,
            TokenName= "DISK Token",
            Issuer= "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv14wFEvQp",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "tDVV",
            Symbol= "ELF",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
            Decimals= 8,
            TotalSupply= 100000000000000000,
            TokenName= "AElf Token",
            Issuer= "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv9wFEvQp",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "tDVV",
            Symbol= "ETH",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
            Decimals= 8,
            TotalSupply= 11934405100000000,
            TokenName= "Ethereum",
            Issuer= "2Fo6mvHWqhc5w1vBdai2YLdKrTEkdjFFLxXHy9XD8cxxxtfz73",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "tDVV",
            Symbol= "BNB",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
            Decimals= 8,
            TotalSupply= 15586517900000000,
            TokenName= "BNB",
            Issuer= "2Fo6mvHWqhc5w1vBdai2YLdKrTEkdjFFLxXHy9XD8cxxxtfz73",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "tDVV",
            Symbol= "USDC",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
            Decimals= 6,
            TotalSupply= 30044395970599550,
            TokenName= "USD Coin",
            Issuer= "2Fo6mvHWqhc5w1vBdai2YLdKrTEkdjFFLxXHy9XD8cxxxtfz73",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "tDVV",
            Symbol= "USDT",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
            Decimals= 6,
            TotalSupply= 81061272679788560,
            TokenName= "Tether USD",
            Issuer= "2Fo6mvHWqhc5w1vBdai2YLdKrTEkdjFFLxXHy9XD8cxxxtfz73",
            IsBurnable= true,
            IssueChainId= 9992731
        },new TokenInfo()
        {
            ChainId= "tDVV",
            Symbol= "DAI",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
            Decimals= 8,
            TotalSupply= 505871838980337100,
            TokenName= "Dai Stablecoin",
            Issuer= "2Fo6mvHWqhc5w1vBdai2YLdKrTEkdjFFLxXHy9XD8cxxxtfz73",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "tDVV",
            Symbol= "PORT",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
            Decimals= 0,
            TotalSupply= 10000000000000000,
            TokenName= "Port All Project Token",
            Issuer= "aeXhTqNwLWxCG6AzxwnYKrPMWRrzZBskW3HWVD9YREMx1rJxG",
            IsBurnable= true,
            IssueChainId= 9992731
        }, new TokenInfo()
        {
            ChainId= "tDVV",
            Symbol= "WRITE",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
            Decimals= 8,
            TotalSupply= 50000000000000000,
            TokenName= "WRITE Token",
            Issuer= "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv9wFEvQp",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "tDVV",
            Symbol= "RAM",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
            Decimals= 8,
            TotalSupply= 50000000000000000,
            TokenName= "RAM Token",
            Issuer= "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv9wFEvQp",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "tDVV",
            Symbol= "CPU",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
            Decimals= 8,
            TotalSupply= 50000000000000000,
            TokenName= "CPU Token",
            Issuer= "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv10wFEvQp",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "tDVV",
            Symbol= "LOT",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
            Decimals= 8,
            TotalSupply= 100000000000000000,
            TokenName= "aelf Lottery Token",
            Issuer= "2vB6223CorAU79ZMtFpva4LC8DrYuyiSndxvZLCKc61CFvjGbP",
            IsBurnable= true,
            IssueChainId= 1866392
        }, new TokenInfo()
        {
            ChainId= "tDVV",
            Symbol= "READ",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
            Decimals= 8,
            TotalSupply= 50000000000000000,
            TokenName= "READ Token",
            Issuer= "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv10wFEvQp",
            IsBurnable= true,
            IssueChainId= 9992731
        },new TokenInfo()
        {
            ChainId= "tDVV",
            Symbol= "STORAGE",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
            Decimals= 8,
            TotalSupply= 50000000000000000,
            TokenName= "STORAGE Token",
            Issuer= "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv11wFEvQp",
            IsBurnable= true,
            IssueChainId= 9992731
        }, new TokenInfo()
        {
            ChainId= "tDVV",
            Symbol= "TRAFFIC",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
            Decimals= 8,
            TotalSupply= 50000000000000000,
            TokenName= "TRAFFIC Token",
            Issuer= "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv12wFEvQp",
            IsBurnable= true,
            IssueChainId= 9992731
        },
        new TokenInfo()
        {
            ChainId= "tDVV",
            Symbol= "NET",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
            Decimals= 8,
            TotalSupply= 50000000000000000,
            TokenName= "NET Token",
            Issuer= "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv13wFEvQp",
            IsBurnable= true,
            IssueChainId= 9992731
        },new TokenInfo()
        {
            ChainId= "tDVV",
            Symbol= "DISK",
            BlockHash= "73b6d1064013c0b34e6b4783d04a7c550863c95bd78e9b372fe8372577e290e8",
            PreviousBlockHash= "0000000000000000000000000000000000000000000000000000000000000000",
            BlockHeight= 1,
            TokenContractAddress= "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
            Decimals= 8,
            TotalSupply= 50000000000000000,
            TokenName= "DISK Token",
            Issuer= "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv14wFEvQp",
            IsBurnable= true,
            IssueChainId= 9992731
        }
    };

}