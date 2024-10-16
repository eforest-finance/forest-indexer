using System.ComponentModel;

namespace Forest.Indexer.Plugin.enums;

public enum TokenCreatedExternalInfoEnum
{
    [Description("__nft_logo_image_url")] NFTLogoImageUrl,

    [Description("__nft_featured_image_link")]
    NFTFeaturedImageLink,
    [Description("__nft_external_link")] NFTExternalLink,
    [Description("__nft_description")] NFTDescription,
    [Description("__nft_payment_tokens")] NFTPaymentTokens,
    [Description("__nft_other")] NFTOther,
    [Description("__nft_image_url")] NFTImageUrl,
    [Description("__seed_owned_symbol")] SeedOwnedSymbol,
    [Description("__seed_exp_time")] SeedExpTime,
    [Description("inscription_image")] SpecialInscriptionImage,
    [Description("__nft_image_uri")] NFTImageUri,
    [Description("__inscription_image")] InscriptionImage
}