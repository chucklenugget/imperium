namespace Oxide.Plugins
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

      if (area == null)
      {
        user.SendChatMessage(Messages.YouAreInTheGreatUnknown);
        return;
      }

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
