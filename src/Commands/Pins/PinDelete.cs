namespace Oxide.Plugins
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
