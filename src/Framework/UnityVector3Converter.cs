namespace Oxide.Plugins
{
  using System;
  using Newtonsoft.Json;
  using UnityEngine;

  public partial class Imperium : RustPlugin
  {
    class UnityVector3Converter : JsonConverter
    {
      public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
      {
        var vector = (Vector3) value;
        writer.WriteValue($"{vector.x} {vector.y} {vector.z}");
      }

      public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
      {
        string[] tokens = reader.Value.ToString().Trim().Split(' ');
        float x = Convert.ToSingle(tokens[0]);
        float y = Convert.ToSingle(tokens[1]);
        float z = Convert.ToSingle(tokens[2]);
        return new Vector3(x, y, z);
      }

      public override bool CanConvert(Type objectType)
      {
        return objectType == typeof(Vector3);
      }
    }
  }
}