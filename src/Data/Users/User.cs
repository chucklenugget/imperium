namespace Oxide.Plugins
{
  using System;
  using System.Text;
  using UnityEngine;

  public partial class Imperium
  {
    class User : MonoBehaviour
    {
      string OriginalName;

      public BasePlayer Player { get; private set; }
      public UserMap Map { get; private set; }
      public UserHudPanel HudPanel { get; private set; }

      public Area CurrentArea { get; set; }
      public Faction Faction { get; private set; }
      public Interaction CurrentInteraction { get; private set; }
      public DateTime CommandCooldownExpirationTime { get; set; }

      public string Id
      {
        get { return Player.UserIDString; }
      }

      public string Name
      {
        get { return OriginalName; }
      }

      public string NameWithFactionTag
      {
        get { return Player.displayName; }
      }

      public void Init(BasePlayer player)
      {
        Player = player;
        CommandCooldownExpirationTime = DateTime.MinValue;
        OriginalName = player.displayName;

        Map = new UserMap(this);
        HudPanel = new UserHudPanel(this);

        InvokeRepeating("UpdateHud", 5f, 5f);
        InvokeRepeating("CheckArea", 2f, 2f);
      }

      void OnDestroy()
      {
        Map.Hide();
        HudPanel.Hide();

        if (IsInvoking("UpdateHud")) CancelInvoke("UpdateHud");
        if (IsInvoking("CheckArea")) CancelInvoke("CheckArea");

        if (Player != null)
          Player.displayName = OriginalName;
      }

      public void SetFaction(Faction faction)
      {
        Faction = faction;

        if (faction == null)
          Player.displayName = OriginalName;
        else
          Player.displayName = $"[{faction.Id}] {Player.displayName}";

        Player.SendNetworkUpdate();
      }

      public bool HasPermission(string permission)
      {
        return Instance.permission.UserHasPermission(Player.UserIDString, permission);
      }

      public void BeginInteraction(Interaction interaction)
      {
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

      public void SendChatMessage(string message, params object[] args)
      {
        string format = Instance.lang.GetMessage(message, Instance, Player.UserIDString);
        Instance.SendReply(Player, format, args);
      }

      public void SendChatMessage(StringBuilder sb)
      {
        Instance.SendReply(Player, sb.ToString().TrimEnd());
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
        Area correctArea = Instance.Areas.GetByEntityPosition(Player);
        if (currentArea != null && correctArea != null && currentArea.Id != correctArea.Id)
        {
          Instance.OnUserExitArea(currentArea, this);
          Instance.OnUserEnterArea(correctArea, this);
        }
      }
    }
  }
}
