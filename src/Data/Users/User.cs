namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Text;
  using UnityEngine;

  public partial class Imperium
  {
    class User : MonoBehaviour
    {
      string OriginalName;
      Dictionary<string, DateTime> CommandCooldownExpirations;

      public BasePlayer Player { get; private set; }
      public UserMap Map { get; private set; }
      public UserHud Hud { get; private set; }
      public UserPreferences Preferences { get; set; }

      public Area CurrentArea { get; set; }
      public HashSet<Zone> CurrentZones { get; private set; }
      public Faction Faction { get; private set; }
      public Interaction CurrentInteraction { get; private set; }
      public DateTime MapCommandCooldownExpiration { get; set; }
      public DateTime PvpCommandCooldownExpiration { get; set; }
      public bool IsInPvpMode { get; set; }

      public string Id
      {
        get { return Player.UserIDString; }
      }

      public string UserName
      {
        get { return OriginalName; }
      }

      public string UserNameWithFactionTag
      {
        get { return Player.displayName; }
      }

      public void Init(BasePlayer player)
      {
        Player = player;
        OriginalName = player.displayName;
        CurrentZones = new HashSet<Zone>();
        CommandCooldownExpirations = new Dictionary<string, DateTime>();
        Preferences = UserPreferences.Default;

        Map = new UserMap(this);
        Hud = new UserHud(this);

        InvokeRepeating("UpdateHud", 5f, 5f);
        InvokeRepeating("CheckArea", 2f, 2f);
      }

      void OnDestroy()
      {
        Map.Hide();
        Hud.Hide();

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
        Hud.Refresh();
      }

      public int GetSecondsLeftOnCooldown(string command)
      {
        DateTime expiration;

        if (!CommandCooldownExpirations.TryGetValue(command, out expiration))
          return 0;

        return (int)Math.Max(0, expiration.Subtract(DateTime.UtcNow).TotalSeconds);
      }

      public void SetCooldownExpiration(string command, DateTime time)
      {
        CommandCooldownExpirations[command] = time;
      }

      void CheckArea()
      {
        Area currentArea = CurrentArea;
        Area correctArea = Instance.Areas.GetByEntityPosition(Player);
        if (currentArea != null && correctArea != null && currentArea.Id != correctArea.Id)
        {
          Api.OnUserLeftArea(this, currentArea);
          Api.OnUserEnteredArea(this, correctArea);
        }
      }

      void CheckZones()
      {

      }
    }
  }
}
