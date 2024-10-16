namespace Drop.Indexer.Plugin;

public class IdGenerateHelper
{
    public static string GetId(params object[] inputs)
    {
        return inputs.JoinAsString("-");
    }

    public static string GetNFTDropClaimId(string dropId, string address)
    {
        return dropId.Substring(0, 20) + address.Substring(20, 10);
    }
}