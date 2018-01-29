﻿namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;

  public partial class Imperium
  {
    class Faction
    {
      public string Id { get; private set; }
      public string OwnerId { get; private set; }
      public HashSet<string> MemberIds { get; }
      public HashSet<string> ManagerIds { get; }
      public HashSet<string> InviteIds { get; }

      public float TaxRate { get; set; }
      public StorageContainer TaxChest { get; set; }
      public DateTime NextUpkeepPaymentTime { get; set; }

      public bool CanCollectTaxes
      {
        get { return TaxRate != 0 && TaxChest != null; }
      }

      public bool IsUpkeepPaid
      {
        get { return DateTime.UtcNow < NextUpkeepPaymentTime; }
      }

      public Faction(string id, User owner)
      {
        Id = id;

        OwnerId = owner.Id;
        MemberIds = new HashSet<string> { owner.Id };
        ManagerIds = new HashSet<string>();
        InviteIds = new HashSet<string>();

        TaxRate = Instance.Options.DefaultTaxRate;
        NextUpkeepPaymentTime = DateTime.UtcNow.AddHours(Instance.Options.UpkeepCollectionPeriodHours);
      }

      public Faction(FactionInfo info)
      {
        Id = info.Id;

        OwnerId = info.OwnerId;
        MemberIds = new HashSet<string>(info.MemberIds);
        ManagerIds = new HashSet<string>(info.ManagerIds);
        MemberIds = new HashSet<string>(info.MemberIds);

        if (info.TaxChestId != null)
        {
          var taxChest = BaseNetworkable.serverEntities.Find((uint)info.TaxChestId) as StorageContainer;

          if (taxChest == null || taxChest.IsDestroyed)
            Instance.PrintWarning($"Couldn't find entity {info.TaxChestId} for faction {Id}'s tax chest. Ignoring.");
          else
            TaxChest = taxChest;
        }

        TaxRate = info.TaxRate;
        NextUpkeepPaymentTime = info.NextUpkeepPaymentTime;
      }

      public bool AddMember(User user)
      {
        if (!MemberIds.Add(user.Id))
          return false;

        InviteIds.Remove(user.Id);

        Api.HandlePlayerJoinedFaction(this, user);
        return true;
      }

      public bool RemoveMember(User user)
      {
        if (!HasMember(user.Id))
          return false;

        if (HasOwner(user.Id))
        {
          if (ManagerIds.Count > 0)
          {
            OwnerId = ManagerIds.FirstOrDefault();
            ManagerIds.Remove(OwnerId);
          }
          else
          {
            OwnerId = MemberIds.FirstOrDefault();
          }
        }

        MemberIds.Remove(user.Id);
        ManagerIds.Remove(user.Id);

        Api.HandlePlayerLeftFaction(this, user);
        return true;
      }

      public bool AddInvite(User user)
      {
        if (!InviteIds.Add(user.Id))
          return false;

        Api.HandlePlayerInvitedToFaction(this, user);
        return true;
      }

      public bool RemoveInvite(User user)
      {
        if (!InviteIds.Remove(user.Id))
          return false;

        Api.HandlePlayerUninvitedFromFaction(this, user);
        return true;
      }

      public bool Promote(User user)
      {
        if (!MemberIds.Contains(user.Id))
          throw new InvalidOperationException($"Cannot promote player {user.Id} in faction {Id}, since they are not a member");

        if (!ManagerIds.Add(user.Id))
          return false;

        Api.HandlePlayerPromoted(this, user);
        return true;
      }

      public bool Demote(User user)
      {
        if (!MemberIds.Contains(user.Id))
          throw new InvalidOperationException($"Cannot demote player {user.Id} in faction {Id}, since they are not a member");

        if (!ManagerIds.Remove(user.Id))
          return false;

        Api.HandlePlayerDemoted(this, user);
        return true;
      }

      public bool HasOwner(User user)
      {
        return HasOwner(user.Id);
      }

      public bool HasOwner(string userId)
      {
        return OwnerId == userId;
      }

      public bool HasLeader(User user)
      {
        return HasLeader(user.Id);
      }

      public bool HasLeader(string userId)
      {
        return HasOwner(userId) || HasManager(userId);
      }

      public bool HasManager(User user)
      {
        return HasManager(user.Id);
      }

      public bool HasManager(string userId)
      {
        return ManagerIds.Contains(userId);
      }

      public bool HasInvite(User user)
      {
        return HasInvite(user.Player.UserIDString);
      }

      public bool HasInvite(string userId)
      {
        return InviteIds.Contains(userId);
      }

      public bool HasMember(User user)
      {
        return HasMember(user.Player.UserIDString);
      }

      public bool HasMember(string userId)
      {
        return MemberIds.Contains(userId);
      }

      public User[] GetAllOnlineUsers()
      {
        return MemberIds.Select(id => Instance.Users.Get(id)).Where(user => user != null).ToArray();
      }

      public void SendChatMessage(string message, params object[] args)
      {
        foreach (User user in GetAllOnlineUsers())
          user.SendChatMessage(message, args);
      }

      public int GetUpkeepPerPeriod()
      {
        var costs = Instance.Options.UpkeepCosts;

        int totalCost = 0;
        for (var num = 0; num < Instance.Areas.GetAllClaimedByFaction(this).Length; num++)
        {
          var index = Mathf.Clamp(num, 0, costs.Count - 1);
          totalCost += costs[index];
        }

        return totalCost;
      }

      public FactionInfo Serialize()
      {
        return new FactionInfo {
          Id = Id,
          OwnerId = OwnerId,
          MemberIds = MemberIds.ToArray(),
          ManagerIds = ManagerIds.ToArray(),
          InviteIds = InviteIds.ToArray(),
          TaxRate = TaxRate,
          TaxChestId = TaxChest?.net?.ID,
          NextUpkeepPaymentTime = NextUpkeepPaymentTime
        };
      }
    }
  }
}
