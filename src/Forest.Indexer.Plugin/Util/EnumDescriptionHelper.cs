using AElf.Contracts.MultiToken;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.GraphQL;

namespace Forest.Indexer.Plugin;

public class EnumDescriptionHelper
{
    public static string GetEnumDescription(TokenCreatedExternalInfoEnum value)
    {
        switch (value)
        {
            case TokenCreatedExternalInfoEnum.NFTLogoImageUrl:
                return "__nft_logo_image_url";
            case TokenCreatedExternalInfoEnum.NFTFeaturedImageLink:
                return "__nft_featured_image_link";
            case TokenCreatedExternalInfoEnum.NFTExternalLink:
                return "__nft_external_link";
            case TokenCreatedExternalInfoEnum.NFTDescription:
                return "__nft_description";
            case TokenCreatedExternalInfoEnum.NFTPaymentTokens:
                return "__nft_payment_tokens";
            case TokenCreatedExternalInfoEnum.NFTOther:
                return "__nft_other";
            case TokenCreatedExternalInfoEnum.NFTImageUrl:
                return "__nft_image_url";
            case TokenCreatedExternalInfoEnum.SeedOwnedSymbol:
                return "__seed_owned_symbol";
            case TokenCreatedExternalInfoEnum.SeedExpTime:
                return "__seed_exp_time";
            case TokenCreatedExternalInfoEnum.SpecialInscriptionImage:
                return "inscription_image";
            case TokenCreatedExternalInfoEnum.NFTImageUri:
                return "__nft_image_uri";
            case TokenCreatedExternalInfoEnum.InscriptionImage:
                return "__inscription_image";
        }

        return "";
    }

    public static string GetExtraInfoValue(ExternalInfo externalInfo, TokenCreatedExternalInfoEnum keyEnum, string defaultValue = null)
    {
        var key = GetEnumDescription(keyEnum);
        return externalInfo.Value.GetValueOrDefault(key, defaultValue);
    }
    
    
    public static string GetExtraInfoValue(IEnumerable<ExternalInfoDictionaryDto> externalInfo, TokenCreatedExternalInfoEnum keyEnum, string defaultValue = null)
    {
        var key = GetEnumDescription(keyEnum);
        return externalInfo.Where(kv => kv.Key.Equals(key))
            .Select(kv => kv.Value)
            .FirstOrDefault(defaultValue);
    }
    
    public static string GetExtraInfoValueForSeedOwnedSymbol(AElf.Contracts.TokenAdapterContract.ExternalInfos externalInfo)
    {
        return EnumDescriptionHelper.GetExtraInfoValue(externalInfo,
            TokenCreatedExternalInfoEnum.SeedOwnedSymbol);
    }
    
    private static string GetExtraInfoValue(AElf.Contracts.TokenAdapterContract.ExternalInfos externalInfo, TokenCreatedExternalInfoEnum keyEnum, string defaultValue = null)
    {
        var key = GetEnumDescription(keyEnum);
        return externalInfo.Value.GetValueOrDefault(key, defaultValue);
    }
}