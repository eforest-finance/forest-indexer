// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: cross_chain_contract.proto
// </auto-generated>
// Original file comments:
// *
// Cross-Chain contract.
#pragma warning disable 0414, 1591
#region Designer generated code

using System.Collections.Generic;
using aelf = global::AElf.CSharp.Core;

namespace AElf.Contracts.CrossChain {

  #region Events
  public partial class SideChainCreatedEvent : aelf::IEvent<SideChainCreatedEvent>
  {
    public global::System.Collections.Generic.IEnumerable<SideChainCreatedEvent> GetIndexed()
    {
      return new List<SideChainCreatedEvent>
      {
      };
    }

    public SideChainCreatedEvent GetNonIndexed()
    {
      return new SideChainCreatedEvent
      {
        Creator = Creator,
        ChainId = ChainId,
      };
    }
  }

  public partial class Disposed : aelf::IEvent<Disposed>
  {
    public global::System.Collections.Generic.IEnumerable<Disposed> GetIndexed()
    {
      return new List<Disposed>
      {
      };
    }

    public Disposed GetNonIndexed()
    {
      return new Disposed
      {
        ChainId = ChainId,
      };
    }
  }

  public partial class SideChainLifetimeControllerChanged : aelf::IEvent<SideChainLifetimeControllerChanged>
  {
    public global::System.Collections.Generic.IEnumerable<SideChainLifetimeControllerChanged> GetIndexed()
    {
      return new List<SideChainLifetimeControllerChanged>
      {
      };
    }

    public SideChainLifetimeControllerChanged GetNonIndexed()
    {
      return new SideChainLifetimeControllerChanged
      {
        AuthorityInfo = AuthorityInfo,
      };
    }
  }

  public partial class CrossChainIndexingControllerChanged : aelf::IEvent<CrossChainIndexingControllerChanged>
  {
    public global::System.Collections.Generic.IEnumerable<CrossChainIndexingControllerChanged> GetIndexed()
    {
      return new List<CrossChainIndexingControllerChanged>
      {
      };
    }

    public CrossChainIndexingControllerChanged GetNonIndexed()
    {
      return new CrossChainIndexingControllerChanged
      {
        AuthorityInfo = AuthorityInfo,
      };
    }
  }

  public partial class SideChainIndexingFeeControllerChanged : aelf::IEvent<SideChainIndexingFeeControllerChanged>
  {
    public global::System.Collections.Generic.IEnumerable<SideChainIndexingFeeControllerChanged> GetIndexed()
    {
      return new List<SideChainIndexingFeeControllerChanged>
      {
      new SideChainIndexingFeeControllerChanged
      {
        ChainId = ChainId
      },
      };
    }

    public SideChainIndexingFeeControllerChanged GetNonIndexed()
    {
      return new SideChainIndexingFeeControllerChanged
      {
        AuthorityInfo = AuthorityInfo,
      };
    }
  }

  #endregion
}
#endregion
