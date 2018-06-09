## About the Plugin

Imperium creates a unique new way to play Rust, first used on servers such as Rust Factions and The Lost Isles. Claim land, fight wars, and battle for supremacy!

At its heart, Imperium adds the idea of "territory" to Rust. The game is divided into a grid of tiles matching those displayed on the in-game map. Players can create factions, and these factions can claim these tiles of land and levy taxes on resources harvested therein. Factions can declare war on one another and battle for control of the territory.

To allow you to restrict PVP only to certain areas, Imperium can also add zones around monuments and events like airdrops and heli crashes. These appear in-game as domes over the areas, and you can choose to block PVP damage outside of these domes.

Imperium is extremely configurable, leading to a wide range of game modes. Here are some ideas!

* Create a RP/PVE server where players can claim land to avoid being raided, and PVP is restricted to monuments and events.
* Create a server similar to PVE-Conflict in Conan Exiles, where player structures can't be raided but PVP is allowed everywhere.
* Create a more traditional PVP server, but factions can claim land and have to declare war on one another in order to raid.

Huge thanks to the authors of ZoneManager, LustyMap and DynamicPVP, which inspired and guided my work on Imperium. Also a huge thanks to Disconnect and Gamegeared, the admins of The Lost Isles and Rust Factions, who contributed countless ideas to Imperium and were patient as I shook out the bugs.

## Development

Pull requests are greatly appreciated. To contribute to the development of Imperium, you'll want to first set up
a local Rust server at `C:\rustserver`. Then, follow these steps:

1. Ensure that your local server has been patched to the most-recent Rust version.
2. Install the current version of Oxide on your local server.

Imperium previously required copying DLLs from the server installation into the `lib` directory, but it now just
references the files directly. This requires that you install the local server at `c:\rustserver`, but also removes
an additional copying step each time Rust or Oxide is patched.

Since Imperium is a rather complex plugin, it would be difficult to keep everything in a single source file. Instead, there is a post-build step that concatenates the files together and writes the "built" file to `build/Imperium.cs`. The post-build step will also copy this file to your local server if it's installed at `C:\rustserver`, meaning you can
re-build and Oxide will detect the change and re-load Imperium.
