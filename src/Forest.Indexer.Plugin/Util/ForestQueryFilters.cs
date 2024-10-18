using System.Linq.Expressions;
using AeFinder.Sdk.Processor;
using Forest.Indexer.Plugin.Entities;

namespace Forest.Indexer.Plugin.Util;

public static class ForestQueryFilters
{
    public static Expression<Func<OfferInfoIndex, bool>> OfferCanceledByExpireTimeFilter(LogEventContext context,OfferCanceledByExpireTime eventValue)
    {
        return index =>
            index.ChainId == context.ChainId
            && index.BizSymbol == eventValue.Symbol
            && index.OfferFrom == eventValue.OfferFrom.ToBase58()
            && index.OfferTo == eventValue.OfferTo.ToBase58()
            && index.ExpireTime == eventValue.ExpireTime.ToDateTime();
    }
}