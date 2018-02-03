// Reference: System.Drawing

/*
 * Copyright (C) 2017 chucklenugget
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
 * associated documentation files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of the Software, and to permit persons to whom the Software
 * is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
 * BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace Oxide.Plugins
{
  using System;
  using System.IO;
  using Oxide.Core;
  using Oxide.Core.Configuration;
  using UnityEngine;
  using System.Collections.Generic;

  [Info("Imperium", "chucklenugget", "1.4.1")]
  public partial class Imperium : RustPlugin
  {
    static Imperium Instance;

    DynamicConfigFile AreasFile;
    DynamicConfigFile FactionsFile;
    DynamicConfigFile WarsFile;

    GameObject GameObject;
    ImperiumOptions Options;
    Timer UpkeepCollectionTimer;

    AreaManager Areas;
    FactionManager Factions;
    HudManager Hud;
    UserManager Users;
    WarManager Wars;
    ZoneManager Zones;

    const string PERM_CHANGE_FACTIONS = "imperium.factions";
    const string PERM_CHANGE_CLAIMS = "imperium.claims";
    const string PERM_CHANGE_BADLANDS = "imperium.badlands";
    const string PERM_CHANGE_TOWNS = "imperium.towns";

    void Init()
    {
      Instance = this;
      GameObject = new GameObject();

      AreasFile = GetDataFile("areas");
      FactionsFile = GetDataFile("factions");
      WarsFile = GetDataFile("wars");

      Areas = new AreaManager();
      Factions = new FactionManager();
      Hud = new HudManager();
      Users = new UserManager();
      Wars = new WarManager();
      Zones = new ZoneManager();

      PrintToChat($"{Title} v{Version} initialized.");
    }

    void Loaded()
    {
      InitLang();

      try
      {
        Options = Config.ReadObject<ImperiumOptions>();
      }
      catch (Exception ex)
      {
        PrintError($"Error while loading configuration: {ex.ToString()}");
      }

      Puts("Area claims are " + (Options.EnableAreaClaims ? "enabled" : "disabled"));
      Puts("Taxation is " + (Options.EnableTaxation ? "enabled" : "disabled"));
      Puts("Badlands are " + (Options.EnableBadlands ? "enabled" : "disabled"));
      Puts("Towns are " + (Options.EnableTowns ? "enabled" : "disabled"));
      Puts("Defensive bonuses are " + (Options.EnableDefensiveBonuses ? "enabled" : "disabled"));
      Puts("Decay reduction is " + (Options.EnableDecayReduction ? "enabled" : "disabled"));
      Puts("Claim upkeep is " + (Options.EnableUpkeep ? "enabled" : "disabled"));
      Puts("Restricted PVP is " + (Options.EnableRestrictedPVP ? "enabled" : "disabled"));
      Puts("Monument zones are " + (Options.EnableMonumentZones ? "enabled" : "disabled"));
      Puts("Event zones are " + (Options.EnableEventZones ? "enabled" : "disabled"));
    }

    void OnServerInitialized()
    {
      permission.RegisterPermission(PERM_CHANGE_BADLANDS, this);
      permission.RegisterPermission(PERM_CHANGE_CLAIMS, this);
      permission.RegisterPermission(PERM_CHANGE_TOWNS, this);

      Factions.Init(TryLoad<FactionInfo>(FactionsFile));
      Areas.Init(TryLoad<AreaInfo>(AreasFile));
      Users.Init();
      Wars.Init(TryLoad<WarInfo>(WarsFile));
      Zones.Init();
      Hud.Init();

      NextTick(() => {
        Hud.GenerateMapOverlayImage();
      });

      if (Options.EnableUpkeep)
        UpkeepCollectionTimer = timer.Every(Options.UpkeepCheckIntervalMinutes * 60, CollectUpkeepForAllFactions);
    }

    void OnServerSave()
    {
      AreasFile.WriteObject(Areas.Serialize());
      FactionsFile.WriteObject(Factions.Serialize());
      WarsFile.WriteObject(Wars.Serialize());
    }

    void Unload()
    {
      Hud.Destroy();
      Zones.Destroy();
      Users.Destroy();
      Wars.Destroy();
      Areas.Destroy();
      Factions.Destroy();

      if (UpkeepCollectionTimer != null && !UpkeepCollectionTimer.Destroyed)
        UpkeepCollectionTimer.Destroy();

      if (GameObject != null)
        UnityEngine.Object.Destroy(GameObject);

      Instance = null;
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

    bool EnsureCanChangeFactionClaims(User user, Faction faction)
    {
      if (faction == null || !faction.HasLeader(user))
      {
        user.SendChatMessage(Messages.NotLeaderOfFaction);
        return false;
      }

      if (faction.MemberIds.Count < Options.MinFactionMembers)
      {
        user.SendChatMessage(Messages.FactionTooSmall, Options.MinFactionMembers);
        return false;
      }

      return true;
    }

    bool EnsureCanUseCupboardAsClaim(User user, BuildingPrivlidge cupboard)
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

    bool EnsureCanManageTowns(User user, Faction faction)
    {
      if (!user.HasPermission(PERM_CHANGE_TOWNS))
      {
        user.SendChatMessage(Messages.NoPermission);
        return false;
      }

      if (faction == null)
      {
        user.SendChatMessage(Messages.NotMemberOfFaction);
        return false;
      }

      return true;
    }

    bool EnsureCanUseCupboardAsTown(User user, BuildingPrivlidge cupboard)
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

    bool EnsureCanEngageInDiplomacy(User user, Faction faction)
    {
      if (faction == null)
      {
        user.SendChatMessage(Messages.NotMemberOfFaction);
        return false;
      }

      if (faction.MemberIds.Count < Options.MinFactionMembers)
      {
        user.SendChatMessage(Messages.FactionTooSmall);
        return false;
      }

      if (Areas.GetAllClaimedByFaction(faction).Length == 0)
      {
        user.SendChatMessage(Messages.FactionDoesNotOwnLand);
        return false;
      }

      return true;
    }

    bool EnforceCommandCooldown(User user)
    {
      int waitSeconds = user.GetSecondsUntilNextCommand();

      if (waitSeconds > 0)
      {
        user.SendChatMessage(Messages.CommandIsOnCooldown, waitSeconds);
        return false;
      }

      user.CommandCooldownExpirationTime = DateTime.UtcNow.AddSeconds(Options.CommandCooldownSeconds);
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

      if (Options.EnableTaxation)
        sb.AppendLine("<color=#ffd479>/tax</color> Manage taxation of your land");

      if (Options.EnableWar)
        sb.AppendLine("<color=#ffd479>/war</color> See active wars, declare war, or offer peace");

      if (Options.EnableTowns)
      {
        if (user.HasPermission(PERM_CHANGE_TOWNS))
          sb.AppendLine("<color=#ffd479>/town</color> Find nearby towns, or create one yourself");
        else
          sb.AppendLine("<color=#ffd479>/town</color> Find nearby towns");
      }

      if (Options.EnableBadlands)
      {
        if (user.HasPermission(PERM_CHANGE_BADLANDS))
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
  using System.Linq;

  public partial class Imperium
  {
    [ChatCommand("badlands")]
    void OnBadlandsCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.EnableBadlands)
      {
        user.SendChatMessage(Messages.BadlandsDisabled);
        return;
      }

      if (args.Length == 0)
      {
        var areas = Areas.GetAllByType(AreaType.Badlands).Select(a => a.Id);
        user.SendChatMessage(Messages.BadlandsList, Util.Format(areas), Options.BadlandsGatherBonus);
        return;
      }

      if (!permission.UserHasPermission(player.UserIDString, PERM_CHANGE_BADLANDS))
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

      if (!Options.EnableAreaClaims)
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

      if (!EnsureCanChangeFactionClaims(user, faction))
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
      if (!user.HasPermission(PERM_CHANGE_CLAIMS))
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

      if (faction.MemberIds.Count < Options.MinFactionMembers)
      {
        user.SendChatMessage(Messages.FactionTooSmall, Options.MinFactionMembers);
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
  using System.Linq;

  public partial class Imperium
  {
    void OnClaimDeleteCommand(User user, string[] args)
    {
      if (args.Length == 0)
      {
        user.SendChatMessage(Messages.Usage, "/claim delete XY [XY XY...]");
        return;
      }

      if (!user.HasPermission(PERM_CHANGE_CLAIMS))
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

      if (!EnsureCanChangeFactionClaims(user, sourceFaction))
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

      if (!EnsureCanChangeFactionClaims(user, faction))
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

      if (!Options.EnableUpkeep)
        sb.AppendLine("  <color=#ffd479>/claim upkeep</color>: Show information about upkeep costs for your faction");

      sb.AppendLine("  <color=#ffd479>/claim help</color>: Prints this message");

      if (user.HasPermission(PERM_CHANGE_CLAIMS))
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

      if (!EnsureCanChangeFactionClaims(user, faction))
        return;

      user.SendChatMessage(Messages.SelectClaimCupboardToRemove);
      user.BeginInteraction(new RemovingClaimInteraction(faction));
    }
  }
}﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimRenameCommand(User user, string[] args)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureCanChangeFactionClaims(user, faction))
        return;

      if (args.Length != 2)
      {
        user.SendChatMessage(Messages.Usage, "/claim rename XY \"NAME\"");
        return;
      }

      var areaId = Util.NormalizeAreaId(args[0]);
      var name = Util.NormalizeAreaName(args[1]);

      if (name == null || name.Length < Options.MinAreaNameLength)
      {
        user.SendChatMessage(Messages.InvalidAreaName, Options.MinAreaNameLength);
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

      if (area.Type == AreaType.Town)
      {
        user.SendChatMessage(Messages.CannotRenameAreaIsTown, area.Id, area.Name);
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
        case AreaType.Town:
          user.SendChatMessage(Messages.AreaIsTown, area.Id, area.Name, area.FactionId);
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
      if (!Options.EnableUpkeep)
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

      if (faction.MemberIds.Count < Options.MinFactionMembers)
      {
        user.SendChatMessage(Messages.FactionTooSmall, Options.MinFactionMembers);
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
  using System.Text.RegularExpressions;

  public partial class Imperium
  {
    void OnFactionCreateCommand(User user, string[] args)
    {
      var idRegex = new Regex("^[a-zA-Z0-9]{2,6}$");

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

      string id = args[0].Trim();

      if (!idRegex.IsMatch(id))
      {
        user.SendChatMessage(Messages.InvalidFactionName);
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
      sb.AppendLine("  <color=#ffd479>/faction create</color>: Create a new faction");
      sb.AppendLine("  <color=#ffd479>/faction join FACTION</color>: Join a faction if you have been invited");
      sb.AppendLine("  <color=#ffd479>/faction leave</color>: Leave your current faction");

      sb.AppendLine("  <color=#ffd479>/faction invite \"PLAYER\"</color>: Invite another player to join your faction");
      sb.AppendLine("  <color=#ffd479>/faction kick \"PLAYER\"</color>: Kick a player out of your faction");
      sb.AppendLine("  <color=#ffd479>/faction promote \"PLAYER\"</color>: Promote a faction member to manager");
      sb.AppendLine("  <color=#ffd479>/faction demote \"PLAYER\"</color>: Remove a faction member as manager");
      sb.AppendLine("  <color=#ffd479>/faction disband forever</color>: Disband your faction immediately (no undo!)");

      sb.AppendLine("  <color=#ffd479>/faction help</color>: Prints this message");

      if (user.HasPermission(PERM_CHANGE_FACTIONS))
      {
        sb.AppendLine("Admin commands:");
        sb.AppendLine("  <color=#ffd479>/faction force promote FACTION \"PLAYER\"</color>: Forcibly promote a member of a faction to manager");
        sb.AppendLine("  <color=#ffd479>/faction force demote FACTION \"PLAYER\"</color>: Forcibly promote a member of a faction to manager");
        sb.AppendLine("  <color=#ffd479>/faction force delete FACTION</color>: Delete a faction (no undo!)");
      }

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

      if (faction.MemberIds.Count == 1)
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

      sb.AppendLine($"<color=#ffd479>{faction.MemberIds.Count}</color> member(s), <color=#ffd479>{activeMembers.Length}</color> online:");
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
    [ChatCommand("tax")]
    void OnTaxCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.EnableTaxation)
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
        user.SendChatMessage(Messages.CannotSetTaxRateInvalidValue, Options.MaxTaxRate * 100);
        return;
      }

      if (taxRate < 0 || taxRate > Options.MaxTaxRate)
      {
        user.SendChatMessage(Messages.CannotSetTaxRateInvalidValue, Options.MaxTaxRate * 100);
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
  using System.Linq;

  public partial class Imperium
  {
    [ChatCommand("town")]
    void OnTownCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.EnableTowns)
      {
        user.SendChatMessage(Messages.TownsDisabled);
        return;
      }

      if (args.Length == 0)
      {
        OnTownHelpCommand(user);
        return;
      }

      var restArguments = args.Skip(1).ToArray();

      switch (args[0].ToLower())
      {
        case "create":
          OnTownCreateCommand(user, restArguments);
          break;
        case "expand":
          OnTownExpandCommand(user);
          break;
        case "remove":
          OnTownRemoveCommand(user);
          break;
        case "disband":
          OnTownDisbandCommand(user);
          break;
        case "list":
          OnTownListCommand(user);
          break;
        default:
          OnTownHelpCommand(user);
          break;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnTownCreateCommand(User user, string[] args)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureCanManageTowns(user, faction))
        return;

      if (args.Length == 0)
      {
        user.SendChatMessage(Messages.Usage, "/town create NAME");
        return;
      }

      Town town = Areas.GetTownByMayor(user);
      if (town != null)
      {
        user.SendChatMessage(Messages.CannotCreateTownAlreadyMayor, town.Name);
        return;
      }

      var name = Util.NormalizeAreaName(args[0]);

      town = Areas.GetTown(name);
      if (town != null)
      {
        user.SendChatMessage(Messages.CannotCreateTownSameNameAlreadyExists, town.Name);
        return;
      }

      user.SendChatMessage(Messages.SelectTownCupboardToCreate, name);
      user.BeginInteraction(new CreatingTownInteraction(faction, name));
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnTownDisbandCommand(User user)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureCanManageTowns(user, faction))
        return;

      Town town = Areas.GetTownByMayor(user);
      if (town == null)
      {
        user.SendChatMessage(Messages.NotMayorOfTown);
        return;
      }

      PrintToChat(Messages.TownDisbandedAnnouncement, faction.Id, town.Name);
      Log($"{Util.Format(user)} disbanded the town faction {town.Name}");

      foreach (Area area in town.Areas)
        Areas.RemoveFromTown(area);
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnTownExpandCommand(User user)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureCanManageTowns(user, faction))
        return;

      Town town = Areas.GetTownByMayor(user);
      if (town == null)
      {
        user.SendChatMessage(Messages.NotMayorOfTown);
        return;
      }

      user.SendChatMessage(Messages.SelectTownCupboardToExpand, town.Name);
      user.BeginInteraction(new AddingAreaToTownInteraction(faction, town));
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Text;

  public partial class Imperium
  {
    void OnTownHelpCommand(User user)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/town list</color> (or <color=#ffd479>/towns</color>): Lists all towns on the island");
      sb.AppendLine("  <color=#ffd479>/town help</color>: Prints this message");

      if (user.HasPermission(PERM_CHANGE_TOWNS))
      {
        sb.AppendLine("Mayor commands:");
        sb.AppendLine("  <color=#ffd479>/town create \"NAME\"</color>: Create a new town");
        sb.AppendLine("  <color=#ffd479>/town expand</color>: Add an area to your town");
        sb.AppendLine("  <color=#ffd479>/town remove</color>: Remove an area from your town");
        sb.AppendLine("  <color=#ffd479>/town disband</color>: Disband your town immediately (no undo!)");
      }

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
    void OnTownListCommand(User user)
    {
      Town[] towns = Areas.GetAllTowns();
      var sb = new StringBuilder();

      if (towns.Length == 0)
      {
        sb.AppendFormat(String.Format("No towns have been founded."));
      }
      else
      {
        sb.AppendLine(String.Format("<color=#ffd479>There are {0} towns on the island:</color>", towns.Length));
        foreach (Town town in towns)
        {
          float distance = (float) Math.Floor(town.GetDistanceFromEntity(user.Player));
          sb.AppendLine(String.Format("  <color=#ffd479>{0}:</color> {1:0.00}m ({2})", town.Name, distance, Util.Format(town.Areas)));
        }
      }

      user.SendChatMessage(sb);
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnTownRemoveCommand(User user)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureCanManageTowns(user, faction))
        return;

      Town town = Areas.GetTownByMayor(user);
      if (town == null)
      {
        user.SendChatMessage(Messages.NotMayorOfTown);
        return;
      }

      user.SendChatMessage(Messages.SelectTownCupboardToRemove);
      user.BeginInteraction(new RemovingAreaFromTownInteraction(faction, town));
    }
  }
}
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    [ChatCommand("towns")]
    void OnTownsCommand(BasePlayer player, string command, string[] args)
    {
      User user = Users.Get(player);
      if (user == null) return;

      if (!Options.EnableTowns)
      {
        user.SendChatMessage(Messages.TownsDisabled);
        return;
      }

      OnTownListCommand(user);
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

      if (!EnforceCommandCooldown(user))
        return;

      user.Hud.Toggle();
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

      if (!user.Map.IsVisible && !EnforceCommandCooldown(user))
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

      if (!user.Map.IsVisible && !EnforceCommandCooldown(user))
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

      if (!Options.EnableWar)
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

      if (!EnsureCanEngageInDiplomacy(user, attacker))
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

      if (cassusBelli.Length < Options.MinCassusBelliLength)
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

      if (!EnsureCanEngageInDiplomacy(user, faction))
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
      public static void HandleAreaChanged(Area area)
      {
        Interface.Call("OnAreaChanged", area);
      }

      public static void HandleUserEnteredArea(User user, Area area)
      {
        Interface.Call("OnUserEnteredArea", user, area);
      }

      public static void HandleUserLeftArea(User user, Area area)
      {
        Interface.Call("OnUserLeftArea", user, area);
      }

      public static void HandleUserEnteredZone(User user, Zone zone)
      {
        Interface.Call("OnUserEnteredZone", user, zone);
      }

      public static void HandleUserLeftZone(User user, Zone zone)
      {
        Interface.Call("OnUserLeftZone", user, zone);
      }

      public static void HandleFactionCreated(Faction faction)
      {
        Interface.Call("OnFactionCreated", faction);
      }

      public static void HandleFactionDisbanded(Faction faction)
      {
        Interface.Call("OnFactionDisbanded", faction);
      }

      public static void HandleFactionTaxesChanged(Faction faction)
      {
        Interface.Call("OnFactionTaxesChanged", faction);
      }

      public static void HandlePlayerJoinedFaction(Faction faction, User user)
      {
        Interface.Call("OnPlayerJoinedFaction", faction, user);
      }

      public static void HandlePlayerLeftFaction(Faction faction, User user)
      {
        Interface.Call("OnPlayerLeftFaction", faction, user);
      }

      public static void HandlePlayerInvitedToFaction(Faction faction, User user)
      {
        Interface.Call("OnPlayerInvitedToFaction", faction, user);
      }

      public static void HandlePlayerUninvitedFromFaction(Faction faction, User user)
      {
        Interface.Call("OnPlayerUninvitedFromFaction", faction, user);
      }

      public static void HandlePlayerPromoted(Faction faction, User user)
      {
        Interface.Call("OnPlayerPromoted", faction, user);
      }

      public static void HandlePlayerDemoted(Faction faction, User user)
      {
        Interface.Call("OnPlayerDemoted", faction, user);
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
    void CollectUpkeepForAllFactions()
    {
      foreach (Faction faction in Factions.GetAll())
        CollectUpkeep(faction);
    }

    void CollectUpkeep(Faction faction)
    {
      Area[] areas = Areas.GetAllClaimedByFaction(faction);

      if (areas.Length == 0)
        return;

      if (faction.IsUpkeepPaid)
      {
        Log($"[UPKEEP] {faction.Id}: Upkeep not due until {faction.NextUpkeepPaymentTime}");
        return;
      }

      int amountOwed = faction.GetUpkeepPerPeriod();
      var hoursSincePaid = (int)DateTime.UtcNow.Subtract(faction.NextUpkeepPaymentTime).TotalHours;

      Log($"[UPKEEP] {faction.Id}: {hoursSincePaid} hours since upkeep paid, trying to collect {amountOwed} scrap for {areas.Length} area claims");

      if (faction.TaxChest != null)
      {
        ItemDefinition scrapDef = ItemManager.FindItemDefinition("scrap");
        List<Item> stacks = faction.TaxChest.inventory.FindItemsByItemID(scrapDef.itemid);
        if (TryCollectFromStacks(scrapDef, stacks, amountOwed))
        {
          faction.NextUpkeepPaymentTime = faction.NextUpkeepPaymentTime.AddHours(Options.UpkeepCollectionPeriodHours);
          Log($"[UPKEEP] {faction.Id}: {amountOwed} scrap upkeep collected, next payment due {faction.NextUpkeepPaymentTime}");
          return;
        }
      }

      if (hoursSincePaid <= Options.UpkeepGracePeriodHours)
      {
        Log($"[UPKEEP] {faction.Id}: Couldn't collect upkeep, but still within {Options.UpkeepGracePeriodHours} hour grace period");
        return;
      }

      Area lostArea = areas.OrderBy(area => Areas.GetDepthInsideFriendlyTerritory(area)).First();

      Log($"[UPKEEP] {faction.Id}: Upkeep not paid in {hoursSincePaid} hours, seizing claim on {lostArea.Id}");
      PrintToChat(Messages.AreaClaimLostUpkeepNotPaidAnnouncement, faction.Id, lostArea.Id);

      Areas.Unclaim(lostArea);
    }

    void ProcessTaxesIfApplicable(ResourceDispenser dispenser, BaseEntity entity, Item item)
    {
      if (!Options.EnableTaxation)
        return;

      var player = entity as BasePlayer;
      if (player == null)
        return;

      User user = Users.Get(player);
      if (user == null)
        return;

      Area area = user.CurrentArea;
      if (area == null || !area.IsClaimed)
        return;

      Faction faction = Factions.Get(area.FactionId);
      if (!faction.CanCollectTaxes || faction.TaxChest.inventory.IsFull())
        return;

      ItemDefinition itemDef = ItemManager.FindItemDefinition(item.info.itemid);
      if (itemDef == null)
        return;

      int bonus;
      if (area.Type == AreaType.Town)
        bonus = (int)(item.amount * Options.TownGatherBonus);
      else
        bonus = (int)(item.amount * Options.ClaimedLandGatherBonus);

      var tax = (int)(item.amount * faction.TaxRate);

      faction.TaxChest.inventory.AddItem(itemDef, tax + bonus);
      item.amount -= tax;
    }

    void AwardBadlandsBonusIfApplicable(ResourceDispenser dispenser, BaseEntity entity, Item item)
    {
      if (!Options.EnableBadlands) return;

      var player = entity as BasePlayer;
      if (player == null) return;

      User user = Users.Get(player);

      if (user.CurrentArea == null)
      {
        PrintWarning("Player gathered outside of a defined area. This shouldn't happen.");
        return;
      }

      if (user.CurrentArea.Type == AreaType.Badlands)
      {
        var bonus = (int)(item.amount * Options.BadlandsGatherBonus);
        item.amount += bonus;
      }
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
  using Network;
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

      return Logistics.AlterDamage(entity, hit);
    }

    object OnTrapTrigger(BaseTrap trap, GameObject obj)
    {
      var player = obj.GetComponent<BasePlayer>();

      if (trap == null || player == null)
        return null;

      User defender = Users.Get(player);
      return Logistics.AlterTrapTrigger(trap, defender);
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

      return Logistics.AlterTurretTrigger(entity, defender);
    }

    void OnEntitySpawned(BaseNetworkable entity)
    {
      var heli = entity as BaseHelicopter;
      if (heli != null)
        Hud.GameEvents.BeginEvent(heli);

      var plane = entity as CargoPlane;
      if (plane != null)
        Hud.GameEvents.BeginEvent(plane);

      var drop = entity as SupplyDrop;
      if (Options.EnableEventZones && drop != null)
        Zones.Create(drop);
    }

    void OnEntityKill(BaseNetworkable networkable)
    {
      var entity = networkable as BaseEntity;

      if (entity == null)
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
      if (Options.EnableTaxation && container != null)
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
      if (Options.EnableEventZones && helicopter != null)
        Zones.Create(helicopter);
    }

    void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
    {
      ProcessTaxesIfApplicable(dispenser, entity, item);
      AwardBadlandsBonusIfApplicable(dispenser, entity, item);
    }

    void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item)
    {
      ProcessTaxesIfApplicable(dispenser, entity, item);
      AwardBadlandsBonusIfApplicable(dispenser, entity, item);
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
      else if (area.Type == AreaType.Town && previousArea.Type != AreaType.Town)
      {
        // The player has entered a town.
        user.SendChatMessage(Messages.EnteredTown, area.Name, area.FactionId);
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
      public const string TownsDisabled = "Towns are currently disabled.";
      public const string UpkeepDisabled = "Upkeep is currently disabled.";
      public const string WarDisabled = "War is currently disabled.";

      public const string AreaIsBadlands = "<color=#ffd479>{0}</color> is a part of the badlands.";
      public const string AreaIsClaimed = "<color=#ffd479>{0}</color> has been claimed by <color=#ffd479>[{1}]</color>.";
      public const string AreaIsHeadquarters = "<color=#ffd479>{0}</color> is the headquarters of <color=#ffd479>[{1}]</color>.";
      public const string AreaIsWilderness = "<color=#ffd479>{0}</color> has not been claimed by a faction.";
      public const string AreaIsTown = "<color=#ffd479>{0}</color> is part of the town of <color=#ffd479>{1}</color>, which is managed by <color=#ffd479>[{2}]</color>.";
      public const string AreaNotBadlands = "<color=#ffd479>{0}</color> is not a part of the badlands.";
      public const string AreaNotOwnedByYourFaction = "<color=#ffd479>{0} is not owned by your faction.";
      public const string AreaNotWilderness = "<color=#ffd479>{0}</color> is not currently wilderness.";
      public const string AreaNotPartOfTown = "<color=#ffd479>{0}</color> is not part of a town.";

      public const string InteractionCanceled = "Command canceled.";
      public const string NoInteractionInProgress = "You aren't currently executing any commands.";
      public const string NoAreasClaimed = "Your faction has not claimed any areas.";
      public const string NotMemberOfFaction = "You are not a member of a faction.";
      public const string AlreadyMemberOfFaction = "You are already a member of a faction.";
      public const string NotLeaderOfFaction = "You must be an owner or a manager of a faction.";
      public const string FactionTooSmall = "To claim land, a faction must have least {0} members.";
      public const string NotMayorOfTown = "You are not the mayor of a town. To create one, use <color=#ffd479>/town create NAME</color>.";
      public const string FactionDoesNotOwnLand = "Your faction must own at least one area.";
      public const string FactionAlreadyExists = "There is already a faction named <color=#ffd479>[{0}]</color>.";
      public const string FactionDoesNotExist = "There is no faction named <color=#ffd479>[{0}]</color>.";
      public const string InvalidUser = "Couldn't find a user whose name matches \"{0}\".";
      public const string InvalidFactionName = "Faction names must be between 2 and 6 alphanumeric characters.";
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

      public const string InvalidAreaName = "An area name must be at least <color=#ffd479>{0}</color> characters long.";
      public const string UnknownArea = "Unknown area <color=#ffd479>{0}</color>.";
      public const string CannotRenameAreaIsTown = "Cannot rename <color=#ffd479>{0}</color>, because it is part of the town <color=#ffd479>{1}</color>.";
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

      public const string CannotCreateTownAlreadyMayor = "You cannot create a new town, because you are already the mayor of <color=#ffd479>{0}</color>. To expand the town instead, use <color=#ffd479>/town expand</color>.";
      public const string CannotCreateTownSameNameAlreadyExists = "You cannot create a new town named <color=#ffd479>{0}</color>, because a town with that name already exists. To expand the town instead, use <color=#ffd479>/town expand</color>.";
      public const string CannotAddToTownAreaIsHeadquarters = "<color=#ffd479>{0}</color> cannot be added to a town, because it is currently your faction's headquarters.";
      public const string CannotAddToTownOneAlreadyExists = "<color=#ffd479>{0}</color> cannot be added to town, because it is already part of the town of <color=#ffd479>{1}</color>.";

      public const string SelectTownCupboardToCreate = "Use the hammer to select a tool cupboard to represent <color=#ffd479>{0}</color>. Say <color=#ffd479>/cancel</color> to cancel.";
      public const string SelectTownCupboardToExpand = "Use the hammer to select a tool cupboard to add to <color=#ffd479>{0}</color>. Say <color=#ffd479>/cancel</color> to cancel.";
      public const string SelectTownCupboardToRemove = "Use the hammer to select the tool cupboard representing the town you want to remove. Say <color=#ffd479>/cancel</color> to cancel.";
      public const string SelectingTownCupboardFailedNotTownCupboard = "That tool cupboard doesn't represent a town!";
      public const string SelectingTownCupboardFailedNotMayor = "That tool cupboard represents <color=#ffd479>{0}</color>, which you are not the mayor of!";
      public const string AreaAddedToTown = "You have added the area <color=#ffd479>{0}</color> to the town of <color=#ffd479>{1}</color>.";
      public const string AreaRemovedFromTown = "You have removed the area <color=#ffd479>{0}</color> from the town of <color=#ffd479>{1}</color>.";

      public const string CannotDeclareWarAgainstYourself = "You cannot declare war against yourself!";
      public const string CannotDeclareWarAlreadyAtWar = "You area already at war with <color=#ffd479>[{0}]</color>!";
      public const string CannotDeclareWarInvalidCassusBelli = "You cannot declare war against <color=#ffd479>[{0}]</color>, because your reason doesn't meet the minimum length.";
      public const string CannotOfferPeaceAlreadyOfferedPeace = "You have already offered peace to <color=#ffd479>[{0}]</color>.";
      public const string PeaceOffered = "You have offered peace to <color=#ffd479>[{0}]</color>. They must accept it before the war will end.";

      public const string EnteredBadlands = "<color=#ff0000>BORDER:</color> You have entered the badlands! Player violence is allowed here.";
      public const string EnteredWilderness = "<color=#ffd479>BORDER:</color> You have entered the wilderness.";
      public const string EnteredTown = "<color=#ffd479>BORDER:</color> You have entered the town of <color=#ffd479>{0}</color>, controlled by <color=#ffd479>[{1}]</color>.";
      public const string EnteredClaimedArea = "<color=#ffd479>BORDER:</color> You have entered land claimed by <color=#ffd479>[{0}]</color>.";

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
      public const string TownCreatedAnnouncement = "<color=#00ff00>TOWN FOUNDED:</color> <color=#ffd479>[{0}]</color> has founded the town of <color=#ffd479>{1}</color> in <color=#ffd479>{2}</color>.";
      public const string TownDisbandedAnnouncement = "<color=#ff0000>TOWN DISBANDED:</color> <color=#ffd479>[{0}]</color> has disbanded the town of <color=#ffd479>{1}</color>.";
      public const string WarDeclaredAnnouncement = "<color=#ff0000>WAR DECLARED:</color> <color=#ffd479>[{0}]</color> has declared war on <color=#ffd479>[{1}]</color>! Their reason: <color=#ffd479>{2}</color>";
      public const string WarEndedTreatyAcceptedAnnouncement = "<color=#00ff00>WAR ENDED:</color> The war between <color=#ffd479>[{0}]</color> and <color=#ffd479>[{1}]</color> has ended after both sides have agreed to a treaty.";
      public const string WarEndedFactionEliminatedAnnouncement = "<color=#00ff00>WAR ENDED:</color> The war between <color=#ffd479>[{0}]</color> and <color=#ffd479>[{1}]</color> has ended, since <color=#ffd479>[{2}]</color> no longer holds any land.";
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
  using System.Linq;

  public partial class Imperium
  {
    static class Logistics
    {
      static string[] ProtectedPrefabs = new[]
      {
        "door.hinged",
        "door.double.hinged",
        "window.bars",
        "wall.window",
        "floor.ladder.hatch",
        "floor.frame",
        "wall.frame",
        "shutter",
        "wall.external",
        "gates.external",
        "cupboard",
        "waterbarrel"
      };

      public static object AlterDamage(BaseCombatEntity entity, HitInfo hit)
      {
        if (hit.damageTypes.Has(Rust.DamageType.Decay))
          return AlterDecayDamage(entity, hit);

        User attacker = Instance.Users.Get(hit.InitiatorPlayer);
        var defendingPlayer = entity as BasePlayer;

        if (attacker == null)
          return null;

        if (defendingPlayer != null)
        {
          // One player is damaging another.
          User defender = Instance.Users.Get(defendingPlayer);

          if (defender == null)
            return null;

          return AlterDamageBetweenPlayers(attacker, defender, hit);
        }

        // A player is damaging a structure.
        return AlterDamageAgainstStructure(attacker, entity, hit);
      }

      public static object AlterTrapTrigger(BaseTrap trap, User defender)
      {
        if (!Instance.Options.EnableRestrictedPVP)
          return null;

        // A player can trigger their own traps, to prevent exploiting this mechanic.
        if (defender.Player.userID == trap.OwnerID)
          return null;

        Area trapArea = Instance.Areas.GetByEntityPosition(trap, true);
        Area defenderArea = Instance.Areas.GetByEntityPosition(defender.Player);

        // A player can trigger a trap if both are in a danger zone.
        if (trapArea.IsDangerous && defenderArea.IsDangerous)
          return null;

        return false;
      }

      public static object AlterTurretTrigger(BaseCombatEntity turret, User defender)
      {
        if (!Instance.Options.EnableRestrictedPVP)
          return null;

        // A player can be targeted by their own turrets, to prevent exploiting this mechanic.
        if (defender.Player.userID == turret.OwnerID)
          return null;

        Area turretArea = Instance.Areas.GetByEntityPosition(turret, true);
        Area defenderArea = Instance.Areas.GetByEntityPosition(defender.Player);

        // A player can be targeted by a turret if both are in a danger zone.
        if (turretArea.IsDangerous && defenderArea.IsDangerous)
          return null;

        return false;
      }

      static object AlterDamageBetweenPlayers(User attacker, User defender, HitInfo hit)
      {
        if (!Instance.Options.EnableRestrictedPVP)
          return null;

        // Allow players to take the easy way out.
        if (hit.damageTypes.Has(Rust.DamageType.Suicide))
          return null;

        if (attacker.CurrentArea == null)
        {
          Instance.PrintWarning("A player damaged another player from an unknown area. This shouldn't happen.");
          return null;
        }

        if (defender.CurrentArea == null)
        {
          Instance.PrintWarning("A player was damaged in an unknown area. This shouldn't happen.");
          return null;
        }

        // If both the attacker and the defender are in a danger zone, they can damage one another.
        if (attacker.CurrentArea.IsDangerous && defender.CurrentArea.IsDangerous)
          return null;

        // If both the attacker and defender are in an event zone, they can damage one another.
        if (attacker.CurrentZones.Count > 0 && defender.CurrentZones.Count > 0)
          return null;

        // Stop the damage.
        return false;
      }

      static object AlterDamageAgainstStructure(User attacker, BaseCombatEntity entity, HitInfo hit)
      {
        if (!Instance.Options.EnableDefensiveBonuses || !ShouldAwardDefensiveBonus(entity))
          return null;

        // If someone is damaging their own entity, don't alter the damage.
        if (attacker.Player.userID == entity.OwnerID)
          return null;

        Area area = Instance.Areas.GetByEntityPosition(entity, true);

        if (area == null)
        {
          Instance.PrintWarning("An entity was damaged in an unknown area. This shouldn't happen.");
          return null;
        }

        // If the area isn't owned by a faction, it conveys no defensive bonuses.
        if (!area.IsClaimed)
          return null;

        // If a member of a faction is attacking an entity within their own lands, don't alter the damage.
        if (attacker.Faction != null && attacker.Faction.Id == area.FactionId)
          return null;

        // Structures cannot be damaged, except during war.
        if (!area.IsWarZone)
          return false;

        float reduction = area.GetDefensiveBonus();

        if (reduction >= 1)
          return false;

        if (reduction > 0)
          hit.damageTypes.ScaleAll(reduction);

        return null;
      }

      static object AlterDecayDamage(BaseEntity entity, HitInfo hit)
      {
        if (!Instance.Options.EnableDecayReduction)
          return null;

        Area area = Instance.Areas.GetByEntityPosition(entity, true);
        float reduction = 0;

        if (area.Type == AreaType.Claimed || area.Type == AreaType.Headquarters)
          reduction = Instance.Options.ClaimedLandDecayReduction;

        if (area.Type == AreaType.Town)
          reduction = Instance.Options.TownDecayReduction;

        if (reduction >= 1)
          return false;

        if (reduction > 0)
          hit.damageTypes.Scale(Rust.DamageType.Decay, reduction);

        return null;
      }

      static bool ShouldAwardDefensiveBonus(BaseEntity entity)
      {
        var buildingBlock = entity as BuildingBlock;

        if (buildingBlock != null)
          return buildingBlock.grade != BuildingGrade.Enum.Twigs;

        if (ProtectedPrefabs.Any(prefab => entity.ShortPrefabName.Contains(prefab)))
          return true;

        return false;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Collections.Generic;

  public partial class Imperium : RustPlugin
  {
    class ImperiumOptions
    {
      public bool EnableAreaClaims;
      public bool EnableBadlands;
      public bool EnableDecayReduction;
      public bool EnableDefensiveBonuses;
      public bool EnableEventZones;
      public bool EnableMonumentZones;
      public bool EnableRestrictedPVP;
      public bool EnableTaxation;
      public bool EnableTowns;
      public bool EnableUpkeep;
      public bool EnableWar;
      public int MinFactionMembers;
      public int MinAreaNameLength;
      public int MinCassusBelliLength;
      public float DefaultTaxRate;
      public float MaxTaxRate;
      public float ClaimedLandGatherBonus;
      public float TownGatherBonus;
      public float BadlandsGatherBonus;
      public float ClaimedLandDecayReduction;
      public float TownDecayReduction;
      public List<int> ClaimCosts = new List<int>();
      public List<int> UpkeepCosts = new List<int>();
      public List<float> DefensiveBonuses = new List<float>();
      public Dictionary<string, float> MonumentZones = new Dictionary<string, float>();
      public int ZoneDomeDarkness;
      public float EventZoneRadius;
      public float EventZoneLifespanSeconds;
      public int UpkeepCheckIntervalMinutes;
      public int UpkeepCollectionPeriodHours;
      public int UpkeepGracePeriodHours;
      public string MapImageUrl;
      public int MapImageSize;
      public int CommandCooldownSeconds;
    }

    protected override void LoadDefaultConfig()
    {
      PrintWarning("Loading default configuration.");

      var options = new ImperiumOptions {
        EnableAreaClaims = true,
        EnableBadlands = true,
        EnableDecayReduction = true,
        EnableDefensiveBonuses = true,
        EnableEventZones = true,
        EnableMonumentZones = true,
        EnableRestrictedPVP = false,
        EnableTaxation = true,
        EnableTowns = true,
        EnableUpkeep = true,
        EnableWar = true,
        MinFactionMembers = 3,
        MinAreaNameLength = 3,
        MinCassusBelliLength = 50,
        DefaultTaxRate = 0.1f,
        MaxTaxRate = 0.2f,
        ClaimedLandGatherBonus = 0.1f,
        TownGatherBonus = 0.1f,
        BadlandsGatherBonus = 0.1f,
        ClaimedLandDecayReduction = 0.5f,
        TownDecayReduction = 1f,
        ClaimCosts = new List<int> { 0, 100, 200, 300, 400, 500 },
        UpkeepCosts = new List<int> { 10, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 },
        UpkeepCheckIntervalMinutes = 15,
        UpkeepCollectionPeriodHours = 24,
        UpkeepGracePeriodHours = 12,
        DefensiveBonuses = new List<float> { 0, 0.5f, 1f },
        MonumentZones = new Dictionary<string, float> {
          { "airfield", 200 },
          { "sphere_tank", 200 },
          { "junkyard", 200 },
          { "launch_site", 200 },
          { "military_tunnel", 200 },
          { "powerplant", 200 },
          { "satellite_dish", 200 },
          { "trainyard", 200 },
          { "water_treatment_plant", 200 }
        },
        ZoneDomeDarkness = 3,
        EventZoneRadius = 100f,
        EventZoneLifespanSeconds = 600f,
        MapImageUrl = "",
        MapImageSize = 1440,
        CommandCooldownSeconds = 10
      };

      Config.WriteObject(options, true);
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

      public bool IsDangerous
      {
        get { return Type == AreaType.Badlands || IsWarZone; }
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
          Api.HandleUserEnteredArea(user, this);
      }

      void OnTriggerExit(Collider collider)
      {
        if (collider.gameObject.layer != (int)Layer.Player_Server)
          return;

        var user = collider.GetComponentInParent<User>();

        if (user != null)
          Api.HandleUserLeftArea(user, this);
      }

      public float GetDistanceFromEntity(BaseEntity entity)
      {
        return Vector3.Distance(entity.transform.position, transform.position);
      }

      public int GetClaimCost(Faction faction)
      {
        var costs = Instance.Options.ClaimCosts;
        int numberOfAreasOwned = Instance.Areas.GetAllClaimedByFaction(faction).Length;
        int index = Mathf.Clamp(numberOfAreasOwned, 0, costs.Count - 1);
        return costs[index];
      }

      public float GetDefensiveBonus()
      {
        var bonuses = Instance.Options.DefensiveBonuses;
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
      const int ENTITY_LOCATION_CACHE_SIZE = 50000;

      MapGrid Grid;
      Dictionary<string, Area> Areas;
      Area[,] Layout;
      LruCache<uint, Area> EntityAreas;

      public int Count
      {
        get { return Areas.Count; }
      }

      public AreaManager()
      {
        Grid = new MapGrid(ConVar.Server.worldsize);
        Areas = new Dictionary<string, Area>();
        Layout = new Area[Grid.NumberOfCells, Grid.NumberOfCells];
        EntityAreas = new LruCache<uint, Area>(ENTITY_LOCATION_CACHE_SIZE);
      }
      
      public Area Get(string areaId)
      {
        Area area;
        if (Areas.TryGetValue(areaId, out area))
          return area;
        else
          return null;
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

      public Area GetByClaimCupboard(BuildingPrivlidge cupboard)
      {
        return GetByClaimCupboard(cupboard.net.ID);
      }

      public Area GetByClaimCupboard(uint cupboardId)
      {
        return Areas.Values.FirstOrDefault(a => a.ClaimCupboard != null && a.ClaimCupboard.net.ID == cupboardId);
      }

      public Town GetTown(string name)
      {
        Area[] areas = GetAllByType(AreaType.Town).Where(area => area.Name == name).ToArray();
        if (areas.Length == 0)
          return null;
        else
          return new Town(areas);
      }

      public Town[] GetAllTowns()
      {
        return GetAllByType(AreaType.Town).GroupBy(a => a.Name).Select(group => new Town(group)).ToArray();
      }

      public Town GetTownByMayor(User user)
      {
        return GetAllTowns().FirstOrDefault(town => town.MayorId == user.Id);
      }

      public Area GetByEntityPosition(BaseEntity entity, bool useCache = false)
      {
        Area area;

        if (useCache && EntityAreas.TryGetValue(entity.net.ID, out area))
          return area;

        var x = entity.transform.position.x;
        var z = entity.transform.position.z;
        var offset = MapGrid.GridCellSize / 2;

        int row;
        for (row = 0; row < Grid.NumberOfCells; row++)
        {
          Vector3 position = Layout[row, 0].Position;
          if (z >= position.z - offset && z <= position.z + offset)
            break;
        }

        int col;
        for (col = 0; col < Grid.NumberOfCells; col++)
        {
          Vector3 position = Layout[0, col].Position;
          if (x >= position.x - offset && x <= position.x + offset)
            break;
        }

        area = Layout[row, col];

        if (useCache)
          EntityAreas.Set(entity.net.ID, area);

        return area;
      }

      public void Claim(Area area, AreaType type, Faction faction, User claimant, BuildingPrivlidge cupboard)
      {
        area.Type = type;
        area.FactionId = faction.Id;
        area.ClaimantId = claimant.Id;
        area.ClaimCupboard = cupboard;

        Api.HandleAreaChanged(area);
      }

      public void SetHeadquarters(Area area, Faction faction)
      {
        // Ensure that no other areas are considered headquarters.
        foreach (Area otherArea in GetAllClaimedByFaction(faction).Where(a => a.Type == AreaType.Headquarters))
        {
          otherArea.Type = AreaType.Claimed;
          Api.HandleAreaChanged(otherArea);
        }

        area.Type = AreaType.Headquarters;
        Api.HandleAreaChanged(area);
      }

      public void AddToTown(string name, User mayor, Area area)
      {
        area.Type = AreaType.Town;
        area.Name = name;
        area.ClaimantId = mayor.Id;

        Api.HandleAreaChanged(area);
      }

      public void RemoveFromTown(Area area)
      {
        area.Type = AreaType.Claimed;
        area.Name = null;

        Api.HandleAreaChanged(area);
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

          Api.HandleAreaChanged(area);
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

          Api.HandleAreaChanged(area);
        }
      }

      public void AddBadlands(IEnumerable<Area> areas)
      {
        AddBadlands(areas.ToArray());
      }

      public int GetDepthInsideFriendlyTerritory(Area area)
      {
        if (!area.IsClaimed)
          return 0;

        var depth = new int[4];

        for (var row = area.Row; row >= 0; row--)
        {
          if (Layout[row, area.Col].FactionId != area.FactionId)
            break;

          depth[0]++;
        }

        for (var row = area.Row; row < Grid.NumberOfCells; row++)
        {
          if (Layout[row, area.Col].FactionId != area.FactionId)
            break;

          depth[1]++;
        }

        for (var col = area.Col; col >= 0; col--)
        {
          if (Layout[area.Row, col].FactionId != area.FactionId)
            break;

          depth[2]++;
        }

        for (var col = area.Col; col < Grid.NumberOfCells; col++)
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

        for (var row = 0; row < Grid.NumberOfCells; row++)
        {
          for (var col = 0; col < Grid.NumberOfCells; col++)
          {
            string areaId = Grid.GetAreaId(row, col);
            Vector3 position = Grid.GetPosition(row, col);
            Vector3 size = new Vector3(MapGrid.GridCellSize, 500, MapGrid.GridCellSize);

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
      Town,
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

        Instance.Log($"[LOAD] Faction {Id}: {MemberIds.Count} members, tax chest = {Util.Format(TaxChest)}");
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

      public User[] GetAllActiveMembers()
      {
        return MemberIds.Select(id => Instance.Users.Get(id)).Where(user => user != null).ToArray();
      }

      public User[] GetAllActiveInvitedUsers()
      {
        return InviteIds.Select(id => Instance.Users.Get(id)).Where(user => user != null).ToArray();
      }

      public void SendChatMessage(string message, params object[] args)
      {
        foreach (User user in GetAllActiveMembers())
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

        Api.HandleFactionCreated(faction);

        return faction;
      }

      public void Disband(Faction faction)
      {
        foreach (User user in faction.GetAllActiveMembers())
          user.SetFaction(null);

        Factions.Remove(faction.Id);
        Api.HandleFactionDisbanded(faction);
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
        Api.HandleFactionTaxesChanged(faction);
      }

      public void SetTaxChest(Faction faction, StorageContainer taxChest)
      {
        faction.TaxChest = taxChest;
        Api.HandleFactionTaxesChanged(faction);
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
  using System.Collections.Generic;
  using System.Linq;

  public partial class Imperium
  {
    class Town
    {
      public string Name { get; private set; }
      public Area[] Areas { get; private set; }
      public string FactionId { get; private set; }
      public string MayorId { get; private set; }

      public Town(IEnumerable<Area> areas)
      {
        Areas = areas.ToArray();
        Name = Areas[0].Name;
        FactionId = Areas[0].FactionId;
        MayorId = Areas[0].ClaimantId;
      }

      public float GetDistanceFromEntity(BaseEntity entity)
      {
        return Areas.Min(area => area.GetDistanceFromEntity(entity));
      }

      public int GetPopulation()
      {
        return Areas.SelectMany(area => area.ClaimCupboard.authorizedPlayers.Select(p => p.userid)).Distinct().Count();
      }
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

      public BasePlayer Player { get; private set; }
      public UserMap Map { get; private set; }
      public UserHud Hud { get; private set; }

      public Area CurrentArea { get; set; }
      public HashSet<Zone> CurrentZones { get; private set; }
      public Faction Faction { get; private set; }
      public Interaction CurrentInteraction { get; private set; }
      public DateTime CommandCooldownExpirationTime { get; set; }

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
        CommandCooldownExpirationTime = DateTime.MinValue;
        OriginalName = player.displayName;
        CurrentZones = new HashSet<Zone>();

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
          Api.HandleUserLeftArea(this, currentArea);
          Api.HandleUserEnteredArea(this, correctArea);
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

      public void Init(ZoneType type, string name, MonoBehaviour owner, Vector3 position, float radius, int darkness, float? lifespan = null)
      {
        Type = type;
        Owner = owner;
        Name = name;

        gameObject.layer = (int)Layer.Reserved1;
        gameObject.name = $"imperium_zone_{name.ToLowerInvariant()}";
        transform.position = position;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));

        for (var idx = 0; idx < darkness; idx++)
        {
          var sphere = GameManager.server.CreateEntity(SpherePrefab, position);

          SphereEntity entity = sphere.GetComponent<SphereEntity>();
          entity.currentRadius = radius * 2;
          entity.lerpSpeed = 0f;

          sphere.Spawn();
          Spheres.Add(sphere);
        }

        var collider = gameObject.AddComponent<SphereCollider>();
        collider.radius = radius;
        collider.isTrigger = true;
        collider.enabled = true;

        if (lifespan != null)
          Invoke("DelayedDestroy", (int)lifespan);
      }

      void OnDestroy()
      {
        var collider = GetComponent<SphereCollider>();

        if (collider != null)
          Destroy(collider);

        foreach (BaseEntity sphere in Spheres)
          sphere.KillMessage();
      }

      void OnTriggerEnter(Collider collider)
      {
        if (collider.gameObject.layer != (int)Layer.Player_Server)
          return;

        var user = collider.GetComponentInParent<User>();

        if (user != null && !user.CurrentZones.Contains(this))
          Api.HandleUserEnteredZone(user, this);
      }

      void OnTriggerExit(Collider collider)
      {
        if (collider.gameObject.layer != (int)Layer.Player_Server)
          return;

        var user = collider.GetComponentInParent<User>();

        if (user != null && user.CurrentZones.Contains(this))
          Api.HandleUserLeftZone(user, this);
      }

      void DelayedDestroy()
      {
        Instance.Zones.Remove(this);
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using UnityEngine;

  public partial class Imperium
  {
    class ZoneManager
    {
      Dictionary<MonoBehaviour, Zone> Zones = new Dictionary<MonoBehaviour, Zone>();

      public void Init()
      {
        if (Instance.Options.EnableMonumentZones && Instance.Options.MonumentZones != null)
        {
          MonumentInfo[] monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>();
          foreach (MonumentInfo monument in monuments)
          {
            float? radius = GetMonumentZoneRadius(monument);
            if (radius != null)
              Create(monument, (float)radius);
          }
        }

        if (Instance.Options.EnableEventZones)
        {
          SupplyDrop[] drops = UnityEngine.Object.FindObjectsOfType<SupplyDrop>();
          foreach (SupplyDrop drop in drops)
            Create(drop);
        }
      }

      public Zone Create(MonumentInfo monument, float radius)
      {
        Vector3 position = monument.transform.position;
        Vector3 size = monument.Bounds.size;
        return Create(ZoneType.Monument, monument.displayPhrase.english, monument, position, radius);
      }

      public Zone Create(SupplyDrop drop)
      {
        Vector3 position = GetGroundPosition(drop.transform.position);
        float radius = Instance.Options.EventZoneRadius;
        float lifespan = Instance.Options.EventZoneLifespanSeconds;
        return Create(ZoneType.SupplyDrop, "Supply Drop", drop, position, radius, lifespan);
      }

      public Zone Create(BaseHelicopter helicopter)
      {
        Vector3 position = GetGroundPosition(helicopter.transform.position);
        float radius = Instance.Options.EventZoneRadius;
        float lifespan = Instance.Options.EventZoneLifespanSeconds;
        return Create(ZoneType.Debris, "Debris Field", helicopter, position, radius, lifespan);
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

      Zone Create(ZoneType type, string name, MonoBehaviour owner, Vector3 position, float radius, float? lifespan = null)
      {
        var zone = new GameObject().AddComponent<Zone>();
        zone.Init(type, name, owner, position, radius, Instance.Options.ZoneDomeDarkness, lifespan);

        Instance.Puts($"Created zone {zone.Name} at {position} with radius {radius}");

        if (lifespan != null)
          Instance.Puts($"Zone {zone.Name} will be destroyed in {lifespan} seconds");

        Zones[owner] = zone;

        return zone;
      }

      float? GetMonumentZoneRadius(MonumentInfo monument)
      {
        if (monument.Type == MonumentType.Cave)
          return null;

        foreach (var entry in Instance.Options.MonumentZones)
        {
          if (monument.name.Contains(entry.Key))
            return entry.Value;
        }

        return null;
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
  public partial class Imperium
  {
    public enum ZoneType
    {
      Monument,
      SupplyDrop,
      Debris
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
      public const int GridCellSize = 150;

      public int MapSize { get; private set; }
      public int NumberOfCells { get; private set; }

      string[] RowIds;
      string[] ColumnIds;
      string[,] AreaIds;
      Vector3[,] Positions;

      public MapGrid(int mapSize)
      {
        MapSize = mapSize;
        NumberOfCells = (int)Math.Ceiling(mapSize / (float)GridCellSize);
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

        for (int row = 0; row < NumberOfCells; row++)
        {
          RowIds[row] = prefix + letter;
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

        for (int col = 0; col < NumberOfCells; col++)
          ColumnIds[col] = col.ToString();

        int z = (MapSize / 2) - GridCellSize;
        for (int row = 0; row < NumberOfCells; row++)
        {
          int x = -(MapSize / 2);
          for (int col = 0; col < NumberOfCells; col++)
          {
            var areaId = RowIds[row] + ColumnIds[col];
            AreaIds[row, col] = areaId;
            Positions[row, col] = new Vector3(x + (GridCellSize / 2), 0, z + (GridCellSize / 2));
            x += GridCellSize;
          }
          z -= GridCellSize;
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

        using (var bitmap = new Bitmap(Instance.Options.MapImageSize, Instance.Options.MapImageSize))
        using (var graphics = Graphics.FromImage(bitmap))
        {
          var mapSize = ConVar.Server.worldsize;
          var tileSize = (int)(Instance.Options.MapImageSize / (mapSize / 150f));
          var grid = new MapGrid(mapSize);

          var colorPicker = new FactionColorPicker();
          var textBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));

          for (int row = 0; row < grid.NumberOfCells; row++)
          {
            for (int col = 0; col < grid.NumberOfCells; col++)
            {
              Area area = Instance.Areas.Get(grid.GetAreaId(row, col));
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
            graphics.DrawLine(gridLinePen, 0, (row * tileSize), (grid.NumberOfCells * tileSize), (row * tileSize));
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
  using System;
  using System.Collections;
  using System.Text;

  public partial class Imperium
  {
    static class Util
    {
      public static string Format(object obj)
      {
        var user = obj as User;
        if (user != null) return Format(user);

        var entity = obj as BaseEntity;
        if (entity != null) return Format(entity);

        var list = obj as IEnumerable;
        if (list != null) return Format(list);

        return obj.ToString();
      }

      public static string Format(User user)
      {
        if (user == null)
          return "(null)";
        else
          return $"{user.UserName} ({user.Id})";
      }

      public static string Format(BaseEntity entity)
      {
        if (entity == null)
          return "(null)";
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

      public static string NormalizeFactionId(string input)
      {
        string factionId = input.Trim();

        if (factionId.StartsWith("[") && factionId.EndsWith("]"))
          factionId = factionId.Substring(1, factionId.Length - 2);

        return factionId;
      }

      public static string RemoveSpecialCharacters(string str)
      {
        if (string.IsNullOrEmpty(str))
          return string.Empty;

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
    }
  }
}
﻿namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    class AddingAreaToTownInteraction : Interaction
    {
      public Faction Faction { get; private set; }
      public Town Town { get; private set; }

      public AddingAreaToTownInteraction(Faction faction, Town town)
      {
        Faction = faction;
        Town = town;
      }

      public override bool TryComplete(HitInfo hit)
      {
        var cupboard = hit.HitEntity as BuildingPrivlidge;

        if (!Instance.EnsureCanManageTowns(User, Faction) || !Instance.EnsureCanUseCupboardAsClaim(User, cupboard))
          return false;

        Area area = Instance.Areas.GetByClaimCupboard(cupboard);

        if (area == null)
        {
          User.SendChatMessage(Messages.SelectingCupboardFailedNotClaimCupboard);
          return false;
        }

        if (area.Type == AreaType.Headquarters)
        {
          User.SendChatMessage(Messages.CannotAddToTownAreaIsHeadquarters, area.Id);
          return false;
        }

        if (area.Type == AreaType.Town)
        {
          User.SendChatMessage(Messages.CannotAddToTownOneAlreadyExists, area.Id, area.Name);
          return false;
        }

        User.SendChatMessage(Messages.AreaAddedToTown, area.Id, Town.Name);
        Instance.Log($"{Util.Format(User)} added {area.Id} to town {Town.Name}");

        Instance.Areas.AddToTown(Town.Name, User, area);
        return true;
      }
    }
  }
}
﻿namespace Oxide.Plugins
{
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

        if (!Instance.EnsureCanChangeFactionClaims(User, Faction) || !Instance.EnsureCanUseCupboardAsClaim(User, cupboard))
          return false;

        Area area = User.CurrentArea;
        AreaType type = (Instance.Areas.GetAllClaimedByFaction(Faction).Length == 0) ? AreaType.Headquarters : AreaType.Claimed;

        if (area == null)
        {
          Instance.PrintWarning("Player attempted to add claim but wasn't in an area. This shouldn't happen.");
          return false;
        }

        if (area.Type == AreaType.Badlands)
        {
          User.SendChatMessage(Messages.AreaIsBadlands, area.Id);
          return false;
        }

        if (area.Type == AreaType.Town)
        {
          User.SendChatMessage(Messages.AreaIsTown, area.Id, area.Name, area.FactionId);
          return false;
        }

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
            Instance.PrintToChat(Messages.AreaClaimedAsHeadquartersAnnouncement, Faction.Id, area.Id);
          else
            Instance.PrintToChat(Messages.AreaClaimedAnnouncement, Faction.Id, area.Id);

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
        AreaType type = (Instance.Areas.GetAllClaimedByFaction(Faction).Length == 0) ? AreaType.Headquarters : AreaType.Claimed;

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
    class CreatingTownInteraction : Interaction
    {
      public Faction Faction { get; private set; }
      public string Name { get; private set; }

      public CreatingTownInteraction(Faction faction, string name)
      {
        Faction = faction;
        Name = name;
      }

      public override bool TryComplete(HitInfo hit)
      {
        var cupboard = hit.HitEntity as BuildingPrivlidge;

        if (!Instance.EnsureCanManageTowns(User, Faction) || !Instance.EnsureCanUseCupboardAsClaim(User, cupboard))
          return false;

        Area area = Instance.Areas.GetByClaimCupboard(cupboard);

        if (area == null)
        {
          User.SendChatMessage(Messages.SelectingCupboardFailedNotClaimCupboard);
          return false;
        }

        if (area.Type == AreaType.Headquarters)
        {
          User.SendChatMessage(Messages.CannotAddToTownAreaIsHeadquarters, area.Id);
          return false;
        }

        if (area.Type == AreaType.Town)
        {
          User.SendChatMessage(Messages.CannotAddToTownOneAlreadyExists, area.Id, area.Name);
          return false;
        }

        Instance.PrintToChat(Messages.TownCreatedAnnouncement, Faction.Id, Name, area.Id);
        Instance.Log($"{Util.Format(User)} created the town {Name} in {area.Id} on behalf of {Faction.Id}");

        Instance.Areas.AddToTown(Name, User, area);
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
  using System;

  public partial class Imperium
  {
    class RemovingAreaFromTownInteraction : Interaction
    {
      public Faction Faction { get; private set; }
      public Town Town { get; private set; }

      public RemovingAreaFromTownInteraction(Faction faction, Town town)
      {
        Faction = faction;
        Town = town;
      }

      public override bool TryComplete(HitInfo hit)
      {
        var cupboard = hit.HitEntity as BuildingPrivlidge;

        if (!Instance.EnsureCanManageTowns(User, Faction) || !Instance.EnsureCanUseCupboardAsClaim(User, cupboard))
          return false;

        Area area = Instance.Areas.GetByClaimCupboard(cupboard);

        if (area == null)
        {
          User.SendChatMessage(Messages.SelectingCupboardFailedNotClaimCupboard);
          return false;
        }

        if (area.Type != AreaType.Town)
        {
          User.SendChatMessage(Messages.AreaNotPartOfTown, area.Id);
          return false;
        }

        User.SendChatMessage(Messages.AreaRemovedFromTown, area.Id, area.Name);
        Instance.Log($"{Util.Format(User)} removed {area.Id} from the town of {area.Name} on behalf of {Faction.Id}");

        Instance.Areas.RemoveFromTown(area);
        return true;
      }
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

        if (!Instance.EnsureCanChangeFactionClaims(User, Faction) || !Instance.EnsureCanUseCupboardAsClaim(User, cupboard))
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

        if (!Instance.EnsureCanChangeFactionClaims(User, Faction) || !Instance.EnsureCanUseCupboardAsClaim(User, cupboard))
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

        if (!Instance.EnsureCanChangeFactionClaims(User, SourceFaction) || !Instance.EnsureCanUseCupboardAsClaim(User, cupboard))
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

        if (TargetFaction.MemberIds.Count < Instance.Options.MinFactionMembers)
        {
          User.SendChatMessage(Messages.FactionTooSmall, Instance.Options.MinFactionMembers);
          return false;
        }

        Instance.PrintToChat(Messages.AreaClaimTransferredAnnouncement, SourceFaction.Id, area.Id, TargetFaction.Id);
        Instance.Log($"{Util.Format(User)} transferred {SourceFaction.Id}'s claim on {area.Id} to {TargetFaction.Id}");

        AreaType type = (Instance.Areas.GetAllClaimedByFaction(TargetFaction).Length == 0) ? AreaType.Headquarters : AreaType.Claimed;
        Instance.Areas.Claim(area, type, TargetFaction, User, cupboard);

        return true;
      }
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

      HashSet<BaseHelicopter> Helicopters = new HashSet<BaseHelicopter>();
      HashSet<CargoPlane> CargoPlanes = new HashSet<CargoPlane>();

      public bool IsHelicopterActive
      {
        get { return Helicopters.Count > 0; }
      }

      public bool IsCargoPlaneActive
      {
        get { return CargoPlanes.Count > 0; }
      }

      void Awake()
      {
        foreach (BaseHelicopter heli in FindObjectsOfType<BaseHelicopter>())
          BeginEvent(heli);

        foreach (CargoPlane plane in FindObjectsOfType<CargoPlane>())
          BeginEvent(plane);

        InvokeRepeating("CheckEvents", CheckIntervalSeconds, CheckIntervalSeconds);
      }

      void OnDestroy()
      {
        if (IsInvoking("CheckEvents"))
          CancelInvoke("CheckEvents");
      }

      public void BeginEvent(BaseHelicopter heli)
      {
        Instance.Puts($"Beginning helicopter event, heli at @ {heli.transform.position}");
        Helicopters.Add(heli);
      }

      public void BeginEvent(CargoPlane plane)
      {
        Instance.Puts($"Beginning cargoplane event, plane at @ {plane.transform.position}");
        CargoPlanes.Add(plane);
      }

      void CheckEvents()
      {
        var endedEvents = Helicopters.RemoveWhere(IsEntityGone) + CargoPlanes.RemoveWhere(IsEntityGone);
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
        if (!String.IsNullOrEmpty(Instance.Options.MapImageUrl))
          RegisterImage(Instance.Options.MapImageUrl);

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
        return new MapMarker
        {
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

      public static MapMarker ForTown(Area area)
      {
        return new MapMarker {
          IconUrl = Ui.MapIcon.Town,
          Label = Util.RemoveSpecialCharacters(area.Name),
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

      static float TranslatePosition(float pos)
      {
        var mapSize = TerrainMeta.Size.x; // TODO: Different from ConVar.Server.worldsize?
        return (pos + mapSize / 2f) / mapSize;
      }
      
      static string GetIconForMonument(MonumentInfo monument)
      {
        if (monument.Type == MonumentType.Cave) return Ui.MapIcon.Cave;
        if (monument.name.Contains("airfield")) return Ui.MapIcon.Airfield;
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
        public const string HudPanel = "Imperium.HudPanel";
        public const string HudPanelTop = "Imperium.HudPanel.Top";
        public const string HudPanelMiddle = "Imperium.HudPanel.Middle";
        public const string HudPanelBottom = "Imperium.HudPanel.Bottom";
        public const string HudPanelText = "Imperium.HudPanel.Text";
        public const string HudPanelIcon = "Imperium.HudPanel.Icon";
        public const string Map = "Imperium.Map";
        public const string MapCloseButton = "Imperium.Map.CloseButton";
        public const string MapBackgroundImage = "Imperium.Map.BackgroundImage";
        public const string MapClaimsImage = "Imperium.Map.ClaimsImage";
        public const string MapOverlay = "Imperium.Map.Overlay";
        public const string MapIcon = "Imperium.Map.Icon";
        public const string MapLabel = "Imperium.Map.Label";
      }

      public static class HudIcon
      {
        public const string Badlands = ImageBaseUrl + "icons/hud/badlands.png";
        public const string CargoPlaneIndicatorOn = ImageBaseUrl + "icons/hud/cargoplane-on.png";
        public const string CargoPlaneIndicatorOff = ImageBaseUrl + "icons/hud/cargoplane-off.png";
        public const string Claimed = ImageBaseUrl + "icons/hud/claimed.png";
        public const string Clock = ImageBaseUrl + "icons/hud/clock.png";
        public const string Debris = ImageBaseUrl + "icons/hud/debris.png";
        public const string Defense = ImageBaseUrl + "icons/hud/defense.png";
        public const string Harvest = ImageBaseUrl + "icons/hud/harvest.png";
        public const string Headquarters = ImageBaseUrl + "icons/hud/headquarters.png";
        public const string HelicopterIndicatorOn = ImageBaseUrl + "icons/hud/helicopter-on.png";
        public const string HelicopterIndicatorOff = ImageBaseUrl + "icons/hud/helicopter-off.png";
        public const string Players = ImageBaseUrl + "icons/hud/players.png";
        public const string Sleepers = ImageBaseUrl + "icons/hud/sleepers.png";
        public const string SupplyDrop = ImageBaseUrl + "icons/hud/supplydrop.png";
        public const string Taxes = ImageBaseUrl + "icons/hud/taxes.png";
        public const string Town = ImageBaseUrl + "icons/hud/town.png";
        public const string Warning = ImageBaseUrl + "icons/hud/warning.png";
        public const string WarZone = ImageBaseUrl + "icons/hud/warzone.png";
        public const string Wilderness = ImageBaseUrl + "icons/hud/wilderness.png";
      }

      public static class MapIcon
      {
        public const string Airfield = ImageBaseUrl + "icons/map/airfield.png";
        public const string Cave = ImageBaseUrl + "icons/map/cave.png";
        public const string Dome = ImageBaseUrl + "icons/map/dome.png";
        public const string GasStation = ImageBaseUrl + "icons/map/gas-station.png";
        public const string Harbor = ImageBaseUrl + "icons/map/harbor.png";
        public const string Headquarters = ImageBaseUrl + "icons/map/headquarters.png";
        public const string Junkyard = ImageBaseUrl + "icons/map/junkyard.png";
        public const string LaunchSite = ImageBaseUrl + "icons/map/launch-site.png";
        public const string Lighthouse = ImageBaseUrl + "icons/map/lighthouse.png";
        public const string MilitaryTunnel = ImageBaseUrl + "icons/map/military-tunnel.png";
        public const string MiningOutpost = ImageBaseUrl + "icons/map/mining-outpost.png";
        public const string Player = ImageBaseUrl + "icons/map/player.png";
        public const string PowerPlant = ImageBaseUrl + "icons/map/power-plant.png";
        public const string Quarry = ImageBaseUrl + "icons/map/quarry.png";
        public const string SatelliteDish = ImageBaseUrl + "icons/map/satellite-dish.png";
        public const string SewerBranch = ImageBaseUrl + "icons/map/sewer-branch.png";
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
        public const string BackgroundNormal = "1 0.95 0.875 0.025";
        public const string BackgroundDanger = "0.77 0.25 0.17 0.5";
        public const string BackgroundSafe = "0.31 0.37 0.20 0.75";
        public const string TextNormal = "0.85 0.85 0.85 0.75";
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
        CuiHelper.DestroyUi(User.Player, Ui.Element.HudPanel);
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

        container.Add(new CuiPanel {
          Image = { Color = "0 0 0 0", Sprite = Ui.TransparentTexture },
          RectTransform = { AnchorMin = "0.008 0.015", AnchorMax = "0.217 0.113" }
        }, Ui.Element.Hud, Ui.Element.HudPanel);

        Area area = User.CurrentArea;

        if (area.Type != AreaType.Wilderness)
        {
          container.Add(new CuiPanel {
            Image = { Color = PanelColor.BackgroundNormal },
            RectTransform = { AnchorMin = "0 0.7", AnchorMax = "1 1" }
          }, Ui.Element.HudPanel, Ui.Element.HudPanelTop);

          if (area.IsClaimed)
          {
            string defensiveBonus = String.Format("{0}%", area.GetDefensiveBonus() * 100);
            AddWidget(container, Ui.Element.HudPanelTop, Ui.HudIcon.Defense, PanelColor.TextNormal, defensiveBonus);
          }

          if (area.IsTaxableClaim)
          {
            string taxRate = String.Format("{0}%", area.GetTaxRate() * 100);
            AddWidget(container, Ui.Element.HudPanelTop, Ui.HudIcon.Taxes, PanelColor.TextNormal, taxRate, 0.33f);
          }

          if (area.Type == AreaType.Badlands)
          {
            string harvestBonus = String.Format("+{0}% Bonus", Instance.Options.BadlandsGatherBonus * 100);
            AddWidget(container, Ui.Element.HudPanelTop, Ui.HudIcon.Harvest, PanelColor.TextNormal, harvestBonus);
          }
        }

        container.Add(new CuiPanel {
          Image = { Color = GetLocationBackgroundColor() },
          RectTransform = { AnchorMin = "0 0.35", AnchorMax = "1 0.65" }
        }, Ui.Element.HudPanel, Ui.Element.HudPanelMiddle);

        AddWidget(container, Ui.Element.HudPanelMiddle, GetLocationIcon(), GetLocationTextColor(), GetLocationDescription());

        container.Add(new CuiPanel {
          Image = { Color = PanelColor.BackgroundNormal },
          RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.3" }
        }, Ui.Element.HudPanel, Ui.Element.HudPanelBottom);

        string currentTime = TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm");
        AddWidget(container, Ui.Element.HudPanelBottom, Ui.HudIcon.Clock, PanelColor.TextNormal, currentTime);

        string activePlayers = BasePlayer.activePlayerList.Count.ToString();
        AddWidget(container, Ui.Element.HudPanelBottom, Ui.HudIcon.Players, PanelColor.TextNormal, activePlayers, 0.3f);

        string sleepingPlayers = BasePlayer.sleepingPlayerList.Count.ToString();
        AddWidget(container, Ui.Element.HudPanelBottom, Ui.HudIcon.Sleepers, PanelColor.TextNormal, sleepingPlayers, 0.53f);

        string planeIcon = Instance.Hud.GameEvents.IsCargoPlaneActive ? Ui.HudIcon.CargoPlaneIndicatorOn : Ui.HudIcon.CargoPlaneIndicatorOff;
        AddWidget(container, Ui.Element.HudPanelBottom, planeIcon, 0.78f);

        string heliIcon = Instance.Hud.GameEvents.IsHelicopterActive ? Ui.HudIcon.HelicopterIndicatorOn : Ui.HudIcon.HelicopterIndicatorOff;
        AddWidget(container, Ui.Element.HudPanelBottom, heliIcon, 0.88f);

        return container;
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
              return Ui.HudIcon.Badlands;
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
          case AreaType.Town:
            return Ui.HudIcon.Town;
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
          case AreaType.Town:
            return $"{area.Id}: {area.Name} ({area.FactionId})";
          default:
            return $"{area.Id}: Wilderness";
        }
      }

      string GetLocationBackgroundColor()
      {
        if (User.CurrentZones.Count > 0)
          return PanelColor.BackgroundDanger;

        Area area = User.CurrentArea;

        if (area.IsWarZone)
          return PanelColor.BackgroundDanger;

        switch (area.Type)
        {
          case AreaType.Badlands:
            return PanelColor.BackgroundDanger;
          case AreaType.Town:
            return PanelColor.BackgroundSafe;
          default:
            return PanelColor.BackgroundNormal;
        }
      }

      string GetLocationTextColor()
      {
        if (User.CurrentZones.Count > 0)
          return PanelColor.TextDanger;

        Area area = User.CurrentArea;

        if (area.IsWarZone)
          return PanelColor.TextDanger;

        switch (area.Type)
        {
          case AreaType.Badlands:
            return PanelColor.TextDanger;
          case AreaType.Town:
            return PanelColor.TextSafe;
          default:
            return PanelColor.TextNormal;
        }
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
            FontSize = 14,
            Align = TextAnchor.MiddleLeft
          },
          RectTransform = {
            AnchorMin = $"{left + IconSize} 0",
            AnchorMax = "1 1",
            OffsetMin = "12 0",
            OffsetMax = "12 0"
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
        CuiHelper.AddUi(User.Player, Build());
        IsVisible = true;
      }

      public void Hide()
      {
        CuiHelper.DestroyUi(User.Player, Ui.Element.Map);
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
          CuiHelper.DestroyUi(User.Player, Ui.Element.Map);
          CuiHelper.AddUi(User.Player, Build());
        }
      }

      CuiElementContainer Build()
      {
        var container = new CuiElementContainer();

        container.Add(new CuiPanel {
          Image = { Color = "0 0 0 1" },
          RectTransform = { AnchorMin = "0.188 0.037", AnchorMax = "0.813 0.963" },
          CursorEnabled = true
        }, Ui.Element.Hud, Ui.Element.Map);

        container.Add(new CuiElement {
          Name = Ui.Element.MapBackgroundImage,
          Parent = Ui.Element.Map,
          Components = {
            Instance.Hud.CreateImageComponent(Instance.Options.MapImageUrl),
            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
          }
        });

        container.Add(new CuiElement {
          Name = Ui.Element.MapClaimsImage,
          Parent = Ui.Element.Map,
          Components = {
            Instance.Hud.CreateImageComponent(Ui.MapOverlayImageUrl),
            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
          }
        });

        var monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>();
        foreach (MonumentInfo monument in monuments.Where(ShowMonumentOnMap))
          AddMarker(container, MapMarker.ForMonument(monument));

        foreach (Area area in Instance.Areas.GetAllByType(AreaType.Headquarters))
        {
          var faction = Instance.Factions.Get(area.FactionId);
          AddMarker(container, MapMarker.ForHeadquarters(area, faction));
        }

        foreach (Area area in Instance.Areas.GetAllByType(AreaType.Town))
          AddMarker(container, MapMarker.ForTown(area));

        AddMarker(container, MapMarker.ForUser(User));

        container.Add(new CuiButton {
          Text = { Text = "X", FontSize = 14, Align = TextAnchor.MiddleCenter },
          Button = { Color = "0 0 0 1", Command = "imperium.map.toggle", FadeIn = 0 },
          RectTransform = { AnchorMin = "0.95 0.961", AnchorMax = "0.999 0.999" }
        }, Ui.Element.Map, Ui.Element.MapCloseButton);

        return container;
      }

      void AddMarker(CuiElementContainer container, MapMarker marker, float iconSize = 0.01f)
      {
        container.Add(new CuiElement {
          Name = Ui.Element.MapIcon + Guid.NewGuid().ToString(),
          Parent = Ui.Element.Map,
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
            Text = { Text = marker.Label, FontSize = 10, Align = TextAnchor.MiddleCenter, FadeIn = 0 },
            RectTransform = {
              AnchorMin = $"{marker.X - 0.1} {marker.Z - iconSize - 0.025}",
              AnchorMax = $"{marker.X + 0.1} {marker.Z - iconSize}"
            }
          }, Ui.Element.Map, Ui.Element.MapLabel + Guid.NewGuid().ToString());
        }
      }

      bool ShowMonumentOnMap(MonumentInfo monument)
      {
        return monument.Type != MonumentType.Cave
          && !monument.name.Contains("power_sub")
          && !monument.name.Contains("water_well");
      }
    }
  }
}
