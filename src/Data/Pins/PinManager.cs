namespace Oxide.Plugins
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
}