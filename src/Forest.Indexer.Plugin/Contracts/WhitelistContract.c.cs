// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: whitelist_contract.proto
// </auto-generated>
#pragma warning disable 0414, 1591
#region Designer generated code

using System.Collections.Generic;
using aelf = global::AElf.CSharp.Core;

namespace Forest.Whitelist {

  #region Events
  public partial class WhitelistCreated : aelf::IEvent<WhitelistCreated>
  {
    public global::System.Collections.Generic.IEnumerable<WhitelistCreated> GetIndexed()
    {
      return new List<WhitelistCreated>
      {
      };
    }

    public WhitelistCreated GetNonIndexed()
    {
      return new WhitelistCreated
      {
        WhitelistId = WhitelistId,
        ExtraInfoIdList = ExtraInfoIdList,
        IsAvailable = IsAvailable,
        IsCloneable = IsCloneable,
        Remark = Remark,
        CloneFrom = CloneFrom,
        Creator = Creator,
        Manager = Manager,
        ProjectId = ProjectId,
        StrategyType = StrategyType,
      };
    }
  }

  public partial class WhitelistSubscribed : aelf::IEvent<WhitelistSubscribed>
  {
    public global::System.Collections.Generic.IEnumerable<WhitelistSubscribed> GetIndexed()
    {
      return new List<WhitelistSubscribed>
      {
      };
    }

    public WhitelistSubscribed GetNonIndexed()
    {
      return new WhitelistSubscribed
      {
        SubscribeId = SubscribeId,
        ProjectId = ProjectId,
        WhitelistId = WhitelistId,
        Subscriber = Subscriber,
        ManagerList = ManagerList,
      };
    }
  }

  public partial class WhitelistUnsubscribed : aelf::IEvent<WhitelistUnsubscribed>
  {
    public global::System.Collections.Generic.IEnumerable<WhitelistUnsubscribed> GetIndexed()
    {
      return new List<WhitelistUnsubscribed>
      {
      };
    }

    public WhitelistUnsubscribed GetNonIndexed()
    {
      return new WhitelistUnsubscribed
      {
        SubscribeId = SubscribeId,
        ProjectId = ProjectId,
        WhitelistId = WhitelistId,
      };
    }
  }

  public partial class WhitelistAddressInfoAdded : aelf::IEvent<WhitelistAddressInfoAdded>
  {
    public global::System.Collections.Generic.IEnumerable<WhitelistAddressInfoAdded> GetIndexed()
    {
      return new List<WhitelistAddressInfoAdded>
      {
      };
    }

    public WhitelistAddressInfoAdded GetNonIndexed()
    {
      return new WhitelistAddressInfoAdded
      {
        WhitelistId = WhitelistId,
        ExtraInfoIdList = ExtraInfoIdList,
      };
    }
  }

  public partial class WhitelistAddressInfoRemoved : aelf::IEvent<WhitelistAddressInfoRemoved>
  {
    public global::System.Collections.Generic.IEnumerable<WhitelistAddressInfoRemoved> GetIndexed()
    {
      return new List<WhitelistAddressInfoRemoved>
      {
      };
    }

    public WhitelistAddressInfoRemoved GetNonIndexed()
    {
      return new WhitelistAddressInfoRemoved
      {
        WhitelistId = WhitelistId,
        ExtraInfoIdList = ExtraInfoIdList,
      };
    }
  }

  public partial class WhitelistDisabled : aelf::IEvent<WhitelistDisabled>
  {
    public global::System.Collections.Generic.IEnumerable<WhitelistDisabled> GetIndexed()
    {
      return new List<WhitelistDisabled>
      {
      };
    }

    public WhitelistDisabled GetNonIndexed()
    {
      return new WhitelistDisabled
      {
        WhitelistId = WhitelistId,
        IsAvailable = IsAvailable,
      };
    }
  }

  public partial class WhitelistReenable : aelf::IEvent<WhitelistReenable>
  {
    public global::System.Collections.Generic.IEnumerable<WhitelistReenable> GetIndexed()
    {
      return new List<WhitelistReenable>
      {
      };
    }

    public WhitelistReenable GetNonIndexed()
    {
      return new WhitelistReenable
      {
        WhitelistId = WhitelistId,
        IsAvailable = IsAvailable,
      };
    }
  }

  public partial class ConsumedListAdded : aelf::IEvent<ConsumedListAdded>
  {
    public global::System.Collections.Generic.IEnumerable<ConsumedListAdded> GetIndexed()
    {
      return new List<ConsumedListAdded>
      {
      };
    }

    public ConsumedListAdded GetNonIndexed()
    {
      return new ConsumedListAdded
      {
        SubscribeId = SubscribeId,
        WhitelistId = WhitelistId,
        ExtraInfoIdList = ExtraInfoIdList,
      };
    }
  }

  public partial class IsCloneableChanged : aelf::IEvent<IsCloneableChanged>
  {
    public global::System.Collections.Generic.IEnumerable<IsCloneableChanged> GetIndexed()
    {
      return new List<IsCloneableChanged>
      {
      };
    }

    public IsCloneableChanged GetNonIndexed()
    {
      return new IsCloneableChanged
      {
        WhitelistId = WhitelistId,
        IsCloneable = IsCloneable,
      };
    }
  }

  public partial class TagInfoAdded : aelf::IEvent<TagInfoAdded>
  {
    public global::System.Collections.Generic.IEnumerable<TagInfoAdded> GetIndexed()
    {
      return new List<TagInfoAdded>
      {
      };
    }

    public TagInfoAdded GetNonIndexed()
    {
      return new TagInfoAdded
      {
        TagInfoId = TagInfoId,
        TagInfo = TagInfo,
        ProjectId = ProjectId,
        WhitelistId = WhitelistId,
      };
    }
  }

  public partial class TagInfoRemoved : aelf::IEvent<TagInfoRemoved>
  {
    public global::System.Collections.Generic.IEnumerable<TagInfoRemoved> GetIndexed()
    {
      return new List<TagInfoRemoved>
      {
      };
    }

    public TagInfoRemoved GetNonIndexed()
    {
      return new TagInfoRemoved
      {
        TagInfoId = TagInfoId,
        TagInfo = TagInfo,
        ProjectId = ProjectId,
        WhitelistId = WhitelistId,
      };
    }
  }

  public partial class ExtraInfoUpdated : aelf::IEvent<ExtraInfoUpdated>
  {
    public global::System.Collections.Generic.IEnumerable<ExtraInfoUpdated> GetIndexed()
    {
      return new List<ExtraInfoUpdated>
      {
      };
    }

    public ExtraInfoUpdated GetNonIndexed()
    {
      return new ExtraInfoUpdated
      {
        WhitelistId = WhitelistId,
        ExtraInfoIdBefore = ExtraInfoIdBefore,
        ExtraInfoIdAfter = ExtraInfoIdAfter,
      };
    }
  }

  public partial class ManagerTransferred : aelf::IEvent<ManagerTransferred>
  {
    public global::System.Collections.Generic.IEnumerable<ManagerTransferred> GetIndexed()
    {
      return new List<ManagerTransferred>
      {
      };
    }

    public ManagerTransferred GetNonIndexed()
    {
      return new ManagerTransferred
      {
        WhitelistId = WhitelistId,
        TransferFrom = TransferFrom,
        TransferTo = TransferTo,
      };
    }
  }

  public partial class ManagerAdded : aelf::IEvent<ManagerAdded>
  {
    public global::System.Collections.Generic.IEnumerable<ManagerAdded> GetIndexed()
    {
      return new List<ManagerAdded>
      {
      };
    }

    public ManagerAdded GetNonIndexed()
    {
      return new ManagerAdded
      {
        WhitelistId = WhitelistId,
        ManagerList = ManagerList,
      };
    }
  }

  public partial class ManagerRemoved : aelf::IEvent<ManagerRemoved>
  {
    public global::System.Collections.Generic.IEnumerable<ManagerRemoved> GetIndexed()
    {
      return new List<ManagerRemoved>
      {
      };
    }

    public ManagerRemoved GetNonIndexed()
    {
      return new ManagerRemoved
      {
        WhitelistId = WhitelistId,
        ManagerList = ManagerList,
      };
    }
  }

  public partial class WhitelistReset : aelf::IEvent<WhitelistReset>
  {
    public global::System.Collections.Generic.IEnumerable<WhitelistReset> GetIndexed()
    {
      return new List<WhitelistReset>
      {
      };
    }

    public WhitelistReset GetNonIndexed()
    {
      return new WhitelistReset
      {
        WhitelistId = WhitelistId,
        ProjectId = ProjectId,
      };
    }
  }

  public partial class SubscribeManagerAdded : aelf::IEvent<SubscribeManagerAdded>
  {
    public global::System.Collections.Generic.IEnumerable<SubscribeManagerAdded> GetIndexed()
    {
      return new List<SubscribeManagerAdded>
      {
      };
    }

    public SubscribeManagerAdded GetNonIndexed()
    {
      return new SubscribeManagerAdded
      {
        SubscribeId = SubscribeId,
        ManagerList = ManagerList,
      };
    }
  }

  public partial class SubscribeManagerRemoved : aelf::IEvent<SubscribeManagerRemoved>
  {
    public global::System.Collections.Generic.IEnumerable<SubscribeManagerRemoved> GetIndexed()
    {
      return new List<SubscribeManagerRemoved>
      {
      };
    }

    public SubscribeManagerRemoved GetNonIndexed()
    {
      return new SubscribeManagerRemoved
      {
        SubscribeId = SubscribeId,
        ManagerList = ManagerList,
      };
    }
  }

  #endregion
}
#endregion
