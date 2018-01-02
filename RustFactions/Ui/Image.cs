namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class RustFactions
  {
    class Image
    {
      [JsonProperty("url")]
      public string Url;

      [JsonProperty("id")]
      public string Id;

      public Image()
      {
      }

      public Image(string url, string id = null)
      {
        Url = url;
        Id = id;
      }
    }
  }
}
