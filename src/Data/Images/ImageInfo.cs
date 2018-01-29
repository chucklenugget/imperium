namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium
  {
    class ImageInfo
    {
      [JsonProperty("url")]
      public string Url;

      [JsonProperty("id")]
      public string Id;
    }
  }
}
