namespace Oxide.Plugins
{
  using System;

  public partial class Imperium
  {
    class Image
    {
      public string Url { get; private set; }
      public string Id { get; private set; }

      public bool IsDownloaded
      {
        get { return Id != null; }
      }

      public bool IsGenerated
      {
        get { return Url != null && !Url.StartsWith("http", StringComparison.Ordinal); }
      }

      public Image(string url, string id = null)
      {
        Url = url;
        Id = id;
      }

      public Image(ImageInfo info)
      {
        Url = info.Url;
        Id = info.Id;
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

      public ImageInfo Serialize()
      {
        return new ImageInfo {
          Url = Url,
          Id = Id
        };
      }
    }
  }
}
