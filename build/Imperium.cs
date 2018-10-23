// Reference: System.Drawing

/*
 * Copyright (C) 2017-2018 chucklenugget
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace Oxide.Plugins
{
  using System;
  using System.IO;
  using Oxide.Core;
  using Oxide.Core.Configuration;
  using UnityEngine;
  using System.Collections.Generic;
  using System.Linq;

  [Info("Imperium", "chucklenugget", "1.9.4")]
  public partial class Imperium : RustPlugin
  {
    static Imperium Instance;

    bool Ready;

    DynamicConfigFile AreasFile;
    DynamicConfigFile FactionsFile;
    DynamicConfigFile PinsFile;
    DynamicConfigFile WarsFile;

    GameObject GameObject;
    ImperiumOptions Options;
    Timer UpkeepCollectionTimer;

    AreaManager Areas;
    FactionManager Factions;
    HudManager Hud;
    PinManager Pins;
    UserManager Users;
    WarManager Wars;
    ZoneManager Zones;

    void Init()
    {
      AreasFile = GetDataFile("areas");
      FactionsFile = GetDataFile("factions");
      PinsFile = GetDataFile("pins");
      WarsFile = GetDataFile("wars");
    }

    void Loaded()
    {
      InitLang();
      Permission.RegisterAll(this);

      try
      {
        Options = Config.ReadObject<ImperiumOptions>();
      }
      catch (Exception ex)
      {
        PrintError($"Error while loading configuration: {ex.ToString()}");
      }

      Puts("Area claims are " + (Options.Claims.Enabled ? "enabled" : "disabled"));
      Puts("Taxation is " + (Options.Taxes.Enabled ? "enabled" : "disabled"));
      Puts("Badlands are " + (Options.Badlands.Enabled ? "enabled" : "disabled"));
      Puts("Map pins are " + (Options.Map.PinsEnabled ? "enabled" : "disabled"));
      Puts("War is " + (Options.War.Enabled ? "enabled" : "disabled"));
      Puts("Decay reduction is " + (Options.Decay.Enabled ? "enabled" : "disabled"));
      Puts("Claim upkeep is " + (Options.Upkeep.Enabled ? "enabled" : "disabled"));
      Puts("Zones are " + (Options.Zones.Enabled ? "enabled" : "disabled"));

      // If the map has already been initialized, we can set up now; otherwise,
      // we need to wait until the savefile has been loaded.
      if (TerrainMeta.Size.x > 0) Setup();
    }

    object OnSaveLoad(Dictionary<BaseEntity, ProtoBuf.Entity> entities)
    {
      Setup();
      return null;
    }

    void Setup()
    {
      Instance = this;
      GameObject = new GameObject();

      Areas = new AreaManager();
      Factions = new FactionManager();
      Hud = new HudManager();
      Pins = new PinManager();
      Users = new UserManager();
      Wars = new WarManager();
      Zones = new ZoneManager();

      Factions.Init(TryLoad<FactionInfo>(FactionsFile));
      Areas.Init(TryLoad<AreaInfo>(AreasFile));
      Pins.Init(TryLoad<PinInfo>(PinsFile));
      Users.Init();
      Wars.Init(TryLoad<WarInfo>(WarsFile));
      Zones.Init();
      Hud.Init();

      Hud.GenerateMapOverlayImage();

      if (Options.Upkeep.Enabled)
        UpkeepCollectionTimer = timer.Every(Options.Upkeep.CheckIntervalMinutes * 60, Upkeep.CollectForAllFactions);

      PrintToChat($"{Title} v{Version} initialized.");
      Ready = true;
    }

    void Unload()
    {
      Hud.Destroy();
      Zones.Destroy();
      Users.Destroy();
      Wars.Destroy();
      Pins.Destroy();
      Areas.Destroy();
      Factions.Destroy();

      if (UpkeepCollectionTimer != null && !UpkeepCollectionTimer.Destroyed)
        UpkeepCollectionTimer.Destroy();

      if (GameObject != null)
        UnityEngine.Object.Destroy(GameObject);

      Instance = null;
    }

    void OnServerSave()
    {
      AreasFile.WriteObject(Areas.Serialize());
      FactionsFile.WriteObject(Factions.Serialize());
      PinsFile.WriteObject(Pins.Serialize());
      WarsFile.WriteObject(Wars.Serialize());
    }

    DynamicConfigFile GetDataFile(string name)
    {
      return Interface.Oxide.DataFileSystem.GetFile(Name + Path.DirectorySeparatorChar + name);
    }

    IEnumerable<T> TryLoad<T>(DynamicConfigFile file)
    {
      List<T> items;

      try
      {
        items = file.ReadObject<List<T>>();
      }
      catch (Exception ex)
      {
        PrintWarning($"Error reading data from {file.Filename}: ${ex.ToString()}");
        items = new List<T>();
      }

      return items;
    }

    void Log(string message, params object[] args)
    {
      LogToFile("log", String.Format(message, args), this, true);
    }

    bool EnsureUserCanChangeFactionClaims(User user, Faction faction)
    {
      if (faction == null || !faction.HasLeader(user))
      {
        user.SendChatMessage(Messages.NotLeaderOfFaction);
        return false;
      }

      if (faction.MemberCount < Options.Claims.MinFactionMembers)
      {
        user.SendChatMessage(Messages.FactionTooSmallToOwnLand, Options.Claims.MinFactionMembers);
        return false;
      }

      return true;
    }

    bool EnsureFactionCanClaimArea(User user, Faction faction, Area area)
    {
      if (area.Type == AreaType.Badlands)
      {
        user.SendChatMessage(Messages.AreaIsBadlands, area.Id);
        return false;
      }

      if (faction.MemberCount < Instance.Options.Claims.MinFactionMembers)
      {
        user.SendChatMessage(Messages.FactionTooSmallToOwnLand, Instance.Options.Claims.MinFactionMembers);
        return false;
      }

      Area[] claimedAreas = Areas.GetAllClaimedByFaction(faction);

      if (Instance.Options.Claims.RequireContiguousClaims && !area.IsClaimed && claimedAreas.Length > 0)
      {
        int contiguousClaims = Areas.GetNumberOfContiguousClaimedAreas(area, faction);
        if (contiguousClaims == 0)
        {
          user.SendChatMessage(Messages.AreaNotContiguous, area.Id, faction.Id);
          return false;
        }
      }

      int? maxClaims = Instance.Options.Claims.MaxClaims;
      if (maxClaims != null && claimedAreas.Length >= maxClaims)
      {
        user.SendChatMessage(Messages.FactionOwnsTooMuchLand, faction.Id, maxClaims);
        return false;
      }

      return true;
    }

    bool EnsureCupboardCanBeUsedForClaim(User user, BuildingPrivlidge cupboard)
    {
      if (cupboard == null)
      {
        user.SendChatMessage(Messages.SelectingCupboardFailedInvalidTarget);
        return false;
      }

      if (!cupboard.IsAuthed(user.Player))
      {
        user.SendChatMessage(Messages.SelectingCupboardFailedNotAuthorized);
        return false;
      }

      return true;
    }

    bool EnsureUserAndFactionCanEngageInDiplomacy(User user, Faction faction)
    {
      if (faction == null)
      {
        user.SendChatMessage(Messages.NotMemberOfFaction);
        return false;
      }

      if (faction.MemberCount < Options.Claims.MinFactionMembers)
      {
        user.SendChatMessage(Messages.FactionTooSmallToOwnLand);
        return false;
      }

      if (Areas.GetAllClaimedByFaction(faction).Length == 0)
      {
        user.SendChatMessage(Messages.FactionDoesNotOwnLand);
        return false;
      }

      return true;
    }

    bool EnforceCommandCooldown(User user, string command, int cooldownSeconds)
    {
      int secondsRemaining = user.GetSecondsLeftOnCooldown(command);

      if (secondsRemaining > 0)
      {
        user.SendChatMessage(Messages.CommandIsOnCooldown, secondsRemaining);
        return false;
      }

      user.SetCooldownExpiration(command, DateTime.UtcNow.AddSeconds(cooldownSeconds));
      return true;
    }

    bool TryCollectFromStacks(ItemDefinition itemDef, IEnumerable<Item> stacks, int amount)
    {
      if (stacks.Sum(item => item.amount) < amount)
        return false;

      int amountRemaining = amount;
      var dirtyContainers = new HashSet<ItemContainer>();

      foreach (Item stack in stacks)
      {
        var amountToTake = Math.Min(stack.amount, amountRemaining);

        stack.amount -= amountToTake;
        amountRemaining -= amountToTake;

        dirtyContainers.Add(stack.GetRootContainer());

        if (stack.amount == 0)
          stack.RemoveFromContainer();

        if (amountRemaining == 0)
          break;
      }

      foreach (ItemContainer container in dirtyContainers)
        container.MarkDirty();

      return true;
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    [ChatCommand("cancel")]
    void OnCancelCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);

      if (user.CurrentInteraction == null)
      {
        user.SendChatMessage(Messages.NoInteractionInProgress);
        return;
      }

      user.SendChatMessage(Messages.InteractionCanceled);
      user.CancelInteraction();
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Text;

  public partial class Imperium
  {
    [ChatCommand("help")]
    void OnHelpCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      var sb = new StringBuilder();

      sb.AppendLine($"<size=18>Welcome to {ConVar.Server.hostname}!</size>");
      sb.AppendLine($"Powered by {Name} v{Version} by <color=#ffd479>chucklenugget</color>");
      sb.AppendLine();

      sb.Append("The following commands are available. To learn more about each command, do <color=#ffd479>/command help</color>. ");
      sb.AppendLine("For example, to learn more about how to claim land, do <color=#ffd479>/claim help</color>.");
      sb.AppendLine();

      sb.AppendLine("<color=#ffd479>/faction</color> Create or join a faction");
      sb.AppendLine("<color=#ffd479>/claim</color> Claim areas of land");

      if (Options.Taxes.Enabled)
        sb.AppendLine("<color=#ffd479>/tax</color> Manage taxation of your land");

      if (Options.Map.PinsEnabled)
        sb.AppendLine("<color=#ffd479>/pin</color> Add pins (points of interest) to the map");

      if (Options.War.Enabled)
        sb.AppendLine("<color=#ffd479>/war</color> See active wars, declare war, or offer peace");

      if (Options.Badlands.Enabled)
      {
        if (user.HasPermission(Permission.AdminBadlands))
          sb.AppendLine("<color=#ffd479>/badlands</color> Find or change badlands areas");
        else
          sb.AppendLine("<color=#ffd479>/badlands</color> Find badlands (PVP) areas");
      }

      user.SendChatMessage(sb);
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    [ChatCommand("pvp")]
    void OnPvpCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);

      if (!Options.Pvp.EnablePvpCommand)
      {
        user.SendChatMessage(Messages.PvpModeDisabled);
        return;
      }

      if (!EnforceCommandCooldown(user, "pvp", Options.Pvp.CommandCooldownSeconds))
        return;

      if (user.IsInPvpMode)
      {
        user.IsInPvpMode = false;
        user.SendChatMessage(Messages.ExitedPvpMode);
      }
      else
      {
        user.IsInPvpMode = true;
        user.SendChatMessage(Messages.EnteredPvpMode);
      }

      user.Hud.Refresh();
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Linq;

  public partial class Imperium
  {
    [ChatCommand("badlands")]
    void OnBadlandsCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.Badlands.Enabled)
      {
        user.SendChatMessage(Messages.BadlandsDisabled);
        return;
      }

      if (args.Length == 0)
      {
        var areas = Areas.GetAllByType(AreaType.Badlands).Select(a => a.Id);
        user.SendChatMessage(Messages.BadlandsList, Util.Format(areas), Options.Taxes.BadlandsGatherBonus);
        return;
      }

      if (!user.HasPermission(Permission.AdminBadlands))
      {
        user.SendChatMessage(Messages.NoPermission);
        return;
      }

      var areaIds = args.Skip(1).Select(arg => Util.NormalizeAreaId(arg)).ToArray();

      switch (args[0].ToLower())
      {
        case "add":
          if (args.Length < 2)
            user.SendChatMessage(Messages.Usage, "/badlands add [XY XY XY...]");
          else
            OnAddBadlandsCommand(user, areaIds);
          break;

        case "remove":
          if (args.Length < 2)
            user.SendChatMessage(Messages.Usage, "/badlands remove [XY XY XY...]");
          else
            OnRemoveBadlandsCommand(user, areaIds);
          break;

        case "set":
          if (args.Length < 2)
            user.SendChatMessage(Messages.Usage, "/badlands set [XY XY XY...]");
          else
            OnSetBadlandsCommand(user, areaIds);
          break;

        case "clear":
          if (args.Length != 1)
            user.SendChatMessage(Messages.Usage, "/badlands clear");
          else
            OnSetBadlandsCommand(user, new string[0]);
          break;

        default:
          OnBadlandsHelpCommand(user);
          break;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;

  public partial class Imperium
  {
    void OnAddBadlandsCommand(User user, string[] args)
    {
      var areas = new List<Area>();

      foreach (string arg in args)
      {
        Area area = Areas.Get(Util.NormalizeAreaId(arg));

        if (area == null)
        {
          user.SendChatMessage(Messages.UnknownArea, arg);
          return;
        }

        if (area.Type != AreaType.Wilderness)
        {
          user.SendChatMessage(Messages.AreaNotWilderness, area.Id);
          return;
        }

        areas.Add(area);
      }

      Areas.AddBadlands(areas);

      user.SendChatMessage(Messages.BadlandsSet, Util.Format(Areas.GetAllByType(AreaType.Badlands)));
      Log($"{Util.Format(user)} added {Util.Format(areas)} to badlands");
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Text;

  public partial class Imperium
  {
    void OnBadlandsHelpCommand(User user)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/badlands add XY [XY XY...]</color>: Add area(s) to the badlands");
      sb.AppendLine("  <color=#ffd479>/badlands remove XY [XY XY...]</color>: Remove area(s) from the badlands");
      sb.AppendLine("  <color=#ffd479>/badlands set XY [XY XY...]</color>: Set the badlands to a list of areas");
      sb.AppendLine("  <color=#ffd479>/badlands clear</color>: Remove all areas from the badlands");
      sb.AppendLine("  <color=#ffd479>/badlands help</color>: Prints this message");

      user.SendChatMessage(sb);
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;

  public partial class Imperium
  {
    void OnRemoveBadlandsCommand(User user, string[] args)
    {
      var areas = new List<Area>();

      foreach (string arg in args)
      {
        Area area = Areas.Get(Util.NormalizeAreaId(arg));

        if (area == null)
        {
          user.SendChatMessage(Messages.UnknownArea, arg);
          return;
        }

        if (area.Type != AreaType.Badlands)
        {
          user.SendChatMessage(Messages.AreaNotBadlands, area.Id);
          return;
        }

        areas.Add(area);
      }

      Areas.Unclaim(areas);

      user.SendChatMessage(Messages.BadlandsSet, Util.Format(Areas.GetAllByType(AreaType.Badlands)));
      Log($"{Util.Format(user)} removed {Util.Format(areas)} from badlands");
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;

  public partial class Imperium
  {
    void OnSetBadlandsCommand(User user, string[] args)
    {
      var areas = new List<Area>();

      foreach (string arg in args)
      {
        Area area = Areas.Get(Util.NormalizeAreaId(arg));

        if (area == null)
        {
          user.SendChatMessage(Messages.UnknownArea, arg);
          return;
        }

        if (area.Type != AreaType.Wilderness)
        {
          user.SendChatMessage(Messages.AreaNotWilderness, area.Id);
          return;
        }

        areas.Add(area);
      }

      Areas.Unclaim(Areas.GetAllByType(AreaType.Badlands));
      Areas.AddBadlands(areas);

      user.SendChatMessage(Messages.BadlandsSet, Util.Format(Areas.GetAllByType(AreaType.Badlands)));
      Log($"{Util.Format(user)} set badlands to {Util.Format(areas)}");
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Linq;

  public partial class Imperium
  {
    [ChatCommand("claim")]
    void OnClaimCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.Claims.Enabled)
      {
        user.SendChatMessage(Messages.AreaClaimsDisabled);
        return;
      }

      if (args.Length == 0)
      {
        OnClaimAddCommand(user);
        return;
      }

      var restArguments = args.Skip(1).ToArray();

      switch (args[0].ToLower())
      {
        case "add":
          OnClaimAddCommand(user);
          break;
        case "remove":
          OnClaimRemoveCommand(user);
          break;
        case "hq":
          OnClaimHeadquartersCommand(user);
          break;
        case "rename":
          OnClaimRenameCommand(user, restArguments);
          break;
        case "give":
          OnClaimGiveCommand(user, restArguments);
          break;
        case "cost":
          OnClaimCostCommand(user, restArguments);
          break;
        case "upkeep":
          OnClaimUpkeepCommand(user);
          break;
        case "show":
          OnClaimShowCommand(user, restArguments);
          break;
        case "list":
          OnClaimListCommand(user, restArguments);
          break;
        case "assign":
          OnClaimAssignCommand(user, restArguments);
          break;
        case "delete":
          OnClaimDeleteCommand(user, restArguments);
          break;
        default:
          OnClaimHelpCommand(user);
          break;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimAddCommand(User user)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureUserCanChangeFactionClaims(user, faction))
        return;

      user.SendChatMessage(Messages.SelectClaimCupboardToAdd);
      user.BeginInteraction(new AddingClaimInteraction(faction));
    }
  }
}﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimAssignCommand(User user, string[] args)
    {
      if (!user.HasPermission(Permission.AdminClaims))
      {
        user.SendChatMessage(Messages.NoPermission);
        return;
      }

      if (args.Length == 0)
      {
        user.SendChatMessage(Messages.Usage, "/claim assign FACTION");
        return;
      }

      string factionId = Util.NormalizeFactionId(args[0]);
      Faction faction = Factions.Get(factionId);

      if (faction == null)
      {
        user.SendChatMessage(Messages.FactionDoesNotExist, factionId);
        return;
      }

      user.SendChatMessage(Messages.SelectClaimCupboardToAssign);
      user.BeginInteraction(new AssigningClaimInteraction(faction));
    }
  }
}﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimCostCommand(User user, string[] args)
    {
      Faction faction = Factions.GetByMember(user);

      if (faction == null)
      {
        user.SendChatMessage(Messages.NotMemberOfFaction);
        return;
      }

      if (faction.MemberCount < Options.Claims.MinFactionMembers)
      {
        user.SendChatMessage(Messages.FactionTooSmallToOwnLand, Options.Claims.MinFactionMembers);
        return;
      }

      if (args.Length > 1)
      {
        user.SendChatMessage(Messages.Usage, "/claim cost [XY]");
        return;
      }

      Area area;
      if (args.Length == 0)
        area = user.CurrentArea;
      else
        area = Areas.Get(Util.NormalizeAreaId(args[0]));

      if (area == null)
      {
        user.SendChatMessage(Messages.Usage, "/claim cost [XY]");
        return;
      }

      if (area.Type == AreaType.Badlands)
      {
        user.SendChatMessage(Messages.AreaIsBadlands, area.Id);
        return;
      }
      else if (area.Type != AreaType.Wilderness)
      {
        user.SendChatMessage(Messages.CannotClaimAreaAlreadyClaimed, area.Id, area.FactionId);
        return;
      }

      int cost = area.GetClaimCost(faction);
      user.SendChatMessage(Messages.ClaimCost, area.Id, faction.Id, cost);
    }
  }
}﻿namespace Oxide.Plugins
{
  using System.Collections.Generic;

  public partial class Imperium
  {
    void OnClaimDeleteCommand(User user, string[] args)
    {
      if (args.Length == 0)
      {
        user.SendChatMessage(Messages.Usage, "/claim delete XY [XY XY...]");
        return;
      }

      if (!user.HasPermission(Permission.AdminClaims))
      {
        user.SendChatMessage(Messages.NoPermission);
        return;
      }

      var areas = new List<Area>();
      foreach (string arg in args)
      {
        Area area = Areas.Get(Util.NormalizeAreaId(arg));

        if (area.Type == AreaType.Badlands)
        {
          user.SendChatMessage(Messages.AreaIsBadlands, area.Id);
          return;
        }

        if (area.Type == AreaType.Wilderness)
        {
          user.SendChatMessage(Messages.AreaIsWilderness, area.Id);
          return;
        }

        areas.Add(area);
      }

      foreach (Area area in areas)
      {
        PrintToChat(Messages.AreaClaimDeletedAnnouncement, area.FactionId, area.Id);
        Log($"{Util.Format(user)} deleted {area.FactionId}'s claim on {area.Id}");
      }

      Areas.Unclaim(areas);
    }
  }
}﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimGiveCommand(User user, string[] args)
    {
      if (args.Length == 0)
      {
        user.SendChatMessage(Messages.Usage, "/claim give FACTION");
        return;
      }

      Faction sourceFaction = Factions.GetByMember(user);

      if (!EnsureUserCanChangeFactionClaims(user, sourceFaction))
        return;

      string factionId = Util.NormalizeFactionId(args[0]);
      Faction targetFaction = Factions.Get(factionId);

      if (targetFaction == null)
      {
        user.SendChatMessage(Messages.FactionDoesNotExist, factionId);
        return;
      }

      user.SendChatMessage(Messages.SelectClaimCupboardToTransfer);
      user.BeginInteraction(new TransferringClaimInteraction(sourceFaction, targetFaction));
    }
  }
}﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimHeadquartersCommand(User user)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureUserCanChangeFactionClaims(user, faction))
        return;

      user.SendChatMessage(Messages.SelectClaimCupboardForHeadquarters);
      user.BeginInteraction(new SelectingHeadquartersInteraction(faction));
    }
  }
}﻿namespace Oxide.Plugins
{
  using System.Text;

  public partial class Imperium
  {
    void OnClaimHelpCommand(User user)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/claim</color>: Add a claim for your faction");
      sb.AppendLine("  <color=#ffd479>/claim hq</color>: Select your faction's headquarters");
      sb.AppendLine("  <color=#ffd479>/claim remove</color>: Remove a claim for your faction (no undo!)");
      sb.AppendLine("  <color=#ffd479>/claim give FACTION</color>: Give a claimed area to another faction (no undo!)");
      sb.AppendLine("  <color=#ffd479>/claim rename XY \"NAME\"</color>: Rename an area claimed by your faction");
      sb.AppendLine("  <color=#ffd479>/claim show XY</color>: Show who owns an area");
      sb.AppendLine("  <color=#ffd479>/claim list FACTION</color>: List all areas claimed for a faction");
      sb.AppendLine("  <color=#ffd479>/claim cost [XY]</color>: Show the cost for your faction to claim an area");

      if (!Options.Upkeep.Enabled)
        sb.AppendLine("  <color=#ffd479>/claim upkeep</color>: Show information about upkeep costs for your faction");

      sb.AppendLine("  <color=#ffd479>/claim help</color>: Prints this message");

      if (user.HasPermission(Permission.AdminClaims))
      {
        sb.AppendLine("Admin commands:");
        sb.AppendLine("  <color=#ffd479>/claim assign FACTION</color>: Use the hammer to assign a claim to another faction");
        sb.AppendLine("  <color=#ffd479>/claim delete XY [XY XY XY...]</color>: Remove the claim on the specified areas (no undo!)");
      }

      user.SendChatMessage(sb);
    }
  }
}﻿namespace Oxide.Plugins
{
  using System;
  using System.Linq;
  using System.Text;

  public partial class Imperium
  {
    void OnClaimListCommand(User user, string[] args)
    {
      if (args.Length != 1)
      {
        user.SendChatMessage(Messages.Usage, "/claim list FACTION");
        return;
      }

      string factionId = Util.NormalizeFactionId(args[0]);
      Faction faction = Factions.Get(factionId);

      if (faction == null)
      {
        user.SendChatMessage(Messages.FactionDoesNotExist, factionId);
        return;
      }

      Area[] areas = Areas.GetAllClaimedByFaction(faction);
      Area headquarters = areas.FirstOrDefault(a => a.Type == AreaType.Headquarters);

      var sb = new StringBuilder();

      if (areas.Length == 0)
      {
        sb.AppendFormat(String.Format("<color=#ffd479>[{0}]</color> has no land holdings.", factionId));
      }
      else
      {
        float percentageOfMap = (areas.Length / (float)Areas.Count) * 100;
        sb.AppendLine(String.Format("<color=#ffd479>[{0}] owns {1} tiles ({2:F2}% of the known world)</color>", faction.Id, areas.Length, percentageOfMap));
        sb.AppendLine(String.Format("Headquarters: {0}", (headquarters == null) ? "Unknown" : headquarters.Id));
        sb.AppendLine(String.Format("Areas claimed: {0}", Util.Format(areas)));
      }

      user.SendChatMessage(sb);
    }
  }
}﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimRemoveCommand(User user)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureUserCanChangeFactionClaims(user, faction))
        return;

      user.SendChatMessage(Messages.SelectClaimCupboardToRemove);
      user.BeginInteraction(new RemovingClaimInteraction(faction));
    }
  }
}﻿namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    void OnClaimRenameCommand(User user, string[] args)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureUserCanChangeFactionClaims(user, faction))
        return;

      if (args.Length != 2)
      {
        user.SendChatMessage(Messages.Usage, "/claim rename XY \"NAME\"");
        return;
      }

      var areaId = Util.NormalizeAreaId(args[0]);
      var name = Util.NormalizeAreaName(args[1]);

      if (name == null || name.Length < Options.Claims.MinAreaNameLength || name.Length > Options.Claims.MaxAreaNameLength)
      {
        user.SendChatMessage(Messages.InvalidAreaName, Options.Claims.MinAreaNameLength, Options.Claims.MaxAreaNameLength);
        return;
      }

      Area area = Areas.Get(areaId);

      if (area == null)
      {
        user.SendChatMessage(Messages.UnknownArea, areaId);
        return;
      }

      if (area.FactionId != faction.Id)
      {
        user.SendChatMessage(Messages.AreaNotOwnedByYourFaction, area.Id);
        return;
      }

      user.SendChatMessage(Messages.AreaRenamed, area.Id, name);
      Log($"{Util.Format(user)} renamed {area.Id} to {name}");

      area.Name = name;
    }
  }
}﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimShowCommand(User user, string[] args)
    {
      if (args.Length != 1)
      {
        user.SendChatMessage(Messages.Usage, "/claim show XY");
        return;
      }

      Area area = Areas.Get(Util.NormalizeAreaId(args[0]));

      switch (area.Type)
      {
        case AreaType.Badlands:
          user.SendChatMessage(Messages.AreaIsBadlands, area.Id);
          return;
        case AreaType.Claimed:
          user.SendChatMessage(Messages.AreaIsClaimed, area.Id, area.FactionId);
          return;
        case AreaType.Headquarters:
          user.SendChatMessage(Messages.AreaIsHeadquarters, area.Id, area.FactionId);
          return;
        default:
          user.SendChatMessage(Messages.AreaIsWilderness, area.Id);
          return;
      }
    }
  }
}﻿namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    void OnClaimUpkeepCommand(User user)
    {
      if (!Options.Upkeep.Enabled)
      {
        user.SendChatMessage(Messages.UpkeepDisabled);
        return;
      }

      Faction faction = Factions.GetByMember(user);

      if (faction == null)
      {
        user.SendChatMessage(Messages.NotMemberOfFaction);
        return;
      }

      if (faction.MemberCount < Options.Claims.MinFactionMembers)
      {
        user.SendChatMessage(Messages.FactionTooSmallToOwnLand, Options.Claims.MinFactionMembers);
        return;
      }

      Area[] areas = Areas.GetAllClaimedByFaction(faction);

      if (areas.Length == 0)
      {
        user.SendChatMessage(Messages.NoAreasClaimed);
        return;
      }

      int upkeep = faction.GetUpkeepPerPeriod();
      var nextPaymentHours = (int)faction.NextUpkeepPaymentTime.Subtract(DateTime.UtcNow).TotalHours;

      if (nextPaymentHours > 0)
        user.SendChatMessage(Messages.UpkeepCost, upkeep, areas.Length, faction.Id, nextPaymentHours);
      else
        user.SendChatMessage(Messages.UpkeepCostOverdue, upkeep, areas.Length, faction.Id, nextPaymentHours);
    }
  }
}﻿namespace Oxide.Plugins
{
  using System.Linq;

  public partial class Imperium
  {
    [ChatCommand("faction")]
    void OnFactionCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (args.Length == 0)
      {
        OnFactionShowCommand(user);
        return;
      }

      var restArguments = args.Skip(1).ToArray();

      switch (args[0].ToLower())
      {
        case "create":
          OnFactionCreateCommand(user, restArguments);
          break;
        case "join":
          OnFactionJoinCommand(user, restArguments);
          break;
        case "leave":
          OnFactionLeaveCommand(user, restArguments);
          break;
        case "invite":
          OnFactionInviteCommand(user, restArguments);
          break;
        case "kick":
          OnFactionKickCommand(user, restArguments);
          break;
        case "promote":
          OnFactionPromoteCommand(user, restArguments);
          break;
        case "demote":
          OnFactionDemoteCommand(user, restArguments);
          break;
        case "disband":
          OnFactionDisbandCommand(user, restArguments);
          break;
        case "help":
        default:
          OnFactionHelpCommand(user);
          break;
      }
    }

    [ChatCommand("clan")]
    void OnClanCommand(BasePlayer player, string command, string[] args)
    {
      OnFactionCommand(player, command, args);
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    [ChatCommand("f")]
    void OnFactionChatCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      string message = String.Join(" ", args).Trim();

      if (message.Length == 0)
      {
        user.SendChatMessage(Messages.Usage, "/f MESSAGE...");
        return;
      }

      Faction faction = Factions.GetByMember(user);

      if (faction == null)
      {
        user.SendChatMessage(Messages.NotMemberOfFaction);
        return;
      }

      faction.SendChatMessage("<color=#a1ff46>(FACTION)</color> {0}: {1}", user.UserName, message);
      Puts("[FACTION] {0} - {1}: {2}", faction.Id, user.UserName, message);
    }

    [ChatCommand("c")]
    void OnClanChatCommand(BasePlayer player, string command, string[] args)
    {
      OnFactionChatCommand(player, command, args);
    }
  }
}﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnFactionCreateCommand(User user, string[] args)
    {
      if (!user.HasPermission(Permission.ManageFactions))
      {
        user.SendChatMessage(Messages.NoPermission);
        return;
      }

      if (user.Faction != null)
      {
        user.SendChatMessage(Messages.AlreadyMemberOfFaction);
        return;
      }

      if (args.Length != 1)
      {
        user.SendChatMessage(Messages.Usage, "/faction create NAME");
        return;
      }

      string id = Util.RemoveSpecialCharacters(args[0].Replace(" ", ""));

      if (id.Length < Options.Factions.MinFactionNameLength || id.Length > Options.Factions.MaxFactionNameLength)
      {
        user.SendChatMessage(Messages.InvalidFactionName, Options.Factions.MinFactionNameLength, Options.Factions.MaxFactionNameLength);
        return;
      }

      if (Factions.Exists(id))
      {
        user.SendChatMessage(Messages.FactionAlreadyExists, id);
        return;
      }

      PrintToChat(Messages.FactionCreatedAnnouncement, id);
      Log($"{Util.Format(user)} created faction {id}");

      Faction faction = Factions.Create(id, user);
      user.SetFaction(faction);
    }
  }
}﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnFactionDemoteCommand(User user, string[] args)
    {
      if (args.Length != 1)
      {
        user.SendChatMessage(Messages.Usage, "/faction demote \"PLAYER\"");
        return;
      }

      Faction faction = Factions.GetByMember(user);

      if (faction == null || !faction.HasLeader(user))
      {
        user.SendChatMessage(Messages.NotLeaderOfFaction);
        return;
      }

      User member = Users.Find(args[0]);

      if (member == null)
      {
        user.SendChatMessage(Messages.InvalidUser, args[0]);
        return;
      }

      if (faction.HasOwner(member))
      {
        user.SendChatMessage(Messages.CannotPromoteOrDemoteOwnerOfFaction, member.UserName, faction.Id);
        return;
      }

      if (!faction.HasManager(member))
      {
        user.SendChatMessage(Messages.UserIsNotManagerOfFaction, member.UserName, faction.Id);
        return;
      }

      user.SendChatMessage(Messages.ManagerRemoved, member.UserName, faction.Id);
      Log($"{Util.Format(user)} demoted {Util.Format(member)} in faction {faction.Id}");

      faction.Demote(member);
    }
  }
}﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnFactionDisbandCommand(User user, string[] args)
    {
      if (args.Length != 1 || args[0].ToLowerInvariant() != "forever")
      {
        user.SendChatMessage(Messages.Usage, "/faction disband forever");
        return;
      }

      Faction faction = user.Faction;

      if (faction == null || !faction.HasLeader(user))
      {
        user.SendChatMessage(Messages.NotLeaderOfFaction);
        return;
      }

      PrintToChat(Messages.FactionDisbandedAnnouncement, faction.Id);
      Log($"{Util.Format(user)} disbanded faction {faction.Id}");

      Factions.Disband(faction);
    }
  }
}﻿namespace Oxide.Plugins
{
  using System.Text;

  public partial class Imperium
  {
    void OnFactionHelpCommand(User user)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/faction</color>: Show information about your faction");
      sb.AppendLine("  <color=#ffd479>/f MESSAGE...</color>: Send a message to all online members of your faction");

      if (user.HasPermission(Permission.ManageFactions))
        sb.AppendLine("  <color=#ffd479>/faction create</color>: Create a new faction");

      sb.AppendLine("  <color=#ffd479>/faction join FACTION</color>: Join a faction if you have been invited");
      sb.AppendLine("  <color=#ffd479>/faction leave</color>: Leave your current faction");
      sb.AppendLine("  <color=#ffd479>/faction invite \"PLAYER\"</color>: Invite another player to join your faction");
      sb.AppendLine("  <color=#ffd479>/faction kick \"PLAYER\"</color>: Kick a player out of your faction");
      sb.AppendLine("  <color=#ffd479>/faction promote \"PLAYER\"</color>: Promote a faction member to manager");
      sb.AppendLine("  <color=#ffd479>/faction demote \"PLAYER\"</color>: Remove a faction member as manager");
      sb.AppendLine("  <color=#ffd479>/faction disband forever</color>: Disband your faction immediately (no undo!)");
      sb.AppendLine("  <color=#ffd479>/faction help</color>: Prints this message");

      user.SendChatMessage(sb);
    }
  }
}﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnFactionInviteCommand(User user, string[] args)
    {
      if (args.Length != 1)
      {
        user.SendChatMessage(Messages.Usage, "/faction invite \"PLAYER\"");
        return;
      }

      Faction faction = Factions.GetByMember(user);

      if (faction == null || !faction.HasLeader(user))
      {
        user.SendChatMessage(Messages.NotLeaderOfFaction);
        return;
      }

      User member = Users.Find(args[0]);

      if (member == null)
      {
        user.SendChatMessage(Messages.InvalidUser, args[0]);
        return;
      }

      if (faction.HasMember(member))
      {
        user.SendChatMessage(Messages.UserIsAlreadyMemberOfFaction, member.UserName, faction.Id);
        return;
      }

      int? maxMembers = Options.Factions.MaxMembers;
      if (maxMembers != null && faction.MemberCount >= maxMembers)
      {
        user.SendChatMessage(Messages.FactionHasTooManyMembers, faction.Id, faction.MemberCount);
        return;
      }

      member.SendChatMessage(Messages.InviteReceived, user.UserName, faction.Id);
      user.SendChatMessage(Messages.InviteAdded, member.UserName, faction.Id);

      Log($"{Util.Format(user)} invited {Util.Format(member)} to faction {faction.Id}");

      faction.AddInvite(member);
    }
  }
}﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnFactionJoinCommand(User user, string[] args)
    {
      if (args.Length != 1)
      {
        user.SendChatMessage(Messages.Usage, "/faction join FACTION");
        return;
      }

      if (user.Faction != null)
      {
        user.SendChatMessage(Messages.AlreadyMemberOfFaction);
        return;
      }

      Faction faction = Factions.Get(args[0]);

      if (faction == null)
      {
        user.SendChatMessage(Messages.FactionDoesNotExist, args[0]);
        return;
      }

      if (!faction.HasInvite(user))
      {
        user.SendChatMessage(Messages.CannotJoinFactionNotInvited, faction.Id);
        return;
      }

      user.SendChatMessage(Messages.YouJoinedFaction, faction.Id);
      PrintToChat(Messages.FactionMemberJoinedAnnouncement, user.UserName, faction.Id);
      Log($"{Util.Format(user)} joined faction {faction.Id}");

      faction.AddMember(user);
      user.SetFaction(faction);

    }
  }
}﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnFactionKickCommand(User user, string[] args)
    {
      if (args.Length != 1)
      {
        user.SendChatMessage(Messages.Usage, "/faction kick \"PLAYER\"");
        return;
      }

      Faction faction = user.Faction;

      if (faction == null || !faction.HasLeader(user))
      {
        user.SendChatMessage(Messages.NotLeaderOfFaction);
        return;
      }

      User member = Users.Find(args[0]);

      if (member == null)
      {
        user.SendChatMessage(Messages.InvalidUser, args[0]);
        return;
      }

      if (faction.HasLeader(member))
      {
        user.SendChatMessage(Messages.CannotKickLeaderOfFaction, member.UserName, faction.Id);
        return;
      }

      user.SendChatMessage(Messages.MemberRemoved, member.UserName, faction.Id);
      PrintToChat(Messages.FactionMemberLeftAnnouncement, member.UserName, faction.Id);

      Log($"{Util.Format(user)} kicked {Util.Format(member)} from faction {faction.Id}");

      faction.RemoveMember(member);
      member.SetFaction(null);
    }
  }
}﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnFactionLeaveCommand(User user, string[] args)
    {
      if (args.Length != 0)
      {
        user.SendChatMessage(Messages.Usage, "/faction leave");
        return;
      }

      Faction faction = user.Faction;

      if (faction == null)
      {
        user.SendChatMessage(Messages.NotMemberOfFaction);
        return;
      }

      if (faction.MemberCount == 1)
      {
        PrintToChat(Messages.FactionDisbandedAnnouncement, faction.Id);
        Log($"{Util.Format(user)} disbanded faction {faction.Id} by leaving as its only member");
        Factions.Disband(faction);
        return;
      }

      user.SendChatMessage(Messages.YouLeftFaction, faction.Id);
      PrintToChat(Messages.FactionMemberLeftAnnouncement, user.UserName, faction.Id);

      Log($"{Util.Format(user)} left faction {faction.Id}");

      faction.RemoveMember(user);
      user.SetFaction(null);
    }
  }
}﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnFactionPromoteCommand(User user, string[] args)
    {
      if (args.Length != 1)
      {
        user.SendChatMessage(Messages.Usage, "/faction promote \"PLAYER\"");
        return;
      }

      Faction faction = Factions.GetByMember(user);

      if (faction == null || !faction.HasLeader(user))
      {
        user.SendChatMessage(Messages.NotLeaderOfFaction);
        return;
      }

      User member = Users.Find(args[0]);

      if (member == null)
      {
        user.SendChatMessage(Messages.InvalidUser, args[0]);
        return;
      }

      if (faction.HasOwner(member))
      {
        user.SendChatMessage(Messages.CannotPromoteOrDemoteOwnerOfFaction, member.UserName, faction.Id);
        return;
      }

      if (faction.HasManager(member))
      {
        user.SendChatMessage(Messages.UserIsAlreadyManagerOfFaction, member.UserName, faction.Id);
        return;
      }

      user.SendChatMessage(Messages.ManagerAdded, member.UserName, faction.Id);
      Log($"{Util.Format(user)} promoted {Util.Format(member)} in faction {faction.Id}");

      faction.Promote(member);
    }
  }
}﻿namespace Oxide.Plugins
{
  using System.Text;

  public partial class Imperium
  {
    void OnFactionShowCommand(User user)
    {
      Faction faction = user.Faction;

      if (faction == null)
      {
        user.SendChatMessage(Messages.NotMemberOfFaction);
        return;
      }

      var sb = new StringBuilder();

      sb.Append("You are ");
      if (faction.HasOwner(user))
        sb.Append("the owner");
      else if (faction.HasManager(user))
        sb.Append("a manager");
      else
        sb.Append("a member");

      sb.AppendLine($"of <color=#ffd479>[{faction.Id}]</color>.");

      User[] activeMembers = faction.GetAllActiveMembers();

      sb.AppendLine($"<color=#ffd479>{faction.MemberCount}</color> member(s), <color=#ffd479>{activeMembers.Length}</color> online:");
      sb.Append("  ");

      foreach (User member in activeMembers)
        sb.Append($"<color=#ffd479>{member.UserName}</color>, ");

      sb.Remove(sb.Length - 2, 2);
      sb.AppendLine();

      if (faction.InviteIds.Count > 0)
      {
        User[] activeInvitedUsers = faction.GetAllActiveInvitedUsers();

        sb.AppendLine($"<color=#ffd479>{faction.InviteIds.Count}</color> invited player(s), <color=#ffd479>{activeInvitedUsers.Length}</color> online:");
        sb.Append("  ");

        foreach (User invitedUser in activeInvitedUsers)
          sb.Append($"<color=#ffd479>{invitedUser.UserName}</color>, ");

        sb.Remove(sb.Length - 2, 2);
        sb.AppendLine();
      }

      user.SendChatMessage(sb);
    }
  }
}﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    [ConsoleCommand("imperium.images.refresh")]
    void OnRefreshImagesConsoleCommand(ConsoleSystem.Arg arg)
    {
      if (!arg.IsAdmin) return;
      arg.ReplyWith("Refreshing images...");
      Hud.RefreshAllImages();
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Linq;

  public partial class Imperium
  {
    [ChatCommand("pin")]
    void OnPinCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.Map.PinsEnabled)
      {
        user.SendChatMessage(Messages.PinsDisabled);
        return;
      };

      if (args.Length == 0)
      {
        OnPinHelpCommand(user);
        return;
      }

      var restArguments = args.Skip(1).ToArray();

      switch (args[0].ToLower())
      {
        case "add":
          OnPinAddCommand(user, restArguments);
          break;
        case "remove":
          OnPinRemoveCommand(user, restArguments);
          break;
        case "list":
          OnPinListCommand(user, restArguments);
          break;
        case "delete":
          OnPinDeleteCommand(user, restArguments);
          break;
        case "help":
        default:
          OnPinHelpCommand(user);
          break;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using UnityEngine;

  public partial class Imperium
  {
    void OnPinAddCommand(User user, string[] args)
    {
      if (args.Length != 2)
      {
        user.SendChatMessage(Messages.Usage, "/pin add TYPE \"NAME\"");
        return;
      }

      if (user.Faction == null)
      {
        user.SendChatMessage(Messages.NotMemberOfFaction);
        return;
      }

      Area area = user.CurrentArea;
      if (area.FactionId == null || area.FactionId != user.Faction.Id)
      {
        user.SendChatMessage(Messages.AreaNotOwnedByYourFaction, area.Id);
        return;
      }

      PinType type;
      if (!Util.TryParseEnum(args[0], out type))
      {
        user.SendChatMessage(Messages.InvalidPinType, args[0]);
        return;
      }

      string name = Util.NormalizePinName(args[1]);
      if (name == null || name.Length < Options.Map.MinPinNameLength || name.Length > Options.Map.MaxPinNameLength)
      {
        user.SendChatMessage(Messages.InvalidPinName, Options.Map.MinPinNameLength, Options.Map.MaxPinNameLength);
        return;
      }

      Pin existingPin = Pins.Get(name);
      if (existingPin != null)
      {
        user.SendChatMessage(Messages.CannotCreatePinAlreadyExists, existingPin.Name, existingPin.AreaId);
        return;
      }

      if (Options.Map.PinCost > 0)
      {
        ItemDefinition scrapDef = ItemManager.FindItemDefinition("scrap");
        List<Item> stacks = user.Player.inventory.FindItemIDs(scrapDef.itemid);

        if (!Instance.TryCollectFromStacks(scrapDef, stacks, Options.Map.PinCost))
        {
          user.SendChatMessage(Messages.CannotCreatePinCannotAfford, Options.Map.PinCost);
          return;
        }
      }

      Vector3 position = user.Player.transform.position;

      var pin = new Pin(position, area, user, type, name);
      Pins.Add(pin);

      PrintToChat(Messages.PinAddedAnnouncement, user.Faction.Id, name, type.ToString().ToLower(), area.Id);
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    void OnPinDeleteCommand(User user, string[] args)
    {
      if (args.Length != 1)
      {
        user.SendChatMessage(Messages.Usage, "/pin delete \"NAME\"");
        return;
      }

      if (!user.HasPermission(Permission.AdminPins))
      {
        user.SendChatMessage(Messages.NoPermission);
        return;
      }

      string name = Util.NormalizePinName(args[0]);
      Pin pin = Pins.Get(name);

      if (pin == null)
      {
        user.SendChatMessage(Messages.UnknownPin, name);
        return;
      }

      Pins.Remove(pin);
      user.SendChatMessage(Messages.PinRemoved, pin.Name);
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  public partial class Imperium
  {
    void OnPinHelpCommand(User user)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/pin list [TYPE]</color>: List all pins (or all of a certain type)");
      sb.AppendLine("  <color=#ffd479>/pin add TYPE \"NAME\"</color>: Create a pin at your current location");
      sb.AppendLine("  <color=#ffd479>/pin remove \"NAME\"</color>: Remove a pin you created");
      sb.AppendLine("  <color=#ffd479>/pin help</color>: Prints this message");

      if (user.HasPermission(Permission.AdminPins))
      {
        sb.AppendLine("Admin commands:");
        sb.AppendLine("  <color=#ffd479>/pin delete XY</color>: Delete a pin from an area");
      }

      sb.Append("Available pin types: ");
      foreach (string type in Enum.GetNames(typeof(PinType)).OrderBy(str => str.ToLowerInvariant()))
        sb.AppendFormat("<color=#ffd479>{0}</color>, ", type.ToLowerInvariant());
      sb.Remove(sb.Length - 2, 2);
      sb.AppendLine();

      user.SendChatMessage(sb);
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Linq;
  using System.Text;

  public partial class Imperium
  {
    void OnPinListCommand(User user, string[] args)
    {
      if (args.Length > 1)
      {
        user.SendChatMessage(Messages.Usage, "/pin list [TYPE]");
        return;
      }

      Pin[] pins = Pins.GetAll();

      if (args.Length == 1)
      {
        PinType type;
        if (!Util.TryParseEnum(args[0], out type))
        {
          user.SendChatMessage(Messages.InvalidPinType, args[0]);
          return;
        }

        pins = pins.Where(pin => pin.Type == type).ToArray();
      }

      if (pins.Length == 0)
      {
        user.SendChatMessage("There are no matching pins.");
        return;
      }

      var sb = new StringBuilder();
      sb.AppendLine(String.Format("There are <color=#ffd479>{0}</color> matching map pins:", pins.Length));
      foreach (Pin pin in pins.OrderBy(pin => pin.GetDistanceFrom(user.Player)))
      {
        int distance = (int)Math.Floor(pin.GetDistanceFrom(user.Player));
        sb.AppendLine(String.Format("  <color=#ffd479>{0} ({1}):</color> {2} (<color=#ffd479>{3}m</color> away)", pin.Name, pin.Type.ToString().ToLowerInvariant(), pin.AreaId, distance));
      }

      user.SendChatMessage(sb);
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    void OnPinRemoveCommand(User user, string[] args)
    {
      if (args.Length != 1)
      {
        user.SendChatMessage(Messages.Usage, "/pin remove \"NAME\"");
        return;
      }

      if (user.Faction == null)
      {
        user.SendChatMessage(Messages.NotMemberOfFaction);
        return;
      }

      string name = Util.NormalizePinName(args[0]);
      Pin pin = Pins.Get(name);

      if (pin == null)
      {
        user.SendChatMessage(Messages.UnknownPin, name);
        return;
      }

      Area area = Areas.Get(pin.AreaId);
      if (area.FactionId != user.Faction.Id)
      {
        user.SendChatMessage(Messages.CannotRemovePinAreaNotOwnedByYourFaction, pin.Name, pin.AreaId);
        return;
      }

      Pins.Remove(pin);
      user.SendChatMessage(Messages.PinRemoved, pin.Name);
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Linq;

  public partial class Imperium
  {
    [ChatCommand("tax")]
    void OnTaxCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.Taxes.Enabled)
      {
        user.SendChatMessage(Messages.TaxationDisabled);
        return;
      };

      if (args.Length == 0)
      {
        OnTaxHelpCommand(user);
        return;
      }

      var restArguments = args.Skip(1).ToArray();

      switch (args[0].ToLower())
      {
        case "chest":
          OnTaxChestCommand(user);
          break;
        case "rate":
          OnTaxRateCommand(user, restArguments);
          break;
        case "help":
        default:
          OnTaxHelpCommand(user);
          break;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnTaxChestCommand(User user)
    {
      Faction faction = Factions.GetByMember(user);

      if (faction == null || !faction.HasLeader(user))
      {
        user.SendChatMessage(Messages.NotLeaderOfFaction);
        return;
      }

      user.SendChatMessage(Messages.SelectTaxChest);
      user.BeginInteraction(new SelectingTaxChestInteraction(faction));
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Text;

  public partial class Imperium
  {
    void OnTaxHelpCommand(User user)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/tax rate NN</color>: Set the tax rate for your faction");
      sb.AppendLine("  <color=#ffd479>/tax chest</color>: Select a container to use as your faction's tax chest");
      sb.AppendLine("  <color=#ffd479>/tax help</color>: Prints this message");

      user.SendChatMessage(sb);
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    void OnTaxRateCommand(User user, string[] args)
    {
      Faction faction = Factions.GetByMember(user);

      if (faction == null || !faction.HasLeader(user))
      {
        user.SendChatMessage(Messages.NotLeaderOfFaction);
        return;
      }

      float taxRate;
      try
      {
        taxRate = Convert.ToInt32(args[0]) / 100f;
      }
      catch
      {
        user.SendChatMessage(Messages.CannotSetTaxRateInvalidValue, Options.Taxes.MaxTaxRate * 100);
        return;
      }

      if (taxRate < 0 || taxRate > Options.Taxes.MaxTaxRate)
      {
        user.SendChatMessage(Messages.CannotSetTaxRateInvalidValue, Options.Taxes.MaxTaxRate * 100);
        return;
      }

      user.SendChatMessage(Messages.SetTaxRateSuccessful, faction.Id, taxRate * 100);
      Log($"{Util.Format(user)} set the tax rate for faction {faction.Id} to {taxRate * 100}%");

      Factions.SetTaxRate(faction, taxRate);
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    [ChatCommand("hud")]
    void OnHudCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!EnforceCommandCooldown(user, "hud", Options.Map.CommandCooldownSeconds))
        return;

      user.Hud.Toggle();
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    [ConsoleCommand("imperium.hud.toggle")]
    void OnHudToggleConsoleCommand(ConsoleSystem.Arg arg)
    {
      var player = arg.Connection.player as BasePlayer;
      if (player == null) return;

      User user = Users.Get(player);
      if (user == null) return;

      if (!EnforceCommandCooldown(user, "hud", Options.Map.CommandCooldownSeconds))
        return;

      user.Hud.Toggle();
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    [ConsoleCommand("imperium.map.togglelayer")]
    void OnMapToggleLayerConsoleCommand(ConsoleSystem.Arg arg)
    {
      var player = arg.Connection.player as BasePlayer;
      if (player == null) return;

      User user = Users.Get(player);
      if (user == null) return;

      if (!user.Map.IsVisible)
        return;

      string str = arg.GetString(0);
      UserMapLayer layer;

      if (String.IsNullOrEmpty(str) || !Util.TryParseEnum(arg.Args[0], out layer))
        return;

      user.Preferences.ToggleMapLayer(layer);
      user.Map.Refresh();
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    [ChatCommand("map")]
    void OnMapCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!user.Map.IsVisible && !EnforceCommandCooldown(user, "map", Options.Map.CommandCooldownSeconds))
        return;

      user.Map.Toggle();
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    [ConsoleCommand("imperium.map.toggle")]
    void OnMapToggleConsoleCommand(ConsoleSystem.Arg arg)
    {
      var player = arg.Connection.player as BasePlayer;
      if (player == null) return;

      User user = Users.Get(player);
      if (user == null) return;

      if (!user.Map.IsVisible && !EnforceCommandCooldown(user, "map", Options.Map.CommandCooldownSeconds))
        return;

      user.Map.Toggle();
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Linq;

  public partial class Imperium
  {
    [ChatCommand("war")]
    void OnWarCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.War.Enabled)
      {
        user.SendChatMessage(Messages.WarDisabled);
        return;
      }

      if (args.Length == 0)
      {
        OnWarHelpCommand(user);
        return;
      }

      var restArgs = args.Skip(1).ToArray();

      switch (args[0].ToLower())
      {
        case "list":
          OnWarListCommand(user);
          break;
        case "status":
          OnWarStatusCommand(user);
          break;
        case "declare":
          OnWarDeclareCommand(user, restArgs);
          break;
        case "end":
          OnWarEndCommand(user, restArgs);
          break;
        default:
          OnWarHelpCommand(user);
          break;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnWarDeclareCommand(User user, string[] args)
    {
      Faction attacker = Factions.GetByMember(user);

      if (!EnsureUserAndFactionCanEngageInDiplomacy(user, attacker))
        return;

      if (args.Length < 2)
      {
        user.SendChatMessage(Messages.Usage, "/war declare FACTION \"REASON\"");
        return;
      }

      Faction defender = Factions.Get(Util.NormalizeFactionId(args[0]));

      if (defender == null)
      {
        user.SendChatMessage(Messages.FactionDoesNotExist, args[0]);
        return;
      }

      if (attacker.Id == defender.Id)
      {
        user.SendChatMessage(Messages.CannotDeclareWarAgainstYourself);
        return;
      }

      War existingWar = Wars.GetActiveWarBetween(attacker, defender);

      if (existingWar != null)
      {
        user.SendChatMessage(Messages.CannotDeclareWarAlreadyAtWar, defender.Id);
        return;
      }

      string cassusBelli = args[1].Trim();

      if (cassusBelli.Length < Options.War.MinCassusBelliLength)
      {
        user.SendChatMessage(Messages.CannotDeclareWarInvalidCassusBelli, defender.Id);
        return;
      }

      War war = Wars.DeclareWar(attacker, defender, user, cassusBelli);
      PrintToChat(Messages.WarDeclaredAnnouncement, war.AttackerId, war.DefenderId, war.CassusBelli);
      Log($"{Util.Format(user)} declared war on faction {war.DefenderId} on behalf of {war.AttackerId} for reason: {war.CassusBelli}");
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnWarEndCommand(User user, string[] args)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureUserAndFactionCanEngageInDiplomacy(user, faction))
        return;

      Faction enemy = Factions.Get(Util.NormalizeFactionId(args[0]));

      if (enemy == null)
      {
        user.SendChatMessage(Messages.FactionDoesNotExist, args[0]);
        return;
      }

      War war = Wars.GetActiveWarBetween(faction, enemy);

      if (war == null)
      {
        user.SendChatMessage(Messages.NotAtWar, enemy.Id);
        return;
      }

      if (war.IsOfferingPeace(faction))
      {
        user.SendChatMessage(Messages.CannotOfferPeaceAlreadyOfferedPeace, enemy.Id);
        return;
      }

      war.OfferPeace(faction);

      if (war.IsAttackerOfferingPeace && war.IsDefenderOfferingPeace)
      {
        PrintToChat(Messages.WarEndedTreatyAcceptedAnnouncement, faction.Id, enemy.Id);
        Log($"{Util.Format(user)} accepted the peace offering of {enemy.Id} on behalf of {faction.Id}");
        Wars.EndWar(war, WarEndReason.Treaty);
        OnDiplomacyChanged();
      }
      else
      {
        user.SendChatMessage(Messages.PeaceOffered, enemy.Id);
        Log($"{Util.Format(user)} offered peace to faction {enemy.Id} on behalf of {faction.Id}");
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Text;

  public partial class Imperium
  {
    void OnWarHelpCommand(User user)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/war list</color>: Show all active wars");
      sb.AppendLine("  <color=#ffd479>/war status</color>: Show all active wars your faction is involved in");
      sb.AppendLine("  <color=#ffd479>/war declare FACTION \"REASON\"</color>: Declare war against another faction");
      sb.AppendLine("  <color=#ffd479>/war end FACTION</color>: Offer to end a war, or accept an offer made to you");
      sb.AppendLine("  <color=#ffd479>/war help</color>: Show this message");

      user.SendChatMessage(sb);
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Text;

  public partial class Imperium
  {
    void OnWarListCommand(User user)
    {
      var sb = new StringBuilder();
      War[] wars = Wars.GetAllActiveWars();

      if (wars.Length == 0)
      {
        sb.Append("The island is at peace... for now. No wars have been declared.");
      }
      else
      {
        sb.AppendLine(String.Format("<color=#ffd479>The island is at war! {0} wars have been declared:</color>", wars.Length));
        for (var idx = 0; idx < wars.Length; idx++)
        {
          War war = wars[idx];
          sb.AppendFormat("{0}. <color=#ffd479>{1}</color> vs <color=#ffd479>{2}</color>: {2}", (idx + 1), war.AttackerId, war.DefenderId, war.CassusBelli);
          sb.AppendLine();
        }
      }

      user.SendChatMessage(sb);
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Text;

  public partial class Imperium
  {
    void OnWarStatusCommand(User user)
    {
      Faction faction = Factions.GetByMember(user);

      if (faction == null)
      {
        user.SendChatMessage(Messages.NotMemberOfFaction);
        return;
      }

      var sb = new StringBuilder();
      War[] wars = Wars.GetAllActiveWarsByFaction(faction);

      if (wars.Length == 0)
      {
        sb.AppendLine("Your faction is not involved in any wars.");
      }
      else
      {
        sb.AppendLine(String.Format("<color=#ffd479>Your faction is involved in {0} wars:</color>", wars.Length));
        for (var idx = 0; idx < wars.Length; idx++)
        {
          War war = wars[idx];
          sb.AppendFormat("{0}. <color=#ffd479>{1}</color> vs <color=#ffd479>{2}</color>", (idx + 1), war.AttackerId, war.DefenderId);
          if (war.IsAttackerOfferingPeace) sb.AppendFormat(": <color=#ffd479>{0}</color> is offering peace!", war.AttackerId);
          if (war.IsDefenderOfferingPeace) sb.AppendFormat(": <color=#ffd479>{0}</color> is offering peace!", war.DefenderId);
          sb.AppendLine();
        }
      }

      user.SendChatMessage(sb);
    }
  }
}
﻿namespace Oxide.Plugins
{
  using Oxide.Core;

  public partial class Imperium : RustPlugin
  {
    static class Api
    {
      public static void OnAreaChanged(Area area)
      {
        Interface.CallHook(nameof(OnAreaChanged), area);
      }

      public static void OnUserEnteredArea(User user, Area area)
      {
        Interface.CallHook(nameof(OnUserEnteredArea), user, area);
      }

      public static void OnUserLeftArea(User user, Area area)
      {
        Interface.CallHook(nameof(OnUserLeftArea), user, area);
      }

      public static void OnUserEnteredZone(User user, Zone zone)
      {
        Interface.CallHook(nameof(OnUserEnteredZone), user, zone);
      }

      public static void OnUserLeftZone(User user, Zone zone)
      {
        Interface.CallHook(nameof(OnUserLeftZone), user, zone);
      }

      public static void OnFactionCreated(Faction faction)
      {
        Interface.CallHook(nameof(OnFactionCreated), faction);
      }

      public static void OnFactionDisbanded(Faction faction)
      {
        Interface.CallHook(nameof(OnFactionDisbanded), faction);
      }

      public static void OnFactionTaxesChanged(Faction faction)
      {
        Interface.CallHook(nameof(OnFactionTaxesChanged), faction);
      }

      public static void OnPlayerJoinedFaction(Faction faction, User user)
      {
        Interface.CallHook(nameof(OnPlayerJoinedFaction), faction, user);
      }

      public static void OnPlayerLeftFaction(Faction faction, User user)
      {
        Interface.CallHook(nameof(OnPlayerLeftFaction), faction, user);
      }

      public static void OnPlayerInvitedToFaction(Faction faction, User user)
      {
        Interface.CallHook(nameof(OnPlayerInvitedToFaction), faction, user);
      }

      public static void OnPlayerUninvitedFromFaction(Faction faction, User user)
      {
        Interface.CallHook(nameof(OnPlayerUninvitedFromFaction), faction, user);
      }

      public static void OnPlayerPromoted(Faction faction, User user)
      {
        Interface.CallHook(nameof(OnPlayerPromoted), faction, user);
      }

      public static void OnPlayerDemoted(Faction faction, User user)
      {
        Interface.CallHook(nameof(OnPlayerDemoted), faction, user);
      }

      public static void OnPinCreated(Pin pin)
      {
        Interface.CallHook(nameof(OnPinCreated), pin);
      }

      public static void OnPinRemoved(Pin pin)
      {
        Interface.CallHook(nameof(OnPinRemoved), pin);
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    static class Decay
    {
      public static object AlterDecayDamage(BaseEntity entity, HitInfo hit)
      {
        if (!Instance.Options.Decay.Enabled)
          return null;

        Area area = GetAreaForDecayCalculation(entity);

        if (area == null)
        {
          Instance.PrintWarning($"An entity decayed in an unknown area. This shouldn't happen.");
          return null;
        }

        float reduction = 0;

        if (area.Type == AreaType.Claimed || area.Type == AreaType.Headquarters)
          reduction = Instance.Options.Decay.ClaimedLandDecayReduction;

        if (reduction >= 1)
          return false;

        if (reduction > 0)
          hit.damageTypes.Scale(Rust.DamageType.Decay, reduction);

        return null;
      }

      static Area GetAreaForDecayCalculation(BaseEntity entity)
      {
        Area area = null;

        // If the entity is controlled by a claim cupboard, use the area the cupboard controls.
        BuildingPrivlidge cupboard = entity.GetBuildingPrivilege();
        if (cupboard)
          area = Instance.Areas.GetByClaimCupboard(cupboard);

        // Otherwise, determine the area by its position in the world.
        if (area == null)
          area = Instance.Areas.GetByEntityPosition(entity);

        return area;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using Network;
  using Oxide.Core;
  using UnityEngine;

  public partial class Imperium : RustPlugin
  {
    void OnUserApprove(Connection connection)
    {
      Users.SetOriginalName(connection.userid.ToString(), connection.username);
    }

    void OnPlayerInit(BasePlayer player)
    {
      if (player == null) return;

      // If the player hasn't fully connected yet, try again in 2 seconds.
      if (player.IsReceivingSnapshot)
      {
        timer.In(2, () => OnPlayerInit(player));
        return;
      }

      Users.Add(player);
    }

    void OnPlayerDisconnected(BasePlayer player)
    {
      if (player != null)
        Users.Remove(player);
    }

    void OnHammerHit(BasePlayer player, HitInfo hit)
    {
      User user = Users.Get(player);

      if (user != null && user.CurrentInteraction != null)
        user.CompleteInteraction(hit);
    }

    object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hit)
    {
      if (entity == null || hit == null)
        return null;

      object externalResult = Interface.CallHook("CanEntityTakeDamage", new object[] { entity, hit });

      if (externalResult != null)
      {
        if ((bool)externalResult == false)
          return false;

        return null;
      }

      if (hit.damageTypes.Has(Rust.DamageType.Decay))
        return Decay.AlterDecayDamage(entity, hit);

      User attacker = null;
      User defender = entity.GetComponent<User>();

      if (hit.InitiatorPlayer != null)
        attacker = hit.InitiatorPlayer.GetComponent<User>();

      // A player is being injured by something other than a player/NPC.
      if (attacker == null && defender != null)
        return Pvp.HandleIncidentalDamage(defender, hit);

      // One player is damaging another player.
      if (attacker != null && defender != null)
        return Pvp.HandleDamageBetweenPlayers(attacker, defender, hit);

      // A player is damaging a structure.
      if (attacker != null && defender == null)
        return Raiding.HandleDamageAgainstStructure(attacker, entity, hit);

      // A structure is taking damage from something that isn't a player.
      return Raiding.HandleIncidentalDamage(entity, hit);
    }

    object OnTrapTrigger(BaseTrap trap, GameObject obj)
    {
      var player = obj.GetComponent<BasePlayer>();

      if (trap == null || player == null)
        return null;

      User defender = Users.Get(player);
      return Pvp.HandleTrapTrigger(trap, defender);
    }

    object CanBeTargeted(BaseCombatEntity target, MonoBehaviour turret)
    {
      if (target == null || turret == null)
        return null;

      // Don't interfere with the helicopter.
      if (turret is HelicopterTurret)
        return null;

      var player = target as BasePlayer;

      if (player == null)
        return null;

      User defender = Users.Get(player);
      var entity = turret as BaseCombatEntity;

      if (defender == null || entity == null)
        return null;

      return Pvp.HandleTurretTarget(entity, defender);
    }

    void OnEntitySpawned(BaseNetworkable entity)
    {
      var plane = entity as CargoPlane;
      if (plane != null)
        Hud.GameEvents.BeginEvent(plane);

      var drop = entity as SupplyDrop;
      if (Options.Zones.Enabled && drop != null)
        Zones.CreateForSupplyDrop(drop);

      var heli = entity as BaseHelicopter;
      if (heli != null)
        Hud.GameEvents.BeginEvent(heli);

      var chinook = entity as CH47Helicopter;
      if (chinook != null)
        Hud.GameEvents.BeginEvent(chinook);

      var crate = entity as HackableLockedCrate;
      if (crate != null)
        Hud.GameEvents.BeginEvent(crate);
    }

    void OnEntityKill(BaseNetworkable networkable)
    {
      var entity = networkable as BaseEntity;

      if (!Ready || entity == null)
        return;

      // If a claim TC was destroyed, remove the claim from the area.
      var cupboard = entity as BuildingPrivlidge;
      if (cupboard != null)
      {
        var area = Areas.GetByClaimCupboard(cupboard);
        if (area != null)
        {
          PrintToChat(Messages.AreaClaimLostCupboardDestroyedAnnouncement, area.FactionId, area.Id);
          Log($"{area.FactionId} lost their claim on {area.Id} because the tool cupboard was destroyed (hook function)");
          Areas.Unclaim(area);
        }
        return;
      }

      // If a tax chest was destroyed, remove it from the faction data.
      var container = entity as StorageContainer;
      if (Options.Taxes.Enabled && container != null)
      {
        Faction faction = Factions.GetByTaxChest(container);
        if (faction != null)
        {
          Log($"{faction.Id}'s tax chest was destroyed (hook function)");
          faction.TaxChest = null;
        }
        return;
      }

      // If a helicopter was destroyed, create an event zone around it.
      var helicopter = entity as BaseHelicopter;
      if (Options.Zones.Enabled && helicopter != null)
        Zones.CreateForDebrisField(helicopter);
    }

    void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
    {
      Taxes.ProcessTaxesIfApplicable(dispenser, entity, item);
      Taxes.AwardBadlandsBonusIfApplicable(dispenser, entity, item);
    }

    void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item)
    {
      Taxes.ProcessTaxesIfApplicable(dispenser, entity, item);
      Taxes.AwardBadlandsBonusIfApplicable(dispenser, entity, item);
    }

    object OnPlayerDie(BasePlayer player, HitInfo hit)
    {
      if (player == null)
        return null;

      // When a player dies, remove them from the area and any zones they were in.
      User user = Users.Get(player);
      if (user != null)
      {
        user.CurrentArea = null;
        user.CurrentZones.Clear();
      }

      return null;
    }

    void OnUserEnteredArea(User user, Area area)
    {
      Area previousArea = user.CurrentArea;

      user.CurrentArea = area;
      user.Hud.Refresh();

      if (previousArea == null)
        return;

      if (area.Type == AreaType.Badlands && previousArea.Type != AreaType.Badlands)
      {
        // The player has entered the badlands.
        user.SendChatMessage(Messages.EnteredBadlands);
      }
      else if (area.Type == AreaType.Wilderness && previousArea.Type != AreaType.Wilderness)
      {
        // The player has entered the wilderness.
        user.SendChatMessage(Messages.EnteredWilderness);
      }
      else if (area.IsClaimed && !previousArea.IsClaimed)
      {
        // The player has entered a faction's territory.
        user.SendChatMessage(Messages.EnteredClaimedArea, area.FactionId);
      }
      else if (area.IsClaimed && previousArea.IsClaimed && area.FactionId != previousArea.FactionId)
      {
        // The player has crossed a border between the territory of two factions.
        user.SendChatMessage(Messages.EnteredClaimedArea, area.FactionId);
      }
    }

    void OnUserEnteredZone(User user, Zone zone)
    {
      user.CurrentZones.Add(zone);
      user.Hud.Refresh();
    }

    void OnUserLeftZone(User user, Zone zone)
    {
      user.CurrentZones.Remove(zone);
      user.Hud.Refresh();
    }

    void OnFactionCreated(Faction faction)
    {
      Hud.RefreshForAllPlayers();
    }

    void OnFactionDisbanded(Faction faction)
    {
      Area[] areas = Instance.Areas.GetAllClaimedByFaction(faction);

      if (areas.Length > 0)
      {
        foreach (Area area in areas)
          PrintToChat(Messages.AreaClaimLostFactionDisbandedAnnouncement, area.FactionId, area.Id);

        Areas.Unclaim(areas);
      }

      Wars.EndAllWarsForEliminatedFactions();
      Hud.RefreshForAllPlayers();
    }

    void OnFactionTaxesChanged(Faction faction)
    {
      Hud.RefreshForAllPlayers();
    }

    void OnAreaChanged(Area area)
    {
      Wars.EndAllWarsForEliminatedFactions();
      Pins.RemoveAllPinsInUnclaimedAreas();
      Hud.GenerateMapOverlayImage();
      Hud.RefreshForAllPlayers();
    }

    void OnDiplomacyChanged()
    {
      Hud.RefreshForAllPlayers();
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Linq;
  using System.Reflection;

  public partial class Imperium : RustPlugin
  {
    static class Messages
    {
      public const string AreaClaimsDisabled = "Area claims are currently disabled.";
      public const string TaxationDisabled = "Taxation is currently disabled.";
      public const string BadlandsDisabled = "Badlands are currently disabled.";
      public const string UpkeepDisabled = "Upkeep is currently disabled.";
      public const string WarDisabled = "War is currently disabled.";
      public const string PinsDisabled = "Map pins are currently disabled.";
      public const string PvpModeDisabled = "PVP Mode is currently not available.";

      public const string AreaIsBadlands = "<color=#ffd479>{0}</color> is a part of the badlands.";
      public const string AreaIsClaimed = "<color=#ffd479>{0}</color> has been claimed by <color=#ffd479>[{1}]</color>.";
      public const string AreaIsHeadquarters = "<color=#ffd479>{0}</color> is the headquarters of <color=#ffd479>[{1}]</color>.";
      public const string AreaIsWilderness = "<color=#ffd479>{0}</color> has not been claimed by a faction.";
      public const string AreaNotBadlands = "<color=#ffd479>{0}</color> is not a part of the badlands.";
      public const string AreaNotOwnedByYourFaction = "<color=#ffd479>{0}</color> is not owned by your faction.";
      public const string AreaNotWilderness = "<color=#ffd479>{0}</color> is not currently wilderness.";
      public const string AreaNotContiguous = "<color=#ffd479>{0}</color> is not connected to territory owned by <color=#ffd479>[{1}]</color>.";

      public const string InteractionCanceled = "Command canceled.";
      public const string NoInteractionInProgress = "You aren't currently executing any commands.";
      public const string NoAreasClaimed = "Your faction has not claimed any areas.";
      public const string NotMemberOfFaction = "You are not a member of a faction.";
      public const string AlreadyMemberOfFaction = "You are already a member of a faction.";
      public const string NotLeaderOfFaction = "You must be an owner or a manager of a faction.";
      public const string FactionTooSmallToOwnLand = "To own land, a faction must have least {0} members.";
      public const string FactionOwnsTooMuchLand = "<color=#ffd479>[{0}]</color> already owns the maximum number of areas (<color=#ffd479>{1}</color>).";
      public const string FactionHasTooManyMembers = "<color=#ffd479>[{0}]</color> already has the maximum number of members (<color=#ffd479>{1}</color>).";
      public const string FactionDoesNotOwnLand = "Your faction must own at least one area.";
      public const string FactionAlreadyExists = "There is already a faction named <color=#ffd479>[{0}]</color>.";
      public const string FactionDoesNotExist = "There is no faction named <color=#ffd479>[{0}]</color>.";
      public const string InvalidUser = "Couldn't find a user whose name matches \"{0}\".";
      public const string InvalidFactionName = "Faction names must be between {0} and {1} alphanumeric characters.";
      public const string NotAtWar = "You are not currently at war with <color=#ffd479>[{0}]</color>!";

      public const string Usage = "Usage: <color=#ffd479>{0}</color>";
      public const string CommandIsOnCooldown = "You can't do that again so quickly. Try again in {0} seconds.";
      public const string NoPermission = "You don't have permission to do that.";

      public const string MemberAdded = "You have added <color=#ffd479>{0}</color> as a member of <color=#ffd479>[{1}]</color>.";
      public const string MemberRemoved = "You have removed <color=#ffd479>{0}</color> as a member of <color=#ffd479>[{1}]</color>.";
      public const string ManagerAdded = "You have added <color=#ffd479>{0}</color> as a manager of <color=#ffd479>[{1}]</color>.";
      public const string ManagerRemoved = "You have removed <color=#ffd479>{0}</color> as a manager of <color=#ffd479>[{1}]</color>.";
      public const string UserIsAlreadyMemberOfFaction = "<color=#ffd479>{0}</color> is already a member of <color=#ffd479>[{1}]</color>.";
      public const string UserIsNotMemberOfFaction = "<color=#ffd479>{0}</color> is not a member of <color=#ffd479>[{1}]</color>.";
      public const string UserIsAlreadyManagerOfFaction = "<color=#ffd479>{0}</color> is already a manager of <color=#ffd479>[{1}]</color>.";
      public const string UserIsNotManagerOfFaction = "<color=#ffd479>{0}</color> is not a manager of <color=#ffd479>[{1}]</color>.";
      public const string CannotPromoteOrDemoteOwnerOfFaction = "<color=#ffd479>{0}</color> cannot be promoted or demoted, since they are the owner of <color=#ffd479>[{1}]</color>.";
      public const string CannotKickLeaderOfFaction = "<color=#ffd479>{0}</color> cannot be kicked, since they are an owner or manager of <color=#ffd479>[{1}]</color>.";
      public const string InviteAdded = "You have invited <color=#ffd479>{0}</color> to join <color=#ffd479>[{1}]</color>.";
      public const string InviteReceived = "<color=#ffd479>{0}</color> has invited you to join <color=#ffd479>[{1}]</color>. Say <color=#ffd479>/faction join {1}</color> to accept.";
      public const string CannotJoinFactionNotInvited = "You cannot join <color=#ffd479>[{0}]</color>, because you have not been invited.";
      public const string YouJoinedFaction = "You are now a member of <color=#ffd479>[{0}]</color>.";
      public const string YouLeftFaction = "You are no longer a member of <color=#ffd479>[{0}]</color>.";

      public const string SelectingCupboardFailedInvalidTarget = "You must select a tool cupboard.";
      public const string SelectingCupboardFailedNotAuthorized = "You must be authorized on the tool cupboard.";
      public const string SelectingCupboardFailedNotClaimCupboard = "That tool cupboard doesn't represent an area claim made by your faction.";

      public const string CannotClaimAreaAlreadyClaimed = "<color=#ffd479>{0}</color> has already been claimed by <color=#ffd479>[{1}]</color>.";
      public const string CannotClaimAreaCannotAfford = "Claiming this area costs <color=#ffd479>{0}</color> scrap. Add this amount to your inventory and try again.";
      public const string CannotClaimAreaAlreadyOwned = "The area <color=#ffd479>{0}</color> is already owned by your faction, and this cupboard represents the claim.";

      public const string SelectClaimCupboardToAdd = "Use the hammer to select a tool cupboard to represent the claim. Say <color=#ffd479>/cancel</color> to cancel.";
      public const string SelectClaimCupboardToRemove = "Use the hammer to select the tool cupboard representing the claim you want to remove. Say <color=#ffd479>/cancel</color> to cancel.";
      public const string SelectClaimCupboardForHeadquarters = "Use the hammer to select the tool cupboard to represent your faction's headquarters. Say <color=#ffd479>/cancel</color> to cancel.";
      public const string SelectClaimCupboardToAssign = "Use the hammer to select a tool cupboard to represent the claim to assign to <color=#ffd479>[{0}]</color>. Say <color=#ffd479>/cancel</color> to cancel.";
      public const string SelectClaimCupboardToTransfer = "Use the hammer to select the tool cupboard representing the claim to give to <color=#ffd479>[{0}]</color>. Say <color=#ffd479>/cancel</color> to cancel.";

      public const string ClaimCupboardMoved = "You have moved the claim <color=#ffd479>{0}</color> to a new tool cupboard.";
      public const string ClaimCaptured = "You have captured <color=#ffd479>{0}</color> from <color=#ffd479>[{1}]</color>!";
      public const string ClaimAdded = "You have claimed <color=#ffd479>{0}</color> for your faction.";
      public const string ClaimRemoved = "You have removed your faction's claim on <color=#ffd479>{0}</color>.";
      public const string ClaimTransferred = "You have transferred ownership of <color=#ffd479>{0}</color> to <color=#ffd479>[{1}]</color>.";

      public const string InvalidAreaName = "Area names must be between <color=#ffd479>{0}</color> and <color=#ffd479>{1}</color> characters long.";
      public const string UnknownArea = "Unknown area <color=#ffd479>{0}</color>.";
      public const string AreaRenamed = "<color=#ffd479>{0}</color> is now known as <color=#ffd479>{1}</color>.";

      public const string ClaimsList = "<color=#ffd479>[{0}]</color> has claimed: <color=#ffd479>{1}</color>";
      public const string ClaimCost = "<color=#ffd479>{0}</color> can be claimed by <color=#ffd479>[{1}]</color> for <color=#ffd479>{2}</color> scrap.";
      public const string UpkeepCost = "It will cost <color=#ffd479>{0}</color> scrap per day to maintain the <color=#ffd479>{1}</color> areas claimed by <color=#ffd479>[{2}]</color>. Upkeep is due <color=#ffd479>{3}</color> hours from now.";
      public const string UpkeepCostOverdue = "It will cost <color=#ffd479>{0}</color> scrap per day to maintain the <color=#ffd479>{1}</color> areas claimed by <color=#ffd479>[{2}]</color>. Your upkeep is <color=#ffd479>{3}</color> hours overdue! Fill your tax chest with scrap immediately, before your claims begin to fall into ruin.";

      public const string SelectTaxChest = "Use the hammer to select the container to receive your faction's tribute. Say <color=#ffd479>/cancel</color> to cancel.";
      public const string SelectingTaxChestFailedInvalidTarget = "That can't be used as a tax chest.";
      public const string SelectingTaxChestSucceeded = "You have selected a new tax chest that will receive <color=#ffd479>{0}%</color> of the materials harvested within land owned by <color=#ffd479>[{1}]</color>. To change the tax rate, say <color=#ffd479>/tax rate PERCENT</color>.";

      public const string CannotSetTaxRateInvalidValue = "You must specify a valid percentage between <color=#ffd479>0-{0}%</color> as a tax rate.";
      public const string SetTaxRateSuccessful = "You have set the tax rate on the land holdings of <color=#ffd479>[{0}]</color> to <color=#ffd479>{1}%</color>.";

      public const string BadlandsSet = "Badlands areas are now: <color=#ffd479>{0}</color>";
      public const string BadlandsList = "Badlands areas are: <color=#ffd479>{0}</color>. Gather bonus is <color=#ffd479>{1}%</color>.";

      public const string CannotDeclareWarAgainstYourself = "You cannot declare war against yourself!";
      public const string CannotDeclareWarAlreadyAtWar = "You area already at war with <color=#ffd479>[{0}]</color>!";
      public const string CannotDeclareWarInvalidCassusBelli = "You cannot declare war against <color=#ffd479>[{0}]</color>, because your reason doesn't meet the minimum length.";
      public const string CannotOfferPeaceAlreadyOfferedPeace = "You have already offered peace to <color=#ffd479>[{0}]</color>.";
      public const string PeaceOffered = "You have offered peace to <color=#ffd479>[{0}]</color>. They must accept it before the war will end.";

      public const string EnteredBadlands = "<color=#ff0000>BORDER:</color> You have entered the badlands! Player violence is allowed here.";
      public const string EnteredWilderness = "<color=#ffd479>BORDER:</color> You have entered the wilderness.";
      public const string EnteredClaimedArea = "<color=#ffd479>BORDER:</color> You have entered land claimed by <color=#ffd479>[{0}]</color>.";

      public const string EnteredPvpMode = "<color=#ff0000>PVP ENABLED:</color> You are now in PVP mode. You can now hurt, and be hurt by, other players who are also in PVP mode.";
      public const string ExitedPvpMode = "<color=#00ff00>PVP DISABLED:</color> You are no longer in PVP mode. You can't be harmed by other players except inside of normal PVP areas.";
      public const string PvpModeOnCooldown = "You must wait at least {0} seconds to exit or re-enter PVP mode.";

      public const string InvalidPinType = "Unknown map pin type <color=#ffd479>{0}</color>. Say <color=#ffd479>/pin types</color> to see a list of available types.";
      public const string InvalidPinName = "Map pin names must be between <color=#ffd479>{0}</color> and <color=#ffd479>{1}</color> characters long.";
      public const string CannotCreatePinCannotAfford = "Creating a new map pin costs <color=#ffd479>{0}</color> scrap. Add this amount to your inventory and try again.";
      public const string CannotCreatePinAlreadyExists = "Cannot create a new map pin named <color=#ffd479>{0}</color>, since one already exists with the same name in <color=#ffd479>{1}</color>.";
      public const string UnknownPin = "Unknown map pin <color=#ffd479>{0}</color>.";
      public const string CannotRemovePinAreaNotOwnedByYourFaction = "Cannot remove the map pin named <color=#ffd479>{0}</color>, because the area <color=#ffd479>{1} is not owned by your faction.";
      public const string PinRemoved = "Removed map pin <color=#ffd479>{0}</color>.";

      public const string FactionCreatedAnnouncement = "<color=#00ff00>FACTION CREATED:</color> A new faction <color=#ffd479>[{0}]</color> has been created!";
      public const string FactionDisbandedAnnouncement = "<color=#00ff00>FACTION DISBANDED:</color> <color=#ffd479>[{0}]</color> has been disbanded!";
      public const string FactionMemberJoinedAnnouncement = "<color=#00ff00>MEMBER JOINED:</color> <color=#ffd479>{0}</color> has joined <color=#ffd479>[{1}]</color>!";
      public const string FactionMemberLeftAnnouncement = "<color=#00ff00>MEMBER LEFT:</color> <color=#ffd479>{0}</color> has left <color=#ffd479>[{1}]</color>!";

      public const string AreaClaimedAnnouncement = "<color=#00ff00>AREA CLAIMED:</color> <color=#ffd479>[{0}]</color> claims <color=#ffd479>{1}</color>!";
      public const string AreaClaimedAsHeadquartersAnnouncement = "<color=#00ff00>AREA CLAIMED:</color> <color=#ffd479>[{0}]</color> claims <color=#ffd479>{1}</color> as their headquarters!";
      public const string AreaCapturedAnnouncement = "<color=#ff0000>AREA CAPTURED:</color> <color=#ffd479>[{0}]</color> has captured <color=#ffd479>{1}</color> from <color=#ffd479>[{2}]</color>!";
      public const string AreaClaimRemovedAnnouncement = "<color=#ff0000>CLAIM REMOVED:</color> <color=#ffd479>[{0}]</color> has relinquished their claim on <color=#ffd479>{1}</color>!";
      public const string AreaClaimTransferredAnnouncement = "<color=#ff0000>CLAIM TRANSFERRED:</color> <color=#ffd479>[{0}]</color> has transferred their claim on <color=#ffd479>{1}</color> to <color=#ffd479>[{2}]</color>!";
      public const string AreaClaimAssignedAnnouncement = "<color=#ff0000>AREA CLAIM ASSIGNED:</color> <color=#ffd479>{0}</color> has been assigned to <color=#ffd479>[{1}]</color> by an admin.";
      public const string AreaClaimDeletedAnnouncement = "<color=#ff0000>AREA CLAIM REMOVED:</color> <color=#ffd479>[{0}]</color>'s claim on <color=#ffd479>{1}</color> has been removed by an admin.";
      public const string AreaClaimLostCupboardDestroyedAnnouncement = "<color=#ff0000>AREA CLAIM LOST:</color> <color=#ffd479>[{0}]</color> has lost its claim on <color=#ffd479>{1}</color>, because the tool cupboard was destroyed!";
      public const string AreaClaimLostFactionDisbandedAnnouncement = "<color=#ff0000>AREA CLAIM LOST:</color> <color=#ffd479>[{0}]</color> has been disbanded, losing its claim on <color=#ffd479>{1}</color>!";
      public const string AreaClaimLostUpkeepNotPaidAnnouncement = "<color=#ff0000>AREA CLAIM LOST:</color>: <color=#ffd479>[{0}]</color> has lost their claim on <color=#ffd479>{1}</color> after it fell into ruin!";
      public const string HeadquartersChangedAnnouncement = "<color=#00ff00>HQ CHANGED:</color> The headquarters of <color=#ffd479>[{0}]</color> is now <color=#ffd479>{1}</color>.";
      public const string WarDeclaredAnnouncement = "<color=#ff0000>WAR DECLARED:</color> <color=#ffd479>[{0}]</color> has declared war on <color=#ffd479>[{1}]</color>! Their reason: <color=#ffd479>{2}</color>";
      public const string WarEndedTreatyAcceptedAnnouncement = "<color=#00ff00>WAR ENDED:</color> The war between <color=#ffd479>[{0}]</color> and <color=#ffd479>[{1}]</color> has ended after both sides have agreed to a treaty.";
      public const string WarEndedFactionEliminatedAnnouncement = "<color=#00ff00>WAR ENDED:</color> The war between <color=#ffd479>[{0}]</color> and <color=#ffd479>[{1}]</color> has ended, since <color=#ffd479>[{2}]</color> no longer holds any land.";
      public const string PinAddedAnnouncement = "<color=#00ff00>POINT OF INTEREST:</color> <color=#ffd479>[{0}]</color> announces the creation of <color=#ffd479>{1}</color>, a new {2} located in <color=#ffd479>{3}</color>!";
    }

    void InitLang()
    {
      var messages = typeof(Messages).GetFields(BindingFlags.Public)
        .Select(f => (string)f.GetRawConstantValue())
        .ToDictionary(str => str);

      lang.RegisterMessages(messages, this);
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Linq;

  public partial class Imperium
  {
    static class Pvp
    {
      static string[] BlockedPrefabs = new[] {
        "fireball_small",
        "fireball_small_arrow",
        "fireball_small_shotgun",
        "fireexplosion"
      };

      public static object HandleDamageBetweenPlayers(User attacker, User defender, HitInfo hit)
      {
        if (!Instance.Options.Pvp.RestrictPvp)
          return null;

        // Allow players to take the easy way out.
        if (hit.damageTypes.Has(Rust.DamageType.Suicide))
          return null;

        if (attacker.CurrentArea == null || defender.CurrentArea == null)
        {
          Instance.PrintWarning("A player dealt or received damage while in an unknown area. This shouldn't happen.");
          return null;
        }

        // If the players are both in factions who are currently at war, they can damage each other anywhere.
        if (attacker.Faction != null && defender.Faction != null && Instance.Wars.AreFactionsAtWar(attacker.Faction, defender.Faction))
          return null;

        // If both the attacker and the defender are in PVP mode, or in a PVP area/zone, they can damage one another.
        if (IsUserInDanger(attacker) && IsUserInDanger(defender))
          return null;

        // Stop the damage.
        return false;
      }

      public static object HandleIncidentalDamage(User defender, HitInfo hit)
      {
        if (!Instance.Options.Pvp.RestrictPvp)
          return null;

        if (hit.Initiator == null)
          return null;

        // If the damage is coming from something other than a blocked prefab, allow it.
        if (!BlockedPrefabs.Contains(hit.Initiator.ShortPrefabName))
          return null;

        // If the player is in a PVP area or in PVP mode, allow the damage.
        if (IsUserInDanger(defender))
          return null;

        return false;
      }

      public static object HandleTrapTrigger(BaseTrap trap, User defender)
      {
        if (!Instance.Options.Pvp.RestrictPvp)
          return null;

        // A player can always trigger their own traps, to prevent exploiting this mechanic.
        if (defender.Player.userID == trap.OwnerID)
          return null;

        Area trapArea = Instance.Areas.GetByEntityPosition(trap);

        if (trapArea == null || defender.CurrentArea == null)
        {
          Instance.PrintWarning("A trap was triggered in an unknown area. This shouldn't happen.");
          return null;
        }

        // If the defender is in a faction, they can trigger traps placed in areas claimed by factions with which they are at war.
        if (defender.Faction != null && trapArea.FactionId != null && Instance.Wars.AreFactionsAtWar(defender.Faction.Id, trapArea.FactionId))
          return null;

        // If the defender is in a PVP area or zone, the trap can trigger.
        // TODO: Ensure the trap is also in the PVP zone.
        if (IsUserInDanger(defender))
          return null;

        // Stop the trap from triggering.
        return false;
      }

      public static object HandleTurretTarget(BaseCombatEntity turret, User defender)
      {
        if (!Instance.Options.Pvp.RestrictPvp)
          return null;

        // A player can be always be targeted by their own turrets, to prevent exploiting this mechanic.
        if (defender.Player.userID == turret.OwnerID)
          return null;

        Area turretArea = Instance.Areas.GetByEntityPosition(turret);

        if (turretArea == null || defender.CurrentArea == null)
        {
          Instance.PrintWarning("A turret tried to acquire a target in an unknown area. This shouldn't happen.");
          return null;
        }

        // If the defender is in a faction, they can be targeted by turrets in areas claimed by factions with which they are at war.
        if (defender.Faction != null && turretArea.FactionId != null && Instance.Wars.AreFactionsAtWar(defender.Faction.Id, turretArea.FactionId))
          return null;

        // If the defender is in a PVP area or zone, the turret can trigger.
        // TODO: Ensure the turret is also in the PVP zone.
        if (IsUserInDanger(defender))
          return null;

        return false;
      }

      static bool IsUserInDanger(User user)
      {
        return user.IsInPvpMode || IsPvpArea(user.CurrentArea) || user.CurrentZones.Any(IsPvpZone);
      }

      static bool IsPvpZone(Zone zone)
      {
        switch (zone.Type)
        {
          case ZoneType.Debris:
          case ZoneType.SupplyDrop:
            return Instance.Options.Pvp.AllowedInEventZones;
          case ZoneType.Monument:
            return Instance.Options.Pvp.AllowedInMonumentZones;
          case ZoneType.Raid:
            return Instance.Options.Pvp.AllowedInRaidZones;
          default:
            throw new InvalidOperationException($"Unknown zone type {zone.Type}");
        }
      }

      static bool IsPvpArea(Area area)
      {
        switch (area.Type)
        {
          case AreaType.Badlands:
            return Instance.Options.Pvp.AllowedInBadlands;
          case AreaType.Claimed:
          case AreaType.Headquarters:
            return Instance.Options.Pvp.AllowedInClaimedLand;
          case AreaType.Wilderness:
            return Instance.Options.Pvp.AllowedInWilderness;
          default:
            throw new InvalidOperationException($"Unknown area type {area.Type}");
        }
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Linq;

  public partial class Imperium
  {
    static class Raiding
    {
      enum DamageResult
      {
        Prevent,
        NotProtected,
        Friendly,
        BeingAttacked
      }

      static string[] BlockedPrefabs = new[] {
        "fireball_small",
        "fireball_small_arrow",
        "fireball_small_shotgun",
        "fireexplosion"
      };

      static string[] ProtectedPrefabs = new[]
      {
        "barricade.concrete",
        "barricade.metal",
        "barricade.sandbags",
        "barricade.stone",
        "barricade.wood",
        "barricade.woodwire",
        "bbq",
        "bed",
        "box.wooden.large",
        "ceilinglight",
        "chair",
        "cupboard",
        "door.double.hinged",
        "door.hinged",
        "dropbox",
        "fireplace",
        "floor.frame",
        "floor.ladder.hatch",
        "fridge",
        "furnace",
        "gates.external",
        "jackolantern",
        "lantern",
        "locker",
        "mailbox",
        "planter.large",
        "planter.small",
        "reactivetarget",
        "refinery_small",
        "repairbench",
        "researchtable",
        "rug",
        "searchlight",
        "shelves",
        "shutter",
        "sign.hanging",
        "sign.huge.wood",
        "sign.large.wood",
        "sign.medium.wood",
        "sign.pictureframe",
        "sign.pole.banner.large",
        "sign.post",
        "sign.small.wood",
        "small_stash",
        "spikes.floor",
        "spinner.wheel",
        "survivalfishtrap",
        "table",
        "tunalight",
        "vendingmachine",
        "wall.external",
        "wall.frame",
        "wall.window",
        "water_barrel",
        "water_catcher",
        "water_desalinator",
        "waterbarrel",
        "waterpurifier",
        "window.bars",
        "woodbox",
        "workbench"
      };

      static string[] RaidTriggeringPrefabs = new[]
      {
        "cupboard",
        "door.double.hinged",
        "door.hinged",
        "floor.ladder.hatch",
        "floor.frame",
        "gates.external",
        "vendingmachine",
        "wall.frame",
        "wall.external",
        "wall.window",
        "window.bars"
      };

      public static object HandleDamageAgainstStructure(User attacker, BaseEntity entity, HitInfo hit)
      {
        Area area = Instance.Areas.GetByEntityPosition(entity);

        if (area == null)
        {
          Instance.PrintWarning("An entity was damaged in an unknown area. This shouldn't happen.");
          return null;
        }

        DamageResult result = DetermineDamageResult(attacker, area, entity);

        if (result == DamageResult.NotProtected || result == DamageResult.Friendly)
          return null;

        if (result == DamageResult.Prevent)
          return false;

        float reduction = area.GetDefensiveBonus();

        if (reduction >= 1)
          return false;

        if (reduction > 0)
          hit.damageTypes.ScaleAll(reduction);

        if (Instance.Options.Zones.Enabled)
        {
          BuildingPrivlidge cupboard = entity.GetBuildingPrivilege();

          if (cupboard != null && IsRaidTriggeringEntity(entity))
          {
            float remainingHealth = entity.Health() - hit.damageTypes.Total();
            if (remainingHealth < 1)
              Instance.Zones.CreateForRaid(cupboard);
          }
        }

        return null;
      }

      static DamageResult DetermineDamageResult(User attacker, Area area, BaseEntity entity)
      {
        // Players can always damage their own entities.
        if (attacker.Player.userID == entity.OwnerID)
          return DamageResult.Friendly;

        if (!IsProtectedEntity(entity))
          return DamageResult.NotProtected;

        if (attacker.Faction != null)
        {
          // Factions can damage any structure built on their own land.
          if (area.FactionId != null && attacker.Faction.Id == area.FactionId)
            return DamageResult.Friendly;

          // Factions who are at war can damage any structure on enemy land, subject to the defensive bonus.
          if (area.FactionId != null && Instance.Wars.AreFactionsAtWar(attacker.Faction.Id, area.FactionId))
            return DamageResult.BeingAttacked;

          // Factions who are at war can damage any structure built by a member of an enemy faction, subject
          // to the defensive bonus.
          BasePlayer owner = BasePlayer.FindByID(entity.OwnerID);
          if (owner != null)
          {
            Faction owningFaction = Instance.Factions.GetByMember(owner.UserIDString);
            if (owningFaction != null && Instance.Wars.AreFactionsAtWar(attacker.Faction, owningFaction))
              return DamageResult.BeingAttacked;
          }
        }

        // If the structure is in a raidable area, it can be damaged subject to the defensive bonus.
        if (IsRaidableArea(area))
          return DamageResult.BeingAttacked;

        // Prevent the damage.
        return DamageResult.Prevent;
      }

      public static object HandleIncidentalDamage(BaseEntity entity, HitInfo hit)
      {
        if (!Instance.Options.Raiding.RestrictRaiding)
          return null;

        Area area = Instance.Areas.GetByEntityPosition(entity);

        if (area == null)
        {
          Instance.PrintWarning("An entity was damaged in an unknown area. This shouldn't happen.");
          return null;
        }

        if (hit.Initiator == null)
          return null;

        // If the damage is coming from something other than a blocked prefab, allow it.
        if (!BlockedPrefabs.Contains(hit.Initiator.ShortPrefabName))
          return null;

        // If the player is in a PVP area or in PVP mode, allow the damage.
        if (IsRaidableArea(area))
          return null;

        return false;
      }

      static bool IsProtectedEntity(BaseEntity entity)
      {
        var buildingBlock = entity as BuildingBlock;

        // All building blocks except for twig are protected.
        if (buildingBlock != null)
          return buildingBlock.grade != BuildingGrade.Enum.Twigs;

        // Some additional entities (doors, boxes, etc.) are also protected.
        if (ProtectedPrefabs.Any(prefab => entity.ShortPrefabName.Contains(prefab)))
          return true;

        return false;
      }

      static bool IsRaidTriggeringEntity(BaseEntity entity)
      {
        var buildingBlock = entity as BuildingBlock;

        // All building blocks except for twig are protected.
        if (buildingBlock != null)
          return buildingBlock.grade != BuildingGrade.Enum.Twigs;

        // Destriction of some additional entities (doors, etc.) will also trigger raids.
        if (RaidTriggeringPrefabs.Any(prefab => entity.ShortPrefabName.Contains(prefab)))
          return true;

        return false;
      }

      static bool IsRaidableArea(Area area)
      {
        if (!Instance.Options.Raiding.RestrictRaiding)
          return true;

        switch (area.Type)
        {
          case AreaType.Badlands:
            return Instance.Options.Raiding.AllowedInBadlands;
          case AreaType.Claimed:
          case AreaType.Headquarters:
            return Instance.Options.Raiding.AllowedInClaimedLand;
          case AreaType.Wilderness:
            return Instance.Options.Raiding.AllowedInWilderness;
          default:
            throw new InvalidOperationException($"Unknown area type {area.Type}");
        }
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    static class Taxes
    {
      public static void ProcessTaxesIfApplicable(ResourceDispenser dispenser, BaseEntity entity, Item item)
      {
        if (!Instance.Options.Taxes.Enabled)
          return;

        var player = entity as BasePlayer;
        if (player == null)
          return;

        User user = Instance.Users.Get(player);
        if (user == null)
          return;

        Area area = user.CurrentArea;
        if (area == null || !area.IsClaimed)
          return;

        Faction faction = Instance.Factions.Get(area.FactionId);
        if (!faction.CanCollectTaxes || faction.TaxChest.inventory.IsFull())
          return;

        ItemDefinition itemDef = ItemManager.FindItemDefinition(item.info.itemid);
        if (itemDef == null)
          return;

        int bonus = (int)(item.amount * Instance.Options.Taxes.ClaimedLandGatherBonus);
        var tax = (int)(item.amount * faction.TaxRate);

        faction.TaxChest.inventory.AddItem(itemDef, tax + bonus);
        item.amount -= tax;
      }

      public static void AwardBadlandsBonusIfApplicable(ResourceDispenser dispenser, BaseEntity entity, Item item)
      {
        if (!Instance.Options.Badlands.Enabled)
          return;

        var player = entity as BasePlayer;
        if (player == null) return;

        User user = Instance.Users.Get(player);

        if (user.CurrentArea == null)
        {
          Instance.PrintWarning("Player gathered outside of a defined area. This shouldn't happen.");
          return;
        }

        if (user.CurrentArea.Type == AreaType.Badlands)
        {
          var bonus = (int)(item.amount * Instance.Options.Taxes.BadlandsGatherBonus);
          item.amount += bonus;
        }
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  public partial class Imperium
  {
    static class Upkeep
    {
      public static void CollectForAllFactions()
      {
        foreach (Faction faction in Instance.Factions.GetAll())
          Collect(faction);
      }

      public static void Collect(Faction faction)
      {
        DateTime now = DateTime.UtcNow;
        Area[] areas = Instance.Areas.GetAllTaxableClaimsByFaction(faction);

        if (areas.Length == 0)
          return;

        if (now < faction.NextUpkeepPaymentTime)
        {
          Instance.Log($"[UPKEEP] {faction.Id}: Upkeep not due until {faction.NextUpkeepPaymentTime}");
          return;
        }

        int amountOwed = faction.GetUpkeepPerPeriod();
        var hoursSincePaid = (int)now.Subtract(faction.NextUpkeepPaymentTime).TotalHours;

        Instance.Log($"[UPKEEP] {faction.Id}: {hoursSincePaid} hours since upkeep paid, trying to collect {amountOwed} scrap for {areas.Length} area claims");

        var headquarters = areas.Where(a => a.Type == AreaType.Headquarters).FirstOrDefault();
        if (headquarters == null || headquarters.ClaimCupboard == null)
        {
          Instance.Log($"[UPKEEP] {faction.Id}: Couldn't collect upkeep, faction has no headquarters");
        }
        else
        {
          ItemDefinition scrapDef = ItemManager.FindItemDefinition("scrap");
          ItemContainer container = headquarters.ClaimCupboard.inventory;
          List<Item> stacks = container.FindItemsByItemID(scrapDef.itemid);

          if (Instance.TryCollectFromStacks(scrapDef, stacks, amountOwed))
          {
            faction.NextUpkeepPaymentTime = faction.NextUpkeepPaymentTime.AddHours(Instance.Options.Upkeep.CollectionPeriodHours);
            faction.IsUpkeepPastDue = false;
            Instance.Log($"[UPKEEP] {faction.Id}: {amountOwed} scrap upkeep collected, next payment due {faction.NextUpkeepPaymentTime}");
            return;
          }
        }

        faction.IsUpkeepPastDue = true;

        if (hoursSincePaid <= Instance.Options.Upkeep.GracePeriodHours)
        {
          Instance.Log($"[UPKEEP] {faction.Id}: Couldn't collect upkeep, but still within {Instance.Options.Upkeep.GracePeriodHours} hour grace period");
          return;
        }

        Area lostArea = areas.OrderBy(area => Instance.Areas.GetDepthInsideFriendlyTerritory(area)).First();

        Instance.Log($"[UPKEEP] {faction.Id}: Upkeep not paid in {hoursSincePaid} hours, seizing claim on {lostArea.Id}");
        Instance.PrintToChat(Messages.AreaClaimLostUpkeepNotPaidAnnouncement, faction.Id, lostArea.Id);

        Instance.Areas.Unclaim(lostArea);
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using Rust;
  using UnityEngine;

  public partial class Imperium
  {
    class Area : MonoBehaviour
    {
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
        InvokeRepeating("CheckClaimCupboard", 60f, 60f);
      }

      void OnDestroy()
      {
        var collider = GetComponent<BoxCollider>();

        if (collider != null)
          Destroy(collider);

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

      void OnTriggerEnter(Collider collider)
      {
        if (collider.gameObject.layer != (int)Layer.Player_Server)
          return;

        var user = collider.GetComponentInParent<User>();

        if (user != null && user.CurrentArea != this)
          Api.OnUserEnteredArea(user, this);
      }

      void OnTriggerExit(Collider collider)
      {
        if (collider.gameObject.layer != (int)Layer.Player_Server)
          return;

        var user = collider.GetComponentInParent<User>();

        if (user != null)
          Api.OnUserLeftArea(user, this);
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
          CupboardId = ClaimCupboard?.net?.ID
        };
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using Newtonsoft.Json;
  using Newtonsoft.Json.Converters;

  public partial class Imperium : RustPlugin
  {
    public class AreaInfo
    {
      [JsonProperty("id")]
      public string Id;

      [JsonProperty("name")]
      public string Name;

      [JsonProperty("type"), JsonConverter(typeof(StringEnumConverter))]
      public AreaType Type;

      [JsonProperty("factionId")]
      public string FactionId;

      [JsonProperty("claimantId")]
      public string ClaimantId;

      [JsonProperty("cupboardId")]
      public uint? CupboardId;
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;

  public partial class Imperium
  {
    class AreaManager
    {
      Dictionary<string, Area> Areas;
      Area[,] Layout;

      public MapGrid MapGrid { get; }

      public int Count
      {
        get { return Areas.Count; }
      }

      public AreaManager()
      {
        MapGrid = new MapGrid();
        Areas = new Dictionary<string, Area>();
        Layout = new Area[MapGrid.NumberOfCells, MapGrid.NumberOfCells];
      }
      
      public Area Get(string areaId)
      {
        Area area;
        if (Areas.TryGetValue(areaId, out area))
          return area;
        else
          return null;
      }

      public Area Get(int row, int col)
      {
        return Layout[row, col];
      }

      public Area[] GetAll()
      {
        return Areas.Values.ToArray();
      }

      public Area[] GetAllByType(AreaType type)
      {
        return Areas.Values.Where(a => a.Type == type).ToArray();
      }

      public Area[] GetAllClaimedByFaction(Faction faction)
      {
        return GetAllClaimedByFaction(faction.Id);
      }

      public Area[] GetAllClaimedByFaction(string factionId)
      {
        return Areas.Values.Where(a => a.FactionId == factionId).ToArray();
      }

      public Area[] GetAllTaxableClaimsByFaction(Faction faction)
      {
        return GetAllTaxableClaimsByFaction(faction.Id);
      }

      public Area[] GetAllTaxableClaimsByFaction(string factionId)
      {
        return Areas.Values.Where(a => a.FactionId == factionId && a.IsTaxableClaim).ToArray();
      }

      public Area GetByClaimCupboard(BuildingPrivlidge cupboard)
      {
        return GetByClaimCupboard(cupboard.net.ID);
      }

      public Area GetByClaimCupboard(uint cupboardId)
      {
        return Areas.Values.FirstOrDefault(a => a.ClaimCupboard != null && a.ClaimCupboard.net.ID == cupboardId);
      }

      public Area GetByEntityPosition(BaseEntity entity)
      {
        Vector3 position = entity.transform.position;

        int row = Mathf.Clamp((int)(MapGrid.MapSize / 2 - position.z) / MapGrid.CellSize, 0, MapGrid.NumberOfCells);
        int col = Mathf.Clamp((int)(position.x + MapGrid.MapSize / 2) / MapGrid.CellSize, 0, MapGrid.NumberOfCells);

        return Layout[row, col];
      }

      public void Claim(Area area, AreaType type, Faction faction, User claimant, BuildingPrivlidge cupboard)
      {
        area.Type = type;
        area.FactionId = faction.Id;
        area.ClaimantId = claimant.Id;
        area.ClaimCupboard = cupboard;

        Api.OnAreaChanged(area);
      }

      public void SetHeadquarters(Area area, Faction faction)
      {
        // Ensure that no other areas are considered headquarters.
        foreach (Area otherArea in GetAllClaimedByFaction(faction).Where(a => a.Type == AreaType.Headquarters))
        {
          otherArea.Type = AreaType.Claimed;
          Api.OnAreaChanged(otherArea);
        }

        area.Type = AreaType.Headquarters;
        Api.OnAreaChanged(area);
      }

      public void Unclaim(IEnumerable<Area> areas)
      {
        Unclaim(areas.ToArray());
      }

      public void Unclaim(params Area[] areas)
      {
        foreach (Area area in areas)
        {
          area.Type = AreaType.Wilderness;
          area.FactionId = null;
          area.ClaimantId = null;
          area.ClaimCupboard = null;

          Api.OnAreaChanged(area);
        }
      }

      public void AddBadlands(params Area[] areas)
      {
        foreach (Area area in areas)
        {
          area.Type = AreaType.Badlands;
          area.FactionId = null;
          area.ClaimantId = null;
          area.ClaimCupboard = null;

          Api.OnAreaChanged(area);
        }
      }

      public void AddBadlands(IEnumerable<Area> areas)
      {
        AddBadlands(areas.ToArray());
      }

      public int GetNumberOfContiguousClaimedAreas(Area area, Faction owner)
      {
        int count = 0;

        // North
        if (area.Row > 0 && Layout[area.Row - 1, area.Col].FactionId == owner.Id)
          count++;

        // South
        if (area.Row < MapGrid.NumberOfCells - 1 && Layout[area.Row + 1, area.Col].FactionId == owner.Id)
          count++;

        // West
        if (area.Col > 0 && Layout[area.Row, area.Col - 1].FactionId == owner.Id)
          count++;

        // East
        if (area.Col < MapGrid.NumberOfCells - 1 && Layout[area.Row, area.Col + 1].FactionId == owner.Id)
          count++;

        return count;
      }

      public int GetDepthInsideFriendlyTerritory(Area area)
      {
        if (!area.IsClaimed)
          return 0;

        var depth = new int[4];

        // North
        for (var row = area.Row; row >= 0; row--)
        {
          if (Layout[row, area.Col].FactionId != area.FactionId)
            break;

          depth[0]++;
        }

        // South
        for (var row = area.Row; row < MapGrid.NumberOfCells; row++)
        {
          if (Layout[row, area.Col].FactionId != area.FactionId)
            break;

          depth[1]++;
        }

        // West
        for (var col = area.Col; col >= 0; col--)
        {
          if (Layout[area.Row, col].FactionId != area.FactionId)
            break;

          depth[2]++;
        }

        // East
        for (var col = area.Col; col < MapGrid.NumberOfCells; col++)
        {
          if (Layout[area.Row, col].FactionId != area.FactionId)
            break;

          depth[3]++;
        }

        return depth.Min() - 1;
      }

      public void Init(IEnumerable<AreaInfo> areaInfos)
      {
        Instance.Puts("Creating area objects...");

        Dictionary<string, AreaInfo> lookup;
        if (areaInfos != null)
          lookup = areaInfos.ToDictionary(a => a.Id);
        else
          lookup = new Dictionary<string, AreaInfo>();

        for (var row = 0; row < MapGrid.NumberOfCells; row++)
        {
          for (var col = 0; col < MapGrid.NumberOfCells; col++)
          {
            string areaId = MapGrid.GetAreaId(row, col);
            Vector3 position = MapGrid.GetPosition(row, col);
            Vector3 size = new Vector3(MapGrid.CellSize, 500, MapGrid.CellSize);

            AreaInfo info = null;
            lookup.TryGetValue(areaId, out info);

            var area = new GameObject().AddComponent<Area>();
            area.Init(areaId, row, col, position, size, info);

            Areas[areaId] = area;
            Layout[row, col] = area;
          }
        }

        Instance.Puts($"Created {Areas.Values.Count} area objects.");
      }

      public void Destroy()
      {
        Area[] areas = UnityEngine.Object.FindObjectsOfType<Area>();

        if (areas != null)
        {
          Instance.Puts($"Destroying {areas.Length} area objects...");
          foreach (Area area in areas)
            UnityEngine.Object.Destroy(area);
        }

        Areas.Clear();
        Array.Clear(Layout, 0, Layout.Length);

        Instance.Puts("Area objects destroyed.");
      }

      public AreaInfo[] Serialize()
      {
        return Areas.Values.Select(area => area.Serialize()).ToArray();
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium : RustPlugin
  {
    public enum AreaType
    {
      Wilderness,
      Claimed,
      Headquarters,
      Badlands
    }
  }
}
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
      public bool IsUpkeepPastDue { get; set; }

      public bool CanCollectTaxes
      {
        get { return TaxChest != null; }
      }

      public int MemberCount
      {
        get { return MemberIds.Count; }
      }

      public Faction(string id, User owner)
      {
        Id = id;

        OwnerId = owner.Id;
        MemberIds = new HashSet<string> { owner.Id };
        ManagerIds = new HashSet<string>();
        InviteIds = new HashSet<string>();

        TaxRate = Instance.Options.Taxes.DefaultTaxRate;
        NextUpkeepPaymentTime = DateTime.UtcNow.AddHours(Instance.Options.Upkeep.CollectionPeriodHours);
      }

      public Faction(FactionInfo info)
      {
        Id = info.Id;

        OwnerId = info.OwnerId;
        MemberIds = new HashSet<string>(info.MemberIds);
        ManagerIds = new HashSet<string>(info.ManagerIds);
        InviteIds = new HashSet<string>(info.InviteIds);

        if (info.TaxChestId != null)
        {
          var taxChest = BaseNetworkable.serverEntities.Find((uint)info.TaxChestId) as StorageContainer;

          if (taxChest == null || taxChest.IsDestroyed)
            Instance.Log($"[LOAD] Faction {Id}: Tax chest entity {info.TaxChestId} was not found");
          else
            TaxChest = taxChest;
        }

        TaxRate = info.TaxRate;
        NextUpkeepPaymentTime = info.NextUpkeepPaymentTime;
        IsUpkeepPastDue = info.IsUpkeepPastDue;

        Instance.Log($"[LOAD] Faction {Id}: {MemberIds.Count} members, tax chest = {Util.Format(TaxChest)}");
      }

      public bool AddMember(User user)
      {
        if (!MemberIds.Add(user.Id))
          return false;

        InviteIds.Remove(user.Id);

        Api.OnPlayerJoinedFaction(this, user);
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

        Api.OnPlayerLeftFaction(this, user);
        return true;
      }

      public bool AddInvite(User user)
      {
        if (!InviteIds.Add(user.Id))
          return false;

        Api.OnPlayerInvitedToFaction(this, user);
        return true;
      }

      public bool RemoveInvite(User user)
      {
        if (!InviteIds.Remove(user.Id))
          return false;

        Api.OnPlayerUninvitedFromFaction(this, user);
        return true;
      }

      public bool Promote(User user)
      {
        if (!MemberIds.Contains(user.Id))
          throw new InvalidOperationException($"Cannot promote player {user.Id} in faction {Id}, since they are not a member");

        if (!ManagerIds.Add(user.Id))
          return false;

        Api.OnPlayerPromoted(this, user);
        return true;
      }

      public bool Demote(User user)
      {
        if (!MemberIds.Contains(user.Id))
          throw new InvalidOperationException($"Cannot demote player {user.Id} in faction {Id}, since they are not a member");

        if (!ManagerIds.Remove(user.Id))
          return false;

        Api.OnPlayerDemoted(this, user);
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

      public User[] GetAllActiveMembers()
      {
        return MemberIds.Select(id => Instance.Users.Get(id)).Where(user => user != null).ToArray();
      }

      public User[] GetAllActiveInvitedUsers()
      {
        return InviteIds.Select(id => Instance.Users.Get(id)).Where(user => user != null).ToArray();
      }

      public int GetUpkeepPerPeriod()
      {
        var costs = Instance.Options.Upkeep.Costs;

        int totalCost = 0;
        for (var num = 0; num < Instance.Areas.GetAllTaxableClaimsByFaction(this).Length; num++)
        {
          var index = Mathf.Clamp(num, 0, costs.Count - 1);
          totalCost += costs[index];
        }

        return totalCost;
      }

      public void SendChatMessage(string message, params object[] args)
      {
        foreach (User user in GetAllActiveMembers())
          user.SendChatMessage(message, args);
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
﻿namespace Oxide.Plugins
{
  using UnityEngine;

  public partial class Imperium
  {
    class FactionEntityMonitor : MonoBehaviour
    {
      void Awake()
      {
        InvokeRepeating("CheckTaxChests", 60f, 60f);
      }

      void OnDestroy()
      {
        if (IsInvoking("CheckTaxChests")) CancelInvoke("CheckTaxChests");
      }

      void EnsureAllTaxChestsStillExist()
      {
        foreach (Faction faction in Instance.Factions.GetAll())
          EnsureTaxChestExists(faction);
      }

      void EnsureTaxChestExists(Faction faction)
      {
        if (faction.TaxChest == null || !faction.TaxChest.IsDestroyed)
          return;

        Instance.Log($"{faction.Id}'s tax chest was destroyed (periodic check)");
        faction.TaxChest = null;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class FactionInfo
    {
      [JsonProperty("id")]
      public string Id;

      [JsonProperty("ownerId")]
      public string OwnerId;

      [JsonProperty("memberIds")]
      public string[] MemberIds;

      [JsonProperty("managerIds")]
      public string[] ManagerIds;

      [JsonProperty("inviteIds")]
      public string[] InviteIds;

      [JsonProperty("taxRate")]
      public float TaxRate;

      [JsonProperty("taxChestId")]
      public uint? TaxChestId;

      [JsonProperty("nextUpkeepPaymentTime")]
      public DateTime NextUpkeepPaymentTime;

      [JsonProperty("isUpkeepPastDue")]
      public bool IsUpkeepPastDue;
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;

  public partial class Imperium
  {
    class FactionManager
    {
      Dictionary<string, Faction> Factions = new Dictionary<string, Faction>();
      FactionEntityMonitor EntityMonitor;

      public FactionManager()
      {
        Factions = new Dictionary<string, Faction>();
        EntityMonitor = Instance.GameObject.AddComponent<FactionEntityMonitor>();
      }

      public Faction Create(string id, User owner)
      {
        Faction faction;

        if (Factions.TryGetValue(id, out faction))
          throw new InvalidOperationException($"Cannot create a new faction named ${id}, since one already exists");

        faction = new Faction(id, owner);
        Factions.Add(id, faction);

        Api.OnFactionCreated(faction);

        return faction;
      }

      public void Disband(Faction faction)
      {
        foreach (User user in faction.GetAllActiveMembers())
          user.SetFaction(null);

        Factions.Remove(faction.Id);
        Api.OnFactionDisbanded(faction);
      }

      public Faction[] GetAll()
      {
        return Factions.Values.ToArray();
      }

      public Faction Get(string id)
      {
        Faction faction;
        if (Factions.TryGetValue(id, out faction))
          return faction;
        else
          return null;
      }

      public bool Exists(string id)
      {
        return Factions.ContainsKey(id);
      }

      public Faction GetByMember(User user)
      {
        return GetByMember(user.Id);
      }

      public Faction GetByMember(string userId)
      {
        return Factions.Values.Where(f => f.HasMember(userId)).FirstOrDefault();
      }

      public Faction GetByTaxChest(StorageContainer container)
      {
        return GetByTaxChest(container.net.ID);
      }

      public Faction GetByTaxChest(uint containerId)
      {
        return Factions.Values.SingleOrDefault(f => f.TaxChest != null && f.TaxChest.net.ID == containerId);
      }

      public void SetTaxRate(Faction faction, float taxRate)
      {
        faction.TaxRate = taxRate;
        Api.OnFactionTaxesChanged(faction);
      }

      public void SetTaxChest(Faction faction, StorageContainer taxChest)
      {
        faction.TaxChest = taxChest;
        Api.OnFactionTaxesChanged(faction);
      }

      public void Init(IEnumerable<FactionInfo> factionInfos)
      {
        Instance.Puts($"Creating factions for {factionInfos.Count()} factions...");

        foreach (FactionInfo info in factionInfos)
        {
          Faction faction = new Faction(info);
          Factions.Add(faction.Id, faction);
        }

        Instance.Puts("Factions created.");
      }

      public void Destroy()
      {
        UnityEngine.Object.Destroy(EntityMonitor);
        Factions.Clear();
      }

      public FactionInfo[] Serialize()
      {
        return Factions.Values.Select(faction => faction.Serialize()).ToArray();
      }
    }
  }
}﻿namespace Oxide.Plugins
{
  using UnityEngine;

  public partial class Imperium
  {
    class Pin
    {
      public Vector3 Position { get; }
      public string AreaId { get; }
      public string CreatorId { get; set; }
      public PinType Type { get; set; }
      public string Name { get; set; }

      public Pin(Vector3 position, Area area, User creator, PinType type, string name)
      {
        Position = position;
        AreaId = area.Id;
        CreatorId = creator.Id;
        Type = type;
        Name = name;
      }

      public Pin(PinInfo info)
      {
        Position = info.Position;
        AreaId = info.AreaId;
        CreatorId = info.CreatorId;
        Type = info.Type;
        Name = info.Name;
      }

      public float GetDistanceFrom(BaseEntity entity)
      {
        return GetDistanceFrom(entity.transform.position);
      }

      public float GetDistanceFrom(Vector3 position)
      {
        return Vector3.Distance(position, Position);
      }

      public PinInfo Serialize()
      {
        return new PinInfo {
          Position = Position,
          AreaId = AreaId,
          CreatorId = CreatorId,
          Type = Type,
          Name = Name
        };
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using Newtonsoft.Json;
  using Newtonsoft.Json.Converters;
  using UnityEngine;

  public partial class Imperium : RustPlugin
  {
    class PinInfo
    {
      [JsonProperty("name")]
      public string Name;

      [JsonProperty("type"), JsonConverter(typeof(StringEnumConverter))]
      public PinType Type;

      [JsonProperty("position"), JsonConverter(typeof(UnityVector3Converter))]
      public Vector3 Position;

      [JsonProperty("areaId")]
      public string AreaId;

      [JsonProperty("creatorId")]
      public string CreatorId;
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  public partial class Imperium
  {
    class PinManager
    {
      Dictionary<string, Pin> Pins;

      public PinManager()
      {
        Pins = new Dictionary<string, Pin>(StringComparer.OrdinalIgnoreCase);
      }

      public Pin Get(string name)
      {
        Pin pin;

        if (!Pins.TryGetValue(name, out pin))
          return null;

        return pin;
      }

      public Pin[] GetAll()
      {
        return Pins.Values.ToArray();
      }

      public void Add(Pin pin)
      {
        Pins.Add(pin.Name, pin);
        Api.OnPinCreated(pin);
      }

      public void Remove(Pin pin)
      {
        Pins.Remove(pin.Name);
        Api.OnPinRemoved(pin);
      }

      public void RemoveAllPinsInUnclaimedAreas()
      {
        foreach (Pin pin in GetAll())
        {
          Area area = Instance.Areas.Get(pin.AreaId);
          if (!area.IsClaimed) Remove(pin);
        }
      }

      public void Init(IEnumerable<PinInfo> pinInfos)
      {
        Instance.Puts($"Creating pins for {pinInfos.Count()} pins...");

        foreach (PinInfo info in pinInfos)
        {
          var pin = new Pin(info);
          Pins.Add(pin.Name, pin);
        }

        Instance.Puts("Pins created.");
      }

      public void Destroy()
      {
        Pins.Clear();
      }

      public PinInfo[] Serialize()
      {
        return Pins.Values.Select(pin => pin.Serialize()).ToArray();
      }
    }
  }
}﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    enum PinType
    {
      Arena,
      Hotel,
      Marina,
      Shop,
      Town
    }
  }
}
﻿namespace Oxide.Plugins
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
﻿namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;

  public partial class Imperium
  {
    class UserManager
    {
      Dictionary<string, User> Users = new Dictionary<string, User>();
      Dictionary<string, string> OriginalNames = new Dictionary<string, string>();

      public User[] GetAll()
      {
        return Users.Values.ToArray();
      }

      public User Get(BasePlayer player)
      {
        if (player == null) return null;
        return Get(player.UserIDString);
      }

      public User Get(string userId)
      {
        User user;
        if (Users.TryGetValue(userId, out user))
          return user;
        else
          return null;
      }

      public User Find(string searchString)
      {
        User user = Get(searchString);

        if (user != null)
          return user;

        return Users.Values
          .Where(u => u.UserName.ToLowerInvariant().Contains(searchString.ToLowerInvariant()))
          .OrderBy(u => Util.GetLevenshteinDistance(searchString.ToLowerInvariant(), u.UserName.ToLowerInvariant()))
          .FirstOrDefault();
      }

      public User Add(BasePlayer player)
      {
        Remove(player);

        string originalName;
        if (OriginalNames.TryGetValue(player.UserIDString, out originalName))
          player.displayName = originalName;
        else
          OriginalNames[player.UserIDString] = player.displayName;

        User user = player.gameObject.AddComponent<User>();
        user.Init(player);

        Faction faction = Instance.Factions.GetByMember(user);
        if (faction != null)
          user.SetFaction(faction);

        Users[user.Player.UserIDString] = user;

        return user;
      }

      public bool Remove(BasePlayer player)
      {
        User user = Get(player);
        if (user == null) return false;

        UnityEngine.Object.DestroyImmediate(user);
        Users.Remove(player.UserIDString);

        return true;
      }

      public void SetOriginalName(string userId, string name)
      {
        OriginalNames[userId] = name;
      }

      public void Init()
      {
        List<BasePlayer> players = BasePlayer.activePlayerList;

        Instance.Puts($"Creating user objects for {players.Count} players...");

        foreach (BasePlayer player in players)
          Add(player);

        Instance.Puts($"Created {Users.Count} user objects.");
      }

      public void Destroy()
      {
        User[] users = UnityEngine.Object.FindObjectsOfType<User>();

        if (users == null)
          return;

        Instance.Puts($"Destroying {users.Length} user objects.");

        foreach (var user in users)
          UnityEngine.Object.DestroyImmediate(user);

        Users.Clear();

        Instance.Puts("User objects destroyed.");
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    class UserPreferences
    {
      public UserMapLayer VisibleMapLayers { get; private set; }

      public void ShowMapLayer(UserMapLayer layer)
      {
        VisibleMapLayers |= layer;
      }

      public void HideMapLayer(UserMapLayer layer)
      {
        VisibleMapLayers &= ~layer;
      }

      public void ToggleMapLayer(UserMapLayer layer)
      {
        if (IsMapLayerVisible(layer))
          HideMapLayer(layer);
        else
          ShowMapLayer(layer);
      }

      public bool IsMapLayerVisible(UserMapLayer layer)
      {
        return (VisibleMapLayers & layer) == layer;
      }

      public static UserPreferences Default
      {
        get
        {
          return new UserPreferences {
            VisibleMapLayers = UserMapLayer.Claims | UserMapLayer.Headquarters | UserMapLayer.Monuments | UserMapLayer.Pins
          };
        }
      }
    }

    [Flags]
    enum UserMapLayer
    {
      Claims = 1,
      Headquarters = 2,
      Monuments = 4,
      Pins = 8
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    class War
    {
      public string AttackerId { get; set; }
      public string DefenderId { get; set; }
      public string DeclarerId { get; set; }
      public string CassusBelli { get; set; }

      public DateTime? AttackerPeaceOfferingTime { get; set; }
      public DateTime? DefenderPeaceOfferingTime { get; set; }

      public DateTime StartTime { get; private set; }
      public DateTime? EndTime { get; set; }
      public WarEndReason? EndReason { get; set; }

      public bool IsActive
      {
        get { return EndTime == null; }
      }

      public bool IsAttackerOfferingPeace
      {
        get { return AttackerPeaceOfferingTime != null; }
      }

      public bool IsDefenderOfferingPeace
      {
        get { return DefenderPeaceOfferingTime != null; }
      }

      public War(Faction attacker, Faction defender, User declarer, string cassusBelli)
      {
        AttackerId = attacker.Id;
        DefenderId = defender.Id;
        DeclarerId = declarer.Id;
        CassusBelli = cassusBelli;
        StartTime = DateTime.Now;
      }

      public War(WarInfo info)
      {
        AttackerId = info.AttackerId;
        DefenderId = info.DefenderId;
        DeclarerId = info.DeclarerId;
        CassusBelli = info.CassusBelli;
        StartTime = info.StartTime;
        EndTime = info.EndTime;
      }

      public void OfferPeace(Faction faction)
      {
        if (AttackerId == faction.Id)
          AttackerPeaceOfferingTime = DateTime.Now;
        else if (DefenderId == faction.Id)
          DefenderPeaceOfferingTime = DateTime.Now;
        else
          throw new InvalidOperationException(String.Format("{0} tried to offer peace but the faction wasn't involved in the war!", faction.Id));
      }

      public bool IsOfferingPeace(Faction faction)
      {
        return IsOfferingPeace(faction.Id);
      }

      public bool IsOfferingPeace(string factionId)
      {
        return (factionId == AttackerId && IsAttackerOfferingPeace) || (factionId == DefenderId && IsDefenderOfferingPeace);
      }

      public WarInfo Serialize()
      {
        return new WarInfo {
          AttackerId = AttackerId,
          DefenderId = DefenderId,
          DeclarerId = DeclarerId,
          CassusBelli = CassusBelli,
          AttackerPeaceOfferingTime = AttackerPeaceOfferingTime,
          DefenderPeaceOfferingTime = DefenderPeaceOfferingTime,
          StartTime = StartTime,
          EndTime = EndTime,
          EndReason = EndReason
        };
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium : RustPlugin
  {
    enum WarEndReason
    {
      Treaty,
      AttackerEliminatedDefender,
      DefenderEliminatedAttacker
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Converters;

  public partial class Imperium : RustPlugin
  {
    class WarInfo
    {
      [JsonProperty("attackerId")]
      public string AttackerId;

      [JsonProperty("defenderId")]
      public string DefenderId;

      [JsonProperty("declarerId")]
      public string DeclarerId;

      [JsonProperty("cassusBelli")]
      public string CassusBelli;

      [JsonProperty("attackerPeaceOfferingTime"), JsonConverter(typeof(IsoDateTimeConverter))]
      public DateTime? AttackerPeaceOfferingTime;

      [JsonProperty("defenderPeaceOfferingTime"), JsonConverter(typeof(IsoDateTimeConverter))]
      public DateTime? DefenderPeaceOfferingTime;

      [JsonProperty("startTime"), JsonConverter(typeof(IsoDateTimeConverter))]
      public DateTime StartTime;

      [JsonProperty("endTime"), JsonConverter(typeof(IsoDateTimeConverter))]
      public DateTime? EndTime;

      [JsonProperty("endReason"), JsonConverter(typeof(StringEnumConverter))]
      public WarEndReason? EndReason;
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  public partial class Imperium
  {
    class WarManager
    {
      List<War> Wars = new List<War>();

      public War[] GetAllActiveWars()
      {
        return Wars.Where(war => war.IsActive).OrderBy(war => war.StartTime).ToArray();
      }

      public War[] GetAllActiveWarsByFaction(Faction faction)
      {
        return GetAllActiveWarsByFaction(faction.Id);
      }

      public War[] GetAllActiveWarsByFaction(string factionId)
      {
        return GetAllActiveWars().Where(war => war.AttackerId == factionId || war.DefenderId == factionId).ToArray();
      }

      public War GetActiveWarBetween(Faction firstFaction, Faction secondFaction)
      {
        return GetActiveWarBetween(firstFaction.Id, secondFaction.Id);
      }

      public War GetActiveWarBetween(string firstFactionId, string secondFactionId)
      {
        return GetAllActiveWars().SingleOrDefault(war =>
          (war.AttackerId == firstFactionId && war.DefenderId == secondFactionId) ||
          (war.DefenderId == firstFactionId && war.AttackerId == secondFactionId)
        );
      }

      public bool AreFactionsAtWar(Faction firstFaction, Faction secondFaction)
      {
        return AreFactionsAtWar(firstFaction.Id, secondFaction.Id);
      }

      public bool AreFactionsAtWar(string firstFactionId, string secondFactionId)
      {
        return GetActiveWarBetween(firstFactionId, secondFactionId) != null;
      }

      public War DeclareWar(Faction attacker, Faction defender, User user, string cassusBelli)
      {
        var war = new War(attacker, defender, user, cassusBelli);
        Wars.Add(war);
        Instance.OnDiplomacyChanged();
        return war;
      }

      public void EndWar(War war, WarEndReason reason)
      {
        war.EndTime = DateTime.UtcNow;
        war.EndReason = reason;
        Instance.OnDiplomacyChanged();
      }

      public void EndAllWarsForEliminatedFactions()
      {
        bool dirty = false;

        foreach (War war in Wars)
        {
          if (Instance.Areas.GetAllClaimedByFaction(war.AttackerId).Length == 0)
          {
            war.EndTime = DateTime.UtcNow;
            war.EndReason = WarEndReason.DefenderEliminatedAttacker;
            dirty = true;
          }
          if (Instance.Areas.GetAllClaimedByFaction(war.DefenderId).Length == 0)
          {
            war.EndTime = DateTime.UtcNow;
            war.EndReason = WarEndReason.AttackerEliminatedDefender;
            dirty = true;
          }
        }

        if (dirty)
          Instance.OnDiplomacyChanged();
      }

      public void Init(IEnumerable<WarInfo> warInfos)
      {
        Instance.Puts($"Loading {warInfos.Count()} wars...");

        foreach (WarInfo info in warInfos)
        {
          var war = new War(info);
          Wars.Add(war);
          Instance.Log($"[LOAD] War {war.AttackerId} vs {war.DefenderId}, isActive = {war.IsActive}");
        }

        Instance.Puts("Wars loaded.");
      }

      public void Destroy()
      {
        Wars.Clear();
      }

      public WarInfo[] Serialize()
      {
        return Wars.Select(war => war.Serialize()).ToArray();
      }
    }
  }
}﻿namespace Oxide.Plugins
{
  using Rust;
  using System;
  using System.Collections.Generic;
  using UnityEngine;

  public partial class Imperium
  {
    class Zone : MonoBehaviour
    {
      const string SpherePrefab = "assets/prefabs/visualization/sphere.prefab";

      List<BaseEntity> Spheres = new List<BaseEntity>();

      public ZoneType Type { get; private set; }
      public string Name { get; private set; }
      public MonoBehaviour Owner { get; private set; }
      public DateTime? EndTime { get; set; }

      public void Init(ZoneType type, string name, MonoBehaviour owner, float radius, int darkness, DateTime? endTime)
      {
        Type = type;
        Name = name;
        Owner = owner;
        EndTime = endTime;

        Vector3 position = GetGroundPosition(owner.transform.position);

        gameObject.layer = (int)Layer.Reserved1;
        gameObject.name = $"imperium_zone_{name.ToLowerInvariant()}";
        transform.position = position;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));

        for (var idx = 0; idx < darkness; idx++)
        {
          var sphere = GameManager.server.CreateEntity(SpherePrefab, position);

          SphereEntity entity = sphere.GetComponent<SphereEntity>();
          entity.lerpRadius = radius * 2;
          entity.currentRadius = radius * 2;
          entity.lerpSpeed = 0f;

          sphere.Spawn();
          Spheres.Add(sphere);
        }

        var collider = gameObject.AddComponent<SphereCollider>();
        collider.radius = radius;
        collider.isTrigger = true;
        collider.enabled = true;

        if (endTime != null)
          InvokeRepeating("CheckIfShouldDestroy", 10f, 5f);
      }

      void OnDestroy()
      {
        var collider = GetComponent<SphereCollider>();

        if (collider != null)
          Destroy(collider);

        foreach (BaseEntity sphere in Spheres)
          sphere.KillMessage();

        if (IsInvoking("CheckIfShouldDestroy"))
          CancelInvoke("CheckIfShouldDestroy");
      }

      void OnTriggerEnter(Collider collider)
      {
        if (collider.gameObject.layer != (int)Layer.Player_Server)
          return;

        var user = collider.GetComponentInParent<User>();

        if (user != null && !user.CurrentZones.Contains(this))
          Api.OnUserEnteredZone(user, this);
      }

      void OnTriggerExit(Collider collider)
      {
        if (collider.gameObject.layer != (int)Layer.Player_Server)
          return;

        var user = collider.GetComponentInParent<User>();

        if (user != null && user.CurrentZones.Contains(this))
          Api.OnUserLeftZone(user, this);
      }

      void CheckIfShouldDestroy()
      {
        if (DateTime.UtcNow >= EndTime)
          Instance.Zones.Remove(this);
      }

      Vector3 GetGroundPosition(Vector3 pos)
      {
        return new Vector3(pos.x, TerrainMeta.HeightMap.GetHeight(pos), pos.z);
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;

  public partial class Imperium
  {
    class ZoneManager
    {
      Dictionary<MonoBehaviour, Zone> Zones = new Dictionary<MonoBehaviour, Zone>();

      public void Init()
      {
        if (!Instance.Options.Zones.Enabled || Instance.Options.Zones.MonumentZones == null)
          return;

        MonumentInfo[] monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>();
        foreach (MonumentInfo monument in monuments)
        {
          float? radius = GetMonumentZoneRadius(monument);
          if (radius != null)
          {
            Vector3 position = monument.transform.position;
            Vector3 size = monument.Bounds.size;
            Create(ZoneType.Monument, monument.displayPhrase.english, monument, (float) radius);
          }
        }
      }

      public Zone GetByOwner(MonoBehaviour owner)
      {
        Zone zone;

        if (Zones.TryGetValue(owner, out zone))
          return zone;

        return null;
      }

      public void CreateForDebrisField(BaseHelicopter helicopter)
      {
        Vector3 position = helicopter.transform.position;
        float radius = Instance.Options.Zones.EventZoneRadius;
        Create(ZoneType.Debris, "Debris Field", helicopter, radius, GetEventEndTime());
      }

      public void CreateForSupplyDrop(SupplyDrop drop)
      {
        Vector3 position = drop.transform.position;
        float radius = Instance.Options.Zones.EventZoneRadius;
        float lifespan = Instance.Options.Zones.EventZoneLifespanSeconds;
        Create(ZoneType.SupplyDrop, "Supply Drop", drop, radius, GetEventEndTime());
      }

      public void CreateForRaid(BuildingPrivlidge cupboard)
      {
        // If the building was already being raided, just extend the lifespan of the existing zone.
        Zone existingZone = GetByOwner(cupboard);
        if (existingZone)
        {
          existingZone.EndTime = GetEventEndTime();
          Instance.Puts($"Extending raid zone end time to {existingZone.EndTime} ({existingZone.EndTime.Value.Subtract(DateTime.UtcNow).ToShortString()} from now)");
          return;
        }

        Vector3 position = cupboard.transform.position;
        float radius = Instance.Options.Zones.EventZoneRadius;

        Create(ZoneType.Raid, "Raid", cupboard, radius, GetEventEndTime());
      }

      public void Remove(Zone zone)
      {
        Instance.Puts($"Destroying zone {zone.name}");

        foreach (User user in Instance.Users.GetAll())
          user.CurrentZones.Remove(zone);

        Zones.Remove(zone.Owner);

        UnityEngine.Object.Destroy(zone);
      }

      public void Destroy()
      {
        Zone[] zones = UnityEngine.Object.FindObjectsOfType<Zone>();

        if (zones != null)
        {
          Instance.Puts($"Destroying {zones.Length} zone objects...");
          foreach (Zone zone in zones)
            UnityEngine.Object.DestroyImmediate(zone);
        }

        Zones.Clear();

        Instance.Puts("Zone objects destroyed.");
      }

      void Create(ZoneType type, string name, MonoBehaviour owner, float radius, DateTime? endTime = null)
      {
        var zone = new GameObject().AddComponent<Zone>();
        zone.Init(type, name, owner, radius, Instance.Options.Zones.DomeDarkness, endTime);

        Instance.Puts($"Created zone {zone.Name} at {zone.transform.position} with radius {radius}");

        if (endTime != null)
          Instance.Puts($"Zone {zone.Name} will be destroyed at {endTime} ({endTime.Value.Subtract(DateTime.UtcNow).ToShortString()} from now)");

        Zones.Add(owner, zone);
      }

      float? GetMonumentZoneRadius(MonumentInfo monument)
      {
        if (monument.Type == MonumentType.Cave)
          return null;

        foreach (var entry in Instance.Options.Zones.MonumentZones)
        {
          if (monument.name.Contains(entry.Key))
            return entry.Value;
        }

        return null;
      }

      DateTime GetEventEndTime()
      {
        return DateTime.UtcNow.AddSeconds(Instance.Options.Zones.EventZoneLifespanSeconds);
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    public enum ZoneType
    {
      Monument,
      Debris,
      SupplyDrop,
      Raid
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Drawing;

  public partial class Imperium
  {
    class FactionColorPicker
    {
      static string[] Colors = new[]
      {
        "#00FF00", "#0000FF", "#FF0000", "#01FFFE", "#FFA6FE",
        "#FFDB66", "#006401", "#010067", "#95003A", "#007DB5",
        "#FF00F6", "#FFEEE8", "#774D00", "#90FB92", "#0076FF",
        "#D5FF00", "#FF937E", "#6A826C", "#FF029D", "#FE8900",
        "#7A4782", "#7E2DD2", "#85A900", "#FF0056", "#A42400",
        "#00AE7E", "#683D3B", "#BDC6FF", "#263400", "#BDD393",
        "#00B917", "#9E008E", "#001544", "#C28C9F", "#FF74A3",
        "#01D0FF", "#004754", "#E56FFE", "#788231", "#0E4CA1",
        "#91D0CB", "#BE9970", "#968AE8", "#BB8800", "#43002C",
        "#DEFF74", "#00FFC6", "#FFE502", "#620E00", "#008F9C",
        "#98FF52", "#7544B1", "#B500FF", "#00FF78", "#FF6E41",
        "#005F39", "#6B6882", "#5FAD4E", "#A75740", "#A5FFD2",
        "#FFB167", "#009BFF", "#E85EBE"
      };

      Dictionary<string, Color> AssignedColors;
      int NextColor = 0;

      public FactionColorPicker()
      {
        AssignedColors = new Dictionary<string, Color>();
      }

      public Color GetColorForFaction(string factionId)
      {
        Color color;

        if (!AssignedColors.TryGetValue(factionId, out color))
        {
          color = Color.FromArgb(128, ColorTranslator.FromHtml(Colors[NextColor]));
          AssignedColors.Add(factionId, color);
          NextColor = (NextColor + 1) % Colors.Length;
        }

        return color;
      }
    }

  }

}
﻿namespace Oxide.Plugins
{
  using System.Collections.Generic;

  public partial class Imperium
  {
    class LruCache<K, V>
    {
      Dictionary<K, LinkedListNode<LruCacheItem>> Nodes;
      LinkedList<LruCacheItem> RecencyList;

      public int Capacity { get; private set; }

      public LruCache(int capacity)
      {
        Capacity = capacity;
        Nodes = new Dictionary<K, LinkedListNode<LruCacheItem>>();
        RecencyList = new LinkedList<LruCacheItem>();
      }

      public bool TryGetValue(K key, out V value)
      {
        LinkedListNode<LruCacheItem> node;

        if (!Nodes.TryGetValue(key, out node))
        {
          value = default(V);
          return false;
        }

        LruCacheItem item = node.Value;
        RecencyList.Remove(node);
        RecencyList.AddLast(node);

        value = item.Value;
        return true;
      }

      public void Set(K key, V value)
      {
        LinkedListNode<LruCacheItem> node;

        if (Nodes.TryGetValue(key, out node))
        {
          RecencyList.Remove(node);
          node.Value.Value = value;
        }
        else
        {
          if (Nodes.Count >= Capacity)
            Evict();

          var item = new LruCacheItem(key, value);
          node = new LinkedListNode<LruCacheItem>(item);
        }

        RecencyList.AddLast(node);
        Nodes[key] = node;
      }

      public bool Remove(K key)
      {
        LinkedListNode<LruCacheItem> node;

        if (!Nodes.TryGetValue(key, out node))
          return false;

        Nodes.Remove(key);
        RecencyList.Remove(node);

        return true;
      }

      public void Clear()
      {
        Nodes.Clear();
        RecencyList.Clear();
      }

      void Evict()
      {
        LruCacheItem item = RecencyList.First.Value;
        RecencyList.RemoveFirst();
        Nodes.Remove(item.Key);
      }

      class LruCacheItem
      {
        public K Key;
        public V Value;

        public LruCacheItem(K key, V value)
        {
          Key = key;
          Value = value;
        }
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using UnityEngine;

  public partial class Imperium
  {
    public class MapGrid
    {
      const int GRID_CELL_SIZE = 150;

      public int MapSize
      {
        get { return (int)TerrainMeta.Size.x; }
      }

      public int CellSize
      {
        get { return GRID_CELL_SIZE; }
      }

      public float CellSizeRatio
      {
        get { return (float)MapSize / CellSize; }
      }

      public int NumberOfCells { get; private set; }

      string[] RowIds;
      string[] ColumnIds;
      string[,] AreaIds;
      Vector3[,] Positions;

      public MapGrid()
      {
        NumberOfCells = (int)Math.Ceiling(MapSize / (float)CellSize);
        RowIds = new string[NumberOfCells];
        ColumnIds = new string[NumberOfCells];
        AreaIds = new string[NumberOfCells, NumberOfCells];
        Positions = new Vector3[NumberOfCells, NumberOfCells];
        Build();
      }

      public string GetRowId(int row)
      {
        return RowIds[row];
      }

      public string GetColumnId(int col)
      {
        return ColumnIds[col];
      }

      public string GetAreaId(int row, int col)
      {
        return AreaIds[row, col];
      }

      public Vector3 GetPosition(int row, int col)
      {
        return Positions[row, col];
      }

      void Build()
      {
        string prefix = "";
        char letter = 'A';

        for (int col = 0; col < NumberOfCells; col++)
        {
          ColumnIds[col] = prefix + letter;
          if (letter == 'Z')
          {
            prefix = "A";
            letter = 'A';
          }
          else
          {
            letter++;
          }
        }

        for (int row = 0; row < NumberOfCells; row++)
          RowIds[row] = row.ToString();

        int z = (MapSize / 2) - CellSize;
        for (int row = 0; row < NumberOfCells; row++)
        {
          int x = -(MapSize / 2);
          for (int col = 0; col < NumberOfCells; col++)
          {
            var areaId = ColumnIds[col] + RowIds[row];
            AreaIds[row, col] = areaId;
            Positions[row, col] = new Vector3(x + (CellSize / 2), 0, z + (CellSize / 2));
            x += CellSize;
          }
          z -= CellSize;
        }
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Collections;
  using System.Drawing;
  using System.Drawing.Drawing2D;

  public partial class Imperium
  {
    class MapOverlayGenerator : UnityEngine.MonoBehaviour
    {
      public bool IsGenerating { get; private set; }

      public void Generate()
      {
        if (!IsGenerating)
          StartCoroutine(GenerateOverlayImage());
      }

      IEnumerator GenerateOverlayImage()
      {
        IsGenerating = true;
        Instance.Puts("Generating new map overlay image...");

        using (var bitmap = new Bitmap(Instance.Options.Map.ImageSize, Instance.Options.Map.ImageSize))
        using (var graphics = Graphics.FromImage(bitmap))
        {
          var grid = Instance.Areas.MapGrid;
          var tileSize = (int)(Instance.Options.Map.ImageSize / grid.CellSizeRatio);

          var colorPicker = new FactionColorPicker();
          var textBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));

          for (int row = 0; row < grid.NumberOfCells; row++)
          {
            for (int col = 0; col < grid.NumberOfCells; col++)
            {
              Area area = Instance.Areas.Get(row, col);
              var x = (col * tileSize);
              var y = (row * tileSize);
              var rect = new Rectangle(x, y, tileSize, tileSize);

              if (area.Type == AreaType.Badlands)
              {
                // If the tile is badlands, color it in black.
                var brush = new HatchBrush(HatchStyle.BackwardDiagonal, Color.FromArgb(32, 0, 0, 0), Color.FromArgb(255, 0, 0, 0));
                graphics.FillRectangle(brush, rect);
              }
              else if (area.Type != AreaType.Wilderness)
              {
                // If the tile is claimed, fill it with a color indicating the faction.
                var brush = new SolidBrush(colorPicker.GetColorForFaction(area.FactionId));
                graphics.FillRectangle(brush, rect);
              }

              yield return null;
            }
          }

          var gridLabelFont = new Font("Consolas", 14, FontStyle.Bold);
          var gridLabelOffset = 5;
          var gridLinePen = new Pen(Color.FromArgb(192, 0, 0, 0), 2);

          for (int row = 0; row < grid.NumberOfCells; row++)
          {
            if (row > 0) graphics.DrawLine(gridLinePen, 0, (row * tileSize), (grid.NumberOfCells * tileSize), (row * tileSize));
            graphics.DrawString(grid.GetRowId(row), gridLabelFont, textBrush, gridLabelOffset, (row * tileSize) + gridLabelOffset);
          }

          for (int col = 1; col < grid.NumberOfCells; col++)
          {
            graphics.DrawLine(gridLinePen, (col * tileSize), 0, (col * tileSize), (grid.NumberOfCells * tileSize));
            graphics.DrawString(grid.GetColumnId(col), gridLabelFont, textBrush, (col * tileSize) + gridLabelOffset, gridLabelOffset);
          }

          var converter = new ImageConverter();
          var imageData = (byte[])converter.ConvertTo(bitmap, typeof(byte[]));

          Image image = Instance.Hud.RegisterImage(Ui.MapOverlayImageUrl, imageData, true);

          Instance.Puts($"Generated new map overlay image {image.Id}.");
          Instance.Log($"Created new map overlay image {image.Id}.");

          IsGenerating = false;
        }
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    public static class MonumentPrefab
    {
      const string PrefabPrefix = "assets/bundled/prefabs/autospawn/monument/";

      public const string Airfield = PrefabPrefix + "large/airfield_1.prefab";
      public const string BanditTown = PrefabPrefix + "medium/bandit_town.prefab";
      public const string Compound = PrefabPrefix + "medium/compound.prefab";
      public const string Dome = PrefabPrefix + "small/sphere_tank.prefab";
      public const string Harbor1 = PrefabPrefix + "harbor/harbor_1.prefab";
      public const string Harbor2 = PrefabPrefix + "harbor/harbor_2.prefab";
      public const string GasStation = PrefabPrefix + "small/gas_station_1.prefab";
      public const string Junkyard = PrefabPrefix + "large/junkyard_1.prefab";
      public const string LaunchSite = PrefabPrefix + "large/launch_site_1.prefab";
      public const string Lighthouse = PrefabPrefix + "lighthouse/lighthouse.prefab";
      public const string MilitaryTunnel = PrefabPrefix + "large/military_tunnel_1.prefab";
      public const string MiningOutpost = PrefabPrefix + "small/warehouse.prefab";
      public const string QuarryStone = PrefabPrefix + "small/mining_quarry_a.prefab";
      public const string QuarrySulfur = PrefabPrefix + "small/mining_quarry_b.prefab";
      public const string QuaryHqm = PrefabPrefix + "small/mining_quarry_c.prefab";
      public const string PowerPlant = PrefabPrefix + "large/powerplant_1.prefab";
      public const string Trainyard = PrefabPrefix + "large/trainyard_1.prefab";
      public const string SatelliteDish = PrefabPrefix + "small/satellite_dish.prefab";
      public const string SewerBranch = PrefabPrefix + "medium/radtown_small_3.prefab";
      public const string Supermarket = PrefabPrefix + "small/supermarket_1.prefab";
      public const string WaterTreatmentPlant = PrefabPrefix + "large/water_treatment_plant_1.prefab";
      public const string WaterWellA = PrefabPrefix + "tiny/water_well_a.prefab";
      public const string WaterWellB = PrefabPrefix + "tiny/water_well_b.prefab";
      public const string WaterWellC = PrefabPrefix + "tiny/water_well_c.prefab";
      public const string WaterWellD = PrefabPrefix + "tiny/water_well_d.prefab";
      public const string WaterWellE = PrefabPrefix + "tiny/water_well_e.prefab";
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Reflection;

  public partial class Imperium : RustPlugin
  {
    public static class Permission
    {
      public const string AdminFactions = "imperium.factions.admin";
      public const string AdminClaims = "imperium.claims.admin";
      public const string AdminBadlands = "imperium.badlands.admin";
      public const string AdminPins = "imperium.pins.admin";
      public const string ManageFactions = "imperium.factions";

      public static void RegisterAll(Imperium instance)
      {
        foreach (FieldInfo field in typeof(Permission).GetFields(BindingFlags.Public | BindingFlags.Static))
          instance.permission.RegisterPermission((string)field.GetRawConstantValue(), instance);
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using Newtonsoft.Json;
  using UnityEngine;

  public partial class Imperium : RustPlugin
  {
    class UnityVector3Converter : JsonConverter
    {
      public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
      {
        var vector = (Vector3) value;
        writer.WriteValue($"{vector.x} {vector.y} {vector.z}");
      }

      public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
      {
        string[] tokens = reader.Value.ToString().Trim().Split(' ');
        float x = Convert.ToSingle(tokens[0]);
        float y = Convert.ToSingle(tokens[1]);
        float z = Convert.ToSingle(tokens[2]);
        return new Vector3(x, y, z);
      }

      public override bool CanConvert(Type objectType)
      {
        return objectType == typeof(Vector3);
      }
    }
  }
}﻿namespace Oxide.Plugins
{
  using System;
  using System.Collections;
  using System.Text;

  public partial class Imperium
  {
    static class Util
    {
      const string NullString = "(null)";

      public static string Format(object obj)
      {
        if (obj == null) return NullString;

        var user = obj as User;
        if (user != null) return Format(user);

        var area = obj as Area;
        if (area != null) return Format(area);

        var entity = obj as BaseEntity;
        if (entity != null) return Format(entity);

        var list = obj as IEnumerable;
        if (list != null) return Format(list);

        return obj.ToString();
      }

      public static string Format(User user)
      {
        if (user == null)
          return NullString;
        else
          return $"{user.UserName} ({user.Id})";
      }

      public static string Format(Area area)
      {
        if (area == null)
          return NullString;
        else if (!String.IsNullOrEmpty(area.Name))
          return $"{area.Id} ({area.Name})";
        else
          return area.Id;
      }

      public static string Format(BaseEntity entity)
      {
        if (entity == null)
          return NullString;
        else if (entity.net == null)
          return "(missing networkable)";
        else
          return entity.net.ID.ToString();
      }

      public static string Format(IEnumerable items)
      {
        var sb = new StringBuilder();

        foreach (object item in items)
          sb.Append($"{Format(item)}, ");

        sb.Remove(sb.Length - 2, 2);

        return sb.ToString();
      }

      public static string NormalizeAreaId(string input)
      {
        return input.ToUpper().Trim();
      }

      public static string NormalizeAreaName(string input)
      {
        return RemoveSpecialCharacters(input.Trim());
      }

      public static string NormalizePinName(string input)
      {
        return RemoveSpecialCharacters(input.Trim());
      }

      public static string NormalizeFactionId(string input)
      {
        string factionId = input.Trim();

        if (factionId.StartsWith("[") && factionId.EndsWith("]"))
          factionId = factionId.Substring(1, factionId.Length - 2);

        return factionId;
      }

      public static string RemoveSpecialCharacters(string str)
      {
        if (String.IsNullOrEmpty(str))
          return String.Empty;

        StringBuilder sb = new StringBuilder(str.Length);
        foreach (char c in str)
        {
          if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= 'А' && c <= 'Я') || (c >= 'а' && c <= 'я') || c == ' ' || c == '.' || c == '_')
            sb.Append(c);
        }

        return sb.ToString();
      }

      public static int GetLevenshteinDistance(string source, string target)
      {
        if (string.IsNullOrEmpty(source) && string.IsNullOrEmpty(target))
          return 0;

        if (source.Length == target.Length)
          return source.Length;

        if (source.Length == 0)
          return target.Length;

        if (target.Length == 0)
          return source.Length;

        var distance = new int[source.Length + 1, target.Length + 1];

        for (int idx = 0; idx <= source.Length; distance[idx, 0] = idx++) ;
        for (int idx = 0; idx <= target.Length; distance[0, idx] = idx++) ;

        for (int i = 1; i <= source.Length; i++)
        {
          for (int j = 1; j <= target.Length; j++)
          {
            int cost = target[j - 1] == source[i - 1] ? 0 : 1;
            distance[i, j] = Math.Min(
              Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
              distance[i - 1, j - 1] + cost
            );
          }
        }

        return distance[source.Length, target.Length];
      }

      public static bool TryParseEnum<T>(string str, out T value) where T : struct
      {
        if (!typeof(T).IsEnum)
          throw new ArgumentException("Type parameter must be an enum");

        foreach (var name in Enum.GetNames(typeof(T)))
        {
          if (String.Equals(name, str, StringComparison.OrdinalIgnoreCase))
          {
            value = (T)Enum.Parse(typeof(T), name);
            return true;
          }
        }

        value = default(T);
        return false;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;

  public partial class Imperium
  {
    class AddingClaimInteraction : Interaction
    {
      public Faction Faction { get; private set; }

      public AddingClaimInteraction(Faction faction)
      {
        Faction = faction;
      }

      public override bool TryComplete(HitInfo hit)
      {
        var cupboard = hit.HitEntity as BuildingPrivlidge;
        Area area = User.CurrentArea;

        if (area == null)
        {
          Instance.PrintWarning("Player attempted to add claim but wasn't in an area. This shouldn't happen.");
          return false;
        }

        if (!Instance.EnsureUserCanChangeFactionClaims(User, Faction))
          return false;

        if (!Instance.EnsureCupboardCanBeUsedForClaim(User, cupboard))
          return false;

        if (!Instance.EnsureFactionCanClaimArea(User, Faction, area))
          return false;

        Area[] claimedAreas = Instance.Areas.GetAllClaimedByFaction(Faction);
        AreaType type = (claimedAreas.Length == 0) ? AreaType.Headquarters : AreaType.Claimed;

        if (area.Type == AreaType.Wilderness)
        {
          int cost = area.GetClaimCost(Faction);

          if (cost > 0)
          {
            ItemDefinition scrapDef = ItemManager.FindItemDefinition("scrap");
            List<Item> stacks = User.Player.inventory.FindItemIDs(scrapDef.itemid);

            if (!Instance.TryCollectFromStacks(scrapDef, stacks, cost))
            {
              User.SendChatMessage(Messages.CannotClaimAreaCannotAfford, cost);
              return false;
            }
          }

          User.SendChatMessage(Messages.ClaimAdded, area.Id);

          if (type == AreaType.Headquarters)
          {
            Instance.PrintToChat(Messages.AreaClaimedAsHeadquartersAnnouncement, Faction.Id, area.Id);
            Faction.NextUpkeepPaymentTime = DateTime.UtcNow.AddHours(Instance.Options.Upkeep.CollectionPeriodHours);
          }
          else
          {
            Instance.PrintToChat(Messages.AreaClaimedAnnouncement, Faction.Id, area.Id);
          }

          Instance.Log($"{Util.Format(User)} claimed {area.Id} on behalf of {Faction.Id}");
          Instance.Areas.Claim(area, type, Faction, User, cupboard);

          return true;
        }

        if (area.FactionId == Faction.Id)
        {
          if (area.ClaimCupboard.net.ID == cupboard.net.ID)
          {
            User.SendChatMessage(Messages.CannotClaimAreaAlreadyOwned, area.Id);
            return false;
          }
          else
          {
            // If the same faction claims a new cupboard within the same area, move the claim to the new cupboard.
            User.SendChatMessage(Messages.ClaimCupboardMoved, area.Id);
            Instance.Log($"{Util.Format(User)} moved {area.FactionId}'s claim on {area.Id} from cupboard {Util.Format(area.ClaimCupboard)} to cupboard {Util.Format(cupboard)}");
            area.ClaimantId = User.Id;
            area.ClaimCupboard = cupboard;
            return true;
          }
        }

        if (area.FactionId != Faction.Id)
        {
          if (area.ClaimCupboard.net.ID != cupboard.net.ID)
          {
            // A new faction can't make a claim on a new cabinet within an area that is already claimed by another faction.
            User.SendChatMessage(Messages.CannotClaimAreaAlreadyClaimed, area.Id, area.FactionId);
            return false;
          }

          string previousFactionId = area.FactionId;

          // If a new faction claims the claim cabinet for an area, they take control of that area.
          User.SendChatMessage(Messages.ClaimCaptured, area.Id, area.FactionId);
          Instance.PrintToChat(Messages.AreaCapturedAnnouncement, Faction.Id, area.Id, area.FactionId);
          Instance.Log($"{Util.Format(User)} captured the claim on {area.Id} from {area.FactionId} on behalf of {Faction.Id}");

          Instance.Areas.Claim(area, type, Faction, User, cupboard);
          return true;
        }

        Instance.PrintWarning("Area was in an unknown state during completion of AddingClaimInteraction. This shouldn't happen.");
        return false;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    class AssigningClaimInteraction : Interaction
    {
      public Faction Faction { get; private set; }

      public AssigningClaimInteraction(Faction faction)
      {
        Faction = faction;
      }

      public override bool TryComplete(HitInfo hit)
      {
        var cupboard = hit.HitEntity as BuildingPrivlidge;

        Area area = User.CurrentArea;

        if (area == null)
        {
          Instance.PrintWarning("Player attempted to assign claim but wasn't in an area. This shouldn't happen.");
          return false;
        }

        if (area.Type == AreaType.Badlands)
        {
          User.SendChatMessage(Messages.AreaIsBadlands, area.Id);
          return false;
        }

        Area[] ownedAreas = Instance.Areas.GetAllClaimedByFaction(Faction);
        AreaType type = (ownedAreas.Length == 0) ? AreaType.Headquarters : AreaType.Claimed;

        Instance.PrintToChat(Messages.AreaClaimAssignedAnnouncement, Faction.Id, area.Id);
        Instance.Log($"{Util.Format(User)} assigned {area.Id} to {Faction.Id}");

        Instance.Areas.Claim(area, type, Faction, User, cupboard);
        return true;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    abstract class Interaction
    {
      public User User { get; set; }
      public abstract bool TryComplete(HitInfo hit);
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    class RemovingClaimInteraction : Interaction
    {
      public Faction Faction { get; private set; }

      public RemovingClaimInteraction(Faction faction)
      {
        Faction = faction;
      }

      public override bool TryComplete(HitInfo hit)
      {
        var cupboard = hit.HitEntity as BuildingPrivlidge;

        if (!Instance.EnsureUserCanChangeFactionClaims(User, Faction) || !Instance.EnsureCupboardCanBeUsedForClaim(User, cupboard))
          return false;

        Area area = Instance.Areas.GetByClaimCupboard(cupboard);

        if (area == null)
        {
          User.SendChatMessage(Messages.SelectingCupboardFailedNotClaimCupboard);
          return false;
        }

        Instance.PrintToChat(Messages.AreaClaimRemovedAnnouncement, Faction.Id, area.Id);
        Instance.Log($"{Util.Format(User)} removed {Faction.Id}'s claim on {area.Id}");

        Instance.Areas.Unclaim(area);
        return true;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    class SelectingHeadquartersInteraction : Interaction
    {
      public Faction Faction { get; private set; }

      public SelectingHeadquartersInteraction(Faction faction)
      {
        Faction = faction;
      }

      public override bool TryComplete(HitInfo hit)
      {
        var cupboard = hit.HitEntity as BuildingPrivlidge;

        if (!Instance.EnsureUserCanChangeFactionClaims(User, Faction) || !Instance.EnsureCupboardCanBeUsedForClaim(User, cupboard))
          return false;

        Area area = Instance.Areas.GetByClaimCupboard(cupboard);
        if (area == null)
        {
          User.SendChatMessage(Messages.SelectingCupboardFailedNotClaimCupboard);
          return false;
        }

        Instance.PrintToChat(Messages.HeadquartersChangedAnnouncement, Faction.Id, area.Id);
        Instance.Log($"{Util.Format(User)} set {Faction.Id}'s headquarters to {area.Id}");

        Instance.Areas.SetHeadquarters(area, Faction);
        return true;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    class SelectingTaxChestInteraction : Interaction
    {
      public Faction Faction { get; private set; }

      public SelectingTaxChestInteraction(Faction faction)
      {
        Faction = faction;
      }

      public override bool TryComplete(HitInfo hit)
      {
        var container = hit.HitEntity as StorageContainer;

        if (container == null)
        {
          User.SendChatMessage(Messages.SelectingTaxChestFailedInvalidTarget);
          return false;
        }

        User.SendChatMessage(Messages.SelectingTaxChestSucceeded, Faction.TaxRate * 100, Faction.Id);
        Instance.Log($"{Util.Format(User)} set {Faction.Id}'s tax chest to entity {Util.Format(container)}");
        Instance.Factions.SetTaxChest(Faction, container);

        return true;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    class TransferringClaimInteraction : Interaction
    {
      public Faction SourceFaction { get; }
      public Faction TargetFaction { get; }

      public TransferringClaimInteraction(Faction sourceFaction, Faction targetFaction)
      {
        SourceFaction = sourceFaction;
        TargetFaction = targetFaction;
      }

      public override bool TryComplete(HitInfo hit)
      {
        var cupboard = hit.HitEntity as BuildingPrivlidge;

        if (!Instance.EnsureUserCanChangeFactionClaims(User, SourceFaction) || !Instance.EnsureCupboardCanBeUsedForClaim(User, cupboard))
          return false;

        Area area = Instance.Areas.GetByClaimCupboard(cupboard);

        if (area == null)
        {
          User.SendChatMessage(Messages.SelectingCupboardFailedNotClaimCupboard);
          return false;
        }

        if (area.FactionId != SourceFaction.Id)
        {
          User.SendChatMessage(Messages.AreaNotOwnedByYourFaction, area.Id);
          return false;
        }

        if (!Instance.EnsureFactionCanClaimArea(User, TargetFaction, area))
          return false;

        Area[] claimedAreas = Instance.Areas.GetAllClaimedByFaction(TargetFaction);
        AreaType type = (claimedAreas.Length == 0) ? AreaType.Headquarters : AreaType.Claimed;

        Instance.PrintToChat(Messages.AreaClaimTransferredAnnouncement, SourceFaction.Id, area.Id, TargetFaction.Id);
        Instance.Log($"{Util.Format(User)} transferred {SourceFaction.Id}'s claim on {area.Id} to {TargetFaction.Id}");

        Instance.Areas.Claim(area, type, TargetFaction, User, cupboard);

        return true;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class BadlandsOptions
    {
      [JsonProperty("enabled")]
      public bool Enabled;

      public static BadlandsOptions Default = new BadlandsOptions {
        Enabled = true
      };
    }
  }
}
﻿namespace Oxide.Plugins
{
  using Newtonsoft.Json;
  using System.Collections.Generic;

  public partial class Imperium : RustPlugin
  {
    class ClaimOptions
    {
      [JsonProperty("enabled")]
      public bool Enabled;

      [JsonProperty("costs")]
      public List<int> Costs = new List<int>();

      [JsonProperty("maxClaims")]
      public int? MaxClaims;

      [JsonProperty("minAreaNameLength")]
      public int MinAreaNameLength;

      [JsonProperty("maxAreaNameLength")]
      public int MaxAreaNameLength;

      [JsonProperty("minFactionMembers")]
      public int MinFactionMembers;

      [JsonProperty("requireContiguousClaims")]
      public bool RequireContiguousClaims;

      public static ClaimOptions Default = new ClaimOptions {
        Enabled = true,
        Costs = new List<int> { 0, 100, 200, 300, 400, 500 },
        MaxClaims = null,
        MinAreaNameLength = 3,
        MaxAreaNameLength = 20,
        MinFactionMembers = 3,
        RequireContiguousClaims = false
      };
    }
  }
}
﻿namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class DecayOptions
    {
      [JsonProperty("enabled")]
      public bool Enabled;

      [JsonProperty("claimedLandDecayReduction")]
      public float ClaimedLandDecayReduction;

      public static DecayOptions Default = new DecayOptions {
        Enabled = false,
        ClaimedLandDecayReduction = 0.5f
      };
    }
  }
}
﻿namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class FactionOptions
    {
      [JsonProperty("minFactionNameLength")]
      public int MinFactionNameLength;

      [JsonProperty("maxFactionNameLength")]
      public int MaxFactionNameLength;

      [JsonProperty("maxMembers")]
      public int? MaxMembers;

      public static FactionOptions Default = new FactionOptions {
        MinFactionNameLength = 1,
        MaxFactionNameLength = 8,
        MaxMembers = null
      };
    }
  }
}
﻿namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class MapOptions
    {
      [JsonProperty("pinsEnabled")]
      public bool PinsEnabled;

      [JsonProperty("minPinNameLength")]
      public int MinPinNameLength;

      [JsonProperty("maxPinNameLength")]
      public int MaxPinNameLength;

      [JsonProperty("pinCost")]
      public int PinCost;

      [JsonProperty("commandCooldownSeconds")]
      public int CommandCooldownSeconds;

      [JsonProperty("imageUrl")]
      public string ImageUrl;

      [JsonProperty("imageSize")]
      public int ImageSize;

      [JsonProperty("serverLogoUrl")]
      public string ServerLogoUrl;

      public static MapOptions Default = new MapOptions {
        PinsEnabled = true,
        MinPinNameLength = 2,
        MaxPinNameLength = 20,
        PinCost = 100,
        CommandCooldownSeconds = 10,
        ImageUrl = "",
        ImageSize = 1440,
        ServerLogoUrl = ""
      };
    }
  }
}
﻿namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class ImperiumOptions
    {
      [JsonProperty("badlands")]
      public BadlandsOptions Badlands;

      [JsonProperty("claims")]
      public ClaimOptions Claims;

      [JsonProperty("decay")]
      public DecayOptions Decay;

      [JsonProperty("factions")]
      public FactionOptions Factions;

      [JsonProperty("map")]
      public MapOptions Map;

      [JsonProperty("pvp")]
      public PvpOptions Pvp;

      [JsonProperty("raiding")]
      public RaidingOptions Raiding;

      [JsonProperty("taxes")]
      public TaxOptions Taxes;

      [JsonProperty("upkeep")]
      public UpkeepOptions Upkeep;

      [JsonProperty("war")]
      public WarOptions War;

      [JsonProperty("zones")]
      public ZoneOptions Zones;

      public static ImperiumOptions Default = new ImperiumOptions {
        Badlands = BadlandsOptions.Default,
        Claims = ClaimOptions.Default,
        Decay = DecayOptions.Default,
        Factions = FactionOptions.Default,
        Map = MapOptions.Default,
        Pvp = PvpOptions.Default,
        Raiding = RaidingOptions.Default,
        Taxes = TaxOptions.Default,
        Upkeep = UpkeepOptions.Default,
        War = WarOptions.Default,
        Zones = ZoneOptions.Default
      };
    }

    protected override void LoadDefaultConfig()
    {
      PrintWarning("Loading default configuration.");
      Config.WriteObject(ImperiumOptions.Default, true);
    }
  }
}
﻿namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class PvpOptions
    {
      [JsonProperty("restrictPvp")]
      public bool RestrictPvp;

      [JsonProperty("allowedInBadlands")]
      public bool AllowedInBadlands;

      [JsonProperty("allowedInClaimedLand")]
      public bool AllowedInClaimedLand;

      [JsonProperty("allowedInWilderness")]
      public bool AllowedInWilderness;

      [JsonProperty("allowedInEventZones")]
      public bool AllowedInEventZones;

      [JsonProperty("allowedInMonumentZones")]
      public bool AllowedInMonumentZones;

      [JsonProperty("allowedInRaidZones")]
      public bool AllowedInRaidZones;

      [JsonProperty("enablePvpCommand")]
      public bool EnablePvpCommand;

      [JsonProperty("commandCooldownSeconds")]
      public int CommandCooldownSeconds;

      public static PvpOptions Default = new PvpOptions {
        RestrictPvp = false,
        AllowedInBadlands = true,
        AllowedInClaimedLand = true,
        AllowedInEventZones = true,
        AllowedInMonumentZones = true,
        AllowedInRaidZones = true,
        AllowedInWilderness = true,
        EnablePvpCommand = false,
        CommandCooldownSeconds = 60
      };
    }
  }
}
﻿namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class RaidingOptions
    {
      [JsonProperty("restrictRaiding")]
      public bool RestrictRaiding;

      [JsonProperty("allowedInBadlands")]
      public bool AllowedInBadlands;

      [JsonProperty("allowedInClaimedLand")]
      public bool AllowedInClaimedLand;

      [JsonProperty("allowedInWilderness")]
      public bool AllowedInWilderness;

      public static RaidingOptions Default = new RaidingOptions {
        RestrictRaiding = false,
        AllowedInBadlands = true,
        AllowedInClaimedLand = true,
        AllowedInWilderness = true
      };
    }
  }
}
﻿namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class TaxOptions
    {
      [JsonProperty("enabled")]
      public bool Enabled;

      [JsonProperty("defaultTaxRate")]
      public float DefaultTaxRate;

      [JsonProperty("maxTaxRate")]
      public float MaxTaxRate;

      [JsonProperty("claimedLandGatherBonus")]
      public float ClaimedLandGatherBonus;

      [JsonProperty("badlandsGatherBonus")]
      public float BadlandsGatherBonus;

      public static TaxOptions Default = new TaxOptions {
        Enabled = true,
        DefaultTaxRate = 0.1f,
        MaxTaxRate = 0.2f,
        ClaimedLandGatherBonus = 0.1f,
        BadlandsGatherBonus = 0.1f
      };
    }
  }
}
﻿namespace Oxide.Plugins
{
  using Newtonsoft.Json;
  using System.Collections.Generic;

  public partial class Imperium : RustPlugin
  {
    class UpkeepOptions
    {
      [JsonProperty("enabled")]
      public bool Enabled;

      [JsonProperty("costs")]
      public List<int> Costs = new List<int>();

      [JsonProperty("checkIntervalMinutes")]
      public int CheckIntervalMinutes;

      [JsonProperty("collectionPeriodHours")]
      public int CollectionPeriodHours;

      [JsonProperty("gracePeriodHours")]
      public int GracePeriodHours;

      public static UpkeepOptions Default = new UpkeepOptions {
        Enabled = false,
        Costs = new List<int> { 10, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 },
        CheckIntervalMinutes = 15,
        CollectionPeriodHours = 24,
        GracePeriodHours = 12
      };
    }
  }
}
﻿namespace Oxide.Plugins
{
  using Newtonsoft.Json;
  using System.Collections.Generic;

  public partial class Imperium : RustPlugin
  {
    class WarOptions
    {
      [JsonProperty("enabled")]
      public bool Enabled;

      [JsonProperty("minCassusBelliLength")]
      public int MinCassusBelliLength;

      [JsonProperty("defensiveBonuses")]
      public List<float> DefensiveBonuses = new List<float>();

      public static WarOptions Default = new WarOptions {
        Enabled = true,
        MinCassusBelliLength = 50,
        DefensiveBonuses = new List<float> { 0, 0.5f, 1f }
      };
    }
  }
}
﻿namespace Oxide.Plugins
{
  using Newtonsoft.Json;
  using System.Collections.Generic;

  public partial class Imperium : RustPlugin
  {
    class ZoneOptions
    {
      [JsonProperty("enabled")]
      public bool Enabled;

      [JsonProperty("domeDarkness")]
      public int DomeDarkness;

      [JsonProperty("eventZoneRadius")]
      public float EventZoneRadius;

      [JsonProperty("eventZoneLifespanSeconds")]
      public float EventZoneLifespanSeconds;

      [JsonProperty("monumentZones")]
      public Dictionary<string, float> MonumentZones = new Dictionary<string, float>();

      public static ZoneOptions Default = new ZoneOptions {
        Enabled = true,
        DomeDarkness = 3,
        EventZoneRadius = 150f,
        EventZoneLifespanSeconds = 600f,
        MonumentZones = new Dictionary<string, float> {
          { "airfield", 200 },
          { "sphere_tank", 120 },
          { "junkyard", 150 },
          { "launch_site", 300 },
          { "military_tunnel", 150 },
          { "powerplant", 175 },
          { "satellite_dish", 130 },
          { "trainyard", 180 },
          { "water_treatment_plant", 180 }
        }
      };
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using UnityEngine;

  public partial class Imperium
  {
    class GameEventWatcher : MonoBehaviour
    {
      const float CheckIntervalSeconds = 5f;

      HashSet<CargoPlane> CargoPlanes = new HashSet<CargoPlane>();
      HashSet<BaseHelicopter> PatrolHelicopters = new HashSet<BaseHelicopter>();
      HashSet<CH47Helicopter> ChinookHelicopters = new HashSet<CH47Helicopter>();
      HashSet<HackableLockedCrate> LockedCrates = new HashSet<HackableLockedCrate>();
      HashSet<CargoShip> CargoShips = new HashSet<CargoShip>();

      public bool IsCargoPlaneActive
      {
        get { return CargoPlanes.Count > 0; }
      }

      public bool IsHelicopterActive
      {
        get { return PatrolHelicopters.Count > 0; }
      }

      public bool IsChinookOrLockedCrateActive
      {
        get { return ChinookHelicopters.Count > 0 || LockedCrates.Count > 0; }
      }

      public bool IsCargoShipActive
      {
        get { return CargoShips.Count > 0; }
      }

      void Awake()
      {
        foreach (CargoPlane plane in FindObjectsOfType<CargoPlane>())
          BeginEvent(plane);

        foreach (BaseHelicopter heli in FindObjectsOfType<BaseHelicopter>())
          BeginEvent(heli);

        foreach (CH47Helicopter chinook in FindObjectsOfType<CH47Helicopter>())
          BeginEvent(chinook);

        foreach (HackableLockedCrate crate in FindObjectsOfType<HackableLockedCrate>())
          BeginEvent(crate);

        foreach (CargoShip ship in FindObjectsOfType<CargoShip>())
          BeginEvent(ship);

        InvokeRepeating("CheckEvents", CheckIntervalSeconds, CheckIntervalSeconds);
      }

      void OnDestroy()
      {
        CancelInvoke();
      }

      public void BeginEvent(CargoPlane plane)
      {
        Instance.Puts($"Beginning cargoplane event, plane at @ {plane.transform.position}");
        CargoPlanes.Add(plane);
      }

      public void BeginEvent(BaseHelicopter heli)
      {
        Instance.Puts($"Beginning patrol helicopter event, heli at @ {heli.transform.position}");
        PatrolHelicopters.Add(heli);
      }

      public void BeginEvent(CH47Helicopter chinook)
      {
        Instance.Puts($"Beginning chinook event, heli at @ {chinook.transform.position}");
        ChinookHelicopters.Add(chinook);
      }

      public void BeginEvent(HackableLockedCrate crate)
      {
        Instance.Puts($"Beginning locked crate event, crate at @ {crate.transform.position}");
        LockedCrates.Add(crate);
      }

      public void BeginEvent(CargoShip ship)
      {
        Instance.Puts($"Beginning cargo ship event, ship at @ {ship.transform.position}");
        CargoShips.Add(ship);
      }

      void CheckEvents()
      {
        var endedEvents = CargoPlanes.RemoveWhere(IsEntityGone)
          + PatrolHelicopters.RemoveWhere(IsEntityGone)
          + ChinookHelicopters.RemoveWhere(IsEntityGone)
          + LockedCrates.RemoveWhere(IsEntityGone)
          + CargoShips.RemoveWhere(IsEntityGone);

        if (endedEvents > 0)
          Instance.Hud.RefreshForAllPlayers();
      }

      bool IsEntityGone(BaseEntity entity)
      {
        return !entity.IsValid() || !entity.gameObject.activeInHierarchy;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Reflection;
  using Oxide.Game.Rust.Cui;

  public partial class Imperium
  {
    class HudManager
    {
      Dictionary<string, Image> Images;
      bool UpdatePending;

      public GameEventWatcher GameEvents { get; private set; }

      ImageDownloader ImageDownloader;
      MapOverlayGenerator MapOverlayGenerator;

      public HudManager()
      {
        Images = new Dictionary<string, Image>();
        GameEvents = Instance.GameObject.AddComponent<GameEventWatcher>();
        ImageDownloader = Instance.GameObject.AddComponent<ImageDownloader>();
        MapOverlayGenerator = Instance.GameObject.AddComponent<MapOverlayGenerator>();
      }

      public void RefreshForAllPlayers()
      {
        if (UpdatePending)
          return;

        Instance.NextTick(() => {
          foreach (User user in Instance.Users.GetAll())
          {
            user.Map.Refresh();
            user.Hud.Refresh();
          }
          UpdatePending = false;
        });

        UpdatePending = true;
      }

      public Image RegisterImage(string url, byte[] imageData = null, bool overwrite = false)
      {
        Image image;

        if (Images.TryGetValue(url, out image) && !overwrite)
          return image;
        else
          image = new Image(url);

        Images[url] = image;

        if (imageData != null)
          image.Save(imageData);
        else
          ImageDownloader.Download(image);

        return image;
      }

      public void RefreshAllImages()
      {
        foreach (Image image in Images.Values.Where(image => !image.IsGenerated))
        {
          image.Delete();
          ImageDownloader.Download(image);
        }
      }

      public CuiRawImageComponent CreateImageComponent(string imageUrl)
      {
        Image image;

        if (String.IsNullOrEmpty(imageUrl))
        {
          Instance.PrintError($"CuiRawImageComponent requested for an image with a null URL. Did you forget to set MapImageUrl in the configuration?");
          return null;
        }

        if (!Images.TryGetValue(imageUrl, out image))
        {
          Instance.PrintError($"CuiRawImageComponent requested for image with an unregistered URL {imageUrl}. This shouldn't happen.");
          return null;
        }

        if (image.Id != null)
          return new CuiRawImageComponent { Png = image.Id, Sprite = Ui.TransparentTexture };
        else
          return new CuiRawImageComponent { Url = image.Url, Sprite = Ui.TransparentTexture };
      }

      public void GenerateMapOverlayImage()
      {
        MapOverlayGenerator.Generate();
      }

      public void Init()
      {
        if (!String.IsNullOrEmpty(Instance.Options.Map.ImageUrl))
          RegisterImage(Instance.Options.Map.ImageUrl);

        if (!String.IsNullOrEmpty(Instance.Options.Map.ServerLogoUrl))
          RegisterImage(Instance.Options.Map.ServerLogoUrl);

        RegisterDefaultImages(typeof(Ui.HudIcon));
        RegisterDefaultImages(typeof(Ui.MapIcon));
      }

      public void Destroy()
      {
        UnityEngine.Object.DestroyImmediate(ImageDownloader);
        UnityEngine.Object.DestroyImmediate(MapOverlayGenerator);
        UnityEngine.Object.DestroyImmediate(GameEvents);

        foreach (Image image in Images.Values)
          image.Delete();

        Images.Clear();
      }

      void RegisterDefaultImages(Type type)
      {
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
          RegisterImage((string)field.GetRawConstantValue());
      }
    }
  }
}
 
 ﻿namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    class Image
    {
      public string Url { get; private set; }
      public string Id { get; private set; }

      public bool IsDownloaded
      {
        get { return Id != null; }
      }

      public bool IsGenerated
      {
        get { return Url != null && !Url.StartsWith("http", StringComparison.Ordinal); }
      }

      public Image(string url, string id = null)
      {
        Url = url;
        Id = id;
      }

      public string Save(byte[] data)
      {
        if (IsDownloaded) Delete();
        Id = FileStorage.server.Store(data, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID, 0).ToString();
        return Id;
      }

      public void Delete()
      {
        if (!IsDownloaded) return;
        FileStorage.server.Remove(Convert.ToUInt32(Id), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID);
        Id = null;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using UnityEngine;

  public partial class Imperium
  {
    class ImageDownloader : MonoBehaviour
    {
      Queue<Image> PendingImages = new Queue<Image>();

      public bool IsDownloading { get; private set; }

      public void Download(Image image)
      {
        PendingImages.Enqueue(image);
        if (!IsDownloading) DownloadNext();
      }

      void DownloadNext()
      {
        if (PendingImages.Count == 0)
        {
          IsDownloading = false;
          return;
        }

        Image image = PendingImages.Dequeue();
        StartCoroutine(DownloadImage(image));

        IsDownloading = true;
      }

      IEnumerator DownloadImage(Image image)
      {
        var www = new WWW(image.Url);
        yield return www;

        if (!String.IsNullOrEmpty(www.error))
        {
          Instance.Puts($"Error while downloading image {image.Url}: {www.error}");
        }
        else if (www.bytes == null || www.bytes.Length == 0)
        {
          Instance.Puts($"Error while downloading image {image.Url}: No data received");
        }
        else
        {
          byte[] data = www.texture.EncodeToPNG();
          image.Save(data);
          DestroyImmediate(www.texture);
          Instance.Puts($"Stored {image.Url} as id {image.Id}");
          DownloadNext();
        }
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;

  public partial class Imperium
  {
    class MapMarker
    {
      public string IconUrl;
      public string Label;
      public float X;
      public float Z;

      public static MapMarker ForUser(User user)
      {
        return new MapMarker {
          IconUrl = Ui.MapIcon.Player,
          X = TranslatePosition(user.Player.transform.position.x),
          Z = TranslatePosition(user.Player.transform.position.z)
        };
      }

      public static MapMarker ForHeadquarters(Area area, Faction faction)
      {
        return new MapMarker {
          IconUrl = Ui.MapIcon.Headquarters,
          Label = Util.RemoveSpecialCharacters(faction.Id),
          X = TranslatePosition(area.ClaimCupboard.transform.position.x),
          Z = TranslatePosition(area.ClaimCupboard.transform.position.z)
        };
      }

      public static MapMarker ForMonument(MonumentInfo monument)
      {
        string iconUrl = GetIconForMonument(monument);
        return new MapMarker {
          IconUrl = iconUrl,
          Label = (iconUrl == Ui.MapIcon.Unknown) ? monument.name : null,
          X = TranslatePosition(monument.transform.position.x),
          Z = TranslatePosition(monument.transform.position.z)
        };
      }

      public static MapMarker ForPin(Pin pin)
      {
        string iconUrl = GetIconForPin(pin);
        return new MapMarker {
          IconUrl = iconUrl,
          Label = pin.Name,
          X = TranslatePosition(pin.Position.x),
          Z = TranslatePosition(pin.Position.z)
        };
      }

      static float TranslatePosition(float pos)
      {
        var mapSize = TerrainMeta.Size.x;
        return (pos + mapSize / 2f) / mapSize;
      }
      
      static string GetIconForMonument(MonumentInfo monument)
      {
        if (monument.Type == MonumentType.Cave) return Ui.MapIcon.Cave;
        if (monument.name.Contains("airfield")) return Ui.MapIcon.Airfield;
        if (monument.name.Contains("bandit_town")) return Ui.MapIcon.BanditTown;
        if (monument.name.Contains("compound")) return Ui.MapIcon.Compound;
        if (monument.name.Contains("sphere_tank")) return Ui.MapIcon.Dome;
        if (monument.name.Contains("harbor")) return Ui.MapIcon.Harbor;
        if (monument.name.Contains("gas_station")) return Ui.MapIcon.GasStation;
        if (monument.name.Contains("junkyard")) return Ui.MapIcon.Junkyard;
        if (monument.name.Contains("launch_site")) return Ui.MapIcon.LaunchSite;
        if (monument.name.Contains("lighthouse")) return Ui.MapIcon.Lighthouse;
        if (monument.name.Contains("military_tunnel")) return Ui.MapIcon.MilitaryTunnel;
        if (monument.name.Contains("warehouse")) return Ui.MapIcon.MiningOutpost;
        if (monument.name.Contains("powerplant")) return Ui.MapIcon.PowerPlant;
        if (monument.name.Contains("quarry")) return Ui.MapIcon.Quarry;
        if (monument.name.Contains("satellite_dish")) return Ui.MapIcon.SatelliteDish;
        if (monument.name.Contains("radtown_small_3")) return Ui.MapIcon.SewerBranch;
        if (monument.name.Contains("power_sub")) return Ui.MapIcon.Substation;
        if (monument.name.Contains("supermarket")) return Ui.MapIcon.Supermarket;
        if (monument.name.Contains("trainyard")) return Ui.MapIcon.Trainyard;
        if (monument.name.Contains("water_treatment_plant")) return Ui.MapIcon.WaterTreatmentPlant;
        return Ui.MapIcon.Unknown;
      }
    }

    static string GetIconForPin(Pin pin)
    {
      switch (pin.Type)
      {
        case PinType.Arena:
          return Ui.MapIcon.Arena;
        case PinType.Hotel:
          return Ui.MapIcon.Hotel;
        case PinType.Marina:
          return Ui.MapIcon.Marina;
        case PinType.Shop:
          return Ui.MapIcon.Shop;
        case PinType.Town:
          return Ui.MapIcon.Town;
        default:
          return Ui.MapIcon.Unknown;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    public static class Ui
    {
      public const string ImageBaseUrl = "http://assets.rustimperium.com/";
      public const string MapOverlayImageUrl = "imperium://map-overlay.png";
      public const string TransparentTexture = "assets/content/textures/generic/fulltransparent.tga";

      public static class Element
      {
        public const string Hud = "Hud";
        public const string HudPanelLeft = "Imperium.HudPanel.Top";
        public const string HudPanelRight = "Imperium.HudPanel.Middle";
        public const string HudPanelWarning = "Imperium.HudPanel.Warning";
        public const string HudPanelText = "Imperium.HudPanel.Text";
        public const string HudPanelIcon = "Imperium.HudPanel.Icon";
        public const string Overlay = "Overlay";
        public const string MapDialog = "Imperium.MapDialog";
        public const string MapHeader = "Imperium.MapDialog.Header";
        public const string MapHeaderTitle = "Imperium.MapDialog.Header.Title";
        public const string MapHeaderCloseButton = "Imperium.MapDialog.Header.CloseButton";
        public const string MapContainer = "Imperium.MapDialog.MapContainer";
        public const string MapTerrainImage = "Imperium.MapDialog.MapTerrainImage";
        public const string MapLayers = "Imperium.MapDialog.MapLayers";
        public const string MapClaimsImage = "Imperium.MapDialog.MapLayers.ClaimsImage";
        public const string MapMarkerIcon = "Imperium.MapDialog.MapLayers.MarkerIcon";
        public const string MapMarkerLabel = "Imperium.MapDialog.MapLayers.MarkerLabel";
        public const string MapSidebar = "Imperium.MapDialog.Sidebar";
        public const string MapButton = "Imperium.MapDialog.Sidebar.Button";
        public const string MapServerLogoImage = "Imperium.MapDialog.Sidebar.ServerLogo";
      }

      public static class HudIcon
      {
        public const string Badlands = ImageBaseUrl + "icons/hud/badlands.png";
        public const string CargoPlaneIndicatorOn = ImageBaseUrl + "icons/hud/cargoplane-on.png";
        public const string CargoPlaneIndicatorOff = ImageBaseUrl + "icons/hud/cargoplane-off.png";
        public const string CargoShipIndicatorOn = ImageBaseUrl + "icons/hud/cargo-ship-on.png";
        public const string CargoShipIndicatorOff = ImageBaseUrl + "icons/hud/cargo-ship-off.png";
        public const string ChinookIndicatorOn = ImageBaseUrl + "icons/hud/chinook-on.png";
        public const string ChinookIndicatorOff = ImageBaseUrl + "icons/hud/chinook-off.png";
        public const string Claimed = ImageBaseUrl + "icons/hud/claimed.png";
        public const string Clock = ImageBaseUrl + "icons/hud/clock.png";
        public const string Debris = ImageBaseUrl + "icons/hud/debris.png";
        public const string Defense = ImageBaseUrl + "icons/hud/defense.png";
        public const string Harvest = ImageBaseUrl + "icons/hud/harvest.png";
        public const string Headquarters = ImageBaseUrl + "icons/hud/headquarters.png";
        public const string HelicopterIndicatorOn = ImageBaseUrl + "icons/hud/helicopter-on.png";
        public const string HelicopterIndicatorOff = ImageBaseUrl + "icons/hud/helicopter-off.png";
        public const string Monument = ImageBaseUrl + "icons/hud/monument.png";
        public const string Players = ImageBaseUrl + "icons/hud/players.png";
        public const string PvpMode = ImageBaseUrl + "icons/hud/pvp.png";
        public const string Raid = ImageBaseUrl + "icons/hud/raid.png";
        public const string Ruins = ImageBaseUrl + "icons/hud/ruins.png";
        public const string Sleepers = ImageBaseUrl + "icons/hud/sleepers.png";
        public const string SupplyDrop = ImageBaseUrl + "icons/hud/supplydrop.png";
        public const string Taxes = ImageBaseUrl + "icons/hud/taxes.png";
        public const string Warning = ImageBaseUrl + "icons/hud/warning.png";
        public const string WarZone = ImageBaseUrl + "icons/hud/warzone.png";
        public const string Wilderness = ImageBaseUrl + "icons/hud/wilderness.png";
      }

      public static class MapIcon
      {
        public const string Airfield = ImageBaseUrl + "icons/map/airfield.png";
        public const string Arena = ImageBaseUrl + "icons/map/arena.png";
        public const string BanditTown = ImageBaseUrl + "icons/map/bandit-town.png";
        public const string Cave = ImageBaseUrl + "icons/map/cave.png";
        public const string Compound = ImageBaseUrl + "icons/map/compound.png";
        public const string Dome = ImageBaseUrl + "icons/map/dome.png";
        public const string GasStation = ImageBaseUrl + "icons/map/gas-station.png";
        public const string Harbor = ImageBaseUrl + "icons/map/harbor.png";
        public const string Headquarters = ImageBaseUrl + "icons/map/headquarters.png";
        public const string Hotel = ImageBaseUrl + "icons/map/hotel.png";
        public const string Junkyard = ImageBaseUrl + "icons/map/junkyard.png";
        public const string LaunchSite = ImageBaseUrl + "icons/map/launch-site.png";
        public const string Lighthouse = ImageBaseUrl + "icons/map/lighthouse.png";
        public const string Marina = ImageBaseUrl + "icons/map/marina.png";
        public const string MilitaryTunnel = ImageBaseUrl + "icons/map/military-tunnel.png";
        public const string MiningOutpost = ImageBaseUrl + "icons/map/mining-outpost.png";
        public const string Player = ImageBaseUrl + "icons/map/player.png";
        public const string PowerPlant = ImageBaseUrl + "icons/map/power-plant.png";
        public const string Quarry = ImageBaseUrl + "icons/map/quarry.png";
        public const string SatelliteDish = ImageBaseUrl + "icons/map/satellite-dish.png";
        public const string SewerBranch = ImageBaseUrl + "icons/map/sewer-branch.png";
        public const string Shop = ImageBaseUrl + "icons/map/shop.png";
        public const string Substation = ImageBaseUrl + "icons/map/substation.png";
        public const string Supermarket = ImageBaseUrl + "icons/map/supermarket.png";
        public const string Town = ImageBaseUrl + "icons/map/town.png";
        public const string Trainyard = ImageBaseUrl + "icons/map/trainyard.png";
        public const string Unknown = ImageBaseUrl + "icons/map/unknown.png";
        public const string WaterTreatmentPlant = ImageBaseUrl + "icons/map/water-treatment-plant.png";
      }
    }
  }
}

﻿namespace Oxide.Plugins
{
  using Oxide.Game.Rust.Cui;
  using System;
  using System.Linq;
  using UnityEngine;

  public partial class Imperium
  {
    class UserHud
    {
      const float IconSize = 0.075f;

      static class PanelColor
      {
        public const string BackgroundNormal = "1 0.95 0.875 0.06";
        public const string BackgroundDanger = "0.77 0.25 0.17 0.75";
        public const string BackgroundSafe = "0.31 0.37 0.20 0.75";
        public const string TextNormal = "0.85 0.85 0.85 1";
        public const string TextDanger = "0.85 0.65 0.65 1";
        public const string TextSafe = "0.67 0.89 0.32 1";
      }

      public User User { get; }
      public bool IsDisabled { get; set; }

      public UserHud(User user)
      {
        User = user;
      }

      public void Show()
      {
        if (User.CurrentArea != null)
          CuiHelper.AddUi(User.Player, Build());
      }

      public void Hide()
      {
        CuiHelper.DestroyUi(User.Player, Ui.Element.HudPanelLeft);
        CuiHelper.DestroyUi(User.Player, Ui.Element.HudPanelRight);
        CuiHelper.DestroyUi(User.Player, Ui.Element.HudPanelWarning);
      }

      public void Toggle()
      {
        if (IsDisabled)
        {
          IsDisabled = false;
          Show();
        }
        else
        {
          IsDisabled = true;
          Hide();
        }
      }

      public void Refresh()
      {
        if (IsDisabled)
          return;

        Hide();
        Show();
      }

      CuiElementContainer Build()
      {
        var container = new CuiElementContainer();

        Area area = User.CurrentArea;

        container.Add(new CuiPanel {
          Image = { Color = GetLeftPanelBackgroundColor() },
          RectTransform = { AnchorMin = "0.006 0.956", AnchorMax = "0.217 0.989" }
        }, Ui.Element.Hud, Ui.Element.HudPanelLeft);

        container.Add(new CuiPanel {
          Image = { Color = PanelColor.BackgroundNormal },
          RectTransform = { AnchorMin = "0.783 0.956", AnchorMax = "0.994 0.989" }
        }, Ui.Element.Hud, Ui.Element.HudPanelRight);

        AddWidget(container, Ui.Element.HudPanelLeft, GetLocationIcon(), GetLeftPanelTextColor(), GetLocationDescription());

        if (area.Type == AreaType.Badlands)
        {
          string harvestBonus = String.Format("+{0}%", Instance.Options.Taxes.BadlandsGatherBonus * 100);
          AddWidget(container, Ui.Element.HudPanelLeft, Ui.HudIcon.Harvest, GetLeftPanelTextColor(), harvestBonus, 0.8f);
        }
        else if (area.IsWarZone)
        {
          string defensiveBonus = String.Format("+{0}%", area.GetDefensiveBonus() * 100);
          AddWidget(container, Ui.Element.HudPanelLeft, Ui.HudIcon.Defense, GetLeftPanelTextColor(), defensiveBonus, 0.8f);
        }
        else
        {
          string taxRate = String.Format("{0}%", area.GetTaxRate() * 100);
          AddWidget(container, Ui.Element.HudPanelLeft, Ui.HudIcon.Taxes, GetLeftPanelTextColor(), taxRate, 0.8f);
        }

        string planeIcon = Instance.Hud.GameEvents.IsCargoPlaneActive ? Ui.HudIcon.CargoPlaneIndicatorOn : Ui.HudIcon.CargoPlaneIndicatorOff;
        AddWidget(container, Ui.Element.HudPanelRight, planeIcon);

        string shipIcon = Instance.Hud.GameEvents.IsCargoShipActive ? Ui.HudIcon.CargoShipIndicatorOn : Ui.HudIcon.CargoShipIndicatorOff;
        AddWidget(container, Ui.Element.HudPanelRight, shipIcon, 0.1f);

        string heliIcon = Instance.Hud.GameEvents.IsHelicopterActive ? Ui.HudIcon.HelicopterIndicatorOn : Ui.HudIcon.HelicopterIndicatorOff;
        AddWidget(container, Ui.Element.HudPanelRight, heliIcon, 0.2f);

        string chinookIcon = Instance.Hud.GameEvents.IsChinookOrLockedCrateActive ? Ui.HudIcon.ChinookIndicatorOn : Ui.HudIcon.ChinookIndicatorOff;
        AddWidget(container, Ui.Element.HudPanelRight, chinookIcon, 0.3f);

        string activePlayers = BasePlayer.activePlayerList.Count.ToString();
        AddWidget(container, Ui.Element.HudPanelRight, Ui.HudIcon.Players, PanelColor.TextNormal, activePlayers, 0.45f);

        string sleepingPlayers = BasePlayer.sleepingPlayerList.Count.ToString();
        AddWidget(container, Ui.Element.HudPanelRight, Ui.HudIcon.Sleepers, PanelColor.TextNormal, sleepingPlayers, 0.625f);

        string currentTime = TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm");
        AddWidget(container, Ui.Element.HudPanelRight, Ui.HudIcon.Clock, PanelColor.TextNormal, currentTime, 0.79f);

        bool claimUpkeepPastDue = Instance.Options.Upkeep.Enabled && User.Faction != null && User.Faction.IsUpkeepPastDue;
        if (User.IsInPvpMode || claimUpkeepPastDue)
        {
          container.Add(new CuiPanel {
            Image = { Color = PanelColor.BackgroundDanger },
            //RectTransform = { AnchorMin = "0.006 0.911", AnchorMax = "0.217 0.944" }
            RectTransform = { AnchorMin = "0.783 0.911", AnchorMax = "0.994 0.944" }
          }, Ui.Element.Hud, Ui.Element.HudPanelWarning);

          if (true || claimUpkeepPastDue)
            AddWidget(container, Ui.Element.HudPanelWarning, Ui.HudIcon.Ruins, PanelColor.TextDanger, "Claim upkeep past due!");
          else
            AddWidget(container, Ui.Element.HudPanelWarning, Ui.HudIcon.PvpMode, PanelColor.TextDanger, "PVP mode enabled");
        }

        return container;
      }

      string GetRightPanelBackgroundColor()
      {
        if (User.IsInPvpMode)
          return PanelColor.BackgroundDanger;
        else
          return PanelColor.BackgroundNormal;
      }

      string GetRightPanelTextColor()
      {
        if (User.IsInPvpMode)
          return PanelColor.TextDanger;
        else
          return PanelColor.TextNormal;
      }

      string GetLocationIcon()
      {
        Zone zone = User.CurrentZones.FirstOrDefault();

        if (zone != null)
        {
          switch (zone.Type)
          {
            case ZoneType.SupplyDrop:
              return Ui.HudIcon.SupplyDrop;
            case ZoneType.Debris:
              return Ui.HudIcon.Debris;
            case ZoneType.Monument:
              return Ui.HudIcon.Monument;
            case ZoneType.Raid:
              return Ui.HudIcon.Raid;
          }
        }

        Area area = User.CurrentArea;

        if (area.IsWarZone)
          return Ui.HudIcon.WarZone;

        switch (area.Type)
        {
          case AreaType.Badlands:
            return Ui.HudIcon.Badlands;
          case AreaType.Claimed:
            return Ui.HudIcon.Claimed;
          case AreaType.Headquarters:
            return Ui.HudIcon.Headquarters;
          default:
            return Ui.HudIcon.Wilderness;
        }
      }

      string GetLocationDescription()
      {
        Area area = User.CurrentArea;
        Zone zone = User.CurrentZones.FirstOrDefault();

        if (zone != null)
          return $"{area.Id}: {zone.Name}";

        switch (area.Type)
        {
          case AreaType.Badlands:
            return $"{area.Id}: Badlands";
          case AreaType.Claimed:
            if (!String.IsNullOrEmpty(area.Name))
              return $"{area.Id}: {area.Name} ({area.FactionId})";
            else
              return $"{area.Id}: {area.FactionId} Territory";
          case AreaType.Headquarters:
            if (!String.IsNullOrEmpty(area.Name))
              return $"{area.Id}: {area.Name} ({area.FactionId} HQ)";
            else
              return $"{area.Id}: {area.FactionId} Headquarters";
          default:
            return $"{area.Id}: Wilderness";
        }
      }

      string GetLeftPanelBackgroundColor()
      {
        if (User.CurrentZones.Count > 0)
          return PanelColor.BackgroundDanger;

        Area area = User.CurrentArea;

        if (area.IsWarZone || area.Type == AreaType.Badlands)
          return PanelColor.BackgroundDanger;
        else
          return PanelColor.BackgroundNormal;
      }

      string GetLeftPanelTextColor()
      {
        if (User.CurrentZones.Count > 0)
          return PanelColor.TextDanger;

        Area area = User.CurrentArea;

        if (area.IsWarZone || area.Type == AreaType.Badlands)
          return PanelColor.TextDanger;
        else
          return PanelColor.TextNormal;
      }

      void AddWidget(CuiElementContainer container, string parent, string iconName, string textColor, string text, float left = 0f)
      {
        var guid = Guid.NewGuid().ToString();

        container.Add(new CuiElement {
          Name = Ui.Element.HudPanelIcon + guid,
          Parent = parent,
          Components = {
            Instance.Hud.CreateImageComponent(iconName),
            new CuiRectTransformComponent {
              AnchorMin = $"{left} {IconSize}",
              AnchorMax = $"{left + IconSize} {1 - IconSize}",
              OffsetMin = "6 0",
              OffsetMax = "6 0"
            }
          }
        });

        container.Add(new CuiLabel {
          Text = {
            Text = text,
            Color = textColor,
            FontSize = 13,
            Align = TextAnchor.MiddleLeft
          },
          RectTransform = {
            AnchorMin = $"{left + IconSize} 0",
            AnchorMax = "1 1",
            OffsetMin = "11 0",
            OffsetMax = "11 0"
          }
        }, parent, Ui.Element.HudPanelText + guid);
      }

      void AddWidget(CuiElementContainer container, string parent, string iconName, float left = 0f)
      {
        var guid = Guid.NewGuid().ToString();

        container.Add(new CuiElement {
          Name = Ui.Element.HudPanelIcon + guid,
          Parent = parent,
          Components = {
            Instance.Hud.CreateImageComponent(iconName),
            new CuiRectTransformComponent {
              AnchorMin = $"{left} {IconSize}",
              AnchorMax = $"{left + IconSize} {1 - IconSize}",
              OffsetMin = "6 0",
              OffsetMax = "6 0"
            }
          }
        });
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;
  using Oxide.Game.Rust.Cui;
  using UnityEngine;
  using System.Linq;

  public partial class Imperium
  {
    class UserMap
    {
      public User User { get; }
      public bool IsVisible { get; private set; }

      public UserMap(User user)
      {
        User = user;
      }

      public void Show()
      {
        CuiHelper.AddUi(User.Player, BuildDialog());
        CuiHelper.AddUi(User.Player, BuildSidebar());
        CuiHelper.AddUi(User.Player, BuildMapLayers());
        IsVisible = true;
      }

      public void Hide()
      {
        CuiHelper.DestroyUi(User.Player, Ui.Element.MapDialog);
        IsVisible = false;
      }

      public void Toggle()
      {
        if (IsVisible)
          Hide();
        else
          Show();
      }

      public void Refresh()
      {
        if (IsVisible)
        {
          CuiHelper.DestroyUi(User.Player, Ui.Element.MapSidebar);
          CuiHelper.DestroyUi(User.Player, Ui.Element.MapLayers);
          CuiHelper.AddUi(User.Player, BuildSidebar());
          CuiHelper.AddUi(User.Player, BuildMapLayers());
        }
      }

      // --- Dialog ---

      CuiElementContainer BuildDialog()
      {
        var container = new CuiElementContainer();

        container.Add(new CuiPanel {
          Image = { Color = "0 0 0 0.75" },
          RectTransform = { AnchorMin = "0.164 0.014", AnchorMax = "0.836 0.986" },
          CursorEnabled = true
        }, Ui.Element.Overlay, Ui.Element.MapDialog);

        container.Add(new CuiPanel {
          Image = { Color = "0 0 0 0" },
          RectTransform = { AnchorMin = "0.012 0.014", AnchorMax = "0.774 0.951" }
        }, Ui.Element.MapDialog, Ui.Element.MapContainer);

        AddDialogHeader(container);
        AddMapTerrainImage(container);

        return container;
      }

      void AddDialogHeader(CuiElementContainer container)
      {
        container.Add(new CuiPanel {
          Image = { Color = "0 0 0 1" },
          RectTransform = { AnchorMin = "0 0.966", AnchorMax = "0.999 0.999" }
        }, Ui.Element.MapDialog, Ui.Element.MapHeader);

        container.Add(new CuiLabel {
          Text = { Text = ConVar.Server.hostname, FontSize = 13, Align = TextAnchor.MiddleLeft, FadeIn = 0 },
          RectTransform = { AnchorMin = "0.012 0.025", AnchorMax = "0.099 0.917" }
        }, Ui.Element.MapHeader, Ui.Element.MapHeaderTitle);

        container.Add(new CuiButton {
          Text = { Text = "X", FontSize = 13, Align = TextAnchor.MiddleCenter },
          Button = { Color = "0 0 0 0", Command = "imperium.map.toggle", FadeIn = 0 },
          RectTransform = { AnchorMin = "0.972 0.083", AnchorMax = "0.995 0.917" }
        }, Ui.Element.MapHeader, Ui.Element.MapHeaderCloseButton);
      }

      void AddMapTerrainImage(CuiElementContainer container)
      {
        CuiRawImageComponent image = Instance.Hud.CreateImageComponent(Instance.Options.Map.ImageUrl);

        // If the image hasn't been loaded, just display a black box so we don't cause an RPC AddUI crash.
        if (image == null)
          image = new CuiRawImageComponent { Color = "0 0 0 1" };

        container.Add(new CuiElement {
          Name = Ui.Element.MapTerrainImage,
          Parent = Ui.Element.MapContainer,
          Components = {
            image,
            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
          }
        });
      }

      // --- Sidebar ---

      CuiElementContainer BuildSidebar()
      {
        var container = new CuiElementContainer();

        container.Add(new CuiPanel {
          Image = { Color = "0 0 0 0" },
          RectTransform = { AnchorMin = "0.786 0.014", AnchorMax = "0.988 0.951" }
        }, Ui.Element.MapDialog, Ui.Element.MapSidebar);

        AddLayerToggleButtons(container);
        AddServerLogo(container);

        return container;
      }

      void AddLayerToggleButtons(CuiElementContainer container)
      {
        container.Add(new CuiButton {
          Text = { Text = "Land Claims", FontSize = 14, Align = TextAnchor.MiddleCenter },
          Button = { Color = GetButtonColor(UserMapLayer.Claims), Command = "imperium.map.togglelayer claims", FadeIn = 0 },
          RectTransform = { AnchorMin = "0 0.924", AnchorMax = "1 1" }
        }, Ui.Element.MapSidebar, Ui.Element.MapButton + Guid.NewGuid().ToString());

        container.Add(new CuiButton {
          Text = { Text = "Faction Headquarters", FontSize = 14, Align = TextAnchor.MiddleCenter },
          Button = { Color = GetButtonColor(UserMapLayer.Headquarters), Command = "imperium.map.togglelayer headquarters", FadeIn = 0 },
          RectTransform = { AnchorMin = "0 0.832", AnchorMax = "1 0.909" }
        }, Ui.Element.MapSidebar, Ui.Element.MapButton + Guid.NewGuid().ToString());

        container.Add(new CuiButton {
          Text = { Text = "Monuments", FontSize = 14, Align = TextAnchor.MiddleCenter },
          Button = { Color = GetButtonColor(UserMapLayer.Monuments), Command = "imperium.map.togglelayer monuments", FadeIn = 0 },
          RectTransform = { AnchorMin = "0 0.741", AnchorMax = "1 0.817" }
        }, Ui.Element.MapSidebar, Ui.Element.MapButton + Guid.NewGuid().ToString());

        container.Add(new CuiButton {
          Text = { Text = "Pins", FontSize = 14, Align = TextAnchor.MiddleCenter },
          Button = { Color = GetButtonColor(UserMapLayer.Pins), Command = "imperium.map.togglelayer pins", FadeIn = 0 },
          RectTransform = { AnchorMin = "0 0.649", AnchorMax = "1 0.726" }
        }, Ui.Element.MapSidebar, Ui.Element.MapButton + Guid.NewGuid().ToString());
      }

      void AddServerLogo(CuiElementContainer container)
      {
        CuiRawImageComponent image = Instance.Hud.CreateImageComponent(Instance.Options.Map.ServerLogoUrl);

        // If the image hasn't been loaded, just display a black box so we don't cause an RPC AddUI crash.
        if (image == null)
          image = new CuiRawImageComponent { Color = "0 0 0 1" };

        container.Add(new CuiElement {
          Name = Ui.Element.MapServerLogoImage,
          Parent = Ui.Element.MapSidebar,
          Components = {
            image,
            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 0.133" }
          }
        });
      }

      // --- Map Layers ---

      CuiElementContainer BuildMapLayers()
      {
        var container = new CuiElementContainer();

        container.Add(new CuiPanel {
          Image = { Color = "0 0 0 0" },
          RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
        }, Ui.Element.MapContainer, Ui.Element.MapLayers);

        if (User.Preferences.IsMapLayerVisible(UserMapLayer.Claims))
          AddClaimsLayer(container);

        if (User.Preferences.IsMapLayerVisible(UserMapLayer.Monuments))
          AddMonumentsLayer(container);

        if (User.Preferences.IsMapLayerVisible(UserMapLayer.Headquarters))
          AddHeadquartersLayer(container);

        if (User.Preferences.IsMapLayerVisible(UserMapLayer.Pins))
          AddPinsLayer(container);

        AddMarker(container, MapMarker.ForUser(User));

        return container;
      }

      void AddClaimsLayer(CuiElementContainer container)
      {
        CuiRawImageComponent image = Instance.Hud.CreateImageComponent(Ui.MapOverlayImageUrl);

        // If the claims overlay hasn't been generated yet, just display a black box so we don't cause an RPC AddUI crash.
        if (image == null)
          image = new CuiRawImageComponent { Color = "0 0 0 1" };

        container.Add(new CuiElement {
          Name = Ui.Element.MapClaimsImage,
          Parent = Ui.Element.MapLayers,
          Components = {
            image,
            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
          }
        });
      }

      void AddMonumentsLayer(CuiElementContainer container)
      {
        var monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>();
        foreach (MonumentInfo monument in monuments.Where(ShowMonumentOnMap))
          AddMarker(container, MapMarker.ForMonument(monument));
      }

      void AddHeadquartersLayer(CuiElementContainer container)
      {
        foreach (Area area in Instance.Areas.GetAllByType(AreaType.Headquarters))
        {
          var faction = Instance.Factions.Get(area.FactionId);
          AddMarker(container, MapMarker.ForHeadquarters(area, faction));
        }
      }

      void AddPinsLayer(CuiElementContainer container)
      {
        foreach (Pin pin in Instance.Pins.GetAll())
          AddMarker(container, MapMarker.ForPin(pin));
      }

      void AddMarker(CuiElementContainer container, MapMarker marker, float iconSize = 0.01f)
      {
        container.Add(new CuiElement {
          Name = Ui.Element.MapMarkerIcon + Guid.NewGuid().ToString(),
          Parent = Ui.Element.MapLayers,
          Components = {
            Instance.Hud.CreateImageComponent(marker.IconUrl),
            new CuiRectTransformComponent {
              AnchorMin = $"{marker.X - iconSize} {marker.Z - iconSize}",
              AnchorMax = $"{marker.X + iconSize} {marker.Z + iconSize}"
            }
          }
        });

        if (!String.IsNullOrEmpty(marker.Label))
        {
          container.Add(new CuiLabel {
            Text = { Text = marker.Label, FontSize = 8, Align = TextAnchor.MiddleCenter, FadeIn = 0 },
            RectTransform = {
              AnchorMin = $"{marker.X - 0.1} {marker.Z - iconSize - 0.0175}",
              AnchorMax = $"{marker.X + 0.1} {marker.Z - iconSize}"
            }
          }, Ui.Element.MapLayers, Ui.Element.MapMarkerLabel + Guid.NewGuid().ToString());
        }
      }

      bool ShowMonumentOnMap(MonumentInfo monument)
      {
        return monument.Type != MonumentType.Cave
          && !monument.name.Contains("power_sub")
          && !monument.name.Contains("water_well")
          && !monument.name.Contains("swamp")
          && !monument.name.Contains("ice_lake");
      }

      string GetButtonColor(UserMapLayer layer)
      {
        if (User.Preferences.IsMapLayerVisible(layer))
          return "0 0 0 1";
        else
          return "0.33 0.33 0.33 1";
      }
    }
  }
}
