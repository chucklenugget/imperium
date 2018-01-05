namespace Oxide.Plugins
{
  using System;
  using System.Text;
  using UnityEngine;

  public partial class RustFactions
  {
    class User : MonoBehaviour
    {
      RustFactions Core;

      public BasePlayer Player { get; private set; }
      public UserMap Map { get; private set; }
      public UserHudPanel HudPanel { get; private set; }

      public Area CurrentArea { get; set; }
      public Interaction CurrentInteraction { get; private set; }
      public DateTime CommandCooldownExpirationTime { get; set; }

      public ulong Id
      {
        get { return Player.userID; }
      }

      public void Init(RustFactions core, BasePlayer player)
      {
        Core = core;
        Player = player;
        CommandCooldownExpirationTime = DateTime.MinValue;

        Map = new UserMap(core, this);
        HudPanel = new UserHudPanel(core, this);

        InvokeRepeating("UpdateHud", 5f, 5f);
        InvokeRepeating("CheckArea", 2f, 2f);
      }

      void OnDestroy()
      {
        Map.Hide();
        HudPanel.Hide();

        if (IsInvoking("UpdateHud"))
          CancelInvoke("UpdateHud");

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

      public void SendConsoleMessage(string message, params object[] args)
      {
        Player.ConsoleMessage(String.Format(message, args));
      }

      void UpdateHud()
      {
        HudPanel.Refresh();
      }

      public int GetSecondsUntilNextCommand()
      {
        return (int)Math.Max(0, CommandCooldownExpirationTime.Subtract(DateTime.UtcNow).TotalSeconds);
      }

      void CheckArea()
      {
        Area currentArea = CurrentArea;
        Area correctArea = Core.Areas.GetByEntityPosition(Player);
        if (currentArea != null && correctArea != null && currentArea.Id != correctArea.Id)
        {
          Core.OnUserExitArea(currentArea, this);
          Core.OnUserEnterArea(correctArea, this);
        }
      }
    }
  }
}
