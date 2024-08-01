using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using ATS_API.Helpers;
using Eremite;
using Eremite.Buildings;
using Eremite.Characters.Villagers;
using Eremite.Model;
using Eremite.Model.Orders;
using Eremite.Model.State;
using Eremite.Services;
using Eremite.Services.Monitors;
using Newtonsoft.Json.Linq;
using QFSW.QC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UniRx;
using UnityEngine;

namespace Ryguy9999.ATS.ATSForAP {
    class ArchipelagoService : GameService, IGameService, IService {
        private static Dictionary<string, Subject<int>> itemCallbacks = new Dictionary<string, Subject<int>>();
        private static ArchipelagoSession session;
        private static long DeathlinkState = -1;
        private static DeathLinkService deathLinkService;
        private static List<IDisposable> GameSubscriptions = new List<IDisposable>();
        private static Queue<string> LocationQueue = new Queue<string>();
        private static Dictionary<string, Sprite> OriginalGoodIcons = new Dictionary<string, Sprite>();
        private static bool ShowAPLockIcons = true;
        private static List<(Sprite icon, string message, string detail)> ItemsForNews = new List<(Sprite icon, string message, string detail)>();
        private static int VillagersToSpawn = 0;
        private static News ApConnectionNews;
        private static List<int> ReputationLocationIndices = new List<int>();

        public static bool TreatBlueprintsAsItems = false;


        /*
        // TODO DELETEME
        [Command("ap.connect", "Connects to Archipelago server. Requires url:port, slotName, and optionally password.", Platform.AllPlatforms, MonoTargetType.Single)]
        public static void InitializeAPConnection() {
            InitializeAPConnection("localhost:38281", "RyanATS", null);
        }
        // TODO DELETEME
        [Command("ap.sendLocation", "For debugging purposes. Sends a location.", Platform.AllPlatforms, MonoTargetType.Single)]
        public static void SendLocation(string location) {
            CheckLocation(location);
        }
        //*/


        [Command("ap.connect", "Connects to Archipelago server. Requires url:port, slotName, and optionally password.", Platform.AllPlatforms, MonoTargetType.Single)]
        public static void InitializeAPConnection([APUrlSuggestion] string url, string player) {
            InitializeAPConnection(url, player, null);
        }

        [Command("ap.connect", "Connects to Archipelago server. Requires url:port, slotName, and optionally password.", Platform.AllPlatforms, MonoTargetType.Single)]
        public static void InitializeAPConnection([APUrlSuggestion] string url, string player, string password) {
            // TODO more trade locations. configure number of random ones like subnautica fish scans?
            // TODO event decisions tag locations
            // TODO food/service utopia locs?
            // TODO global resolve filler items?
            // TODO goal options, ie require multiple tasks per phase or Item macguffins
            // TODO locations in sealed forest as well
            // TODO add option to continue normal BP drafts in addition to Items
            // TODO harder logic mode

            // TODO item gifting?

            // TODO more convenient good blocking, eg event rewards (patch? Relic.AddAllRewards)
            // TODO preset training expedition to recommended settings
            // TODO block Shift N next season
            // TODO block checks after settlement complete

            // TODO check all 50 Resolve locations, some dont send?
            // TODO repro goal softlock
            // TODO repro .3.3 -> .4.x lizard 1st rep doesnt send unti 5th order

            if (session != null) {
                DisconnectFromGame();
            }

            session = ArchipelagoSessionFactory.CreateSession(url);
            LoginResult loginResult;
            try {
                loginResult = session.TryConnectAndLogin(Constants.AP_GAME_NAME, player, ItemsHandlingFlags.AllItems, password: password);
            }
            catch (Exception e) {
                loginResult = new LoginFailure(e.GetBaseException().Message);
            }

            if (!loginResult.Successful) {
                LoginFailure failure = (LoginFailure)loginResult;
                string errorMessage = $"Failed to Connect to {url} as {player}:";
                foreach (string error in failure.Errors) {
                    errorMessage += $"\n    {error}";
                }
                foreach (ConnectionRefusedError error in failure.ErrorCodes) {
                    errorMessage += $"\n    {error}";
                }

                Plugin.Log(errorMessage);
                GameMB.NewsService.PublishNews("Failed to connect to AP!", "The connection to the AP server failed. Check the BepInEx console for potentially more information.", AlertSeverity.Critical);
                return;
            }
            LoginSuccessful loginSuccess = loginResult as LoginSuccessful;

            if(GameMB.IsGameActive) {
                SyncGameStateToAP();
                if(ApConnectionNews != null) {
                    GameMB.NewsService.RemoveNews(ApConnectionNews);
                }
            }

            if(LocationQueue.Any()) {
                Plugin.Log($"Detected {LocationQueue.Count} location(s) stored while disconnected from AP. Sending now.");
                var flushQueue = new Queue<string>(LocationQueue);
                LocationQueue.Clear();

                while(flushQueue.Any()) {
                    CheckLocation(flushQueue.Dequeue());
                }
            }

            if (loginSuccess.SlotData.ContainsKey("blueprint_items")) {
                TreatBlueprintsAsItems = (long)loginSuccess.SlotData["blueprint_items"] == 1;
            } else {
                Plugin.Log("Could not find blueprint_items in SlotData, falling back to vanilla blueprint behavior.");
                TreatBlueprintsAsItems = false;
            }

            if (loginSuccess.SlotData.ContainsKey("rep_location_indices")) {
                ReputationLocationIndices = (loginSuccess.SlotData["rep_location_indices"] as JArray).ToObject<List<int>>();
            } else {
                Plugin.Log("Could not find rep_location_indices in SlotData, falling back to 1st and 10th Rep.");
                ReputationLocationIndices = new List<int> { 1, 10 };
            }
            if ((long)loginSuccess.SlotData["recipe_shuffle"] >= Constants.RECIPE_SHUFFLE_EXCLUDE_CRUDE_WS) {
                if (loginSuccess.SlotData.ContainsKey("production_recipes")) {
                    Dictionary<string, List<List<JValue>>> recipeDict = (loginSuccess.SlotData["production_recipes"] as JObject).ToObject<Dictionary<string, List<List<JValue>>>>();

                    LoadBuildingRecipesFromSlotData(recipeDict);
                } else {
                    Plugin.Log("Could not find production_recipes in SlotData, falling back to SlotData seed.");

                    if (loginSuccess.SlotData.ContainsKey("seed")) {
                        RandomizeBuildingRecipes((long)loginSuccess.SlotData["recipe_shuffle"] == Constants.RECIPE_SHUFFLE_EXCLUDE_CRUDE_WS, (long)loginSuccess.SlotData["seed"]);
                    } else {
                        Plugin.Log("Could not find seed in SlotData, falling back to random seed.");
                        RandomizeBuildingRecipes((long)loginSuccess.SlotData["recipe_shuffle"] == Constants.RECIPE_SHUFFLE_EXCLUDE_CRUDE_WS);
                    }
                }

                if (GameMB.IsGameActive && GameMB.GameSaveService.IsNewGame()) {
                    GameMB.NewsService.PublishNews("Cannot randomize recipes in a game!", "Recipe Shuffle option detected from AP. Unfortunately, recipe randomization only takes effect on creation of a new settlement. Your recipes have been shuffled! It just won't affect this settlement.", AlertSeverity.Warning);
                }
            }

            session.Items.ItemReceived += (receivedItemsHelper) => {
                ItemInfo item = session.Items.DequeueItem();
                if(GameMB.IsGameActive && item.ItemName != null) {
                    HandleItemReceived(item.ItemName);
                }
            };
            while(session.Items.Any()) {
                session.Items.DequeueItem();
            }

            session.MessageLog.OnMessageReceived += (message) => {
                var console = GameObject.FindObjectOfType<QuantumConsole>();
                if(console != null) {
                    console.LogToConsole(message.ToString());
                }
            };

            if (loginSuccess.SlotData.ContainsKey("deathlink")) {
                DeathlinkState = (long)loginSuccess.SlotData["deathlink"];
            } else {
                Plugin.Log("Could not find deathlink in SlotData, falling back to off.");
                DeathlinkState = Constants.DEATHLINK_OFF;
            }
            if ((long)loginSuccess.SlotData["deathlink"] >= Constants.DEATHLINK_DEATH_ONLY) {
                deathLinkService = session.CreateDeathLinkService();
                deathLinkService.EnableDeathLink();
                deathLinkService.OnDeathLinkReceived += (deathLink) => {
                    if(GameMB.IsGameActive) {
                        GameMB.VillagersService.KillVillagers(1, VillagerLossType.Death, Constants.DEATHLINK_REASON);
                    }
                };
            }
        }

        public static bool HasReceivedItem(string item) {
            item = Constants.ITEM_DICT.ContainsKey(item) ? Constants.ITEM_DICT[item].ToName() : item;

            return !StateService.Effects.rawGoodsProductionBonus.ContainsKey(item) || StateService.Effects.rawGoodsProductionBonus[item] > -Constants.PRODUCTIVITY_MODIFIER / 2;
        }

        public static void SyncGameStateToAP() {
            if(session == null) {
                Plugin.Log("Tried to sync to AP with a null AP session.");
                return;
            }

            // TODO replace Amber payment with location scout
            if (!GameMB.StateService.Trade.tradeTowns.Any(town => town.id == Constants.TRADE_TOWN_ID)) {
                var offers = new List<TownOfferState>();
                foreach (var loc in session.Locations.AllMissingLocations) {
                    var location = session.Locations.GetLocationNameFromId(loc);

                    Match match = new Regex(@"Trade - (\d+) (.+)").Match(location);
                    if (match.Success) {
                        int tradeQty = Int32.Parse(match.Groups[1].ToString());
                        string tradeItem = match.Groups[2].ToString();

                        // v0.3.3 apworld -> v0.4.x mod compatibility
                        tradeItem = tradeItem.Replace("Packs of ", "Pack of ");

                        if (Constants.ITEM_DICT.ContainsKey(tradeItem)) {
                            tradeItem = Constants.ITEM_DICT[tradeItem].ToName();
                        } else if (tradeItem.Contains("Water")) {
                            tradeItem = "[Water] " + tradeItem;
                        }

                        offers.Add(new TownOfferState {
                            townId = Constants.TRADE_TOWN_ID,
                            townName = "Archipelago",
                            hasStaticName = true,
                            good = new Good(tradeItem, tradeQty),
                            price = 0,
                            fuel = 0,
                            travelTime = 0.01f
                        });
                    }
                }
                GameMB.StateService.Trade.tradeTowns.Add(new TradeTownState {
                    id = Constants.TRADE_TOWN_ID,
                    distance = 0,
                    townName = "Archipelago",
                    hasStaticName = true,
                    biome = "Royal Woodlands",
                    // TODO look into custom faction/biome??
                    //faction = null,
                    //bonusRoutes = 0,
                    isMaxStanding = true,
                    offers = offers
                });
            }

            foreach (var item in session.Items.AllItemsReceived) {
                if(Constants.ITEM_DICT.ContainsKey(item.ItemName)) {
                    // Goods items will be handled in the next loop
                    continue;
                }

                Match match = new Regex(@"(\d+) Starting (.+)").Match(item.ItemName);
                // TODO in non new games, no way of knowing which filler was already received
                if (match.Success && GameMB.GameSaveService.IsNewGame()) {
                    var fillerQty = Int32.Parse(match.Groups[1].ToString());
                    var fillerType = match.Groups[2].ToString();
                    if (fillerType == "Villagers") {
                        VillagersToSpawn += fillerQty;
                    } else {
                        if(!Constants.ITEM_DICT.ContainsKey(fillerType)) {
                            Plugin.Log("Could not find filler item: " + fillerType);
                        }
                        GameMB.StorageService.Store(new Good(Constants.ITEM_DICT[fillerType].ToName(), fillerQty), StorageOperationType.Other);
                        ItemsForNews.Add((GameMB.Settings.GetGoodIcon(Constants.ITEM_DICT[fillerType].ToName()), $"{fillerQty} {fillerType} received from AP!", $"{fillerType} received. You will also receive this bonus in all future settlements."));
                    }

                    continue;
                }

                // If it wasn't in ITEM_DICT and wasn't .*Starting.* filler, it's a blueprint
                HandleItemReceived(item.ItemName);
            }

            foreach (KeyValuePair<string, GoodsTypes> pair in Constants.ITEM_DICT) {
                if (session.Items.AllItemsReceived.Any(item => item.ItemName == pair.Key)) {
                    if (!HasReceivedItem(pair.Key)) {
                        HandleItemReceived(pair.Key);
                    }
                    // No else, AP thinks we have the item, and the game state does as well
                } else {
                    if(ShowAPLockIcons) {
                        if (!OriginalGoodIcons.Keys.Contains(pair.Value.ToName())) {
                            OriginalGoodIcons.Add(pair.Value.ToName(), GameMB.Settings.GetGood(pair.Value.ToName()).icon);
                        }
                        GameMB.Settings.GetGood(pair.Value.ToName()).icon = TextureHelper.GetImageAsSprite("good-locked.png", TextureHelper.SpriteType.EffectIcon);
                    }

                    if (HasReceivedItem(pair.Key)) {
                        SO.EffectsService.GrantRawGoodProduction(pair.Value.ToName(), -Constants.PRODUCTIVITY_MODIFIER);
                    }
                    // No else, AP thinks we shouldn't have the item, further decreasing its productivity would cause a bug
                }
            }
        }

        public static void EnterGame() {
            if(session == null) {
                GameMB.NewsService.PublishNews("No AP session detected! Remember to connect!", "ap.connect url:port slotName [password]", AlertSeverity.Critical);
                ApConnectionNews = (GameMB.NewsService.News.GetType().GetProperty("Value").GetValue(GameMB.NewsService.News, null) as List<News>)[0];
            } else {
                SyncGameStateToAP();
            }

            if(GameSubscriptions.Count > 0) {
                foreach (IDisposable disposable in GameSubscriptions) {
                    disposable.Dispose();
                }
            }

            GameSubscriptions.Add(GameMB.OrdersService.OnOrderCompleted.Subscribe(new Action<OrderState>(HandleOrderRewards)));
            GameSubscriptions.Add(GameMB.ReputationService.OnGameResult.Subscribe(new Action<bool>(HandleGameResult)));
            GameSubscriptions.Add(GameMB.GameBlackboardService.OnHubLeveledUp.Subscribe(new Action<Hearth>(HandleHubLevelUp)));
            GameSubscriptions.Add(GameMB.GameBlackboardService.OnRelicResolved.Subscribe(new Action<Relic>(HandleRelicResolve)));
            GameSubscriptions.Add(GameMB.RXService.Interval(1, true).Subscribe(new Action(HandleSlowUpdate)));
            GameSubscriptions.Add(GameMB.TradeRoutesService.OnStandingLeveledUp.Subscribe(new Action<TradeTownState>(HandleStandingLevelUp)));
            // TODO location routes will trigger in game route completions. Minor unintended behavior
            GameSubscriptions.Add(GameMB.TradeRoutesService.OnRouteCollected.Subscribe(new Action<RouteState>(HandleTradeRouteCollect)));
        }

        [Command("ap.toggleLockIcons", "Toggles items locked by AP should have the AP lock icon.", Platform.AllPlatforms, MonoTargetType.Single)]
        public static void ToggleLockIcons() {
            ToggleLockIcons(!ShowAPLockIcons);
        }

        [Command("ap.toggleLockIcons", "Toggles items locked by AP should have the AP lock icon.", Platform.AllPlatforms, MonoTargetType.Single)]
        public static void ToggleLockIcons(bool trueFalse) {
            if(ShowAPLockIcons == trueFalse) {
                return;
            }
            ShowAPLockIcons = trueFalse;

            foreach (KeyValuePair<string, GoodsTypes> pair in Constants.ITEM_DICT) {
                if (ShowAPLockIcons && !HasReceivedItem(pair.Key)) {
                    if(!OriginalGoodIcons.Keys.Contains(pair.Value.ToName())) {
                        OriginalGoodIcons.Add(pair.Value.ToName(), GameMB.Settings.GetGood(pair.Value.ToName()).icon);
                    }
                    GameMB.Settings.GetGood(pair.Value.ToName()).icon = TextureHelper.GetImageAsSprite("good-locked.png", TextureHelper.SpriteType.EffectIcon);
                } else {
                    if (OriginalGoodIcons.Keys.Contains(pair.Value.ToName())) {
                        GameMB.Settings.GetGood(pair.Value.ToName()).icon = OriginalGoodIcons[pair.Value.ToName()];
                    }
                }
            }
        }

        [Command("ap.restoreProduction", "For debugging purposes. Removes the negative production modifiers applied by the AP mod.")]
        public static void RestoreProduction() {
            foreach (KeyValuePair<string, GoodsTypes> pair in Constants.ITEM_DICT) {
                string itemId = pair.Value.ToName();
                if (!HasReceivedItem(itemId)) {
                    SO.EffectsService.GrantRawGoodProduction(itemId, Constants.PRODUCTIVITY_MODIFIER);
                }
            }
        }

        [Command("ap.disconnect", "Disconnects from Archipelago server.", Platform.AllPlatforms, MonoTargetType.Single)]
        public static void DisconnectFromGame() {
            if(session != null) {
                session.Socket.DisconnectAsync();
                session = null;
            }

            foreach(IDisposable disposable in GameSubscriptions) {
                disposable.Dispose();
            }
        }

        [Command("ap.randomizeRecipes", "For debugging purposes. Randomizes building recipes. Will only take effect on a new settlement.")]
        public static void RandomizeBuildingRecipes() {
            RandomizeBuildingRecipes(false, new System.Random().Next());
        }
        [Command("ap.randomizeRecipes", "For debugging purposes. Randomizes building recipes. Will only take effect on a new settlement.")]
        public static void RandomizeBuildingRecipes(bool skipCrudeWS) {
            RandomizeBuildingRecipes(skipCrudeWS, new System.Random().Next());
        }
        [Command("ap.randomizeRecipes", "For debugging purposes. Randomizes building recipes. Will only take effect on a new settlement.")]
        public static void RandomizeBuildingRecipes(bool skipCrudeWS, long seed) {
            var recipeList = new List<WorkshopRecipeModel>();
            foreach (BuildingModel buildingModel in GameMB.Settings.Buildings) {
                WorkshopModel workshopModel = buildingModel as WorkshopModel;
                if (workshopModel == null || skipCrudeWS && workshopModel == (MB.Settings.GetBuilding("Crude Workstation") as WorkshopModel)) {
                    continue;
                }

                recipeList.AddRange(workshopModel.recipes);
            }
            var rng = new System.Random(Convert.ToInt32(seed));
            foreach (BuildingModel buildingModel in Serviceable.Settings.Buildings) {
                WorkshopModel workshopModel = buildingModel as WorkshopModel;
                if (workshopModel == null || skipCrudeWS && workshopModel == (MB.Settings.GetBuilding("Crude Workstation") as WorkshopModel)) {
                    continue;
                }

                for (int i = 0; i < workshopModel.recipes.Length; i++) {
                    var nextRecipeIndex = rng.Next(recipeList.Count);
                    workshopModel.recipes[i] = recipeList[nextRecipeIndex];
                    recipeList.RemoveAt(nextRecipeIndex);
                }
            }

            // This service is responsible for the tooltips that show where goods are produced. It's capable of remapping the recipes after randomization, we just need to reach inside and tell it to do so
            StaticRecipesService.GetType().GetField("goodsSourcesMap", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(SO.StaticRecipesService, new Dictionary<string, List<BuildingModel>>());
            StaticRecipesService.GetType().GetMethod("MapGoodsSources", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(SO.StaticRecipesService, new object[0]);
        }

        private static void LoadBuildingRecipesFromSlotData(Dictionary<string, List<List<JValue>>> recipeDict) {
            // Use existing recipes from the game rather than reconstruct them ourselves
            var recipeList = new List<WorkshopRecipeModel>();
            foreach (BuildingModel buildingModel in GameMB.Settings.Buildings) {
                WorkshopModel workshopModel = buildingModel as WorkshopModel;
                if (workshopModel == null) {
                    continue;
                }

                recipeList.AddRange(workshopModel.recipes);
            }

            foreach (var building in recipeDict) {
                if(!GameMB.Settings.ContainsBuilding(GetIDFromWorkshopName(building.Key))) {
                    Plugin.Log("Unknown building key from SlotData: " + building.Key);
                    continue;
                }
                WorkshopModel model = GameMB.Settings.GetBuilding(GetIDFromWorkshopName(building.Key)) as WorkshopModel;
                if(model == null) {
                    Plugin.Log("Null workshop model for building: " + building.Key);
                    continue;
                }

                var i = 0;
                foreach(var recipe in building.Value) {
                    var recipeModelIndex = recipeList.FindIndex(r => r.GetProducedGood() == Constants.ITEM_DICT[recipe[0].ToObject<string>()].ToName() && r.grade.level == recipe[1].ToObject<int>());

                    model.recipes[i] = recipeList[recipeModelIndex];
                    recipeList.RemoveAt(recipeModelIndex);
                    i++;
                }
            }

            // This service is responsible for the tooltips that show where goods are produced. It's capable of remapping the recipes after randomization, we just need to reach inside and tell it to do so
            StaticRecipesService.GetType().GetField("goodsSourcesMap", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(SO.StaticRecipesService, new Dictionary<string, List<BuildingModel>>());
            StaticRecipesService.GetType().GetMethod("MapGoodsSources", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(SO.StaticRecipesService, new object[0]);
        }

        [Command("ap.say", "Sends text to AP server, to use for AP chat log, !commands, etc.")]
        public static void SendMessageToAP(string message) {
            session.Socket.SendPacket(new SayPacket() { Text = message });
        }

        private static Dictionary<long, int> locationsAlreadySent = new Dictionary<long, int>();
        public static bool CheckLocation(string location) {
            if(session == null) {
                LocationQueue.Enqueue(location);
            }

            var locId = session.Locations.GetLocationIdFromName(Constants.AP_GAME_NAME, location);
            if (locId < 0) {
                Plugin.Log($"Location with unknown id: {location}");
                return false;
            }
            if(session.Locations.AllLocationsChecked.Contains(locId)) {
                if(locationsAlreadySent.ContainsKey(locId)) {
                    locationsAlreadySent[locId]++;
                    if (locationsAlreadySent[locId] == 10) {
                        Plugin.Log($"Silencing: {location}");
                    } else if(locationsAlreadySent[locId] < 10) {
                        Plugin.Log($"Location already checked: {location}");
                    }
                } else {
                    locationsAlreadySent[locId] = 1;
                    Plugin.Log($"Location already checked: {location}");
                }
                return false;
            }

            Plugin.Log($"Sending AP Location: {location}");
            session.Locations.CompleteLocationChecks(locId);
            return true;
        }

        private static string GetOrdinalSuffix(int number) {
            switch(number) {
            case 1:
                return "st";
            case 2:
                return "nd";
            case 3:
                return "rd";
            default:
                return "th";
            }
        }

        private static string GetIDFromWorkshopName(string name) {
            return name
                .Replace("Druids Hut", "Druid")
                .Replace("Flawless Druids Hut", "Flawless Druid")
                .Replace("Alchemists Hut", "Alchemist Hut")
                .Replace("Teahouse", "Tea House")
                .Replace("Greenhouse", "Greenhouse Workshop")
                .Replace("Leatherworker", "Leatherworks")
                .Replace("Flawless Leatherworker", "Flawless Leatherworks")
                .Replace("Clay Pit", "Clay Pit Workshop")
                .Replace("Advanced Rain Collector", "Advanced Rain Catcher")
                .Replace("Lumber Mill", "Lumbermill");
        }

        private static void HandleOrderRewards(OrderState order) {
            int orderIndex = GameMB.StateService.Orders.currentOrders.FindIndex(o => o.id == order.id) + 1;
            CheckLocation("Completed Order - " + orderIndex + GetOrdinalSuffix(orderIndex) + " Pack");
        }

        private static void HandleGameResult(bool gameWon) {
            if(!gameWon) {
                return;
            }

            // Biome Victory
            CheckLocation("Victory - " + GameMB.MetaStateService.GameConditions.biome);

            // Sealed Forest (Goal) Victory
            if(GameMB.GameSealService.IsSealedBiome() && GameMB.GameSealService.IsSealCompleted()) {
                session.SetGoalAchieved();
            }
        }

        private static int GetFullyUpgradedHousedAmount(string speciesName) {
            int count = 0;

            foreach(var pair in GameMB.BuildingsService.Houses) {
                if(pair.Value.UpgradableState.level >= 2 && pair.Value.model.housingRaces.Contains(GameMB.Settings.GetRace(speciesName))) {
                    count += pair.Value.state.residents.Count;
                }
            }

            return count;
        }

        private static void HandleHubLevelUp(Hearth hearth) {
            var hearthLevel = hearth.state.hubIndex + 1;
            CheckLocation($"Upgraded Hearth - {hearthLevel + GetOrdinalSuffix(hearthLevel)} Tier");
        }

        private static void HandleRelicResolve(Relic relic) {
            if(relic.model.dangerLevel == DangerLevel.Dangerous) {
                CheckLocation("Complete a Dangerous Glade Event");
            }
            if(relic.model.dangerLevel == DangerLevel.Forbidden) {
                CheckLocation("Complete a Forbidden Glade Event");
            }
        }

        private static void HandleSlowUpdate() {
            if(!GameMB.IsGameActive) {
                return;
            }

            // Reputation from Resolve
            if (GameMB.RacesService.HasRace("Human") && GameMB.ResolveService.GetReputationGainFor("Human") >= 1) {
                CheckLocation("First Reputation through Resolve - Humans");
            }
            if (GameMB.RacesService.HasRace("Lizard") && GameMB.ResolveService.GetReputationGainFor("Lizard") >= 1) {
                CheckLocation("First Reputation through Resolve - Lizards");
            }
            if (GameMB.RacesService.HasRace("Beaver") && GameMB.ResolveService.GetReputationGainFor("Beaver") >= 1) {
                CheckLocation("First Reputation through Resolve - Beavers");
            }
            if (GameMB.RacesService.HasRace("Harpy") && GameMB.ResolveService.GetReputationGainFor("Harpy") >= 1) {
                CheckLocation("First Reputation through Resolve - Harpies");
            }
            if (GameMB.RacesService.HasRace("Foxes") && GameMB.ResolveService.GetReputationGainFor("Foxes") >= 1) {
                CheckLocation("First Reputation through Resolve - Foxes");
            }

            // Overall Reputation for biome
            var repGained = GameMB.ReputationService.GetReputationGainedFrom(ReputationChangeSource.Order) + GameMB.ReputationService.GetReputationGainedFrom(ReputationChangeSource.Other) + GameMB.ReputationService.GetReputationGainedFrom(ReputationChangeSource.Relics) + GameMB.ReputationService.GetReputationGainedFrom(ReputationChangeSource.Resolve);

            foreach (int repIndex in ReputationLocationIndices) {
                if (repGained >= repIndex) {
                    var biome = GameMB.MetaStateService.GameConditions.biome.Replace("Moorlands", "Scarlet Orchard");
                    CheckLocation($"{repIndex}{GetOrdinalSuffix(repIndex)} Reputation - {biome}");
                }
            }

            // 50 Resolve check
            if (GameMB.RacesService.HasRace("Human") && GameMB.ResolveService.GetResolveFor("Human") >= 50) {
                CheckLocation("50 Resolve - Humans");
            }
            if (GameMB.RacesService.HasRace("Lizard") && GameMB.ResolveService.GetResolveFor("Lizard") >= 50) {
                CheckLocation("50 Resolve - Lizards");
            }
            if (GameMB.RacesService.HasRace("Beaver") && GameMB.ResolveService.GetResolveFor("Beaver") >= 50) {
                CheckLocation("50 Resolve - Beavers");
            }
            if (GameMB.RacesService.HasRace("Harpy") && GameMB.ResolveService.GetResolveFor("Harpy") >= 50) {
                CheckLocation("50 Resolve - Harpies");
            }
            if (GameMB.RacesService.HasRace("Foxes") && GameMB.ResolveService.GetResolveFor("Foxes") >= 50) {
                CheckLocation("50 Resolve - Foxes");
            }

            // 30 Housed Villagers
            if (GameMB.RacesService.HasRace("Human") && GetFullyUpgradedHousedAmount("Human") >= 30) {
                CheckLocation("Have 30 Villagers in fully upgraded Housing - Humans");
            }
            if (GameMB.RacesService.HasRace("Beaver") && GetFullyUpgradedHousedAmount("Beaver") >= 30) {
                CheckLocation("Have 30 Villagers in fully upgraded Housing - Beavers");
            }
            if (GameMB.RacesService.HasRace("Lizard") && GetFullyUpgradedHousedAmount("Lizard") >= 30) {
                CheckLocation("Have 30 Villagers in fully upgraded Housing - Lizards");
            }
            if (GameMB.RacesService.HasRace("Harpy") && GetFullyUpgradedHousedAmount("Harpy") >= 30) {
                CheckLocation("Have 30 Villagers in fully upgraded Housing - Harpies");
            }
            if (GameMB.RacesService.HasRace("Foxes") && GetFullyUpgradedHousedAmount("Foxes") >= 30) {
                CheckLocation("Have 30 Villagers in fully upgraded Housing - Foxes");
            }

            // Handle filler villagers received mid game in a game thread, as ItemReceived causes crashes
            if (VillagersToSpawn > 0) {
                GameMB.NewsService.PublishNews($"{VillagersToSpawn} Villagers arrived!", $"{VillagersToSpawn} extra Villagers received from AP!", AlertSeverity.Info);
                while(VillagersToSpawn > 0) {
                    GameMB.VillagersService.SpawnNewVillager(SO.RacesService.GetRandom());
                    VillagersToSpawn--;
                }
            }

            // Handle filler item news in a game thread, as ItemReceived causes crashes
            while (ItemsForNews.Any()) {
                GameMB.NewsService.PublishNews(ItemsForNews[0].message, ItemsForNews[0].detail, AlertSeverity.Info, ItemsForNews[0].icon);
                ItemsForNews.RemoveAt(0);
            }
        }

        public static void HandleVillagerDeath(VillagerLossType lossType, string reasonKey) {
            if(session == null || reasonKey == Constants.DEATHLINK_REASON) {
                return;
            }
            if(lossType == VillagerLossType.Leave && DeathlinkState < Constants.DEATHLINK_LEAVE_AND_DEATH) {
                return;
            }
            if(lossType == VillagerLossType.Death && DeathlinkState < Constants.DEATHLINK_DEATH_ONLY) {
                return;
            }

            deathLinkService.SendDeathLink(new DeathLink(session.Players.GetPlayerName(session.ConnectionInfo.Slot), $"Let villager {(lossType == VillagerLossType.Leave ? "leave" : "perish")}."));
        }

        private static void HandleStandingLevelUp(TradeTownState tradeTownState) {
            for(int i = tradeTownState.standingLevel; i > 0; i--) {
                CheckLocation($"Reach level {i} standing with a neighbor");
            }

            var standings = GameMB.StateService.Trade.tradeTowns.ConvertAll<int>(tradeTown => tradeTown.standingLevel);
            if(standings.Count >= 4) {
                standings.Sort();
                var minStanding = standings[standings.Count - 4]; // Check "ALL 4 neighbor" level by looking at 4th highest neighbor
                for (int i = minStanding; i > 0; i--) {
                    CheckLocation($"Reach level {i} standing with ALL 4 neighbors");
                }
            }
        }

        private static void HandleTradeRouteCollect(RouteState route) {
            if(route.townId != Constants.TRADE_TOWN_ID) {
                // We don't care about normal trade routes for now
                return;
            }

            string goodName = route.good.name.Contains("[Water]") ? route.good.name.Replace("[Water] ", "") :  Constants.ITEM_DICT.FirstOrDefault(pair => pair.Value.ToName() == route.good.name).Key;
            CheckLocation($"Trade - {route.good.amount} {goodName}");
        }

        private static void HandleItemReceived(string itemName) {
            if (!GameMB.IsGameActive) {
                return;
            }

            Match match = new Regex(@"(\d+) Starting (.+)").Match(itemName);
            if(match.Success) {
                var fillerQty = Int32.Parse(match.Groups[1].ToString());
                var fillerType = match.Groups[2].ToString();
                if (fillerType == "Villagers") {
                    VillagersToSpawn += fillerQty;
                } else {
                    GameMB.StorageService.Store(new Good(Constants.ITEM_DICT[fillerType].ToName(), fillerQty), StorageOperationType.Other);
                    ItemsForNews.Add((GameMB.Settings.GetGoodIcon(Constants.ITEM_DICT[fillerType].ToName()), $"{fillerQty} {fillerType} received from AP!", $"{fillerQty} {fillerType} received. You will also receive this bonus in all future settlements."));
                }

                return;
            }

            // Convert the visible AP name to the in game id, where only these few are different
            itemName = GetIDFromWorkshopName(itemName);
            if(GameMB.Settings.ContainsBuilding(itemName)) {
                GameMB.GameContentService.Unlock(GameMB.Settings.GetBuilding(itemName));
                ItemsForNews.Add((null, $"{itemName} received from AP!", $"{itemName} received. You can now build this blueprint in this and all future settlements."));
                return;
            }

            if (Constants.ITEM_DICT.ContainsKey(itemName)) {
                string itemId = Constants.ITEM_DICT[itemName].ToName();
                if (!HasReceivedItem(itemId)) {
                    if (OriginalGoodIcons.Keys.Contains(itemId)) {
                        GameMB.Settings.GetGood(itemId).icon = OriginalGoodIcons[itemId];
                    }
                    SO.EffectsService.GrantRawGoodProduction(itemId, Constants.PRODUCTIVITY_MODIFIER);
                    ItemsForNews.Add((GameMB.Settings.GetGoodIcon(Constants.ITEM_DICT[itemName].ToName()), $"{itemName} unlocked!", $"{itemName} received from AP. You can now produce, gather, and obtain {itemName}."));
                }
                return;
            }

            Plugin.Log("Warning: Received unknown item " + itemName + " from AP!");
            foreach (var b in GameMB.Settings.Buildings) {
                Plugin.Log(b.name);
            }
        }

        // Deprecated: used by deprecated Custom Hooked Effect Model approach
        public static IObservable<int> OnAPItemReceived(string itemName) {
            if (!itemCallbacks.ContainsKey(itemName)) {
                itemCallbacks.Add(itemName, new Subject<int>());
            }
            return itemCallbacks[itemName];
        }
    }
}
