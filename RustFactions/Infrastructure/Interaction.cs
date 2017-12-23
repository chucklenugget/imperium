namespace Oxide.Plugins
{
  public partial class RustFactions
  {
    abstract class Interaction
    {
    }

    class AddingClaimInteraction : Interaction
    {
    }

    class RemovingClaimInteraction : Interaction
    {
    }

    class SelectingHeadquartersInteraction : Interaction
    {
    }

    class SelectingTaxChestInteraction : Interaction
    {
    }

    class CreatingTownInteraction : Interaction
    {
      public string Name { get; private set; }

      public CreatingTownInteraction(string name)
      {
        Name = name;
      }
    }

    class RemovingTownInteraction : Interaction
    {
    }
  }
}
