namespace Oxide.Plugins
{
  using Rust;
  using UnityEngine;

  public partial class Imperium
  {
    class Area : MonoBehaviour
    {
      Imperium Core;

      public Vector3 Position { get; private set; }
      public Vector3 Size { get; private set; }

      public string Id { get; private set; }
      public int Row { get; private set; }
      public int Col { get; private set; }

      public AreaType Type { get; set; }
      public string Name { get; set; }
      public string FactionId { get; set; }
      public ulong? ClaimantId { get; set; }
      public BuildingPrivlidge ClaimCupboard { get; set; }

      public bool IsClaimed
      {
        get { return FactionId != null; }
      }

      public bool IsTaxableClaim
      {
        get { return Type == AreaType.Claimed || Type == AreaType.Headquarters; }
      }

      public bool IsWarzone
      {
        get { return GetActiveWars().Length > 0; }
      }

      public void Init(Imperium core, string id, int row, int col, Vector3 position, Vector3 size, AreaInfo info)
      {
        Core = core;
        Id = id;
        Row = row;
        Col = col;
        Position = position;
        Size = size;

        if (info != null)
          TryLoadInfo(info);

        gameObject.layer = (int)Layer.Reserved1;
        gameObject.name = $"Imperium Area {id}";
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
        InvokeRepeating("CheckClaimCupboard", 60f, 60f);
      }

      void OnDestroy()
      {
        if (IsInvoking("CheckClaimCupboard"))
          CancelInvoke("CheckClaimCupboard");
      }

      void TryLoadInfo(AreaInfo info)
      {
        BuildingPrivlidge cupboard = null;

        if (info.CupboardId != null)
        {
          cupboard = BaseNetworkable.serverEntities.Find((uint)info.CupboardId) as BuildingPrivlidge;
          if (cupboard == null)
          {
            Core.PrintWarning($"Couldn't find cupboard entity {info.CupboardId} for area {info.AreaId}.");
            return;
          }
        }

        if (info.FactionId != null)
        {
          Faction faction = Core.Factions.Get(info.FactionId);
          if (faction == null)
          {
            Core.PrintWarning($"Area {Id} is owned by unknown faction {info.FactionId}. Ignoring.");
            return;
          }
        }

        Name = info.Name;
        Type = info.Type;
        FactionId = info.FactionId;
        ClaimantId = info.ClaimantId;
        ClaimCupboard = cupboard;
      }

      void CheckClaimCupboard()
      {
        if (ClaimCupboard == null || !ClaimCupboard.IsDestroyed)
          return;

        Core.PrintWarning($"Cupboard entity {ClaimCupboard.net.ID} was destroyed, but is still holding a claim on area {Id}. Removing claim.");
        Core.PrintToChat(Messages.AreaClaimLostCupboardDestroyedAnnouncement, FactionId, Id);
        Core.Areas.Unclaim(this);
      }

      void OnTriggerEnter(Collider collider)
      {
        var user = collider.GetComponentInParent<User>();
        if (user != null && user.CurrentArea != this)
          Core.OnUserEnterArea(this, user);
      }

      void OnTriggerExit(Collider collider)
      {
        var user = collider.GetComponentInParent<User>();
        if (user != null)
          Core.OnUserExitArea(this, user);
      }

      public float GetDistanceFromEntity(BaseEntity entity)
      {
        return Vector3.Distance(entity.transform.position, transform.position);
      }

      public int GetClaimCost(Faction faction)
      {
        var costs = Core.Options.ClaimCosts;
        int numberOfAreasOwned = Core.Areas.GetAllClaimedByFaction(faction).Length;
        int index = Mathf.Clamp(numberOfAreasOwned, 0, costs.Count - 1);
        return costs[index];
      }

      public float GetDefensiveBonus()
      {
        var bonuses = Core.Options.DefensiveBonuses;
        var depth = Core.Areas.GetDepthInsideFriendlyTerritory(this);
        int index = Mathf.Clamp(depth, 0, bonuses.Count - 1);
        return bonuses[index];
      }

      public float GetTaxRate()
      {
        if (!IsTaxableClaim)
          return 0;

        Faction faction = Core.Factions.Get(FactionId);

        if (!faction.CanCollectTaxes)
          return 0;

        return faction.TaxRate;
      }

      public War[] GetActiveWars()
      {
        if (FactionId == null)
          return new War[0];

        return Core.Wars.GetAllActiveWarsByFaction(FactionId);
      }

      public AreaInfo Serialize()
      {
        return new AreaInfo {
          AreaId = Id,
          Name = Name,
          Type = Type,
          FactionId = FactionId,
          ClaimantId = ClaimantId,
          CupboardId = ClaimCupboard?.net.ID
        };
      }
    }
  }
}
