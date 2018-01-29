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

  [Info("Imperium", "chucklenugget", "1.2.0")]
  public partial class Imperium : RustPlugin
  {
    static Imperium Instance;

    DynamicConfigFile AreasFile;
    DynamicConfigFile FactionsFile;
    DynamicConfigFile WarsFile;
    DynamicConfigFile HistoryFile;
    DynamicConfigFile ImagesFile;

    GameObject GameObject;
    ImperiumOptions Options;
    Timer UpkeepCollectionTimer;

    AreaManager Areas;
    FactionManager Factions;
    WarManager Wars;
    UserManager Users;
    ImageManager Images;

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
      HistoryFile = GetDataFile("history");
      ImagesFile = GetDataFile("images");

      Factions = new FactionManager();
      Areas = new AreaManager();
      Wars = new WarManager();
      Users = new UserManager();
      Images = new ImageManager();

      PrintToChat($"{Title} v{Version} initialized.");
    }

    void Loaded()
    {
      InitLang();
      Options = LoadOptions(Config);
      Puts("Area claims are " + (Options.EnableAreaClaims ? "enabled" : "disabled"));
      Puts("Taxation is " + (Options.EnableTaxation ? "enabled" : "disabled"));
      Puts("Badlands are " + (Options.EnableBadlands ? "enabled" : "disabled"));
      Puts("Towns are " + (Options.EnableTowns ? "enabled" : "disabled"));
      Puts("Defensive bonuses are " + (Options.EnableDefensiveBonuses ? "enabled" : "disabled"));
      Puts("Decay reduction is " + (Options.EnableDecayReduction ? "enabled" : "disabled"));
      Puts("Claim upkeep is " + (Options.EnableUpkeep ? "enabled" : "disabled"));
    }

    void Unload()
    {
      Images.Destroy();
      Users.Destroy();
      Wars.Destroy();
      Areas.Destroy();
      Factions.Destroy();

      if (UpkeepCollectionTimer != null && !UpkeepCollectionTimer.Destroyed)
        UpkeepCollectionTimer.Destroy();

      UnityEngine.Object.Destroy(GameObject);
    }

    void OnServerInitialized()
    {
      permission.RegisterPermission(PERM_CHANGE_BADLANDS, this);
      permission.RegisterPermission(PERM_CHANGE_CLAIMS, this);
      permission.RegisterPermission(PERM_CHANGE_TOWNS, this);

      Areas.Init(TryLoad<AreaInfo>(AreasFile));
      Factions.Init(TryLoad<FactionInfo>(FactionsFile));
      Users.Init();
      Wars.Init(TryLoad<WarInfo>(WarsFile));
      Images.Init(TryLoad<ImageInfo>(ImagesFile));

      Images.GenerateMapOverlayImage();

      if (Options.EnableUpkeep)
        UpkeepCollectionTimer = timer.Every(Options.UpkeepCheckIntervalMinutes * 60, CollectUpkeepForAllFactions);
    }

    void OnServerSave()
    {
      AreasFile.WriteObject(Areas.Serialize());
      FactionsFile.WriteObject(Factions.Serialize());
      WarsFile.WriteObject(Wars.Serialize());
      ImagesFile.WriteObject(Images.Serialize());
    }

    DynamicConfigFile GetDataFile(string name)
    {
      return Interface.Oxide.DataFileSystem.GetFile(Name + Path.DirectorySeparatorChar + name);
    }

    T[] TryLoad<T>(DynamicConfigFile file)
    {
      T[] items = new T[0];

      try
      {
        items = file.ReadObject<T[]>();
      }
      catch (Exception ex)
      {
        PrintWarning($"Error reading data from {file.Filename}: ${ex.ToString()}");
      }

      return items;
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
