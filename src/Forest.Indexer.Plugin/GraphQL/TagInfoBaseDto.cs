using System.Reflection;
using Google.Protobuf;

namespace Forest.Indexer.Plugin.GraphQL;

public class TagInfoBaseDto
{
    public string ChainId { get; set; }
    public string TagHash { get; set; }
    public string Name { get; set; }
    public string Info { get; set; }
    public PriceTagInfoDto PriceTagInfo { get; set; }

    public T DecodeInfo<T>() where T : class
    {
        if (Info == null) return default;
        if (typeof(T) == typeof(string)) return (T)(object)Info;

        var type = typeof(T);
        if (!typeof(IMessage).IsAssignableFrom(type))
            throw new Exception("The type parameter T must be a protobuf message that implements IMessage.");

        var parserProperty = type.GetProperty("Parser", BindingFlags.Static | BindingFlags.Public);
        var parser = parserProperty.GetValue(null);
        var parseFromMethod = parser.GetType().GetMethod("ParseFrom", new Type[] { typeof(byte[]) });
        var byteString = ByteString.FromBase64(Info);
        return (T)parseFromMethod.Invoke(parser, new object[] { byteString.ToByteArray() });
    }
}