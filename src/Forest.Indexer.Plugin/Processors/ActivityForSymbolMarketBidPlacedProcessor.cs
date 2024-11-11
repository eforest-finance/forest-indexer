using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using Forest.Contracts.SymbolRegistrar;
using Forest.Indexer.Plugin.Entities;
using Forest.Indexer.Plugin.Util;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace Forest.Indexer.Plugin.Processors;

// public class ActivityForSymbolMarketBidPlacedProcessor : LogEventProcessorBase<Forest.Contracts.Auction.BidPlaced>
// {
//     private readonly IObjectMapper _objectMapper;
//     protected const string FeeMapTypeElf = "ELF";
//     private const string ExtraPropertiesKeyTransactionFee = "TransactionFee";
//     private const string ExtraPropertiesKeyResourceFee = "ResourceFee";
//
//     public ActivityForSymbolMarketBidPlacedProcessor(
//         IObjectMapper objectMapper)
//     {
//         _objectMapper = objectMapper;
//     }
//
//     public override string GetContractAddress(string chainId)
//     {
//         return ContractInfoHelper.GetAuctionContractAddress(chainId);
//     }
//
//     public override async Task ProcessAsync(Forest.Contracts.Auction.BidPlaced eventValue,
//         LogEventContext context)
//     {
//         if (eventValue == null || context == null) return;
//         var auctionInfoIndex =
//             await GetEntityAsync<SymbolAuctionInfoIndex>(eventValue.AuctionId.ToHex());
//         if (auctionInfoIndex == null)
//         {
//             return;
//         }
//
//         var seedSymbolIndexId = IdGenerateHelper.GetSeedSymbolId(context.ChainId,auctionInfoIndex.Symbol);
//         var seedSymbolIndex =
//             await GetEntityAsync<SeedSymbolIndex>(seedSymbolIndexId);
//
//         if (seedSymbolIndex == null) return;
//         var symbolAuctionInfoIndex =
//             await GetEntityAsync<SymbolAuctionInfoIndex>(eventValue.AuctionId.ToHex());
//
//         if (symbolAuctionInfoIndex == null) return;
//         var symbolMarketActivityId = IdGenerateHelper.GetSymbolMarketActivityId(
//             SymbolMarketActivityType.Buy.ToString(), context.ChainId, seedSymbolIndex.SeedOwnedSymbol,
//             context.Transaction.From, context.Transaction.To, context.Transaction.TransactionId);
//         var symbolMarketActivityIndex =
//             await GetEntityAsync<SymbolMarketActivityIndex>(symbolMarketActivityId);
//
//         if (symbolMarketActivityIndex != null) return;
//
//         symbolMarketActivityIndex =
//             await buildSymbolMarketActivityIndexAsync(symbolMarketActivityId, eventValue, context,
//                 seedSymbolIndex.SeedType, symbolAuctionInfoIndex.Symbol);
//         _objectMapper.Map(context, symbolMarketActivityIndex);
//         symbolMarketActivityIndex.SeedSymbol = seedSymbolIndex.Symbol;
//         await SaveEntityAsync(symbolMarketActivityIndex);
//
//     }
//
//     private async Task<SymbolMarketActivityIndex> buildSymbolMarketActivityIndexAsync(string symbolMarketActivityId,
//         Forest.Contracts.Auction.BidPlaced eventValue,
//         LogEventContext context, SeedType seedType, string symbol)
//     {
//         var symbolMarketActivityIndex = new SymbolMarketActivityIndex
//         {
//             Id = symbolMarketActivityId,
//             Price = eventValue.Price.Amount,
//             PriceSymbol = eventValue.Price.Symbol,
//             TransactionDateTime = eventValue.BidTime.ToDateTime(),
//             Symbol = symbol,
//             Address = FullAddressHelper.ToFullAddress(eventValue.Bidder.ToBase58(), context.ChainId),
//             TransactionFee = GetFeeTypeElfAmount(context.Transaction.ExtraProperties),
//             TransactionFeeSymbol = FeeMapTypeElf,
//             TransactionId = context.Transaction.TransactionId,
//         };
//
//         symbolMarketActivityIndex.OfType(SymbolMarketActivityType.Bid);
//         symbolMarketActivityIndex.OfType(seedType);
//         return symbolMarketActivityIndex;
//     }
//     private long GetFeeTypeElfAmount(Dictionary<string, string> extraProperties)
//     {
//         var feeMap = GetTransactionFee(extraProperties);
//         if (feeMap.TryGetValue(FeeMapTypeElf, out var value))
//         {
//             return value;
//         }
//
//         return 0;
//     }
//     
//     private Dictionary<string, long> GetTransactionFee(Dictionary<string, string> extraProperties)
//     {
//         var feeMap = new Dictionary<string, long>();
//         if (extraProperties.TryGetValue(ExtraPropertiesKeyTransactionFee, out var transactionFee))
//         {
//             Logger.LogDebug("ActivityForSymbolMarketBidPlacedProcessor TransactionFee {Fee}",transactionFee);
//             feeMap = JsonConvert.DeserializeObject<Dictionary<string, long>>(transactionFee) ??
//                      new Dictionary<string, long>();
//         }
//
//         if (extraProperties.TryGetValue(ExtraPropertiesKeyResourceFee, out var resourceFee))
//         {
//             Logger.LogDebug("ActivityForSymbolMarketBidPlacedProcessor ResourceFee {Fee}",resourceFee);
//             var resourceFeeMap = JsonConvert.DeserializeObject<Dictionary<string, long>>(resourceFee) ??
//                                  new Dictionary<string, long>();
//             foreach (var (symbol, fee) in resourceFeeMap)
//             {
//                 if (feeMap.ContainsKey(symbol))
//                 {
//                     feeMap[symbol] += fee;
//                 }
//                 else
//                 {
//                     feeMap[symbol] = fee;
//                 }
//             }
//         }
//
//         return feeMap;
//     }
// }