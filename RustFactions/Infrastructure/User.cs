namespace Oxide.Plugins
{
  using System.Text;
  using UnityEngine;

  public partial class RustFactions
  {
    class User : MonoBehaviour
    {
      RustFactions Core;

      public BasePlayer Player { get; private set; }
      public UserMap Map { get; private set; }
      public UserLocationPanel LocationPanel { get; private set; }

      public Area CurrentArea { get; set; }
      public Interaction CurrentInteraction { get; private set; }

      public ulong Id
      {
        get { return Player.userID; }
      }

      public void Init(RustFactions core, BasePlayer player)
      {
        Core = core;
        Player = player;
        Map = new UserMap(core, this);
        LocationPanel = new UserLocationPanel(core, this);
      }

      void Awake()
      {
        InvokeRepeating("CheckArea", 2f, 2f);
      }

      void OnDestroy()
      {
        Map.Hide();
        LocationPanel.Hide();

        if (IsInvoking("CheckArea"))
          CancelInvoke("CheckArea");
      }

      public bool HasPermission(string permission)
      {
        return Core.permission.UserHasPermission(Player.UserIDString, permission);
      }

      public void BeginInteraction(Interaction interaction)
      {
        interaction.Core = Core;
        interaction.User = this;
        CurrentInteraction = interaction;
      }

      public void CompleteInteraction(HitInfo hit)
      {
        if (CurrentInteraction.TryComplete(hit))
          CurrentInteraction = null;
      }

      public void CancelInteraction()
      {
        CurrentInteraction = null;
      }

      public void SendMessage(string message, params object[] args)
      {
        string format = Core.lang.GetMessage(message, Core, Player.UserIDString);
        Core.SendReply(Player, format, args);
      }

      public void SendMessage(StringBuilder sb)
      {
        Core.SendReply(Player, sb.ToString().TrimEnd());
      }

      void CheckArea()
      {
        Area currentArea = CurrentArea;
        Area correctArea = Core.Areas.GetByEntityPosition(Player);
        if (currentArea.Id != correctArea.Id)
        {
          Core.OnUserExitArea(currentArea, this);
          Core.OnUserEnterArea(correctArea, this);
        }
      }
    }
  }
}
