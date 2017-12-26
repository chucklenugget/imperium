namespace Oxide.Plugins
{
  using Rust;
  using UnityEngine;

  public partial class RustFactions
  {
    class Area : MonoBehaviour
    {
      RustFactions Core;

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

      public bool IsTaxableClaim
      {
        get { return Type == AreaType.Claimed || Type == AreaType.Headquarters; }
      }

      public void Init(RustFactions core, string id, int row, int col, Vector3 position, Vector3 size, AreaInfo info)
      {
        Core = core;
        Id = id;
        Row = row;
        Col = col;
        Position = position;
        Size = size;

        if (info != null && info.Type != AreaType.Unclaimed)
          TryLoadInfo(info);

        gameObject.layer = (int)Layer.Reserved1;
        gameObject.name = $"RustFactions Area {id}";
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
        var cupboard = BaseNetworkable.serverEntities.Find((uint)info.CupboardId) as BuildingPrivlidge;

        if (cupboard == null)
        {
          Core.PrintWarning($"Couldn't find cupboard entity {info.CupboardId} for area {info.AreaId}. Treating as unclaimed.");
          return;
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
