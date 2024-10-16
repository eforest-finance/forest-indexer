using System.Reflection;
using AeFinder.Sdk.Entities;
using Google.Protobuf;
using Nest;

namespace Forest.Indexer.Plugin.Entities;

public class TagInfoIndex : TagInfoBase, IAeFinderEntity
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string WhitelistId { get; set; }
    [Keyword] public string WhitelistInfoId { get; set; }
    [Keyword] public string LastModifyTime { get; set; }

    public T DecodeInfo<T>() where T : class
    {
        // return null data
        if (Info == null) return default;
        // return Info as string
        if (typeof(T) == typeof(string)) return (T)(object)Info;

        var type = typeof(T);
        // T must be a protobuf message
        if (!typeof(IMessage).IsAssignableFrom(type))
            throw new Exception("The type parameter T must be a protobuf message that implements IMessage.");

        // decode Info to T object
        var parserProperty = type.GetProperty("Parser", BindingFlags.Static | BindingFlags.Public);
        var parser = parserProperty.GetValue(null);
        var parseFromMethod = parser.GetType().GetMethod("ParseFrom", new Type[] { typeof(byte[]) });
        var byteString = ByteString.FromBase64(Info);
        return (T)parseFromMethod.Invoke(parser, new object[] { byteString.ToByteArray() });
    }
}