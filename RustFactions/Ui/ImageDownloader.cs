namespace Oxide.Plugins
{
  using System;
  using System.Collections;
  using UnityEngine;

  public partial class RustFactions
  {
    class ImageDownloader : MonoBehaviour
    {
      RustFactions Core;

      public void Init(RustFactions core)
      {
        Core = core;
      }

      public void Download(Image image)
      {
        StartCoroutine(DownloadImage(image));
      }

      IEnumerator DownloadImage(Image image)
      {
        var www = new WWW(image.Url);
        yield return www;

        if (!String.IsNullOrEmpty(www.error))
        {
          Core.PrintWarning($"Error while downloading image {image.Url}: {www.error}");
        }
        else
        {
          image.Id = FileStorage.server.Store(www.bytes, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID, 0).ToString();
          Core.Puts($"Stored {image.Url} as id {image.Id}");
        }
      }
    }
  }
}
