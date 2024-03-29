// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: auction_contract.proto
// </auto-generated>
// Original file comments:
// the version of the language, use proto3 for contracts
#pragma warning disable 0414, 1591
#region Designer generated code

using System.Collections.Generic;
using aelf = global::AElf.CSharp.Core;

namespace Forest.Contracts.Auction {

  #region Events
  public partial class AuctionCreated : aelf::IEvent<AuctionCreated>
  {
    public global::System.Collections.Generic.IEnumerable<AuctionCreated> GetIndexed()
    {
      return new List<AuctionCreated>
      {
      };
    }

    public AuctionCreated GetNonIndexed()
    {
      return new AuctionCreated
      {
        Creator = Creator,
        AuctionId = AuctionId,
        StartPrice = StartPrice,
        StartTime = StartTime,
        EndTime = EndTime,
        MaxEndTime = MaxEndTime,
        AuctionType = AuctionType,
        Symbol = Symbol,
        AuctionConfig = AuctionConfig,
        ReceivingAddress = ReceivingAddress,
      };
    }
  }

  public partial class BidPlaced : aelf::IEvent<BidPlaced>
  {
    public global::System.Collections.Generic.IEnumerable<BidPlaced> GetIndexed()
    {
      return new List<BidPlaced>
      {
      };
    }

    public BidPlaced GetNonIndexed()
    {
      return new BidPlaced
      {
        AuctionId = AuctionId,
        Bidder = Bidder,
        Price = Price,
        BidTime = BidTime,
      };
    }
  }

  public partial class AuctionTimeUpdated : aelf::IEvent<AuctionTimeUpdated>
  {
    public global::System.Collections.Generic.IEnumerable<AuctionTimeUpdated> GetIndexed()
    {
      return new List<AuctionTimeUpdated>
      {
      };
    }

    public AuctionTimeUpdated GetNonIndexed()
    {
      return new AuctionTimeUpdated
      {
        AuctionId = AuctionId,
        StartTime = StartTime,
        EndTime = EndTime,
        MaxEndTime = MaxEndTime,
      };
    }
  }

  public partial class Claimed : aelf::IEvent<Claimed>
  {
    public global::System.Collections.Generic.IEnumerable<Claimed> GetIndexed()
    {
      return new List<Claimed>
      {
      };
    }

    public Claimed GetNonIndexed()
    {
      return new Claimed
      {
        AuctionId = AuctionId,
        FinishTime = FinishTime,
        Bidder = Bidder,
      };
    }
  }

  public partial class SaleControllerAdded : aelf::IEvent<SaleControllerAdded>
  {
    public global::System.Collections.Generic.IEnumerable<SaleControllerAdded> GetIndexed()
    {
      return new List<SaleControllerAdded>
      {
      };
    }

    public SaleControllerAdded GetNonIndexed()
    {
      return new SaleControllerAdded
      {
        Addresses = Addresses,
      };
    }
  }

  public partial class SaleControllerRemoved : aelf::IEvent<SaleControllerRemoved>
  {
    public global::System.Collections.Generic.IEnumerable<SaleControllerRemoved> GetIndexed()
    {
      return new List<SaleControllerRemoved>
      {
      };
    }

    public SaleControllerRemoved GetNonIndexed()
    {
      return new SaleControllerRemoved
      {
        Addresses = Addresses,
      };
    }
  }

  #endregion
  /// <summary>
  /// the contract definition: a gRPC service definition.
  /// </summary>
  public static partial class AuctionContractContainer
  {
    static readonly string __ServiceName = "AuctionContract";

    #region Marshallers
    static readonly aelf::Marshaller<global::Forest.Contracts.Auction.InitializeInput> __Marshaller_InitializeInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.Contracts.Auction.InitializeInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Google.Protobuf.WellKnownTypes.Empty> __Marshaller_google_protobuf_Empty = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Google.Protobuf.WellKnownTypes.Empty.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::AElf.Types.Address> __Marshaller_aelf_Address = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::AElf.Types.Address.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.Contracts.Auction.CreateAuctionInput> __Marshaller_CreateAuctionInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.Contracts.Auction.CreateAuctionInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.Contracts.Auction.PlaceBidInput> __Marshaller_PlaceBidInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.Contracts.Auction.PlaceBidInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.Contracts.Auction.ClaimInput> __Marshaller_ClaimInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.Contracts.Auction.ClaimInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.Contracts.Auction.AddSaleControllerInput> __Marshaller_AddSaleControllerInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.Contracts.Auction.AddSaleControllerInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.Contracts.Auction.RemoveSaleControllerInput> __Marshaller_RemoveSaleControllerInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.Contracts.Auction.RemoveSaleControllerInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.Contracts.Auction.GetAuctionInfoInput> __Marshaller_GetAuctionInfoInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.Contracts.Auction.GetAuctionInfoInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.Contracts.Auction.AuctionInfo> __Marshaller_AuctionInfo = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.Contracts.Auction.AuctionInfo.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Google.Protobuf.WellKnownTypes.StringValue> __Marshaller_google_protobuf_StringValue = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Google.Protobuf.WellKnownTypes.StringValue.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Google.Protobuf.WellKnownTypes.Int64Value> __Marshaller_google_protobuf_Int64Value = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Google.Protobuf.WellKnownTypes.Int64Value.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.Contracts.Auction.ControllerList> __Marshaller_ControllerList = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.Contracts.Auction.ControllerList.Parser.ParseFrom);
    #endregion

    #region Methods
    static readonly aelf::Method<global::Forest.Contracts.Auction.InitializeInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_Initialize = new aelf::Method<global::Forest.Contracts.Auction.InitializeInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "Initialize",
        __Marshaller_InitializeInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::AElf.Types.Address, global::Google.Protobuf.WellKnownTypes.Empty> __Method_SetAdmin = new aelf::Method<global::AElf.Types.Address, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "SetAdmin",
        __Marshaller_aelf_Address,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.Contracts.Auction.CreateAuctionInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_CreateAuction = new aelf::Method<global::Forest.Contracts.Auction.CreateAuctionInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "CreateAuction",
        __Marshaller_CreateAuctionInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.Contracts.Auction.PlaceBidInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_PlaceBid = new aelf::Method<global::Forest.Contracts.Auction.PlaceBidInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "PlaceBid",
        __Marshaller_PlaceBidInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.Contracts.Auction.ClaimInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_Claim = new aelf::Method<global::Forest.Contracts.Auction.ClaimInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "Claim",
        __Marshaller_ClaimInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.Contracts.Auction.AddSaleControllerInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_AddSaleController = new aelf::Method<global::Forest.Contracts.Auction.AddSaleControllerInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "AddSaleController",
        __Marshaller_AddSaleControllerInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.Contracts.Auction.RemoveSaleControllerInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_RemoveSaleController = new aelf::Method<global::Forest.Contracts.Auction.RemoveSaleControllerInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "RemoveSaleController",
        __Marshaller_RemoveSaleControllerInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.Contracts.Auction.GetAuctionInfoInput, global::Forest.Contracts.Auction.AuctionInfo> __Method_GetAuctionInfo = new aelf::Method<global::Forest.Contracts.Auction.GetAuctionInfoInput, global::Forest.Contracts.Auction.AuctionInfo>(
        aelf::MethodType.View,
        __ServiceName,
        "GetAuctionInfo",
        __Marshaller_GetAuctionInfoInput,
        __Marshaller_AuctionInfo);

    static readonly aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::AElf.Types.Address> __Method_GetAdmin = new aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::AElf.Types.Address>(
        aelf::MethodType.View,
        __ServiceName,
        "GetAdmin",
        __Marshaller_google_protobuf_Empty,
        __Marshaller_aelf_Address);

    static readonly aelf::Method<global::Google.Protobuf.WellKnownTypes.StringValue, global::Google.Protobuf.WellKnownTypes.Int64Value> __Method_GetCurrentCounter = new aelf::Method<global::Google.Protobuf.WellKnownTypes.StringValue, global::Google.Protobuf.WellKnownTypes.Int64Value>(
        aelf::MethodType.View,
        __ServiceName,
        "GetCurrentCounter",
        __Marshaller_google_protobuf_StringValue,
        __Marshaller_google_protobuf_Int64Value);

    static readonly aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Forest.Contracts.Auction.ControllerList> __Method_GetSaleController = new aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Forest.Contracts.Auction.ControllerList>(
        aelf::MethodType.View,
        __ServiceName,
        "GetSaleController",
        __Marshaller_google_protobuf_Empty,
        __Marshaller_ControllerList);

    #endregion

    #region Descriptors
    public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
    {
      get { return global::Forest.Contracts.Auction.AuctionContractReflection.Descriptor.Services[0]; }
    }

    public static global::System.Collections.Generic.IReadOnlyList<global::Google.Protobuf.Reflection.ServiceDescriptor> Descriptors
    {
      get
      {
        return new global::System.Collections.Generic.List<global::Google.Protobuf.Reflection.ServiceDescriptor>()
        {
          global::AElf.Standards.ACS12.Acs12Reflection.Descriptor.Services[0],
          global::Forest.Contracts.Auction.AuctionContractReflection.Descriptor.Services[0],
        };
      }
    }
    #endregion

    /// <summary>Base class for the contract of AuctionContract</summary>
  //   public abstract partial class AuctionContractBase : AElf.Sdk.CSharp.CSharpSmartContract<Forest.Contracts.Auction.AuctionContractState>
  //   {
  //     public virtual global::Google.Protobuf.WellKnownTypes.Empty Initialize(global::Forest.Contracts.Auction.InitializeInput input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::Google.Protobuf.WellKnownTypes.Empty SetAdmin(global::AElf.Types.Address input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::Google.Protobuf.WellKnownTypes.Empty CreateAuction(global::Forest.Contracts.Auction.CreateAuctionInput input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::Google.Protobuf.WellKnownTypes.Empty PlaceBid(global::Forest.Contracts.Auction.PlaceBidInput input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::Google.Protobuf.WellKnownTypes.Empty Claim(global::Forest.Contracts.Auction.ClaimInput input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::Google.Protobuf.WellKnownTypes.Empty AddSaleController(global::Forest.Contracts.Auction.AddSaleControllerInput input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::Google.Protobuf.WellKnownTypes.Empty RemoveSaleController(global::Forest.Contracts.Auction.RemoveSaleControllerInput input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::Forest.Contracts.Auction.AuctionInfo GetAuctionInfo(global::Forest.Contracts.Auction.GetAuctionInfoInput input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::AElf.Types.Address GetAdmin(global::Google.Protobuf.WellKnownTypes.Empty input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::Google.Protobuf.WellKnownTypes.Int64Value GetCurrentCounter(global::Google.Protobuf.WellKnownTypes.StringValue input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //     public virtual global::Forest.Contracts.Auction.ControllerList GetSaleController(global::Google.Protobuf.WellKnownTypes.Empty input)
  //     {
  //       throw new global::System.NotImplementedException();
  //     }
  //
  //   }
  //
  //   public static aelf::ServerServiceDefinition BindService(AuctionContractBase serviceImpl)
  //   {
  //     return aelf::ServerServiceDefinition.CreateBuilder()
  //         .AddDescriptors(Descriptors)
  //         .AddMethod(__Method_Initialize, serviceImpl.Initialize)
  //         .AddMethod(__Method_SetAdmin, serviceImpl.SetAdmin)
  //         .AddMethod(__Method_CreateAuction, serviceImpl.CreateAuction)
  //         .AddMethod(__Method_PlaceBid, serviceImpl.PlaceBid)
  //         .AddMethod(__Method_Claim, serviceImpl.Claim)
  //         .AddMethod(__Method_AddSaleController, serviceImpl.AddSaleController)
  //         .AddMethod(__Method_RemoveSaleController, serviceImpl.RemoveSaleController)
  //         .AddMethod(__Method_GetAuctionInfo, serviceImpl.GetAuctionInfo)
  //         .AddMethod(__Method_GetAdmin, serviceImpl.GetAdmin)
  //         .AddMethod(__Method_GetCurrentCounter, serviceImpl.GetCurrentCounter)
  //         .AddMethod(__Method_GetSaleController, serviceImpl.GetSaleController).Build();
  //   }
  //
  }
}
#endregion

