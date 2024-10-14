using System.Linq.Expressions;
using Drop.Indexer.Plugin.Entities;
using Forest.Contracts.Drop;

namespace Drop.Indexer.Plugin.Util;

public static class DropQueryFilters
{
    public static Expression<Func<NFTDropIndex, bool>> DropStateMustNot()
    {
        return nft => nft.State != DropState.Create && nft.State != DropState.Cancel;
    }
    
    public static Expression<Func<NFTDropIndex, bool>> StartTimeBeforeMust(DateTime utcNow)
    {
        return a => a.StartTime < utcNow;
    }
    public static Expression<Func<NFTDropIndex, bool>> StartTimeAfterMust(DateTime utcNow)
    {
        return a => a.StartTime > utcNow;
    }
    public static Expression<Func<NFTDropIndex, bool>> ExpireTimeAfterMust(DateTime utcNow)
    {
        return a => a.ExpireTime > utcNow;
    }
    public static Expression<Func<NFTDropIndex, bool>> ExpireTimeBeforeMust(DateTime utcNow)
    {
        return a => a.ExpireTime < utcNow;
    }
}