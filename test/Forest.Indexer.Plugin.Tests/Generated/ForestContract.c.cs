// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: forest_contract.proto
// </auto-generated>
// Original file comments:
// *
// Forest Contract.
#pragma warning disable 0414, 1591
#region Designer generated code

using System.Collections.Generic;
using aelf = global::AElf.CSharp.Core;

namespace Forest {

  #region Events
  public partial class FixedPriceNFTListed : aelf::IEvent<FixedPriceNFTListed>
  {
    public global::System.Collections.Generic.IEnumerable<FixedPriceNFTListed> GetIndexed()
    {
      return new List<FixedPriceNFTListed>
      {
      };
    }

    public FixedPriceNFTListed GetNonIndexed()
    {
      return new FixedPriceNFTListed
      {
        Symbol = Symbol,
        Owner = Owner,
        Quantity = Quantity,
        Price = Price,
        Duration = Duration,
        IsMergedToPreviousListedInfo = IsMergedToPreviousListedInfo,
        WhitelistId = WhitelistId,
      };
    }
  }

  public partial class NFTDelisted : aelf::IEvent<NFTDelisted>
  {
    public global::System.Collections.Generic.IEnumerable<NFTDelisted> GetIndexed()
    {
      return new List<NFTDelisted>
      {
      };
    }

    public NFTDelisted GetNonIndexed()
    {
      return new NFTDelisted
      {
        Symbol = Symbol,
        Owner = Owner,
        Quantity = Quantity,
      };
    }
  }

  public partial class TokenWhiteListChanged : aelf::IEvent<TokenWhiteListChanged>
  {
    public global::System.Collections.Generic.IEnumerable<TokenWhiteListChanged> GetIndexed()
    {
      return new List<TokenWhiteListChanged>
      {
      };
    }

    public TokenWhiteListChanged GetNonIndexed()
    {
      return new TokenWhiteListChanged
      {
        Symbol = Symbol,
        TokenWhiteList = TokenWhiteList,
      };
    }
  }

  public partial class GlobalTokenWhiteListChanged : aelf::IEvent<GlobalTokenWhiteListChanged>
  {
    public global::System.Collections.Generic.IEnumerable<GlobalTokenWhiteListChanged> GetIndexed()
    {
      return new List<GlobalTokenWhiteListChanged>
      {
      };
    }

    public GlobalTokenWhiteListChanged GetNonIndexed()
    {
      return new GlobalTokenWhiteListChanged
      {
        TokenWhiteList = TokenWhiteList,
      };
    }
  }

  public partial class OfferMade : aelf::IEvent<OfferMade>
  {
    public global::System.Collections.Generic.IEnumerable<OfferMade> GetIndexed()
    {
      return new List<OfferMade>
      {
      };
    }

    public OfferMade GetNonIndexed()
    {
      return new OfferMade
      {
        Symbol = Symbol,
        OfferFrom = OfferFrom,
        OfferTo = OfferTo,
        Price = Price,
        Quantity = Quantity,
        ExpireTime = ExpireTime,
      };
    }
  }

  public partial class OfferCanceled : aelf::IEvent<OfferCanceled>
  {
    public global::System.Collections.Generic.IEnumerable<OfferCanceled> GetIndexed()
    {
      return new List<OfferCanceled>
      {
      };
    }

    public OfferCanceled GetNonIndexed()
    {
      return new OfferCanceled
      {
        Symbol = Symbol,
        OfferFrom = OfferFrom,
        OfferTo = OfferTo,
        IndexList = IndexList,
      };
    }
  }

  public partial class Sold : aelf::IEvent<Sold>
  {
    public global::System.Collections.Generic.IEnumerable<Sold> GetIndexed()
    {
      return new List<Sold>
      {
      };
    }

    public Sold GetNonIndexed()
    {
      return new Sold
      {
        NftFrom = NftFrom,
        NftTo = NftTo,
        NftSymbol = NftSymbol,
        NftQuantity = NftQuantity,
        PurchaseSymbol = PurchaseSymbol,
        PurchaseAmount = PurchaseAmount,
      };
    }
  }

  public partial class OfferAdded : aelf::IEvent<OfferAdded>
  {
    public global::System.Collections.Generic.IEnumerable<OfferAdded> GetIndexed()
    {
      return new List<OfferAdded>
      {
      };
    }

    public OfferAdded GetNonIndexed()
    {
      return new OfferAdded
      {
        Symbol = Symbol,
        OfferFrom = OfferFrom,
        OfferTo = OfferTo,
        Price = Price,
        Quantity = Quantity,
        ExpireTime = ExpireTime,
        OriginBalance = OriginBalance,
        OriginBalanceSymbol = OriginBalanceSymbol,
      };
    }
  }

  public partial class OfferChanged : aelf::IEvent<OfferChanged>
  {
    public global::System.Collections.Generic.IEnumerable<OfferChanged> GetIndexed()
    {
      return new List<OfferChanged>
      {
      };
    }

    public OfferChanged GetNonIndexed()
    {
      return new OfferChanged
      {
        Symbol = Symbol,
        OfferFrom = OfferFrom,
        OfferTo = OfferTo,
        Price = Price,
        Quantity = Quantity,
        ExpireTime = ExpireTime,
        OriginBalance = OriginBalance,
        OriginBalanceSymbol = OriginBalanceSymbol,
      };
    }
  }

  public partial class OfferRemoved : aelf::IEvent<OfferRemoved>
  {
    public global::System.Collections.Generic.IEnumerable<OfferRemoved> GetIndexed()
    {
      return new List<OfferRemoved>
      {
      };
    }

    public OfferRemoved GetNonIndexed()
    {
      return new OfferRemoved
      {
        Symbol = Symbol,
        OfferFrom = OfferFrom,
        OfferTo = OfferTo,
        ExpireTime = ExpireTime,
      };
    }
  }

  public partial class ListedNFTAdded : aelf::IEvent<ListedNFTAdded>
  {
    public global::System.Collections.Generic.IEnumerable<ListedNFTAdded> GetIndexed()
    {
      return new List<ListedNFTAdded>
      {
      };
    }

    public ListedNFTAdded GetNonIndexed()
    {
      return new ListedNFTAdded
      {
        Symbol = Symbol,
        Owner = Owner,
        Quantity = Quantity,
        Price = Price,
        Duration = Duration,
        WhitelistId = WhitelistId,
      };
    }
  }

  public partial class ListedNFTChanged : aelf::IEvent<ListedNFTChanged>
  {
    public global::System.Collections.Generic.IEnumerable<ListedNFTChanged> GetIndexed()
    {
      return new List<ListedNFTChanged>
      {
      };
    }

    public ListedNFTChanged GetNonIndexed()
    {
      return new ListedNFTChanged
      {
        Symbol = Symbol,
        Owner = Owner,
        Quantity = Quantity,
        Price = Price,
        Duration = Duration,
        PreviousDuration = PreviousDuration,
        WhitelistId = WhitelistId,
      };
    }
  }

  public partial class ListedNFTRemoved : aelf::IEvent<ListedNFTRemoved>
  {
    public global::System.Collections.Generic.IEnumerable<ListedNFTRemoved> GetIndexed()
    {
      return new List<ListedNFTRemoved>
      {
      };
    }

    public ListedNFTRemoved GetNonIndexed()
    {
      return new ListedNFTRemoved
      {
        Symbol = Symbol,
        Owner = Owner,
        Duration = Duration,
        Price = Price,
      };
    }
  }

  #endregion
  public static partial class ForestContractContainer
  {
    static readonly string __ServiceName = "Forest.ForestContract";

    #region Marshallers
    static readonly aelf::Marshaller<global::Forest.InitializeInput> __Marshaller_Forest_InitializeInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.InitializeInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Google.Protobuf.WellKnownTypes.Empty> __Marshaller_google_protobuf_Empty = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Google.Protobuf.WellKnownTypes.Empty.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.ListWithFixedPriceInput> __Marshaller_Forest_ListWithFixedPriceInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.ListWithFixedPriceInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.DealInput> __Marshaller_Forest_DealInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.DealInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.DelistInput> __Marshaller_Forest_DelistInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.DelistInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.BatchDeListInput> __Marshaller_Forest_BatchDeListInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.BatchDeListInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.MakeOfferInput> __Marshaller_Forest_MakeOfferInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.MakeOfferInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.CancelOfferInput> __Marshaller_Forest_CancelOfferInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.CancelOfferInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.BatchBuyNowInput> __Marshaller_Forest_BatchBuyNowInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.BatchBuyNowInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.SetRoyaltyInput> __Marshaller_Forest_SetRoyaltyInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.SetRoyaltyInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.SetTokenWhiteListInput> __Marshaller_Forest_SetTokenWhiteListInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.SetTokenWhiteListInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::AElf.Types.Address> __Marshaller_aelf_Address = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::AElf.Types.Address.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.SetServiceFeeInput> __Marshaller_Forest_SetServiceFeeInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.SetServiceFeeInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.StringList> __Marshaller_Forest_StringList = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.StringList.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.BizConfig> __Marshaller_Forest_BizConfig = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.BizConfig.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.SetOfferTotalAmountInput> __Marshaller_Forest_SetOfferTotalAmountInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.SetOfferTotalAmountInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.GetListedNFTInfoListInput> __Marshaller_Forest_GetListedNFTInfoListInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.GetListedNFTInfoListInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.ListedNFTInfoList> __Marshaller_Forest_ListedNFTInfoList = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.ListedNFTInfoList.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.GetWhitelistIdInput> __Marshaller_Forest_GetWhitelistIdInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.GetWhitelistIdInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.GetWhitelistIdOutput> __Marshaller_Forest_GetWhitelistIdOutput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.GetWhitelistIdOutput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.GetAddressListInput> __Marshaller_Forest_GetAddressListInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.GetAddressListInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.AddressList> __Marshaller_Forest_AddressList = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.AddressList.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.GetOfferListInput> __Marshaller_Forest_GetOfferListInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.GetOfferListInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.OfferList> __Marshaller_Forest_OfferList = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.OfferList.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Google.Protobuf.WellKnownTypes.StringValue> __Marshaller_google_protobuf_StringValue = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Google.Protobuf.WellKnownTypes.StringValue.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.GetRoyaltyInput> __Marshaller_Forest_GetRoyaltyInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.GetRoyaltyInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.RoyaltyInfo> __Marshaller_Forest_RoyaltyInfo = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.RoyaltyInfo.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.ServiceFeeInfo> __Marshaller_Forest_ServiceFeeInfo = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.ServiceFeeInfo.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.GetTotalOfferAmountInput> __Marshaller_Forest_GetTotalOfferAmountInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.GetTotalOfferAmountInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.GetTotalOfferAmountOutput> __Marshaller_Forest_GetTotalOfferAmountOutput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.GetTotalOfferAmountOutput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.GetTotalEffectiveListedNFTAmountInput> __Marshaller_Forest_GetTotalEffectiveListedNFTAmountInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.GetTotalEffectiveListedNFTAmountInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.GetTotalEffectiveListedNFTAmountOutput> __Marshaller_Forest_GetTotalEffectiveListedNFTAmountOutput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.GetTotalEffectiveListedNFTAmountOutput.Parser.ParseFrom);
    #endregion

    #region Methods
    static readonly aelf::Method<global::Forest.InitializeInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_Initialize = new aelf::Method<global::Forest.InitializeInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "Initialize",
        __Marshaller_Forest_InitializeInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.ListWithFixedPriceInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_ListWithFixedPrice = new aelf::Method<global::Forest.ListWithFixedPriceInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "ListWithFixedPrice",
        __Marshaller_Forest_ListWithFixedPriceInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.DealInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_Deal = new aelf::Method<global::Forest.DealInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "Deal",
        __Marshaller_Forest_DealInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.DelistInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_Delist = new aelf::Method<global::Forest.DelistInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "Delist",
        __Marshaller_Forest_DelistInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.BatchDeListInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_BatchDeList = new aelf::Method<global::Forest.BatchDeListInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "BatchDeList",
        __Marshaller_Forest_BatchDeListInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.MakeOfferInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_MakeOffer = new aelf::Method<global::Forest.MakeOfferInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "MakeOffer",
        __Marshaller_Forest_MakeOfferInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.CancelOfferInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_CancelOffer = new aelf::Method<global::Forest.CancelOfferInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "CancelOffer",
        __Marshaller_Forest_CancelOfferInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.BatchBuyNowInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_BatchBuyNow = new aelf::Method<global::Forest.BatchBuyNowInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "BatchBuyNow",
        __Marshaller_Forest_BatchBuyNowInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.SetRoyaltyInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_SetRoyalty = new aelf::Method<global::Forest.SetRoyaltyInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "SetRoyalty",
        __Marshaller_Forest_SetRoyaltyInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.SetTokenWhiteListInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_SetTokenWhiteList = new aelf::Method<global::Forest.SetTokenWhiteListInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "SetTokenWhiteList",
        __Marshaller_Forest_SetTokenWhiteListInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::AElf.Types.Address, global::Google.Protobuf.WellKnownTypes.Empty> __Method_SetAdministrator = new aelf::Method<global::AElf.Types.Address, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "SetAdministrator",
        __Marshaller_aelf_Address,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.SetServiceFeeInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_SetServiceFee = new aelf::Method<global::Forest.SetServiceFeeInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "SetServiceFee",
        __Marshaller_Forest_SetServiceFeeInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.StringList, global::Google.Protobuf.WellKnownTypes.Empty> __Method_SetGlobalTokenWhiteList = new aelf::Method<global::Forest.StringList, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "SetGlobalTokenWhiteList",
        __Marshaller_Forest_StringList,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::AElf.Types.Address, global::Google.Protobuf.WellKnownTypes.Empty> __Method_SetWhitelistContract = new aelf::Method<global::AElf.Types.Address, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "SetWhitelistContract",
        __Marshaller_aelf_Address,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.BizConfig, global::Google.Protobuf.WellKnownTypes.Empty> __Method_SetBizConfig = new aelf::Method<global::Forest.BizConfig, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "SetBizConfig",
        __Marshaller_Forest_BizConfig,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.SetOfferTotalAmountInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_SetOfferTotalAmount = new aelf::Method<global::Forest.SetOfferTotalAmountInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "SetOfferTotalAmount",
        __Marshaller_Forest_SetOfferTotalAmountInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.GetListedNFTInfoListInput, global::Forest.ListedNFTInfoList> __Method_GetListedNFTInfoList = new aelf::Method<global::Forest.GetListedNFTInfoListInput, global::Forest.ListedNFTInfoList>(
        aelf::MethodType.View,
        __ServiceName,
        "GetListedNFTInfoList",
        __Marshaller_Forest_GetListedNFTInfoListInput,
        __Marshaller_Forest_ListedNFTInfoList);

    static readonly aelf::Method<global::Forest.GetWhitelistIdInput, global::Forest.GetWhitelistIdOutput> __Method_GetWhitelistId = new aelf::Method<global::Forest.GetWhitelistIdInput, global::Forest.GetWhitelistIdOutput>(
        aelf::MethodType.View,
        __ServiceName,
        "GetWhitelistId",
        __Marshaller_Forest_GetWhitelistIdInput,
        __Marshaller_Forest_GetWhitelistIdOutput);

    static readonly aelf::Method<global::Forest.GetAddressListInput, global::Forest.AddressList> __Method_GetOfferAddressList = new aelf::Method<global::Forest.GetAddressListInput, global::Forest.AddressList>(
        aelf::MethodType.View,
        __ServiceName,
        "GetOfferAddressList",
        __Marshaller_Forest_GetAddressListInput,
        __Marshaller_Forest_AddressList);

    static readonly aelf::Method<global::Forest.GetOfferListInput, global::Forest.OfferList> __Method_GetOfferList = new aelf::Method<global::Forest.GetOfferListInput, global::Forest.OfferList>(
        aelf::MethodType.View,
        __ServiceName,
        "GetOfferList",
        __Marshaller_Forest_GetOfferListInput,
        __Marshaller_Forest_OfferList);

    static readonly aelf::Method<global::Google.Protobuf.WellKnownTypes.StringValue, global::Forest.StringList> __Method_GetTokenWhiteList = new aelf::Method<global::Google.Protobuf.WellKnownTypes.StringValue, global::Forest.StringList>(
        aelf::MethodType.View,
        __ServiceName,
        "GetTokenWhiteList",
        __Marshaller_google_protobuf_StringValue,
        __Marshaller_Forest_StringList);

    static readonly aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Forest.StringList> __Method_GetGlobalTokenWhiteList = new aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Forest.StringList>(
        aelf::MethodType.View,
        __ServiceName,
        "GetGlobalTokenWhiteList",
        __Marshaller_google_protobuf_Empty,
        __Marshaller_Forest_StringList);

    static readonly aelf::Method<global::Forest.GetRoyaltyInput, global::Forest.RoyaltyInfo> __Method_GetRoyalty = new aelf::Method<global::Forest.GetRoyaltyInput, global::Forest.RoyaltyInfo>(
        aelf::MethodType.View,
        __ServiceName,
        "GetRoyalty",
        __Marshaller_Forest_GetRoyaltyInput,
        __Marshaller_Forest_RoyaltyInfo);

    static readonly aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Forest.ServiceFeeInfo> __Method_GetServiceFeeInfo = new aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Forest.ServiceFeeInfo>(
        aelf::MethodType.View,
        __ServiceName,
        "GetServiceFeeInfo",
        __Marshaller_google_protobuf_Empty,
        __Marshaller_Forest_ServiceFeeInfo);

    static readonly aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::AElf.Types.Address> __Method_GetAdministrator = new aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::AElf.Types.Address>(
        aelf::MethodType.View,
        __ServiceName,
        "GetAdministrator",
        __Marshaller_google_protobuf_Empty,
        __Marshaller_aelf_Address);

    static readonly aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Forest.BizConfig> __Method_GetBizConfig = new aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Forest.BizConfig>(
        aelf::MethodType.View,
        __ServiceName,
        "GetBizConfig",
        __Marshaller_google_protobuf_Empty,
        __Marshaller_Forest_BizConfig);

    static readonly aelf::Method<global::Forest.GetTotalOfferAmountInput, global::Forest.GetTotalOfferAmountOutput> __Method_GetTotalOfferAmount = new aelf::Method<global::Forest.GetTotalOfferAmountInput, global::Forest.GetTotalOfferAmountOutput>(
        aelf::MethodType.View,
        __ServiceName,
        "GetTotalOfferAmount",
        __Marshaller_Forest_GetTotalOfferAmountInput,
        __Marshaller_Forest_GetTotalOfferAmountOutput);

    static readonly aelf::Method<global::Forest.GetTotalEffectiveListedNFTAmountInput, global::Forest.GetTotalEffectiveListedNFTAmountOutput> __Method_GetTotalEffectiveListedNFTAmount = new aelf::Method<global::Forest.GetTotalEffectiveListedNFTAmountInput, global::Forest.GetTotalEffectiveListedNFTAmountOutput>(
        aelf::MethodType.View,
        __ServiceName,
        "GetTotalEffectiveListedNFTAmount",
        __Marshaller_Forest_GetTotalEffectiveListedNFTAmountInput,
        __Marshaller_Forest_GetTotalEffectiveListedNFTAmountOutput);

    #endregion

    #region Descriptors
    public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
    {
      get { return global::Forest.ForestContractReflection.Descriptor.Services[0]; }
    }

    public static global::System.Collections.Generic.IReadOnlyList<global::Google.Protobuf.Reflection.ServiceDescriptor> Descriptors
    {
      get
      {
        return new global::System.Collections.Generic.List<global::Google.Protobuf.Reflection.ServiceDescriptor>()
        {
          global::AElf.Standards.ACS12.Acs12Reflection.Descriptor.Services[0],
          global::Forest.ForestContractReflection.Descriptor.Services[0],
        };
      }
    }
    #endregion

    /// <summary>Base class for the contract of ForestContract</summary>
    public abstract partial class ForestContractBase
    {
      public virtual global::Google.Protobuf.WellKnownTypes.Empty Initialize(global::Forest.InitializeInput input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Google.Protobuf.WellKnownTypes.Empty ListWithFixedPrice(global::Forest.ListWithFixedPriceInput input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Google.Protobuf.WellKnownTypes.Empty Deal(global::Forest.DealInput input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Google.Protobuf.WellKnownTypes.Empty Delist(global::Forest.DelistInput input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Google.Protobuf.WellKnownTypes.Empty BatchDeList(global::Forest.BatchDeListInput input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Google.Protobuf.WellKnownTypes.Empty MakeOffer(global::Forest.MakeOfferInput input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Google.Protobuf.WellKnownTypes.Empty CancelOffer(global::Forest.CancelOfferInput input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Google.Protobuf.WellKnownTypes.Empty BatchBuyNow(global::Forest.BatchBuyNowInput input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Google.Protobuf.WellKnownTypes.Empty SetRoyalty(global::Forest.SetRoyaltyInput input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Google.Protobuf.WellKnownTypes.Empty SetTokenWhiteList(global::Forest.SetTokenWhiteListInput input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Google.Protobuf.WellKnownTypes.Empty SetAdministrator(global::AElf.Types.Address input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Google.Protobuf.WellKnownTypes.Empty SetServiceFee(global::Forest.SetServiceFeeInput input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Google.Protobuf.WellKnownTypes.Empty SetGlobalTokenWhiteList(global::Forest.StringList input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Google.Protobuf.WellKnownTypes.Empty SetWhitelistContract(global::AElf.Types.Address input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Google.Protobuf.WellKnownTypes.Empty SetBizConfig(global::Forest.BizConfig input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Google.Protobuf.WellKnownTypes.Empty SetOfferTotalAmount(global::Forest.SetOfferTotalAmountInput input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Forest.ListedNFTInfoList GetListedNFTInfoList(global::Forest.GetListedNFTInfoListInput input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Forest.GetWhitelistIdOutput GetWhitelistId(global::Forest.GetWhitelistIdInput input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Forest.AddressList GetOfferAddressList(global::Forest.GetAddressListInput input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Forest.OfferList GetOfferList(global::Forest.GetOfferListInput input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Forest.StringList GetTokenWhiteList(global::Google.Protobuf.WellKnownTypes.StringValue input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Forest.StringList GetGlobalTokenWhiteList(global::Google.Protobuf.WellKnownTypes.Empty input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Forest.RoyaltyInfo GetRoyalty(global::Forest.GetRoyaltyInput input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Forest.ServiceFeeInfo GetServiceFeeInfo(global::Google.Protobuf.WellKnownTypes.Empty input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::AElf.Types.Address GetAdministrator(global::Google.Protobuf.WellKnownTypes.Empty input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Forest.BizConfig GetBizConfig(global::Google.Protobuf.WellKnownTypes.Empty input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Forest.GetTotalOfferAmountOutput GetTotalOfferAmount(global::Forest.GetTotalOfferAmountInput input)
      {
        throw new global::System.NotImplementedException();
      }

      public virtual global::Forest.GetTotalEffectiveListedNFTAmountOutput GetTotalEffectiveListedNFTAmount(global::Forest.GetTotalEffectiveListedNFTAmountInput input)
      {
        throw new global::System.NotImplementedException();
      }

    }

    public static aelf::ServerServiceDefinition BindService(ForestContractBase serviceImpl)
    {
      return aelf::ServerServiceDefinition.CreateBuilder()
          .AddDescriptors(Descriptors)
          .AddMethod(__Method_Initialize, serviceImpl.Initialize)
          .AddMethod(__Method_ListWithFixedPrice, serviceImpl.ListWithFixedPrice)
          .AddMethod(__Method_Deal, serviceImpl.Deal)
          .AddMethod(__Method_Delist, serviceImpl.Delist)
          .AddMethod(__Method_BatchDeList, serviceImpl.BatchDeList)
          .AddMethod(__Method_MakeOffer, serviceImpl.MakeOffer)
          .AddMethod(__Method_CancelOffer, serviceImpl.CancelOffer)
          .AddMethod(__Method_BatchBuyNow, serviceImpl.BatchBuyNow)
          .AddMethod(__Method_SetRoyalty, serviceImpl.SetRoyalty)
          .AddMethod(__Method_SetTokenWhiteList, serviceImpl.SetTokenWhiteList)
          .AddMethod(__Method_SetAdministrator, serviceImpl.SetAdministrator)
          .AddMethod(__Method_SetServiceFee, serviceImpl.SetServiceFee)
          .AddMethod(__Method_SetGlobalTokenWhiteList, serviceImpl.SetGlobalTokenWhiteList)
          .AddMethod(__Method_SetWhitelistContract, serviceImpl.SetWhitelistContract)
          .AddMethod(__Method_SetBizConfig, serviceImpl.SetBizConfig)
          .AddMethod(__Method_SetOfferTotalAmount, serviceImpl.SetOfferTotalAmount)
          .AddMethod(__Method_GetListedNFTInfoList, serviceImpl.GetListedNFTInfoList)
          .AddMethod(__Method_GetWhitelistId, serviceImpl.GetWhitelistId)
          .AddMethod(__Method_GetOfferAddressList, serviceImpl.GetOfferAddressList)
          .AddMethod(__Method_GetOfferList, serviceImpl.GetOfferList)
          .AddMethod(__Method_GetTokenWhiteList, serviceImpl.GetTokenWhiteList)
          .AddMethod(__Method_GetGlobalTokenWhiteList, serviceImpl.GetGlobalTokenWhiteList)
          .AddMethod(__Method_GetRoyalty, serviceImpl.GetRoyalty)
          .AddMethod(__Method_GetServiceFeeInfo, serviceImpl.GetServiceFeeInfo)
          .AddMethod(__Method_GetAdministrator, serviceImpl.GetAdministrator)
          .AddMethod(__Method_GetBizConfig, serviceImpl.GetBizConfig)
          .AddMethod(__Method_GetTotalOfferAmount, serviceImpl.GetTotalOfferAmount)
          .AddMethod(__Method_GetTotalEffectiveListedNFTAmount, serviceImpl.GetTotalEffectiveListedNFTAmount).Build();
    }

  }
}
#endregion

