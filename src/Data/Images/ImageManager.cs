namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Reflection;
  using Oxide.Game.Rust.Cui;

  public partial class Imperium
  {
    class ImageManager
    {
      Dictionary<string, Image> Images;
      ImageDownloader ImageDownloader;
      MapOverlayGenerator MapOverlayGenerator;

      public ImageManager()
      {
        Images = new Dictionary<string, Image>();
        ImageDownloader = Instance.GameObject.AddComponent<ImageDownloader>();
        MapOverlayGenerator = Instance.GameObject.AddComponent<MapOverlayGenerator>();
      }

      public Image RegisterImage(string url, byte[] imageData = null, bool overwrite = false)
      {
        Image image;

        if (Images.TryGetValue(url, out image) && !overwrite)
          return image;
        else
          image = new Image(url);

        Images[url] = image;

        if (imageData != null)
          image.Save(imageData);
        else
          ImageDownloader.Download(image);

        return image;
      }

      public void RefreshAllImages()
      {
        foreach (Image image in Images.Values.Where(image => !image.IsGenerated))
        {
          image.Delete();
          ImageDownloader.Download(image);
        }
      }

      public CuiRawImageComponent CreateImageComponent(string imageUrl)
      {
        Image image;

        if (!Images.TryGetValue(imageUrl, out image))
        {
          Instance.PrintWarning($"Tried to create CuiRawImageComponent for unregistered image {imageUrl}. This shouldn't happen.");
          return null;
        }

        if (image.Id != null)
          return new CuiRawImageComponent { Png = image.Id, Sprite = UI_TRANSPARENT_TEXTURE };
        else
          return new CuiRawImageComponent { Url = image.Url, Sprite = UI_TRANSPARENT_TEXTURE };
      }

      public void GenerateMapOverlayImage()
      {
        MapOverlayGenerator.Generate();
      }

      public void Init(IEnumerable<ImageInfo> imageInfos)
      {
        foreach (ImageInfo info in imageInfos)
          Images.Add(info.Url, new Image(info));

        Instance.Puts($"Loaded {Images.Values.Count} cached images.");

        if (!String.IsNullOrEmpty(Instance.Options.MapImageUrl))
          RegisterImage(Instance.Options.MapImageUrl);

        RegisterDefaultImages(typeof(UiHudIcon));
        RegisterDefaultImages(typeof(UiMapIcon));
      }

      public ImageInfo[] Serialize()
      {
        return Images.Values.Select(image => image.Serialize()).ToArray();
      }

      public void Destroy()
      {
        UnityEngine.Object.DestroyImmediate(ImageDownloader);
        UnityEngine.Object.DestroyImmediate(MapOverlayGenerator);
        Images.Clear();
      }

      void RegisterDefaultImages(Type type)
      {
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
          RegisterImage((string)field.GetRawConstantValue());
      }
    }
  }
}
 
 