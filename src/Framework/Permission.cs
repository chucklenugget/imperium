﻿namespace Oxide.Plugins
{
  using System.Reflection;

  public partial class Imperium : RustPlugin
  {
    public static class Permission
    {
      public const string AdminFactions = "imperium.factions.admin";
      public const string AdminClaims = "imperium.claims.admin";
      public const string AdminBadlands = "imperium.badlands.admin";
      public const string AdminPins = "imperium.pins.admin";
      public const string AdminWars = "imperium.wars.admin";
      public const string ManageFactions = "imperium.factions";

      public static void RegisterAll(Imperium instance)
      {
        foreach (FieldInfo field in typeof(Permission).GetFields(BindingFlags.Public | BindingFlags.Static))
          instance.permission.RegisterPermission((string)field.GetRawConstantValue(), instance);
      }
    }
  }
}
