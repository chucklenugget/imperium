namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Converters;
  using Oxide.Core.Configuration;

  public partial class Imperium : RustPlugin
  {
    public enum EventType
    {
      AreaClaimed,
      AreaClaimRemoved,
      AreaCaptured,
      AreaClaimDeleted,
      HeadquartersMoved
    }

    class Event
    {
      [JsonProperty("type"), JsonConverter(typeof(StringEnumConverter))]
      public EventType Type;

      [JsonProperty("time"), JsonConverter(typeof(IsoDateTimeConverter))]
      public DateTime Time;

      [JsonProperty("playerId")]
      public ulong? PlayerId;

      [JsonProperty("playerName")]
      public string PlayerName;

      [JsonProperty("areaId")]
      public string AreaId;

      [JsonProperty("factionId")]
      public string FactionId;

      [JsonProperty("text")]
      public string Text;

      public Event(EventType type, Area area, Faction faction, User user)
      {
        Type = type;
        Time = DateTime.Now;
        PlayerId = user?.Player.userID;
        PlayerName = user?.Player.displayName;
        AreaId = area?.Id;
        FactionId = faction?.Id;
      }
    }

    class HistoryManager : ImperiumComponent
    {
      List<Event> Events = new List<Event>();

      public HistoryManager(Imperium core)
        : base(core)
      {
      }

      public void Record(EventType type, Area area, Faction faction, User user)
      {
        Events.Add(new Event(type, area, faction, user));
      }

      public void Load(DynamicConfigFile file)
      {
        try
        {
          Events = new List<Event>(file.ReadObject<Event[]>());
        }
        catch
        {
          Core.PrintWarning("Couldn't load history.");
        }
      }

      public void Save(DynamicConfigFile file)
      {
        file.WriteObject(Events.ToArray());
      }
    }
  }
}
