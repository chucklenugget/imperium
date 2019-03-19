﻿namespace Oxide.Plugins
{
  using Rust;
  using System;
  using UnityEngine;

  public partial class Imperium
  {
    class Area : MonoBehaviour
    {
      public static bool IsPvp(Area area)
      {
        if (area == null)
          return Instance.Options.Pvp.AllowedInDeepWater;

        if (area.IsWarZone)
          return true;

        switch (area.Type)
        {
          case AreaType.Badlands:
            return Instance.Options.Pvp.AllowedInBadlands;
          case AreaType.Claimed:
          case AreaType.Headquarters:
            return (area.IsPvpEnabled != null) ? area.IsPvpEnabled.Value : Instance.Options.Pvp.AllowedInClaimedLand;
          case AreaType.Wilderness:
            return Instance.Options.Pvp.AllowedInWilderness;
          default:
            throw new InvalidOperationException($"Unknown area type {area.Type}");
        }
      }

      public Vector3 Position { get; private set; }
      public Vector3 Size { get; private set; }

      public string Id { get; private set; }
      public int Row { get; private set; }
      public int Col { get; private set; }

      public AreaType Type { get; set; }
      public string Name { get; set; }
      public string FactionId { get; set; }
      public string ClaimantId { get; set; }
      public BuildingPrivlidge ClaimCupboard { get; set; }
      public bool? IsPvpEnabled { get; private set; }
      public bool? PendingIsPvpEnabled { get; private set; }

      public bool IsClaimed
      {
        get { return FactionId != null; }
      }

      public bool IsTaxableClaim
      {
        get { return Type == AreaType.Claimed || Type == AreaType.Headquarters; }
      }

      public bool IsWarZone
      {
        get { return GetActiveWars().Length > 0; }
      }

      public bool HasPendingRuleChange
      {
        get { return PendingIsPvpEnabled != null; }
      }

      public void SetPvpEnabled(bool isPvpEnabled)
      {
        if (HasPendingRuleChange)
          throw new InvalidOperationException("Attempted to change rules on an area where a rule change was already pending");

        PendingIsPvpEnabled = isPvpEnabled;
        Invoke(nameof(CommitPvpRuleChange), Instance.Options.Pvp.RuleChangeDelaySeconds);
      }

      public void Init(string id, int row, int col, Vector3 position, Vector3 size, AreaInfo info)
      {
        Id = id;
        Row = row;
        Col = col;
        Position = position;
        Size = size;

        if (info != null)
          TryLoadInfo(info);

        gameObject.layer = (int)Layer.Reserved1;
        gameObject.name = $"imperium_area_{id}";
        transform.position = position;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));

        var collider = gameObject.AddComponent<BoxCollider>();
        collider.size = Size;
        collider.isTrigger = true;
        collider.enabled = true;

        gameObject.SetActive(true);
        enabled = true;
      }

      void Awake()
      {
        InvokeRepeating(nameof(CheckClaimCupboard), 60f, 60f);
      }

      void OnDestroy()
      {
        var collider = GetComponent<BoxCollider>();

        if (collider != null)
          Destroy(collider);

        if (IsInvoking(nameof(CheckClaimCupboard)))
          CancelInvoke(nameof(CheckClaimCupboard));
      }

      void TryLoadInfo(AreaInfo info)
      {
        BuildingPrivlidge cupboard = null;

        if (info.CupboardId != null)
        {
          cupboard = BaseNetworkable.serverEntities.Find((uint)info.CupboardId) as BuildingPrivlidge;
          if (cupboard == null)
          {
            Instance.Log($"[LOAD] Area {Id}: Cupboard entity {info.CupboardId} not found, treating as unclaimed");
            return;
          }
        }

        if (info.FactionId != null)
        {
          Faction faction = Instance.Factions.Get(info.FactionId);
          if (faction == null)
          {
            Instance.Log($"[LOAD] Area {Id}: Claimed by unknown faction {info.FactionId}, treating as unclaimed");
            return;
          }
        }

        Name = info.Name;
        Type = info.Type;
        FactionId = info.FactionId;
        ClaimantId = info.ClaimantId;
        ClaimCupboard = cupboard;
        IsPvpEnabled = info.Pvp;

        if (FactionId != null)
          Instance.Log($"[LOAD] Area {Id}: Claimed by {FactionId}, type = {Type}, cupboard = {Util.Format(ClaimCupboard)}");
      }

      void CheckClaimCupboard()
      {
        if (ClaimCupboard == null || !ClaimCupboard.IsDestroyed)
          return;

        Instance.Log($"{FactionId} lost their claim on {Id} because the tool cupboard was destroyed (periodic check)");
        Instance.PrintToChat(Messages.AreaClaimLostCupboardDestroyedAnnouncement, FactionId, Id);
        Instance.Areas.Unclaim(this);
      }

      void CommitPvpRuleChange()
      {
        IsPvpEnabled = PendingIsPvpEnabled;
        PendingIsPvpEnabled = null;
        Events.OnAreaChanged(this);
      }

      void OnTriggerEnter(Collider collider)
      {
        if (collider.gameObject.layer != (int)Layer.Player_Server)
          return;

        var user = collider.GetComponentInParent<User>();

        if (user != null && user.CurrentArea != this)
          Events.OnUserEnteredArea(user, this);
      }

      void OnTriggerExit(Collider collider)
      {
        if (collider.gameObject.layer != (int)Layer.Player_Server)
          return;

        var user = collider.GetComponentInParent<User>();

        if (user != null)
          Events.OnUserLeftArea(user, this);
      }

      public float GetDistanceFromEntity(BaseEntity entity)
      {
        return Vector3.Distance(entity.transform.position, transform.position);
      }

      public int GetClaimCost(Faction faction)
      {
        var costs = Instance.Options.Claims.Costs;
        int numberOfAreasOwned = Instance.Areas.GetAllClaimedByFaction(faction).Length;
        int index = Mathf.Clamp(numberOfAreasOwned, 0, costs.Count - 1);
        return costs[index];
      }

      public float GetDefensiveBonus()
      {
        var bonuses = Instance.Options.War.DefensiveBonuses;
        var depth = Instance.Areas.GetDepthInsideFriendlyTerritory(this);
        int index = Mathf.Clamp(depth, 0, bonuses.Count - 1);
        return bonuses[index];
      }

      public float GetTaxRate()
      {
        if (!IsTaxableClaim)
          return 0;

        Faction faction = Instance.Factions.Get(FactionId);

        if (!faction.CanCollectTaxes)
          return 0;

        return faction.TaxRate;
      }

      public War[] GetActiveWars()
      {
        if (FactionId == null)
          return new War[0];

        return Instance.Wars.GetAllActiveWarsByFaction(FactionId);
      }

      public AreaInfo Serialize()
      {
        return new AreaInfo {
          Id = Id,
          Name = Name,
          Type = Type,
          FactionId = FactionId,
          ClaimantId = ClaimantId,
          CupboardId = ClaimCupboard?.net?.ID,
          Pvp = IsPvpEnabled
        };
      }
    }
  }
}
