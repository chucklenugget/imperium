namespace Oxide.Plugins
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
