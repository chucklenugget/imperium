namespace Oxide.Plugins
{
  using System;
  using Newtonsoft.Json;

  public partial class RustFactions
  {
    class Image
    {
      [JsonProperty("url")]
      public string Url;

      [JsonProperty("id")]
      public string Id;

      public bool IsDownloaded
      {
        get { return Id != null; }
      }

      public bool IsGenerated
      {
        get { return Url != null && !Url.StartsWith("http", StringComparison.Ordinal); }
      }

      public Image()
      {
      }

      public Image(string url, string id = null)
      {
        Url = url;
        Id = id;
      }

      public string Save(byte[] data)
      {
        if (IsDownloaded) Delete();
        Id = FileStorage.server.Store(data, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID, 0).ToString();
        return Id;
      }

      public void Delete()
      {
        if (!IsDownloaded) return;
        FileStorage.server.RemoveEntityNum(Convert.ToUInt32(Id), 0);
        Id = null;
      }
    }
  }
}
