namespace Oxide.Plugins
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
