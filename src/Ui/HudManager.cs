namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Reflection;
  using Oxide.Game.Rust.Cui;

  public partial class Imperium
  {
    class HudManager
    {
      Dictionary<string, Image> Images;
      bool UpdatePending;

      public GameEventWatcher GameEvents { get; private set; }

      ImageDownloader ImageDownloader;
      MapOverlayGenerator MapOverlayGenerator;

      public HudManager()
      {
        Images = new Dictionary<string, Image>();
        GameEvents = Instance.GameObject.AddComponent<GameEventWatcher>();
        ImageDownloader = Instance.GameObject.AddComponent<ImageDownloader>();
        MapOverlayGenerator = Instance.GameObject.AddComponent<MapOverlayGenerator>();
      }

      public void RefreshForAllPlayers()
      {
        if (UpdatePending)
          return;

        Instance.NextTick(() => {
          foreach (User user in Instance.Users.GetAll())
          {
            user.Map.Refresh();
            user.Hud.Refresh();
          }
          UpdatePending = false;
        });

        UpdatePending = true;
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

        if (String.IsNullOrEmpty(imageUrl))
        {
          Instance.PrintError($"CuiRawImageComponent requested for an image with a null URL. Did you forget to set MapImageUrl in the configuration?");
          return null;
        }

        if (!Images.TryGetValue(imageUrl, out image))
        {
          Instance.PrintError($"CuiRawImageComponent requested for image with an unregistered URL {imageUrl}. This shouldn't happen.");
          return null;
        }

        if (image.Id != null)
          return new CuiRawImageComponent { Png = image.Id, Sprite = Ui.TransparentTexture };
        else
          return new CuiRawImageComponent { Url = image.Url, Sprite = Ui.TransparentTexture };
      }

      public void GenerateMapOverlayImage()
      {
        MapOverlayGenerator.Generate();
      }

      public void Init()
      {
        if (!String.IsNullOrEmpty(Instance.Options.Map.ImageUrl))
          RegisterImage(Instance.Options.Map.ImageUrl);

        RegisterDefaultImages(typeof(Ui.HudIcon));
        RegisterDefaultImages(typeof(Ui.MapIcon));
      }

      public void Destroy()
      {
        UnityEngine.Object.DestroyImmediate(ImageDownloader);
        UnityEngine.Object.DestroyImmediate(MapOverlayGenerator);
        UnityEngine.Object.DestroyImmediate(GameEvents);

        foreach (Image image in Images.Values)
          image.Delete();

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
 
 