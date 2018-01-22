namespace Oxide.Plugins
{
  public partial class Imperium : RustPlugin
  {
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
      if (!Options.EnableDefensiveBonuses)
        return null;

      if (entity == null || hit == null)
        return null;

      if (hit.damageTypes.Has(Rust.DamageType.Decay))
        return ScaleDamageForDecay(entity, hit);

      User user = Users.Get(hit.InitiatorPlayer);
      if (user == null)
        return null;

      return ScaleDamageForDefensiveBonus(entity, hit, user);
    }

    void OnEntityKill(BaseNetworkable entity)
    {
      // If a player dies in an area, remove them from the area.
      var player = entity as BasePlayer;
      if (player != null)
      {
        User user = Users.Get(player);
        if (user != null && user.CurrentArea != null)
          user.CurrentArea = null;
      }

      // If a claim TC is destroyed, remove the claim from the area.
      var cupboard = entity as BuildingPrivlidge;
      if (cupboard != null)
      {
        var area = Areas.GetByClaimCupboard(cupboard);
        if (area != null)
        {
          PrintToChat(Messages.AreaClaimLostCupboardDestroyedAnnouncement, area.FactionId, area.Id);
          Areas.Unclaim(area);
        }
      }

      // If a tax chest is destroyed, remove it from the faction data.
      if (Options.EnableTaxation)
      {
        var container = entity as StorageContainer;
        if (container != null)
        {
          Faction faction = Factions.GetByTaxChest(container);
          if (faction != null)
            faction.TaxChest = null;
        }
      }
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

    void OnUserEnterArea(Area area, User user)
    {
      Area previousArea = user.CurrentArea;

      user.CurrentArea = area;
      user.HudPanel.Refresh();

      if (previousArea == null)
        return;

      if (area.Type == AreaType.Badlands && previousArea.Type != AreaType.Badlands)
      {
        // The player has entered the badlands.
        user.SendMessage(Messages.EnteredBadlands);
      }
      else if (area.Type == AreaType.Wilderness && previousArea.Type != AreaType.Wilderness)
      {
        // The player has entered the wilderness.
        user.SendMessage(Messages.EnteredWilderness);
      }
      else if (area.Type == AreaType.Town && previousArea.Type != AreaType.Town)
      {
        // The player has entered a town.
        user.SendMessage(Messages.EnteredTown, area.Name, area.FactionId);
      }
      else if (area.IsClaimed && !previousArea.IsClaimed)
      {
        // The player has entered a faction's territory.
        user.SendMessage(Messages.EnteredClaimedArea, area.FactionId);
      }
      else if (area.IsClaimed && previousArea.IsClaimed && area.FactionId != previousArea.FactionId)
      {
        // The player has crossed a border between the territory of two factions.
        user.SendMessage(Messages.EnteredClaimedArea, area.FactionId);
      }
    }

    void OnUserExitArea(Area area, User user)
    {
      // TODO: If we don't need this hook, we should remove it.
    }

    void OnClanCreate(string factionId)
    {
      Factions.HandleFactionCreated(factionId);
    }

    void OnClanUpdate(string factionId)
    {
      Factions.HandleFactionChanged(factionId);
    }

    void OnClanDestroy(string factionId)
    {
      Area[] areas = Areas.GetAllClaimedByFaction(factionId);

      if (areas.Length > 0)
      {
        foreach (Area area in areas)
          PrintToChat(Messages.AreaClaimLostFactionDisbandedAnnouncement, area.FactionId, area.Id);

        Areas.Unclaim(areas);
      }

      Wars.EndAllWarsForEliminatedFactions();
      Factions.HandleFactionDestroyed(factionId);

      OnFactionsChanged();
    }

    void OnAreasChanged()
    {
      Wars.EndAllWarsForEliminatedFactions();
      Ui.GenerateMapOverlayImage();
      Ui.RefreshUiForAllPlayers();
    }

    void OnFactionsChanged()
    {
      Ui.RefreshUiForAllPlayers();
    }

    void OnDiplomacyChanged()
    {
      Ui.RefreshUiForAllPlayers();
    }
  }
}
