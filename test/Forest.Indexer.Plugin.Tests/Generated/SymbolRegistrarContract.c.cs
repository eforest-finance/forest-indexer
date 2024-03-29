// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: symbol_registrar_contract.proto
// </auto-generated>
// Original file comments:
// the version of the language, use proto3 for contracts
#pragma warning disable 0414, 1591
#region Designer generated code

using System.Collections.Generic;
using aelf = global::AElf.CSharp.Core;

namespace Forest.SymbolRegistrar {

  #region Events
  public partial class SeedCreated : aelf::IEvent<SeedCreated>
  {
    public global::System.Collections.Generic.IEnumerable<SeedCreated> GetIndexed()
    {
      return new List<SeedCreated>
      {
      };
    }

    public SeedCreated GetNonIndexed()
    {
      return new SeedCreated
      {
        Symbol = Symbol,
        OwnedSymbol = OwnedSymbol,
        ExpireTime = ExpireTime,
        SeedType = SeedType,
        To = To,
      };
    }
  }

  public partial class SeedsPriceChanged : aelf::IEvent<SeedsPriceChanged>
  {
    public global::System.Collections.Generic.IEnumerable<SeedsPriceChanged> GetIndexed()
    {
      return new List<SeedsPriceChanged>
      {
      };
    }

    public SeedsPriceChanged GetNonIndexed()
    {
      return new SeedsPriceChanged
      {
        FtPriceList = FtPriceList,
        NftPriceList = NftPriceList,
      };
    }
  }

  public partial class SpecialSeedAdded : aelf::IEvent<SpecialSeedAdded>
  {
    public global::System.Collections.Generic.IEnumerable<SpecialSeedAdded> GetIndexed()
    {
      return new List<SpecialSeedAdded>
      {
      };
    }

    public SpecialSeedAdded GetNonIndexed()
    {
      return new SpecialSeedAdded
      {
        AddList = AddList,
      };
    }
  }

  public partial class SpecialSeedRemoved : aelf::IEvent<SpecialSeedRemoved>
  {
    public global::System.Collections.Generic.IEnumerable<SpecialSeedRemoved> GetIndexed()
    {
      return new List<SpecialSeedRemoved>
      {
      };
    }

    public SpecialSeedRemoved GetNonIndexed()
    {
      return new SpecialSeedRemoved
      {
        RemoveList = RemoveList,
      };
    }
  }

  public partial class Bought : aelf::IEvent<Bought>
  {
    public global::System.Collections.Generic.IEnumerable<Bought> GetIndexed()
    {
      return new List<Bought>
      {
      };
    }

    public Bought GetNonIndexed()
    {
      return new Bought
      {
        Buyer = Buyer,
        Symbol = Symbol,
        Price = Price,
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

  public partial class SymbolAuthorMapped : aelf::IEvent<SymbolAuthorMapped>
  {
    public global::System.Collections.Generic.IEnumerable<SymbolAuthorMapped> GetIndexed()
    {
      return new List<SymbolAuthorMapped>
      {
      };
    }

    public SymbolAuthorMapped GetNonIndexed()
    {
      return new SymbolAuthorMapped
      {
        Symbol = Symbol,
        Address = Address,
      };
    }
  }

  public partial class AuctionEndTimeExtended : aelf::IEvent<AuctionEndTimeExtended>
  {
    public global::System.Collections.Generic.IEnumerable<AuctionEndTimeExtended> GetIndexed()
    {
      return new List<AuctionEndTimeExtended>
      {
      };
    }

    public AuctionEndTimeExtended GetNonIndexed()
    {
      return new AuctionEndTimeExtended
      {
        Symbol = Symbol,
        NewEndTime = NewEndTime,
      };
    }
  }

  public partial class SeedExpirationConfigChanged : aelf::IEvent<SeedExpirationConfigChanged>
  {
    public global::System.Collections.Generic.IEnumerable<SeedExpirationConfigChanged> GetIndexed()
    {
      return new List<SeedExpirationConfigChanged>
      {
      };
    }

    public SeedExpirationConfigChanged GetNonIndexed()
    {
      return new SeedExpirationConfigChanged
      {
        SeedExpirationConfig = SeedExpirationConfig,
      };
    }
  }

  #endregion
  /// <summary>
  /// the contract definition: a gRPC service definition.
  /// </summary>
  public static partial class SymbolRegistrarContractContainer
  {
    static readonly string __ServiceName = "SymbolRegistrarContract";

    #region Marshallers
    static readonly aelf::Marshaller<global::Forest.SymbolRegistrar.InitializeInput> __Marshaller_InitializeInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.SymbolRegistrar.InitializeInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Google.Protobuf.WellKnownTypes.Empty> __Marshaller_google_protobuf_Empty = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Google.Protobuf.WellKnownTypes.Empty.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.SymbolRegistrar.CreateSeedInput> __Marshaller_CreateSeedInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.SymbolRegistrar.CreateSeedInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.SymbolRegistrar.IssueSeedInput> __Marshaller_IssueSeedInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.SymbolRegistrar.IssueSeedInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.SymbolRegistrar.BuyInput> __Marshaller_BuyInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.SymbolRegistrar.BuyInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.SymbolRegistrar.SpecialSeedList> __Marshaller_SpecialSeedList = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.SymbolRegistrar.SpecialSeedList.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::AElf.Types.Address> __Marshaller_aelf_Address = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::AElf.Types.Address.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Google.Protobuf.WellKnownTypes.Int64Value> __Marshaller_google_protobuf_Int64Value = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Google.Protobuf.WellKnownTypes.Int64Value.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.SymbolRegistrar.SeedsPriceInput> __Marshaller_SeedsPriceInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.SymbolRegistrar.SeedsPriceInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.SymbolRegistrar.AuctionConfig> __Marshaller_AuctionConfig = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.SymbolRegistrar.AuctionConfig.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.SymbolRegistrar.AddSaleControllerInput> __Marshaller_AddSaleControllerInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.SymbolRegistrar.AddSaleControllerInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.SymbolRegistrar.RemoveSaleControllerInput> __Marshaller_RemoveSaleControllerInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.SymbolRegistrar.RemoveSaleControllerInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.SymbolRegistrar.SeedExpirationConfig> __Marshaller_SeedExpirationConfig = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.SymbolRegistrar.SeedExpirationConfig.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.SymbolRegistrar.GetSeedsPriceOutput> __Marshaller_GetSeedsPriceOutput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.SymbolRegistrar.GetSeedsPriceOutput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Google.Protobuf.WellKnownTypes.StringValue> __Marshaller_google_protobuf_StringValue = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Google.Protobuf.WellKnownTypes.StringValue.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.SymbolRegistrar.SpecialSeed> __Marshaller_SpecialSeed = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.SymbolRegistrar.SpecialSeed.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.SymbolRegistrar.BizConfig> __Marshaller_BizConfig = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.SymbolRegistrar.BizConfig.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Forest.SymbolRegistrar.ControllerList> __Marshaller_ControllerList = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Forest.SymbolRegistrar.ControllerList.Parser.ParseFrom);
    #endregion

    #region Methods
    static readonly aelf::Method<global::Forest.SymbolRegistrar.InitializeInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_Initialize = new aelf::Method<global::Forest.SymbolRegistrar.InitializeInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "Initialize",
        __Marshaller_InitializeInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.SymbolRegistrar.CreateSeedInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_CreateSeed = new aelf::Method<global::Forest.SymbolRegistrar.CreateSeedInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "CreateSeed",
        __Marshaller_CreateSeedInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.SymbolRegistrar.IssueSeedInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_IssueSeed = new aelf::Method<global::Forest.SymbolRegistrar.IssueSeedInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "IssueSeed",
        __Marshaller_IssueSeedInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.SymbolRegistrar.BuyInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_Buy = new aelf::Method<global::Forest.SymbolRegistrar.BuyInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "Buy",
        __Marshaller_BuyInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.SymbolRegistrar.SpecialSeedList, global::Google.Protobuf.WellKnownTypes.Empty> __Method_AddSpecialSeeds = new aelf::Method<global::Forest.SymbolRegistrar.SpecialSeedList, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "AddSpecialSeeds",
        __Marshaller_SpecialSeedList,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.SymbolRegistrar.SpecialSeedList, global::Google.Protobuf.WellKnownTypes.Empty> __Method_RemoveSpecialSeeds = new aelf::Method<global::Forest.SymbolRegistrar.SpecialSeedList, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "RemoveSpecialSeeds",
        __Marshaller_SpecialSeedList,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::AElf.Types.Address, global::Google.Protobuf.WellKnownTypes.Empty> __Method_SetAdmin = new aelf::Method<global::AElf.Types.Address, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "SetAdmin",
        __Marshaller_aelf_Address,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Google.Protobuf.WellKnownTypes.Int64Value, global::Google.Protobuf.WellKnownTypes.Empty> __Method_SetLastSeedId = new aelf::Method<global::Google.Protobuf.WellKnownTypes.Int64Value, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "SetLastSeedId",
        __Marshaller_google_protobuf_Int64Value,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::AElf.Types.Address, global::Google.Protobuf.WellKnownTypes.Empty> __Method_SetReceivingAccount = new aelf::Method<global::AElf.Types.Address, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "SetReceivingAccount",
        __Marshaller_aelf_Address,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.SymbolRegistrar.SeedsPriceInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_SetSeedsPrice = new aelf::Method<global::Forest.SymbolRegistrar.SeedsPriceInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "SetSeedsPrice",
        __Marshaller_SeedsPriceInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.SymbolRegistrar.AuctionConfig, global::Google.Protobuf.WellKnownTypes.Empty> __Method_SetAuctionConfig = new aelf::Method<global::Forest.SymbolRegistrar.AuctionConfig, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "SetAuctionConfig",
        __Marshaller_AuctionConfig,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.SymbolRegistrar.AddSaleControllerInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_AddSaleController = new aelf::Method<global::Forest.SymbolRegistrar.AddSaleControllerInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "AddSaleController",
        __Marshaller_AddSaleControllerInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.SymbolRegistrar.RemoveSaleControllerInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_RemoveSaleController = new aelf::Method<global::Forest.SymbolRegistrar.RemoveSaleControllerInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "RemoveSaleController",
        __Marshaller_RemoveSaleControllerInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Forest.SymbolRegistrar.SeedExpirationConfig, global::Google.Protobuf.WellKnownTypes.Empty> __Method_SetSeedExpirationConfig = new aelf::Method<global::Forest.SymbolRegistrar.SeedExpirationConfig, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "SetSeedExpirationConfig",
        __Marshaller_SeedExpirationConfig,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Forest.SymbolRegistrar.GetSeedsPriceOutput> __Method_GetSeedsPrice = new aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Forest.SymbolRegistrar.GetSeedsPriceOutput>(
        aelf::MethodType.View,
        __ServiceName,
        "GetSeedsPrice",
        __Marshaller_google_protobuf_Empty,
        __Marshaller_GetSeedsPriceOutput);

    static readonly aelf::Method<global::Google.Protobuf.WellKnownTypes.StringValue, global::Forest.SymbolRegistrar.SpecialSeed> __Method_GetSpecialSeed = new aelf::Method<global::Google.Protobuf.WellKnownTypes.StringValue, global::Forest.SymbolRegistrar.SpecialSeed>(
        aelf::MethodType.View,
        __ServiceName,
        "GetSpecialSeed",
        __Marshaller_google_protobuf_StringValue,
        __Marshaller_SpecialSeed);

    static readonly aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Forest.SymbolRegistrar.BizConfig> __Method_GetBizConfig = new aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Forest.SymbolRegistrar.BizConfig>(
        aelf::MethodType.View,
        __ServiceName,
        "GetBizConfig",
        __Marshaller_google_protobuf_Empty,
        __Marshaller_BizConfig);

    static readonly aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Forest.SymbolRegistrar.SeedExpirationConfig> __Method_GetSeedExpirationConfig = new aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Forest.SymbolRegistrar.SeedExpirationConfig>(
        aelf::MethodType.View,
        __ServiceName,
        "GetSeedExpirationConfig",
        __Marshaller_google_protobuf_Empty,
        __Marshaller_SeedExpirationConfig);

    static readonly aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Forest.SymbolRegistrar.AuctionConfig> __Method_GetAuctionConfig = new aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Forest.SymbolRegistrar.AuctionConfig>(
        aelf::MethodType.View,
        __ServiceName,
        "GetAuctionConfig",
        __Marshaller_google_protobuf_Empty,
        __Marshaller_AuctionConfig);

    static readonly aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Forest.SymbolRegistrar.ControllerList> __Method_GetSaleController = new aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Forest.SymbolRegistrar.ControllerList>(
        aelf::MethodType.View,
        __ServiceName,
        "GetSaleController",
        __Marshaller_google_protobuf_Empty,
        __Marshaller_ControllerList);

    #endregion

    #region Descriptors
    public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
    {
      get { return global::Forest.SymbolRegistrar.SymbolRegistrarContractReflection.Descriptor.Services[0]; }
    }

    public static global::System.Collections.Generic.IReadOnlyList<global::Google.Protobuf.Reflection.ServiceDescriptor> Descriptors
    {
      get
      {
        return new global::System.Collections.Generic.List<global::Google.Protobuf.Reflection.ServiceDescriptor>()
        {
          global::AElf.Standards.ACS12.Acs12Reflection.Descriptor.Services[0],
          global::Forest.SymbolRegistrar.SymbolRegistrarContractReflection.Descriptor.Services[0],
        };
      }
    }
    #endregion

    /// <summary>Base class for the contract of SymbolRegistrarContract</summary>
    
  }
}
#endregion

