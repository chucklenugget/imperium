// Requires: RustFactions
// Requires: ImageLibrary
// Reference: System.Drawing

using System;
using System.Collections.Generic;
using System.Drawing;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
  [Info("RustFactionsMap", "chucklenugget", "0.1.3")]
  public class RustFactionsMap : RustPlugin
  {
    // ----------------------------------------------------------------------------------------------------

    [PluginReference] RustFactions RustFactions;
    [PluginReference] ImageLibrary ImageLibrary;

    const string UI_MAP = "RustFactionsMapPanel";
    const string UI_MAP_IMAGE = "RustFactionsMapImage";
    const string UI_MAP_OVERLAY = "RustFactionsMapOverlay";
    const string UI_MAP_IMAGE_URL = "http://i.imgur.com/I4w4H5T.jpg";
    const int UI_MAP_SIZE = 1024;

    const string UI_MAP_ANCHOR_MIN = "0.2271875 0.015";
    const string UI_MAP_ANCHOR_MAX = "0.7728125 0.985";

    const string UI_COLOR_BLACK = "0 0 0 1";
    const string UI_COLOR_TRANSPARENT = "0 0 0 0";

    CuiElementContainer Map;

    // ----------------------------------------------------------------------------------------------------

    void OnServerInitialized()
    {
      InitializeMap();
    }

    void Unload()
    {
      var playerMaps = UnityEngine.Object.FindObjectsOfType<PlayerMap>();
      if (playerMaps != null)
      {
        foreach (var playerMap in playerMaps)
          UnityEngine.Object.DestroyImmediate(playerMap);
      }
    }

    // ----------------------------------------------------------------------------------------------------

    [ChatCommand("xmap")]
    void OnMapCommand(BasePlayer player, string command, string[] args)
    {
      ToggleMap(player);
    }

    [ConsoleCommand("xrustfactions.map.toggle")]
    void OnMapConsoleCommand(ConsoleSystem.Arg arg)
    {
      var player = arg.Connection.player as BasePlayer;
      if (player == null) return;
      ToggleMap(player);
    }

    void ToggleMap(BasePlayer player)
    {
      var playerMap = player.gameObject.GetComponent<PlayerMap>();

      if (playerMap)
      {
        Puts("Destroying");
        CuiHelper.DestroyUi(player, UI_MAP);
        CuiHelper.DestroyUi(player, UI_MAP_IMAGE);
        UnityEngine.Object.DestroyImmediate(playerMap);
      }
      else
      {
        Puts("Creating map for player");
        playerMap = player.gameObject.AddComponent<PlayerMap>();
        playerMap.Setup(this);
        CuiHelper.AddUi(player, Map);
      }
    }

    class PlayerMap : MonoBehaviour
    {
      RustFactionsMap Plugin;
      BasePlayer Player;

      void Awake()
      {
        Player = GetComponent<BasePlayer>();
      }

      public void Setup(RustFactionsMap plugin)
      {
        Plugin = plugin;
      }

      void OnDestroy()
      {
      }

      void UpdateMap()
      {
      }
    }

    void InitializeMap()
    {
      var mapContainer = CreateElementContainer(UI_MAP, UI_COLOR_BLACK, UI_MAP_ANCHOR_MIN, UI_MAP_ANCHOR_MAX, false);

      mapContainer.Add(new CuiElement {
        Name = UI_MAP_IMAGE,
        Parent = UI_MAP,
        Components = {
          new CuiRawImageComponent { Url = UI_MAP_IMAGE_URL, Sprite = "assets/content/textures/generic/fulltransparent.tga" },
          new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
        }
      });

      mapContainer.Add(new CuiButton {
        Button = { Color = "0 0 0 1", Command = "xrustfactions.map.toggle", FadeIn = 0 },
        RectTransform = { AnchorMin = "0.95 0.961", AnchorMax = "0.999 0.999" },
        Text = { Text = "X", FontSize = 14, Align = TextAnchor.MiddleCenter }
      }, UI_MAP, "Close");

      Map = mapContainer;
      Puts("Map initialized");
    }

    static CuiElementContainer CreateElementContainer(string name, string color, string anchorMin, string anchorMax, bool cursorEnabled = false, string parent = "Hud")
    {
      var panel = new CuiPanel {
        Image = { Color = color },
        RectTransform = { AnchorMin = anchorMin, AnchorMax = anchorMax },
        CursorEnabled = true
      };

      var container = new CuiElementContainer();
      container.Add(panel, parent, name);

      return container;
    }

    // ----------------------------------------------------------------------------------------------------

    void OnAreaClaimsChanged(JObject claims)
    {
      // TODO: Re-generate map overlay when claims change
    }

    // ----------------------------------------------------------------------------------------------------

  }
}
