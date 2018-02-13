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
  using System.Linq;

  [Info("Imperium", "chucklenugget", "1.4.3")]
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

      Puts("Area claims are " + (Options.Claims.Enabled ? "enabled" : "disabled"));
      Puts("Taxation is " + (Options.Taxes.Enabled ? "enabled" : "disabled"));
      Puts("Badlands are " + (Options.Badlands.Enabled ? "enabled" : "disabled"));
      Puts("Towns are " + (Options.Towns.Enabled ? "enabled" : "disabled"));
      Puts("War is " + (Options.War.Enabled ? "enabled" : "disabled"));
      Puts("Decay reduction is " + (Options.Decay.Enabled ? "enabled" : "disabled"));
      Puts("Claim upkeep is " + (Options.Upkeep.Enabled ? "enabled" : "disabled"));
      Puts("Zones are " + (Options.Zones.Enabled ? "enabled" : "disabled"));
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

      if (Options.Upkeep.Enabled)
        UpkeepCollectionTimer = timer.Every(Options.Upkeep.CheckIntervalMinutes * 60, Upkeep.CollectForAllFactions);
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

    bool EnsureUserCanManageTowns(User user, Faction faction)
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

    bool EnsureCupboardCanBeUsedForTown(User user, BuildingPrivlidge cupboard)
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

    bool EnforceCommandCooldown(User user)
    {
      int waitSeconds = user.GetSecondsUntilNextCommand();

      if (waitSeconds > 0)
      {
        user.SendChatMessage(Messages.CommandIsOnCooldown, waitSeconds);
        return false;
      }

      user.CommandCooldownExpirationTime = DateTime.UtcNow.AddSeconds(Options.Map.CommandCooldownSeconds);
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
