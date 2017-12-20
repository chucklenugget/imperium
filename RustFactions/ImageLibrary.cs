using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ImageLibrary", "Absolut & K1lly0u", "2.0.10", ResourceId = 2193)]
    class ImageLibrary : RustPlugin
    {
        #region Fields
        private ImageIdentifiers imageIdentifiers;
        private ImageURLs imageUrls;
        private SkinInformation skinInformation;
        private DynamicConfigFile identifiers;
        private DynamicConfigFile urls;
        private DynamicConfigFile skininfo;

        private static ImageLibrary il;
        private ImageAssets assets;

        private Queue<LoadOrder> loadOrders = new Queue<LoadOrder>();
        private bool orderPending;
        
        private readonly Regex avatarFilter = new Regex(@"<avatarFull><!\[CDATA\[(.*)\]\]></avatarFull>");
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            identifiers = Interface.Oxide.DataFileSystem.GetFile("ImageLibrary/image_data");
            urls = Interface.Oxide.DataFileSystem.GetFile("ImageLibrary/image_urls");
            skininfo = Interface.Oxide.DataFileSystem.GetFile("ImageLibrary/skin_data");
        }
        void OnServerInitialized()
        {
            il = this;
            LoadVariables();
            LoadData();
            foreach (var item in ItemManager.itemList)
                workshopNameToShortname.Add(item.displayName.english.ToLower().Replace("skin", "").Replace(" ", "").Replace("-", ""), item.shortname);            
            CheckForRefresh();
            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerInit(player);
        }
        void OnPlayerInit(BasePlayer player) => GetPlayerAvatar(player?.UserIDString);  
        void Unload()
        {
            SaveData();
            UnityEngine.Object.Destroy(assets);
        }
        #endregion

        #region Functions        
        private void GetItemIcons()
        {            
            if (!configData.DLWhenNecessary)
            {
                Dictionary<string, string> imageList = new Dictionary<string, string>();
                PrintWarning($"Preparing {ItemManager.itemList.Count} default item skins for processing");
                foreach (var item in ItemManager.itemList)
                {
                    if (imageUrls.URLs.ContainsKey($"{item.shortname}_0"))
                        imageList[$"{item.shortname}_0"] = imageUrls.URLs[item.shortname];
                    else PrintError($"No image URL defined for {item.shortname}");
                }
                PrintWarning("Default item skin list is queued for download");
                loadOrders.Enqueue(new LoadOrder("Default icons", imageList));                
            }
            GetItemSkins();
        }
        private void GetItemSkins()
        {
            PrintWarning("Retrieving item skin lists...");
            webrequest.Enqueue("http://s3.amazonaws.com/s3.playrust.com/icons/inventory/rust/schema.json", null, (code, response) =>
            {
                if (!(response == null && code == 200))
                {
                    Rust.Workshop.ItemSchema.Item[] items = JsonConvert.DeserializeObject<Rust.Workshop.ItemSchema>(response).items;
                    Dictionary<string, string> imageList = new Dictionary<string, string>();
                    PrintWarning($"Found {items.Length} item skins. Gathering image URLs");
                    foreach (var item in items)
                    {
                        if (!string.IsNullOrEmpty(item.itemshortname) && !string.IsNullOrEmpty(item.icon_url))
                        {
                            string identifier;
                            ItemDefinition def = ItemManager.FindItemDefinition(item.itemshortname);
                            if (def == null) continue;
                            int skinCount = def.skins.Where(k => k.id == item.itemdefid).Count();
                            if (skinCount == 0)
                                identifier = $"{item.itemshortname}_{item.workshopid}";
                            else identifier = $"{item.itemshortname}_{item.itemdefid}";
                            if (!imageUrls.URLs.ContainsKey(identifier))
                                imageUrls.URLs.Add(identifier, item.icon_url);

                            if (configData.GetSkinData)
                            {
                                skinInformation.skinData[identifier] = new Dictionary<string, object>
                                {
                                    {"title", item.name },
                                    {"votesup", 0 },
                                    {"votesdown", 0 },
                                    {"description", item.description },
                                    {"score", 0 },
                                    {"views", 0 },
                                    {"created", new DateTime() },
                                };
                            }

                            if (!configData.DLWhenNecessary)
                                imageList[identifier] = imageUrls.URLs[identifier];
                        }
                    }
                    SaveUrls();
                    SaveSkinInfo();
                    if (!configData.DLWhenNecessary)
                    {
                        PrintWarning("Item skin list queued for download");
                        loadOrders.Enqueue(new LoadOrder("Item Skins", imageList));
                    }
                    if (configData.WorkshopImages)
                        ServerMgr.Instance.StartCoroutine(GetWorkshopSkins());
                    else ProcessLoadOrders();
                }
            }, this);
        }
        private IEnumerator GetWorkshopSkins()
        {
            var query = Rust.Global.SteamServer.Workshop.CreateQuery();
            query.Page = 1;
            query.PerPage = 50000;
            query.RequireTags.Add("version3");
            query.RequireTags.Add("skin");
            query.RequireAllTags = true;
            query.Run();
            Puts("Querying Steam for available workshop items. Please wait for a response from Steam...");
            yield return new WaitWhile(new Func<bool>(() => query.IsRunning));
            Puts($"Found {query.Items.Length} workshop items. Gathering image URLs");
            Dictionary<string, string> imageList = new Dictionary<string, string>();
            foreach (var item in query.Items)
            {
                if (!string.IsNullOrEmpty(item.PreviewImageUrl))
                {
                    foreach (var tag in item.Tags)
                    {
                        var adjTag = tag.ToLower().Replace("skin", "").Replace(" ", "").Replace("-", "");
                        if (workshopNameToShortname.ContainsKey(adjTag))
                        {
                            string identifier = $"{workshopNameToShortname[adjTag]}_{item.Id}";

                            if (!imageUrls.URLs.ContainsKey(identifier))
                                imageUrls.URLs.Add(identifier, item.PreviewImageUrl);

                            if (configData.GetSkinData)
                            {
                                skinInformation.skinData[identifier] = new Dictionary<string, object>
                                {
                                    {"title", item.Title },
                                    {"votesup", item.VotesUp },
                                    {"votesdown", item.VotesDown },
                                    {"description", item.Description },
                                    {"score", item.Score },
                                    {"views", item.WebsiteViews },
                                    {"created", item.Created },
                                };
                            }

                            if (!configData.DLWhenNecessary)
                                imageList[identifier] = item.PreviewImageUrl;

                            continue;
                        }
                    }
                }
            }
            query.Dispose();
            SaveUrls();
            SaveSkinInfo();
            if (!configData.DLWhenNecessary)
            {
                PrintWarning("Workshop skins queued for download");
                loadOrders.Enqueue(new LoadOrder("Workshop skins", imageList));
                ProcessLoadOrders();
            }
        }
        private void ProcessLoadOrders()
        {
            if (loadOrders.Count > 0)
            {               
                LoadOrder nextLoad = loadOrders.Dequeue();
                Puts("Starting order " + nextLoad.loadName);
                if (nextLoad.imageList.Count > 0)
                {
                    foreach (var item in nextLoad.imageList)
                        assets.Add(item.Key, item.Value);
                }
                if (nextLoad.imageData.Count > 0)
                {
                    foreach (var item in nextLoad.imageData)
                        assets.Add(item.Key, null, item.Value);
                }
                orderPending = true;
                assets.BeginLoad(nextLoad.loadName);
            }
        }
        private void GetPlayerAvatar(string userId)
        {
            if (!configData.StoreAvatars || string.IsNullOrEmpty(userId) || HasImage(userId, 0))
                return;
           
            webrequest.Enqueue($"http://steamcommunity.com/profiles/{userId}?xml=1",null, (code, response) =>
            {
                string avatar = null;
                if (response != null && code == 200)
                {
                    avatar = avatarFilter.Match(response).Groups[1].ToString();
                    AddImage(avatar, userId, 0);
                }
            }, this);
        }
        private void RefreshImagery()
        {
            imageIdentifiers.imageIds.Clear();
            imageIdentifiers.lastCEID = CommunityEntity.ServerInstance.net.ID;

            AddImage("http://i.imgur.com/sZepiWv.png", "NONE", 0);
            foreach (var image in configData.UserImages)
            {
                if (!string.IsNullOrEmpty(image.Value))
                    AddImage(image.Value, image.Key, 0);
            }
            GetItemSkins();
        }
        private void CheckForRefresh()
        {
            if (assets == null) assets = new GameObject("WebObject").AddComponent<ImageAssets>();
            if (imageIdentifiers.lastCEID != CommunityEntity.ServerInstance.net.ID || imageUrls.URLs.Count == 0)                            
                RefreshImagery();                        
        }
        #endregion

        #region Workshop Names and Image URLs
        Dictionary<string, string> workshopNameToShortname = new Dictionary<string, string>
        {
            {"longtshirt", "tshirt.long" },
            {"cap", "hat.cap" },
            {"beenie", "hat.beenie" },
            {"boonie", "hat.boonie" },
            {"balaclava", "mask.balaclava" },
            {"pipeshotgun", "shotgun.waterpipe" },
            {"woodstorage", "box.wooden" },
            {"ak47", "rifle.ak" },
            {"bearrug", "rug.bear" },
            {"boltrifle", "rifle.bolt" },
            {"bandana", "mask.bandana" },
            {"hideshirt", "attire.hide.vest" },
            {"snowjacket", "jacket.snow" },
            {"buckethat", "bucket.helmet" },
            {"semiautopistol", "pistol.semiauto" },
            {"burlapgloves", "burlap.gloves" },
            {"roadsignvest", "roadsign.jacket" },
            {"roadsignpants", "roadsign.kilt" },
            {"burlappants", "burlap.trousers" },
            {"collaredshirt", "shirt.collared" },
            {"mp5", "smg.mp5" },
            {"sword", "salvaged.sword" },
            {"workboots", "shoes.boots" },
            {"vagabondjacket", "jacket" },
            {"hideshoes", "attire.hide.boots" },
            {"deerskullmask", "deer.skull.mask" },
            {"minerhat", "hat.miner" },
            {"lr300", "rifle.lr300" }
        };
        private Hash<string, string> defaultUrls = new Hash<string, string>
        {
            {"tshirt","http://imgur.com/SAD8dWX.png"},
            {"pants","http://imgur.com/iiFJAso.png"},
            {"shoes.boots","http://imgur.com/b8HJ3TJ.png"},
            {"tshirt.long","http://imgur.com/KPxtIQI.png"},
            {"mask.bandana","http://imgur.com/PImuCst.png"},
            {"mask.balaclava","http://imgur.com/BYFgE5c.png"},
            {"jacket.snow","http://imgur.com/32ZO3jO.png"},
            {"jacket","http://imgur.com/zU7TQPR.png"},
            {"hoodie","http://imgur.com/EvGigZB.png"},
            {"hat.cap","http://imgur.com/TfycJC9.png"},
            {"hat.beenie","http://imgur.com/yDkGk47.png"},
            {"burlap.gloves","http://imgur.com/8aFVMgl.png"},
            {"burlap.shirt","http://imgur.com/MUs4xL6.png"},
            {"hat.boonie","http://imgur.com/2b4OjxB.png"},
            {"santahat","http://imgur.com/bmOV0aX.png"},
            {"shirt.tanktop","http://vignette4.wikia.nocookie.net/play-rust/images/1/1e/Tank_Top_icon.png"},
            {"shirt.collared","http://vignette1.wikia.nocookie.net/play-rust/images/8/8c/Shirt_icon.png"},
            {"pants.shorts","http://vignette4.wikia.nocookie.net/play-rust/images/4/46/Shorts_icon.png"},
            {"hazmat.pants","http://imgur.com/ZsaLNUK.png"},
            {"hazmat.jacket","http://imgur.com/uKk9ghN.png"},
            {"hazmat.helmet","http://imgur.com/BHSrFsh.png"},
            {"hazmat.gloves","http://imgur.com/JYTXvnx.png"},
            {"hazmat.boots","http://imgur.com/sfU4PdX.png"},
            {"hat.miner","http://imgur.com/RtRy2ne.png"},
            {"hat.candle","http://imgur.com/F7nP0PC.png"},
            {"hat.wolf","http://imgur.com/D2Z8QjL.png"},
            {"burlap.trousers","http://imgur.com/tDqEh7T.png"},
            {"burlap.shoes","http://imgur.com/wXrkSxd.png"},
            {"burlap.headwrap","http://imgur.com/u6YLWda.png"},
            {"bucket.helmet","http://imgur.com/Sb5cnpz.png"},
            {"wood.armor.pants","http://imgur.com/k2O9xEX.png"},
            {"wood.armor.jacket","http://imgur.com/9PUyVIv.png"},
            {"roadsign.kilt","http://imgur.com/WLh1Nv4.png"},
            {"roadsign.jacket","http://imgur.com/tqpDp2V.png"},
            {"riot.helmet","http://imgur.com/NlxGOum.png"},
            {"metal.plate.torso","http://imgur.com/lMw6ez2.png"},
            {"metal.facemask","http://imgur.com/BPd5q6h.png"},
            {"coffeecan.helmet","http://imgur.com/RrY8aMM.png"},
            {"bone.armor.suit","http://imgur.com/FkFR1kX.png"},
            {"attire.hide.vest","http://imgur.com/RQ8LJ5q.png"},
            {"attire.hide.skirt","http://imgur.com/nRlYLJW.png"},
            {"attire.hide.poncho","http://imgur.com/cqHND3g.png"},
            {"attire.hide.pants","http://imgur.com/rJy27KQ.png"},
            {"attire.hide.helterneck","http://imgur.com/2RXe7cg.png"},
            {"attire.hide.boots","http://imgur.com/6S98FbC.png"},
            {"deer.skull.mask","http://imgur.com/sqLjUSE.png"},
            {"pistol.revolver","http://imgur.com/C6BHyBB.png"},
            {"pistol.semiauto","http://imgur.com/Zwqg3ic.png"},
            {"rifle.ak","http://imgur.com/qlgloXW.png"},
            {"rifle.bolt","http://imgur.com/8oVVXJS.png"},
            {"shotgun.pump","http://imgur.com/OHRph6g.png"},
            {"shotgun.waterpipe","http://imgur.com/3BliJtR.png"},
            {"rifle.lr300","http://imgur.com/NYffUwv.png"},
            {"crossbow","http://imgur.com/nDBFhTA.png"},
            {"smg.thompson","http://imgur.com/rSQ5nHj.png"},
            {"weapon.mod.small.scope","http://imgur.com/jMvDHLz.png"},
            {"weapon.mod.silencer","http://imgur.com/oighpzk.png"},
            {"weapon.mod.muzzlebrake","http://imgur.com/sjxJIjT.png"},
            {"weapon.mod.muzzleboost","http://imgur.com/U9aMaPN.png"},
            {"weapon.mod.lasersight","http://imgur.com/rxIzDwY.png"},
            {"weapon.mod.holosight","http://imgur.com/R76B83t.png"},
            {"weapon.mod.flashlight","http://imgur.com/4gFapPt.png"},
            {"spear.wooden","http://imgur.com/7QpIs8B.png"},
            {"spear.stone","http://imgur.com/Y3HstyV.png"},
            {"smg.2","http://imgur.com/ElXI2uv.png"},
            {"smg.mp5","http://imgur.com/ohazNYk.png"},
            {"shotgun.double","http://imgur.com/Pm2Q4Dj.png"},
            {"salvaged.sword","http://imgur.com/M6gWbNv.png"},
            {"salvaged.cleaver","http://imgur.com/DrelWEg.png"},
            {"rocket.launcher","http://imgur.com/2yDyb9p.png"},
            {"rifle.semiauto","http://imgur.com/UfGP5kq.png"},
            {"pistol.eoka","http://imgur.com/SSb9czm.png"},
            {"machete","http://imgur.com/KfwkwV8.png"},
            {"mace","http://imgur.com/OtsvCkC.png"},
            {"longsword","http://imgur.com/1StsKVe.png"},
            {"lmg.m249","http://imgur.com/f7Rzrn2.png"},
            {"knife.bone","http://imgur.com/9TaVbYX.png"},
            {"flamethrower","http://imgur.com/CwhZ8i7.png"},
            {"bow.hunting","http://imgur.com/Myv79jT.png"},
            {"bone.club","http://imgur.com/ib11D8V.png"},
            {"grenade.f1","http://imgur.com/ZwrVuXh.png"},
            {"grenade.beancan","http://imgur.com/FQZOd7m.png"},
            {"ammo.handmade.shell","http://imgur.com/V0CyZ7j.png"},
            {"ammo.pistol","http://imgur.com/gDNR7oj.png"},
            {"ammo.pistol.fire","http://imgur.com/VyX0pAu.png"},
            {"ammo.pistol.hv","http://imgur.com/E1dB4Nb.png"},
            {"ammo.rifle","http://imgur.com/rqVkjX3.png"},
            {"ammo.rifle.explosive","http://imgur.com/hpAxKQc.png"},
            {"ammo.rifle.hv","http://imgur.com/BkG4hLM.png"},
            {"ammo.rifle.incendiary","http://imgur.com/SN4XV2S.png"},
            {"ammo.rocket.basic","http://imgur.com/Weg1M6y.png"},
            {"ammo.rocket.fire","http://imgur.com/j4XMSmO.png"},
            {"ammo.rocket.hv","http://imgur.com/5mdVIIV.png"},
            {"ammo.rocket.smoke","http://imgur.com/kMTgSEI.png"},
            {"ammo.shotgun","http://imgur.com/caFY5Bp.png"},
            {"ammo.shotgun.slug","http://imgur.com/ti5fCBp.png"},
            {"arrow.hv","http://imgur.com/r6VLTt2.png"},
            {"arrow.wooden","http://imgur.com/yMCfjKh.png"},
            {"bandage","http://imgur.com/TuMpnnu.png"},
            {"syringe.medical","http://imgur.com/DPDicE6.png"},
            {"largemedkit","http://imgur.com/iPsWViD.png"},
            {"antiradpills","http://imgur.com/SIhXEtB.png"},
            {"blood","http://imgur.com/Mdtvg2m.png"},
            {"bed","http://imgur.com/K0zQtwh.png"},
            {"box.wooden","http://imgur.com/dFqTUTQ.png"},
            {"box.wooden.large","http://imgur.com/qImBEtL.png"},
            {"campfire","http://i.imgur.com/TiAlJpv.png"},
            {"ceilinglight","http://imgur.com/3sikyL6.png"},
            {"door.double.hinged.metal","http://imgur.com/awNuhRv.png"},
            {"door.double.hinged.toptier","http://imgur.com/oJCqHd6.png"},
            {"door.double.hinged.wood","http://imgur.com/tcHmZXZ.png"},
            {"door.hinged.metal","http://imgur.com/UGZftiQ.png"},
            {"door.hinged.toptier","http://imgur.com/bc2TrfQ.png"},
            {"door.hinged.wood","http://imgur.com/PrrWSN2.png"},
            {"floor.grill","http://imgur.com/bp7ZOkE.png"},
            {"floor.ladder.hatch","http://imgur.com/suML6jj.png"},
            {"gates.external.high.stone","http://imgur.com/o4NWWXp.png"},
            {"gates.external.high.wood","http://imgur.com/DRa9a8G.png"},
            {"cupboard.tool","http://imgur.com/OzUewI1.png"},
            {"shelves","http://imgur.com/vjtdyk5.png"},
            {"shutter.metal.embrasure.a","http://imgur.com/1ke0LVO.png"},
            {"shutter.metal.embrasure.b","http://imgur.com/uRtgNRH.png"},
            {"shutter.wood.a","http://imgur.com/VngPUi2.png"},
            {"sign.hanging","http://imgur.com/VIeRGh9.png"},
            {"sign.hanging.banner.large","http://imgur.com/Owr3668.png"},
            {"sign.hanging.ornate","http://imgur.com/nQ1xHYb.png"},
            {"sign.pictureframe.landscape","http://imgur.com/nNh1uro.png"},
            {"sign.pictureframe.portrait","http://imgur.com/CQr8UYq.png"},
            {"sign.pictureframe.tall","http://imgur.com/3b51GfA.png"},
            {"sign.pictureframe.xl","http://imgur.com/3zdBDqa.png"},
            {"sign.pictureframe.xxl","http://imgur.com/9xSgewe.png"},
            {"sign.pole.banner.large","http://imgur.com/nGRDZrO.png"},
            {"sign.post.double","http://imgur.com/CXUsPSn.png"},
            {"sign.post.single","http://imgur.com/0qXuSMs.png"},
            {"sign.post.town","http://imgur.com/KgN4T1C.png"},
            {"sign.post.town.roof","http://imgur.com/hCLJXg4.png"},
            {"sign.wooden.huge","http://imgur.com/DehcZTb.png"},
            {"sign.wooden.large","http://imgur.com/BItcvBB.png"},
            {"sign.wooden.medium","http://imgur.com/zXJcB26.png"},
            {"sign.wooden.small","http://imgur.com/wfDYYYW.png"},
            {"jackolantern.angry","http://imgur.com/NRdMCfb.png"},
            {"jackolantern.happy","http://imgur.com/2gIfuAO.png"},
            {"ladder.wooden.wall","http://imgur.com/E3haHSe.png"},
            {"lantern","http://imgur.com/UHQdu3Q.png"},
            {"lock.code","http://imgur.com/pAXI8ZY.png"},
            {"mining.quarry","http://imgur.com/4Mgh1nK.png"},
            {"mining.pumpjack","http://imgur.com/FWbMASw.png"},
            {"wall.external.high","http://imgur.com/mB8oila.png"},
            {"wall.external.high.stone","http://imgur.com/7t3BdwH.png"},
            {"wall.frame.cell","http://imgur.com/oLj65GS.png"},
            {"wall.frame.cell.gate","http://imgur.com/iAcwJmG.png"},
            {"wall.frame.fence","http://imgur.com/4HVSY9Y.png"},
            {"wall.frame.fence.gate","http://imgur.com/mpmO78C.png"},
            {"wall.frame.shopfront","http://imgur.com/G7fB7kk.png"},
            {"wall.window.bars.metal","http://imgur.com/QmkIpkZ.png"},
            {"wall.window.bars.toptier","http://imgur.com/AsMdaCc.png"},
            {"wall.window.bars.wood","http://imgur.com/VS3SVVB.png"},
            {"lock.key","http://imgur.com/HuelWn0.png"},
            {"barricade.concrete","http://imgur.com/91Ob9XP.png"},
            {"barricade.metal","http://imgur.com/7rseBMC.png"},
            {"barricade.sandbags","http://imgur.com/gBQLSgQ.png"},
            {"barricade.wood","http://imgur.com/ycYTO3W.png"},
            {"barricade.woodwire","http://imgur.com/PMEFBla.png"},
            {"barricade.stone","http://imgur.com/W8qTCEX.png"},
            {"bone.fragments","http://imgur.com/iOJbBGT.png"},
            {"charcoal","http://imgur.com/G2hyxqi.png"},
            {"cloth","http://imgur.com/0olknLW.png"},
            {"coal","http://imgur.com/SIWOdbj.png"},
            {"crude.oil","http://imgur.com/VmQvwPS.png"},
            {"fat.animal","http://imgur.com/7NdUBKm.png"},
            {"hq.metal.ore","http://imgur.com/kdBrQ2P.png"},
            {"lowgradefuel","http://imgur.com/CSNPLYX.png"},
            {"metal.fragments","http://imgur.com/1bzDvUs.png"},
            {"metal.ore","http://imgur.com/yrTGHvv.png"},
            {"leather","http://imgur.com/9rqWrIy.png"},
            {"metal.refined","http://imgur.com/j2947YU.png"},
            {"wood","http://imgur.com/AChzDls.png"},
            {"seed.corn","http://imgur.com/u9ZPaeG.png"},
            {"seed.hemp","http://imgur.com/wO6aojb.png"},
            {"seed.pumpkin","http://imgur.com/mHaV8ei.png"},
            {"skull.human","http://imgur.com/ZFnWubS.png"},
            {"skull.wolf","http://imgur.com/f4MRE72.png"},
            {"stones","http://imgur.com/cluFzuZ.png"},
            {"sulfur","http://imgur.com/1RTTB7k.png"},
            {"sulfur.ore","http://imgur.com/AdxkKGb.png"},
            {"gunpowder","http://imgur.com/qV7b4WD.png"},
            {"researchpaper","http://imgur.com/Pv8jxrl.png"},
            {"explosives","http://imgur.com/S43G64k.png"},
            {"botabag","http://imgur.com/MkIOiUs.png"},
            {"box.repair.bench","http://imgur.com/HpwYNjI.png"},
            {"bucket.water","http://imgur.com/svlCdlv.png"},
            {"explosive.satchel","http://imgur.com/dlUW54q.png"},
            {"explosive.timed","http://imgur.com/CtxUCgC.png"},
            {"flare","http://imgur.com/MS0JcRT.png"},
            {"fun.guitar","http://imgur.com/l96owHe.png"},
            {"furnace","http://imgur.com/77i4nqb.png"},
            {"furnace.large","http://imgur.com/NmsmUzo.png"},
            {"hatchet","http://imgur.com/5juFLRZ.png"},
            {"icepick.salvaged","http://imgur.com/ZTJLWdI.png"},
            {"axe.salvaged","http://imgur.com/muTaCg2.png"},
            {"pickaxe","http://imgur.com/QNirWhG.png"},
            {"research.table","http://imgur.com/C9wL7Kk.png"},
            {"small.oil.refinery","http://imgur.com/Qqz6RgS.png"},
            {"stone.pickaxe","http://imgur.com/54azzFs.png"},
            {"stonehatchet","http://imgur.com/toLaFZd.png"},
            {"supply.signal","http://imgur.com/wj6yzow.png"},
            {"surveycharge","http://imgur.com/UPNvuY0.png"},
            {"target.reactive","http://imgur.com/BNcKZnU.png"},
            {"tool.camera","http://imgur.com/4AaLCfW.png"},
            {"water.barrel","http://imgur.com/JsmzCeU.png"},
            {"water.catcher.large","http://imgur.com/YWrJQoa.png"},
            {"water.catcher.small","http://imgur.com/PTXcYXs.png"},
            {"water.purifier","http://imgur.com/L7R4Ral.png"},
            {"rock","http://imgur.com/2GMBs5M.png"},
            {"torch","http://imgur.com/qKYxg5E.png"},
            {"stash.small","http://imgur.com/fH4RWZe.png"},
            {"sleepingbag","http://imgur.com/oJes3Lo.png"},
            {"hammer.salvaged","http://imgur.com/5oh3Wke.png"},
            {"hammer","http://imgur.com/KNG2Gvs.png"},
            {"blueprulongbase","http://imgur.com/gMdRr6G.png"},
            {"fishtrap.small","http://imgur.com/spuGlOj.png"},
            {"building.planner","http://imgur.com/oXu5F27.png"},
            {"battery.small","http://imgur.com/214z05n.png"},
            {"can.tuna.empty","http://imgur.com/GB02zHx.png"},
            {"can.beans.empty","http://imgur.com/9K5In35.png"},
            {"cctv.camera","http://imgur.com/4j4LD01.png"},
            {"pookie.bear","http://imgur.com/KJSccj0.png"},
            {"targeting.computer","http://imgur.com/oPMPl3B.png"},
            {"trap.bear","http://imgur.com/GZD4bVy.png"},
            {"trap.landmine","http://imgur.com/YR0lVCs.png"},
            {"autoturret","http://imgur.com/4R0ByHj.png"},
            {"spikes.floor","http://imgur.com/Nj0yJs0.png"},
            {"note","http://imgur.com/AM3Uech.png"},
            {"paper","http://imgur.com/pK49c6M.png"},
            {"map","http://imgur.com/u8HBelr.png"},
            {"stocking.large","http://imgur.com/di39MBT.png"},
            {"stocking.small","http://imgur.com/6eAg1zi.png"},
            {"generator.wind.scrap","http://imgur.com/fuQaE1H.png"},
            {"xmas.present.large","http://imgur.com/dU3nhYo.png"},
            {"xmas.present.medium","http://imgur.com/Ov5YUty.png"},
            {"xmas.present.small","http://imgur.com/hWCd67B.png"},
            {"door.key","http://imgur.com/kw8UAN2.png"},
            {"wolfmeat.burned","http://imgur.com/zAJhDNd.png"},
            {"wolfmeat.cooked","http://imgur.com/LKlgpMe.png"},
            {"wolfmeat.raw","http://imgur.com/qvMvis8.png"},
            {"wolfmeat.spoiled","http://imgur.com/8kXOVyJ.png"},
            {"waterjug","http://imgur.com/BJzeMkc.png"},
            {"water.salt","http://imgur.com/d4ihUtv.png"},
            {"water","http://imgur.com/xdz5L7M.png"},
            {"smallwaterbottle","http://imgur.com/YTLCucH.png"},
            {"pumpkin","http://imgur.com/Gb9NvdQ.png"},
            {"mushroom","http://imgur.com/FeWuvuh.png"},
            {"meat.boar","http://imgur.com/4ijrHrn.png"},
            {"meat.pork.burned","http://imgur.com/5Dam9qQ.png"},
            {"meat.pork.cooked","http://imgur.com/yhgxCPG.png"},
            {"humanmeat.burned","http://imgur.com/DloSZvl.png"},
            {"humanmeat.cooked","http://imgur.com/ba2j2rG.png"},
            {"humanmeat.raw","http://imgur.com/28SpF8Y.png"},
            {"humanmeat.spoiled","http://imgur.com/mSWVRUi.png"},
            {"granolabar","http://imgur.com/3rvzSwj.png"},
            {"fish.cooked","http://imgur.com/Idtzv1t.png"},
            {"fish.minnows","http://imgur.com/7LXZH2S.png"},
            {"fish.troutsmall","http://imgur.com/aJ2PquF.png"},
            {"fish.raw","http://imgur.com/GdErxqf.png"},
            {"corn","http://imgur.com/6V5SJZ0.png"},
            {"chocholate","http://imgur.com/Ymq7PsV.png"},
            {"chicken.burned","http://imgur.com/34sYfir.png"},
            {"chicken.cooked","http://imgur.com/UvHbBhH.png"},
            {"chicken.raw","http://imgur.com/gMldKSz.png"},
            {"chicken.spoiled","http://imgur.com/hiOEwGn.png"},
            {"cactusflesh","http://imgur.com/8R16YDP.png"},
            {"candycane","http://imgur.com/DSxrXOI.png"},
            {"can.tuna","http://imgur.com/c8rDUP3.png"},
            {"can.beans","http://imgur.com/Ysn6ThW.png"},
            {"blueberries","http://imgur.com/tFZ66fB.png"},
            {"black.raspberries","http://imgur.com/HZjKpX9.png"},
            {"bearmeat","http://imgur.com/hpL2I64.png"},
            {"bearmeat.burned","http://imgur.com/f1eVA0W.png"},
            {"bearmeat.cooked","http://imgur.com/e5Z6w1y.png"},
            {"apple","http://imgur.com/goMCM2w.png"},
            {"apple.spoiled","http://imgur.com/2pi2sUH.png"},
            {"bleach","http://vignette3.wikia.nocookie.net/play-rust/images/a/ac/Bleach_icon.png"},
            {"ducttape","http://vignette1.wikia.nocookie.net/play-rust/images/f/f8/Duct_Tape_icon.png"},
            {"propanetank","http://vignette4.wikia.nocookie.net/play-rust/images/a/a8/Empty_Propane_Tank_icon.png"},
            {"gears","http://vignette2.wikia.nocookie.net/play-rust/images/7/72/Gears_icon.png"},
            {"glue","http://vignette3.wikia.nocookie.net/play-rust/images/6/66/Glue_icon.png"},
            {"metalblade","http://vignette4.wikia.nocookie.net/play-rust/images/9/9b/Metal_Blade_icon.png"},
            {"metalpipe","http://vignette2.wikia.nocookie.net/play-rust/images/4/4a/Metal_Pipe_icon.png"},
            {"metalspring","http://vignette2.wikia.nocookie.net/play-rust/images/3/3d/Metal_Spring_icon.png"},
            {"riflebody","http://vignette2.wikia.nocookie.net/play-rust/images/0/08/Rifle_Body_icon.png"},
            {"roadsigns","http://vignette3.wikia.nocookie.net/play-rust/images/a/a5/Road_Signs_icon.png"},
            {"rope","http://vignette1.wikia.nocookie.net/play-rust/images/1/15/Rope_icon.png"},
            {"sewingkit","http://vignette1.wikia.nocookie.net/play-rust/images/2/29/Sewing_Kit_icon.png"},
            {"sheetmetal","http://vignette3.wikia.nocookie.net/play-rust/images/3/39/Sheet_Metal_icon.png"},
            {"smgbody","http://vignette3.wikia.nocookie.net/play-rust/images/d/d8/SMG_Body_icon.png"},
            {"sticks","http://vignette1.wikia.nocookie.net/play-rust/images/d/d5/Sticks_icon.png"},
            {"tarp","http://vignette4.wikia.nocookie.net/play-rust/images/1/12/Tarp_icon.png"},
            {"techparts","http://vignette2.wikia.nocookie.net/play-rust/images/e/eb/Tech_Trash_icon.png"},
            {"hazmatsuit","http://vignette2.wikia.nocookie.net/play-rust/images/3/36/Hazmat_Suit_icon.png"},
            {"pistol.m92","http://vignette2.wikia.nocookie.net/play-rust/images/4/43/M92_Pistol_icon.png"},
            {"semibody","http://vignette2.wikia.nocookie.net/play-rust/images/a/ac/Semi_Automatic_Body_icon.png"},
            {"blueprintbase","http://vignette3.wikia.nocookie.net/play-rust/images/8/83/Blueprint_icon.png"},
            {"pistol.python","http://vignette2.wikia.nocookie.net/play-rust/images/d/d4/Python_Revolver_icon.png"},
            {"clone.corn","http://vignette4.wikia.nocookie.net/play-rust/images/6/65/Corn_Clone_icon.png"},
            {"clone.hemp","http://vignette2.wikia.nocookie.net/play-rust/images/c/c9/Hemp_Clone_icon.png"},
            {"clone.pumpkin","http://vignette4.wikia.nocookie.net/play-rust/images/8/82/Pumpkin_Plant_Clone_icon.png"},
            {"vending.machine","http://vignette2.wikia.nocookie.net/play-rust/images/5/5c/Vending_Machine_icon.png"},
            {"flameturret","http://vignette3.wikia.nocookie.net/play-rust/images/f/f9/Flame_Turret_icon.png"},
            {"fridge","http://vignette4.wikia.nocookie.net/play-rust/images/8/88/Fridge_icon.png"},
            {"tunalight","http://vignette2.wikia.nocookie.net/play-rust/images/b/b2/Tuna_Can_Lamp_icon.png"},
            {"door.closer","http://i.imgur.com/QIKkGqT.png"},
            {"heavy.plate.pants","http://vignette4.wikia.nocookie.net/play-rust/images/1/1e/Heavy_Plate_Pants_icon.png"},
            {"heavy.plate.jacket","http://vignette1.wikia.nocookie.net/play-rust/images/c/c7/Heavy_Plate_Jacket_icon.png"},
            {"heavy.plate.helmet","http://vignette3.wikia.nocookie.net/play-rust/images/c/cb/Heavy_Plate_Helmet_icon.png"},
            {"chair","http://vignette1.wikia.nocookie.net/play-rust/images/3/3c/Chair_icon.png"},
            {"wall.frame.shopfront.metal","http://vignette2.wikia.nocookie.net/play-rust/images/4/46/Metal_Shop_Front_icon.png"},
            {"table","http://vignette3.wikia.nocookie.net/play-rust/images/5/5d/Table_icon.png"},
            {"planter.small","http://vignette3.wikia.nocookie.net/play-rust/images/a/a7/Small_Planter_Box_icon.png"},
            {"rug","http://vignette3.wikia.nocookie.net/play-rust/images/c/c5/Rug_icon.png"},
            {"rug.bear", "https://vignette4.wikia.nocookie.net/play-rust/images/d/d2/Rug_Bear_Skin_icon.png" },
            {"planter.large","http://vignette1.wikia.nocookie.net/play-rust/images/3/35/Large_Planter_Box_icon.png"},
            {"locker","http://vignette3.wikia.nocookie.net/play-rust/images/3/39/Locker_icon.png"},
            {"wall.frame.netting","http://vignette1.wikia.nocookie.net/play-rust/images/b/bc/Netting_icon.png"},
            {"spinner.wheel","http://vignette1.wikia.nocookie.net/play-rust/images/5/51/Spinning_wheel_icon.png"},
            {"tool.binoculars", "https://vignette2.wikia.nocookie.net/play-rust/images/1/1b/Binoculars_icon.png" },
            {"wood.armor.helmet", "https://vignette1.wikia.nocookie.net/play-rust/images/0/0f/Ef4af380406f0c3385ed80fc87971b60.png" },            
            {"deermeat.raw","http://imgur.com/hpL2I64.png"},
            {"deermeat.burned","http://imgur.com/f1eVA0W.png"},
            {"deermeat.cooked","http://imgur.com/e5Z6w1y.png"},
            {"searchlight", "https://vignette2.wikia.nocookie.net/play-rust/images/c/c6/Search_Light_icon.png" },
            {"weapon.mod.simplesight", "https://vignette1.wikia.nocookie.net/play-rust/images/9/93/Simple_Handmade_Sight_icon.png" },
            {"guntrap", "http://i.imgur.com/iNFOxbT.png" },
            {"dropbox", "http://i.imgur.com/KqV8FcU.png" },
            {"mailbox", "http://i.imgur.com/DaDrDIK.png" },
       };
        #endregion

        #region API
        [HookMethod("AddImage")]
        public bool AddImage(string url, string imageName, ulong imageId)
        {
            assets.BeginIndividual($"{imageName}_{imageId}", url);
            return true;
        } 
         
        [HookMethod("AddImageData")]
        public bool AddImageData(string imageName, byte[]array, ulong imageId)
        {
            assets.BeginIndividual($"{imageName}_{imageId}", string.Empty, array);
            return true;
        }

        [HookMethod("ForceFullDownload")]
        public void ForceFullDownload(string title)
        {
            Dictionary<string, string> newLoadOrder = new Dictionary<string, string>();
            foreach (var image in imageUrls.URLs)
            {
                var index = image.Key.IndexOf("_");
                string shortname = image.Key.Substring(0, index + 1);
                ulong skinId = ulong.Parse(image.Key.Substring(index + 1));
                if (HasImage(shortname, skinId))
                    continue;
                newLoadOrder.Add(image.Key, image.Value);
            }
            if (newLoadOrder.Count > 0)
            {
                loadOrders.Enqueue(new LoadOrder(title, newLoadOrder));
                if (!orderPending)
                    ProcessLoadOrders();
            }
        }

        [HookMethod("GetImageURL")]
        public string GetImageURL(string imageName, ulong imageId = 0)
        {
            string identifier = $"{imageName}_{imageId}";
            string value;
            if (imageUrls.URLs.TryGetValue(identifier, out value))
                return value;
            return imageIdentifiers.imageIds["NONE_0"];
        }

        [HookMethod("GetImage")]
        public string GetImage(string imageName, ulong imageId = 0, bool returnUrl = false)
        {
            string identifier = $"{imageName}_{imageId}";
            string value;
            if (imageIdentifiers.imageIds.TryGetValue(identifier, out value))
                return value;
            if (returnUrl && imageUrls.URLs.TryGetValue(identifier, out value))
            {
                AddImage(value, imageName, imageId);
                return value;
            }            
            return imageIdentifiers.imageIds["NONE_0"];
        }

        [HookMethod("GetImageList")]
        public List<ulong> GetImageList(string name)
        {
            List<ulong> skinIds = new List<ulong>();
            var matches = imageUrls.URLs.Keys.Where(x => x.StartsWith(name)).ToArray();
            for (int i = 0; i < matches.Length; i++)
            {
                var index = matches[i].IndexOf("_");
                if (matches[i].Substring(0, index) == name)
                {
                    ulong skinID;
                    if (ulong.TryParse(matches[i].Substring(index + 1), out skinID))
                        skinIds.Add(ulong.Parse(matches[i].Substring(index + 1)));
                }
            }
            return skinIds;
        }

        [HookMethod("GetSkinInfo")]
        public Dictionary<string, object> GetSkinInfo(string name, ulong id)
        {
            Dictionary<string, object> skinInfo;
            if (skinInformation.skinData.TryGetValue($"{name}_{id}", out skinInfo))            
                return skinInfo;            
            return null;            
        }

        [HookMethod("HasImage")]
        public bool HasImage(string imageName, ulong imageId) => imageIdentifiers.imageIds.ContainsKey($"{imageName}_{imageId}");

        [HookMethod("IsReady")]
        public bool IsReady() => loadOrders.Count == 0;

        [HookMethod("ImportImageList")]
        public void ImportImageList(string title, Dictionary<string, string> imageList, ulong imageId = 0, bool replace = false)
        {
            Dictionary<string, string> newLoadOrder = new Dictionary<string, string>();
            foreach (var image in imageList)
            {
                if (!replace && HasImage(image.Key, imageId))
                    continue;
                newLoadOrder[$"{image.Key}_{imageId}"] = image.Value;
            }
            if (newLoadOrder.Count > 0)
            {
                loadOrders.Enqueue(new LoadOrder(title, newLoadOrder));
                if (!orderPending)
                    ProcessLoadOrders();
            }
        }

        [HookMethod("ImportImageData")]
        public void ImportImageData(string title, Dictionary<string, byte[]> imageList, ulong imageId = 0, bool replace = false)
        {
            Dictionary<string, byte[]> newLoadOrder = new Dictionary<string, byte[]>();
            foreach (var image in imageList)
            {
                if (!replace && HasImage(image.Key, imageId))
                    continue;
                newLoadOrder[$"{image.Key}_{imageId}"] = image.Value;
            }
            if (newLoadOrder.Count > 0)
            {
                loadOrders.Enqueue(new LoadOrder(title, newLoadOrder));
                if (!orderPending)
                    ProcessLoadOrders();
            }
        }

        [HookMethod("LoadImageList")]
        public void LoadImageList(string title, List<KeyValuePair<string, ulong>> imageList)
        {
            Dictionary<string, string> newLoadOrder = new Dictionary<string, string>();
            foreach (var image in imageList)
            {
                if (HasImage(image.Key, image.Value))
                    continue;
                string identifier = $"{image.Key}_{image.Value}";
                if (imageUrls.URLs.ContainsKey(identifier))
                    newLoadOrder[identifier] = imageUrls.URLs[identifier];
            }
            if (newLoadOrder.Count > 0)
            {
                loadOrders.Enqueue(new LoadOrder(title, newLoadOrder));
                if (!orderPending)
                    ProcessLoadOrders();
            }
        }
        #endregion

        #region Commands
        [ConsoleCommand("workshopimages")]
        private void cmdWorkshopImages(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.authLevel > 0)
            {                               
                ServerMgr.Instance.StartCoroutine(GetWorkshopSkins());
            }
        }

        [ConsoleCommand("refreshallimages")]
        private void cmdRefreshAllImages(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.authLevel > 0)
            {
                PrintWarning("Reloading all images!");
                RefreshImagery();
            }
        }

        [ConsoleCommand("cancelstorage")]
        private void cmdCancelStorage(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.authLevel > 0)
            {
                if (!orderPending)
                    PrintWarning("No images are currently being downloaded");
                else
                {
                    assets.ClearList();
                    loadOrders.Clear();
                    PrintWarning("Pending image downloads have been cancelled!");
                }
            }
        }
        #endregion

        #region Image Storage
        class LoadOrder
        {
            public string loadName;
            public Dictionary<string, string> imageList = new Dictionary<string, string>();
            public Dictionary<string, byte[]> imageData = new Dictionary<string, byte[]>();

            public LoadOrder() { }
            public LoadOrder(string loadName, Dictionary<string, string> imageList)
            {
                this.loadName = loadName;
                this.imageList = imageList;
            }
            public LoadOrder(string loadName, Dictionary<string, byte[]> imageData)
            {
                this.loadName = loadName;
                this.imageData = imageData;
            }
        }
        class ImageAssets : MonoBehaviour
        {
            private Queue<QueueItem> queueList = new Queue<QueueItem>();
            private bool isLoading;
            private double nextUpdate;
            private int listCount;
            private string request;

            private void OnDestroy()
            {
                queueList.Clear();
            }
            public void Add(string name, string url = null, byte[] bytes = null)
            {
                queueList.Enqueue(new QueueItem(name, url, bytes));
            }
            public void BeginIndividual(string name, string url = null, byte[] bytes = null)
            {
                queueList.Enqueue(new QueueItem(name, url, bytes));
                if (queueList.Count == 1)
                    Next();
            }
            public void BeginLoad(string request)
            {
                this.request = request;
                nextUpdate = UnityEngine.Time.time + il.configData.UpdateInterval;
                listCount = queueList.Count;
                Next();
            }            
            public void ClearList()
            {
                queueList.Clear();
                il.orderPending = false;
            }
            private void Next()
            {
                if (queueList.Count <= 0)
                {
                    il.orderPending = false;
                    il.SaveData();
                    if (!string.IsNullOrEmpty(request))
                        print($"Image batch ({request}) has been stored successfully");

                    request = string.Empty;
                    listCount = 0;

                    il.ProcessLoadOrders();                    
                    return;
                }
                if (il.configData.ShowProgress && listCount > 1)
                {
                    var time = UnityEngine.Time.time;
                    if (time > nextUpdate)
                    {
                        var amountDone = listCount - queueList.Count;
                        print($"{request} storage process at {Math.Round(((float)amountDone / (float)listCount) * 100, 0)}% ({amountDone}/{listCount})");
                        nextUpdate = time + il.configData.UpdateInterval;
                    }
                }
                isLoading = true;

                QueueItem queueItem = queueList.Dequeue();
                if (!string.IsNullOrEmpty(queueItem.url))
                    StartCoroutine(DownloadImage(queueItem));
                else StoreByteArray(queueItem.bytes, queueItem.name);
            }
            IEnumerator DownloadImage(QueueItem info)
            {
                using (var www = new WWW(info.url))
                {
                    yield return www;
                    if (il == null) yield break;
                    if (info.bytes == null && www.error != null)
                    {
                        print(string.Format("Image loading fail! Error: {0} - Image Name: {1} - Image URL: {2}", www.error, info.name, info.url));
                    }
                    else
                    {
                        var tex = www.texture;
                        byte[] bytes = tex.EncodeToPNG();                        
                        StoreByteArray(bytes, info.name);
                        DestroyImmediate(tex);
                        yield break;
                    }
                    isLoading = false;
                    Next();                    
                }
            }
            internal void StoreByteArray(byte[] bytes, string name)
            {
                if (bytes != null) il.imageIdentifiers.imageIds[name] = FileStorage.server.Store(bytes, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();
                isLoading = false;
                Next();
            }
            internal class QueueItem
            {
                public byte[] bytes;
                public string url;
                public string name;
                public QueueItem(string name, string url = null, byte[] bytes = null)
                {
                    this.bytes = bytes;
                    this.url = url;
                    this.name = name;
                }
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Avatars - Store player avatars")]
            public bool StoreAvatars { get; set; }
            [JsonProperty(PropertyName = "Workshop - Download workshop images")]
            public bool WorkshopImages { get; set; }
            [JsonProperty(PropertyName = "Images - Only download image's when required")]
            public bool DLWhenNecessary { get; set; }
            [JsonProperty(PropertyName = "Workshop - Pull skin information")]
            public bool GetSkinData { get; set; }
            [JsonProperty(PropertyName = "Progress - Show download progress in console")]
            public bool ShowProgress { get; set; }
            [JsonProperty(PropertyName = "Progress - Time between update notifications")]
            public int UpdateInterval { get; set; }
            [JsonProperty(PropertyName = "User Images - Manually define images to be loaded")]
            public Dictionary<string,string> UserImages { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                GetSkinData = true,
                ShowProgress = true,
                StoreAvatars = true,
                WorkshopImages = true,
                DLWhenNecessary = true,
                UpdateInterval = 20,
                UserImages = new Dictionary<string, string>
                {
                    {"worldmap", string.Empty }
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Data Management
        void SaveData() => identifiers.WriteObject(imageIdentifiers);
        void SaveSkinInfo() => skininfo.WriteObject(skinInformation);
        void SaveUrls() => urls.WriteObject(imageUrls);

        void LoadData()
        {
            try
            {
                imageIdentifiers = identifiers.ReadObject<ImageIdentifiers>();
            }
            catch
            {
                imageIdentifiers = new ImageIdentifiers();
            }
            try
            {
                skinInformation = skininfo.ReadObject<SkinInformation>();
            }
            catch
            {
                skinInformation = new SkinInformation();
            }
            try
            {
                imageUrls = urls.ReadObject<ImageURLs>();
            }
            catch
            {
                imageUrls = new ImageURLs();                               
            }
            if (skinInformation == null)
                skinInformation = new SkinInformation();
            if (imageIdentifiers == null)
                imageIdentifiers = new ImageIdentifiers();
            if (imageUrls == null)
                imageUrls = new ImageURLs();
            if (imageUrls.URLs.Count == 0)
            {
                foreach (var item in defaultUrls)
                    imageUrls.URLs.Add($"{item.Key}_0", item.Value);
            }
        }
        class ImageIdentifiers
        {
            public uint lastCEID;
            public Hash<string, string> imageIds = new Hash<string, string>();
        }
        class SkinInformation
        {
            public Hash<string, Dictionary<string, object>> skinData = new Hash<string, Dictionary<string, object>>();
        }
        class ImageURLs
        {
            public Hash<string, string> URLs = new Hash<string, string>();
        }
        #endregion
    }
}