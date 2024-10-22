using System.ComponentModel;
using AElf.Contracts.MultiToken;
using Forest.Indexer.Plugin.enums;
using Forest.Indexer.Plugin.GraphQL;

namespace Forest.Indexer.Plugin;

public class EnumDescriptionHelper
{
    public static string GetEnumDescription(TokenCreatedExternalInfoEnum value)
    {
        var fieldInfo = value.GetType().GetField(value.ToString());
        var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute));
        return attribute?.Description ?? value.ToString();
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