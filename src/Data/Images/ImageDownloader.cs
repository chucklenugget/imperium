namespace Oxide.Plugins
{
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using UnityEngine;

  public partial class Imperium
  {
    class ImageDownloader : MonoBehaviour
    {
      Queue<Image> PendingImages = new Queue<Image>();

      public bool IsDownloading { get; private set; }

      public void Download(Image image)
      {
        PendingImages.Enqueue(image);
        if (!IsDownloading) DownloadNext();
      }

      void DownloadNext()
      {
        if (PendingImages.Count == 0)
        {
          IsDownloading = false;
          return;
        }

        Image image = PendingImages.Dequeue();
        StartCoroutine(DownloadImage(image));

        IsDownloading = true;
      }

      IEnumerator DownloadImage(Image image)
      {
        var www = new WWW(image.Url);
        yield return www;

        if (!String.IsNullOrEmpty(www.error))
        {
          Instance.PrintWarning($"Error while downloading image {image.Url}: {www.error}");
        }
        else if (www.bytes == null || www.bytes.Length == 0)
        {
          Instance.PrintWarning($"Error while downloading image {image.Url}: No data received");
        }
        else
        {
          byte[] data = www.texture.EncodeToPNG();
          image.Save(data);
          DestroyImmediate(www.texture);
          Instance.Puts($"Stored {image.Url} as id {image.Id}");
          DownloadNext();
        }
      }
    }
  }
}
