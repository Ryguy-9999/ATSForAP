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
using QFSW.QC.Actions;
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
        public static ArchipelagoSession session;
        private static long DeathlinkState = -1;
        private static DeathLinkService deathLinkService;
        private static List<IDisposable> GameSubscriptions = new List<IDisposable>();
        private static Queue<string> LocationQueue = new Queue<string>();
        private static Dictionary<string, Sprite> OriginalGoodIcons = new Dictionary<string, Sprite>();
        private static Sprite lockedGoodSprite;
        private static bool ShowAPLockIcons = true;
        private static bool ShowLocationAlerts = true;
        public static List<Action> UnityLambdaQueue = new List<Action>();
        private static int VillagersToSpawn = 0;
        private static News ApConnectionNews;
        private static List<int> ReputationLocationIndices = new List<int>();

        private static bool EnableBiomeKeys = false;
        public static bool EnabledKeepersDLC = false;
        public static bool EnabledNightwatchersDLC = false;
        public static bool PreventNaturalBPSelection = false;
        public static int RequiredSealTasks = 1;
        public static bool RequiredGuardianParts = false;
        public static Dictionary<string, ScoutedItemInfo> LocationScouts = new Dictionary<string, ScoutedItemInfo>();

        [Command("ap.c", "Connects to Archipelago server, using most recently used url:port and slotName. Still takes optional password.", Platform.AllPlatforms, MonoTargetType.Single)]
        public static IEnumerator<ICommandAction> InitializeAPConnectionShortcut() {
            string savedUrl = PlayerPrefs.GetString($"ap.url.{GameMB.ProfilesService.GetProfileDisplayName()}");
            string savedSlot = PlayerPrefs.GetString($"ap.slotName.{GameMB.ProfilesService.GetProfileDisplayName()}");

            return InitializeAPConnection(savedUrl, savedSlot, null);
        }

        [Command("ap.c", "Connects to Archipelago server, using most recently used url:port and slotName. Still takes optional password.", Platform.AllPlatforms, MonoTargetType.Single)]
        public static IEnumerator<ICommandAction> InitializeAPConnectionShortcut(string password) {
            string savedUrl = PlayerPrefs.GetString($"ap.url.{GameMB.ProfilesService.GetProfileDisplayName()}");
            string savedSlot = PlayerPrefs.GetString($"ap.slotName.{GameMB.ProfilesService.GetProfileDisplayName()}");

            return InitializeAPConnection(savedUrl, savedSlot, password);
        }

        [Command("ap.connect", "Connects to Archipelago server. Requires url:port, slotName, and optionally password.", Platform.AllPlatforms, MonoTargetType.Single)]
        public static IEnumerator<ICommandAction> AttemptInitializeAPConnection([APUrlSuggestion] string url, string player) {
            return AttemptInitializeAPConnection(url, player, null);
        }

        [Command("ap.connect", "Connects to Archipelago server. Requires url:port, slotName, and optionally password.", Platform.AllPlatforms, MonoTargetType.Single)]
        public static IEnumerator<ICommandAction> AttemptInitializeAPConnection([APUrlSuggestion] string url, string player, string password) {
            string savedUrl = PlayerPrefs.GetString($"ap.url.{GameMB.ProfilesService.GetProfileDisplayName()}");
            string savedSlot = PlayerPrefs.GetString($"ap.slotName.{GameMB.ProfilesService.GetProfileDisplayName()}");

            if(savedUrl != string.Empty && savedUrl != url || savedSlot != string.Empty && savedSlot != player) {
                if(GameMB.IsGameActive) {
                    GameMB.NewsService.PublishNews("WARNING: Not Connected!", "Connection attempt to AP blocked! The given url and player name are different from the one saved to this profile. To overwrite it, please use ap.connectForce.", AlertSeverity.Critical);
                }
                yield return new Value($"WARNING: Connection attempt to AP blocked! The given url or player name ('{url}' and '{player}') are different from the one saved to this profile ('{savedUrl}' and '{savedSlot}'). To overwrite it, please use ap.connectForce.");
            } else {
                // I have no intention of generating enumerated console responses, but this is the easiest way to play nice with the expected response structure
                var enumerator = InitializeAPConnection(url, player, password);
                while(enumerator.MoveNext()) {
                    yield return enumerator.Current;
                }
            }
        }

            [Command("ap.connectForce", "Connects to Archipelago server. Requires url:port, slotName, and optionally password. Overwrites saved url and slot name for current profile.", Platform.AllPlatforms, MonoTargetType.Single)]
        public static IEnumerator<ICommandAction> InitializeAPConnection([APUrlSuggestion] string url, string player) {
            return InitializeAPConnection(url, player, null);
        }

        [Command("ap.connectForce", "Connects to Archipelago server. Requires url:port, slotName, and optionally password. Overwrites saved url and slot name for current profile.", Platform.AllPlatforms, MonoTargetType.Single)]
        public static IEnumerator<ICommandAction> InitializeAPConnection([APUrlSuggestion] string url, string player, string password) {
            if (session != null) {
                DisconnectFromGame();
            }

            Plugin.Log(GameMB.ProfilesService.GetProfileDisplayName());

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
                yield return new Value($"Failed to connect to {url} as {player}. See BepInEx console for possibly more info.");
                yield break;
            }
            LoginSuccessful loginSuccess = loginResult as LoginSuccessful;

            PlayerPrefs.SetString($"ap.url.{GameMB.ProfilesService.GetProfileDisplayName()}", url);
            PlayerPrefs.SetString($"ap.slotName.{GameMB.ProfilesService.GetProfileDisplayName()}", player);

            session.Socket.ErrorReceived += (e, message) => {
                session = null;
                Plugin.Log(message);
                UnityLambdaQueue.Add(() => {
                    GameMB.NewsService.PublishNews("AP Connection Lost!", "Connection to AP has been lost! Make sure to reconnect, or you won't be able to send or receive items!", AlertSeverity.Critical);
                });
            };

            lockedGoodSprite = TextureHelper.GetImageAsSprite("good-locked2.png", TextureHelper.SpriteType.EffectIcon);

            if (GameMB.IsGameActive) {
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

            Plugin.Log("Checking blueprint rando...");
            if (loginSuccess.SlotData.ContainsKey("blueprint_items")) {
                if ((long)loginSuccess.SlotData["blueprint_items"] == 1) {
                    if (loginSuccess.SlotData.ContainsKey("continue_blueprints_for_reputation")) {
                        PreventNaturalBPSelection = (long)loginSuccess.SlotData["continue_blueprints_for_reputation"] != 1;
                    } else {
                        Plugin.Log("Could not find continue_blueprints_for_reputation in SlotData, falling back to blueprint AP items only.");
                        PreventNaturalBPSelection = true;
                    }
                } else {
                    PreventNaturalBPSelection = false;
                }
            } else {
                Plugin.Log("Could not find blueprint_items in SlotData, falling back to vanilla blueprint behavior.");
                PreventNaturalBPSelection = false;
            }

            if (loginSuccess.SlotData.ContainsKey("rep_location_indices")) {
                ReputationLocationIndices = (loginSuccess.SlotData["rep_location_indices"] as JArray).ToObject<List<int>>();
            } else {
                Plugin.Log("Could not find rep_location_indices in SlotData, falling back to 1st and 10th Rep.");
                ReputationLocationIndices = new List<int> { 1, 10 };
            }

            Plugin.Log("Checking recipe rando...");
            if ((long)loginSuccess.SlotData["recipe_shuffle"] >= Constants.RECIPE_SHUFFLE_EXCLUDE_CRUDE_WS_AND_MS_POST) {
                if (loginSuccess.SlotData.ContainsKey("production_recipes")) {
                    Dictionary<string, List<List<JValue>>> recipeDict = (loginSuccess.SlotData["production_recipes"] as JObject).ToObject<Dictionary<string, List<List<JValue>>>>();

                    LoadBuildingRecipesFromSlotData(recipeDict);
                } else {
                    Plugin.Log("Could not find production_recipes in SlotData, falling back to SlotData seed.");

                    if (loginSuccess.SlotData.ContainsKey("seed")) {
                        Utils.RandomizeBuildingRecipes((long)loginSuccess.SlotData["recipe_shuffle"] == Constants.RECIPE_SHUFFLE_EXCLUDE_CRUDE_WS, (long)loginSuccess.SlotData["seed"]);
                    } else {
                        Plugin.Log("Could not find seed in SlotData, falling back to random seed.");
                        Utils.RandomizeBuildingRecipes((long)loginSuccess.SlotData["recipe_shuffle"] == Constants.RECIPE_SHUFFLE_EXCLUDE_CRUDE_WS);
                    }
                }

                if (GameMB.IsGameActive && GameMB.GameSaveService.IsNewGame()) {
                    GameMB.NewsService.PublishNews("Cannot randomize recipes in a game!", "Recipe Shuffle option detected from AP. Unfortunately, recipe randomization only takes effect on creation of a new settlement. Your recipes have been shuffled! It just won't affect this settlement.", AlertSeverity.Warning);
                }
            }

            Plugin.Log("Checking DLC settings...");
            if (loginSuccess.SlotData.ContainsKey("enable_keepers_dlc")) {
                EnabledKeepersDLC = (long)loginSuccess.SlotData["enable_keepers_dlc"] == 1;
            } else {
                Plugin.Log("Could not find enable_keepers_dlc in SlotData, falling back to false.");
                EnabledKeepersDLC = false;
            }
            if (loginSuccess.SlotData.ContainsKey("enable_nightwatchers_dlc")) {
                EnabledNightwatchersDLC = (long)loginSuccess.SlotData["enable_nightwatchers_dlc"] == 1;
            } else {
                Plugin.Log("Could not find enable_nightwatchers_dlc in SlotData, falling back to false.");
                EnabledNightwatchersDLC = false;
            }

            Plugin.Log("Checking biome key settings...");
            if (loginSuccess.SlotData.ContainsKey("enable_biome_keys")) {
                EnableBiomeKeys = (long)loginSuccess.SlotData["enable_biome_keys"] == 1;
            } else {
                Plugin.Log("Could not find enable_biome_keys in SlotData, falling back to false.");
                EnableBiomeKeys = false;
            }

            Plugin.Log("Checking final map settings...");
            if (loginSuccess.SlotData.ContainsKey("seal_items")) {
                RequiredGuardianParts = (long)loginSuccess.SlotData["seal_items"] == 1;
            } else {
                Plugin.Log("Could not find seal_items in SlotData, falling back to false.");
                RequiredGuardianParts = false;
            }
            if (loginSuccess.SlotData.ContainsKey("required_seal_tasks")) {
                RequiredSealTasks = (int)(long)loginSuccess.SlotData["required_seal_tasks"];
            } else {
                Plugin.Log("Could not find required_seal_tasks in SlotData, falling back to 1.");
                RequiredSealTasks = 1;
            }

            session.Items.ItemReceived += (receivedItemsHelper) => {
                ItemInfo item = session.Items.DequeueItem();
                if(GameMB.IsGameActive && item.ItemName != null) {
                    HandleItemReceived(item.ItemName);
                }
                PlayerPrefs.SetInt("ap.previouslyProcessedLength", session.Items.AllItemsReceived.Count);
            };
            while(session.Items.Any()) {
                session.Items.DequeueItem();
            }

            session.MessageLog.OnMessageReceived += (message) => {
                Plugin.Log("[AP]    " + message);
            };

            Plugin.Log("Checking deathlink...");
            if (loginSuccess.SlotData.ContainsKey("deathlink")) {
                DeathlinkState = (long)loginSuccess.SlotData["deathlink"];
            } else {
                Plugin.Log("Could not find deathlink in SlotData, falling back to off.");
                DeathlinkState = Constants.DEATHLINK_OFF;
            }
            deathLinkService = session.CreateDeathLinkService();
            deathLinkService.EnableDeathLink();
            deathLinkService.OnDeathLinkReceived += (deathLink) => {
                if (GameMB.IsGameActive && DeathlinkState >= Constants.DEATHLINK_DEATH_ONLY) {
                    UnityLambdaQueue.Add(() => {
                        GameMB.VillagersService.KillVillagers(1, VillagerLossType.Death, Constants.DEATHLINK_REASON);
                    });
                }
            };

            Plugin.Log("Initializing gifting service...");
            ATSGiftingService.InitializeGifting(session);

            Plugin.Log("Scouting trade locations...");
            var tradeLocationIds = new List<long>();
            foreach (var loc in session.Locations.AllMissingLocations) {
                var location = session.Locations.GetLocationNameFromId(loc);

                Match match = new Regex(@"Trade - (\d+) (.+)").Match(location);
                if (match.Success) {
                    tradeLocationIds.Add(loc);
                }
            }

            session.Locations.ScoutLocationsAsync(HintCreationPolicy.CreateAndAnnounceOnce, tradeLocationIds.ToArray()).ContinueWith(locationInfoPacket => {
                Plugin.Log("Interpreting trade location scout response...");
                foreach(var scout in locationInfoPacket.Result) {
                    LocationScouts.Add(scout.Value.LocationDisplayName, scout.Value);
                }
            });

            UnityLambdaQueue.Add(() => {
                GameMB.NewsService.PublishNews("AP Connected", $"Successfully connected to {url} as {player}. Have fun!", AlertSeverity.Info);
            });
            Plugin.Log($"Connection to {url} as {player} complete!");
            yield return new Value($"Connection to {url} as {player} complete!");
        }

        public static void SyncGameStateToAP() {
            if (session == null) {
                Plugin.Log("Tried to sync to AP with a null AP session.");
                return;
            }

            if (!GameMB.StateService.Trade.tradeTowns.Any(town => town.id == Constants.TRADE_TOWN_ID)) {
                var offers = new List<TownOfferState>();
                foreach (var loc in session.Locations.AllMissingLocations) {
                    var location = session.Locations.GetLocationNameFromId(loc);

                    Match match = new Regex(@"Trade - (\d+) (.+)").Match(location);
                    if (match.Success) {
                        int tradeQty = Int32.Parse(match.Groups[1].ToString());
                        string tradeItem = match.Groups[2].ToString();

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
                    isMaxStanding = true,
                    offers = offers
                });
            }

            int previouslyProcessedLength = PlayerPrefs.GetInt("ap.previouslyProcessedLength", 0);
            foreach (var (item, index) in session.Items.AllItemsReceived.Select((item, index) => (item, index))) {
                // Goods unlocks
                if (Constants.ITEM_DICT.ContainsKey(item.ItemName)) {
                    // Goods items will be handled in the next loop
                    continue;
                }

                // Filler
                if (item.ItemName == "Survivor Bonding" && index >= previouslyProcessedLength) {
                    GameMB.Settings.GetEffect("AncientGate_Hardships").Apply();
                    continue;
                }
                Match match = new Regex(@"(\d+) Starting (.+)").Match(item.ItemName);
                if (match.Success && index >= previouslyProcessedLength) {
                    var fillerQty = Int32.Parse(match.Groups[1].ToString());
                    var fillerType = match.Groups[2].ToString();
                    if (fillerType == "Villagers") {
                        VillagersToSpawn += fillerQty;
                    } else {
                        if(!Constants.ITEM_DICT.ContainsKey(fillerType)) {
                            Plugin.Log("Could not find filler item: " + fillerType);
                        }
                        GameMB.StorageService.Store(new Good(Constants.ITEM_DICT[fillerType].ToName(), fillerQty), StorageOperationType.Other);
                    }

                    continue;
                }

                // Blueprints
                string buildingID = Utils.GetIDFromWorkshopName(item.ItemName);
                if (GameMB.Settings.ContainsBuilding(buildingID)) {
                    GameMB.GameContentService.Unlock(GameMB.Settings.GetBuilding(buildingID));
                }
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
                        GameMB.Settings.GetGood(pair.Value.ToName()).icon = lockedGoodSprite;
                        if (Constants.SERVICE_MAPPING.ContainsKey(pair.Key)) {
                            GameMB.Settings.Needs.FirstOrDefault(need => need.Name == Constants.SERVICE_MAPPING[pair.Key]).presentation.overrideIcon = lockedGoodSprite;
                        }
                    }

                    if (HasReceivedItem(pair.Key)) {
                        SO.EffectsService.GrantRawGoodProduction(pair.Value.ToName(), -Constants.PRODUCTIVITY_MODIFIER);
                    }
                    // No else, AP thinks we shouldn't have the item, further decreasing its productivity would cause a bug
                }
            }
            PlayerPrefs.SetInt("ap.previouslyProcessedLength", session.Items.AllItemsReceived.Count);
        }

        public static void EnterGame() {
            if(session == null) {
                GameMB.NewsService.PublishNews("No AP session detected! Remember to connect!", "ap.connect url:port slotName [password]", AlertSeverity.Critical);
                ApConnectionNews = (GameMB.NewsService.News.GetType().GetProperty("Value").GetValue(GameMB.NewsService.News, null) as List<News>)[0];
            } else {
                // Reset the process tally so filler can be reprocessed
                PlayerPrefs.SetInt("ap.previouslyProcessedLength", 0);
                SyncGameStateToAP();
            }

            ATSGiftingService.EnterGame();

            if(GameSubscriptions.Count > 0) {
                foreach (IDisposable disposable in GameSubscriptions) {
                    disposable.Dispose();
                }
            }

            GameSubscriptions.Add(GameMB.OrdersService.OnOrderCompleted.Subscribe(new Action<OrderState>(HandleOrderRewards)));
            GameSubscriptions.Add(GameMB.ReputationService.OnGameResult.Subscribe(new Action<bool>(HandleGameResult)));
            GameSubscriptions.Add(GameMB.GameBlackboardService.OnHubLeveledUp.Subscribe(new Action<Hearth>(HandleHubLevelUp)));
            // For some reason this subscription causes errors for some users inconsistently. Replacing with Harmony prefix
            //GameSubscriptions.Add(GameMB.GameBlackboardService.OnRelicResolved.Subscribe(new Action<Relic>(HandleRelicResolve)));
            GameSubscriptions.Add(GameMB.RXService.Interval(1, true).Subscribe(new Action(HandleSlowUpdate)));
            GameSubscriptions.Add(GameMB.TradeRoutesService.OnStandingLeveledUp.Subscribe(new Action<TradeTownState>(HandleStandingLevelUp)));
            GameSubscriptions.Add(GameMB.TradeRoutesService.OnRouteCollected.Subscribe(new Action<RouteState>(HandleTradeRouteCollect)));
        }

        [Command("ap.sendLocation", "For debugging purposes. Sends a location.", Platform.AllPlatforms, MonoTargetType.Single)]
        public static void SendLocation(string location) {
            CheckLocation(location);
        }

        [Command("ap.toggleLocationAlerts", "Toggles whether you receive alerts for checking AP locations.", Platform.AllPlatforms, MonoTargetType.Single)]
        public static void ToggleLocationAlerts() {
            ToggleLocationAlerts(!ShowLocationAlerts);
        }

        [Command("ap.toggleLocationAlerts", "Toggles whether you receive alerts for checking AP locations.", Platform.AllPlatforms, MonoTargetType.Single)]
        public static void ToggleLocationAlerts(bool trueFalse) {
            ShowLocationAlerts = trueFalse;
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
                    GameMB.Settings.GetGood(pair.Value.ToName()).icon = lockedGoodSprite;
                    if (Constants.SERVICE_MAPPING.ContainsKey(pair.Key)) {
                        GameMB.Settings.Needs.FirstOrDefault(need => need.Name == Constants.SERVICE_MAPPING[pair.Key]).presentation.overrideIcon = lockedGoodSprite;
                    }
                } else {
                    if (OriginalGoodIcons.Keys.Contains(pair.Value.ToName())) {
                        GameMB.Settings.GetGood(pair.Value.ToName()).icon = OriginalGoodIcons[pair.Value.ToName()];
                        if (Constants.SERVICE_MAPPING.ContainsKey(pair.Key)) {
                            GameMB.Settings.Needs.FirstOrDefault(need => need.Name == Constants.SERVICE_MAPPING[pair.Key]).presentation.overrideIcon = OriginalGoodIcons[pair.Value.ToName()];
                        }
                    }
                }
            }
        }

        [Command("ap.toggleDeathlink", "Changes deathlink handling from in game.", Platform.AllPlatforms, MonoTargetType.Single)]
        public static IEnumerator<ICommandAction> ToggleDeathlink() {
            // Cycle from off -> death_only -> leave_and_death -> off
            switch (DeathlinkState) {
            case Constants.DEATHLINK_OFF:
                return ToggleDeathlink("death_only");
            case Constants.DEATHLINK_DEATH_ONLY:
                return ToggleDeathlink("leave_and_death");
            case Constants.DEATHLINK_LEAVE_AND_DEATH:
            default:
                return ToggleDeathlink("off");
            }
        }
        [Command("ap.toggleDeathlink", "Changes deathlink handling from in game.", Platform.AllPlatforms, MonoTargetType.Single)]
        public static IEnumerator<ICommandAction> ToggleDeathlink([APDeathlinkSuggestion] string deathlinkSetting) {
            switch (deathlinkSetting) {
            case "off":
                DeathlinkState = Constants.DEATHLINK_OFF;
                yield return new Value("Deathlink turned off!");
                break;
            case "death_only":
                DeathlinkState = Constants.DEATHLINK_DEATH_ONLY;
                yield return new Value("Deathlink set to deaths only!");
                break;
            case "leave_and_death":
                DeathlinkState = Constants.DEATHLINK_LEAVE_AND_DEATH;
                yield return new Value("Deathlink set to leaving and deaths!");
                break;
            }
        }

        [Command("ap.disconnect", "Disconnects from Archipelago server.", Platform.AllPlatforms, MonoTargetType.Single)]
        public static void DisconnectFromGame() {
            if (session != null) {
                session.Socket.DisconnectAsync();
                session = null;
            }
        }

        [Command("ap.say", "Sends text to AP server, to use for AP chat log, !commands, etc.")]
        public static void SendMessageToAP(string message) {
            if(session != null) {
                session.Socket.SendPacket(new SayPacket() { Text = message });
            }
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
                if (!GameMB.Settings.ContainsBuilding(Utils.GetIDFromWorkshopName(building.Key))) {
                    Plugin.Log("Unknown building key from SlotData: " + building.Key);
                    continue;
                }
                WorkshopModel model = GameMB.Settings.GetBuilding(Utils.GetIDFromWorkshopName(building.Key)) as WorkshopModel;
                if(model == null) {
                    Plugin.Log("Null workshop model for building: " + building.Key);
                    continue;
                }

                var i = 0;
                foreach(var recipe in building.Value) {
                    var recipeModelIndex = recipeList.FindIndex(r => r.GetProducedGood() == Constants.ITEM_DICT[recipe[0].ToObject<string>()].ToName() && r.grade.level == recipe[1].ToObject<int>());

                    if(recipeModelIndex < 0) {
                        Plugin.Log("========================================================================");
                        Plugin.Log("ERROR: recipeModelIndex was < 0. This probably means the apworld does not have the correct recipe definitions.");
                        Plugin.Log("========================================================================");
                        return;
                    }

                    model.recipes[i] = recipeList[recipeModelIndex];
                    recipeList.RemoveAt(recipeModelIndex);
                    i++;
                }
            }

            // This service is responsible for the tooltips that show where goods are produced. It's capable of remapping the recipes after randomization, we just need to reach inside and tell it to do so
            StaticRecipesService.GetType().GetField("goodsSourcesMap", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(SO.StaticRecipesService, new Dictionary<string, List<BuildingModel>>());
            StaticRecipesService.GetType().GetMethod("MapGoodsSources", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(SO.StaticRecipesService, new object[0]);
        }

        private static Dictionary<long, int> locationsAlreadySent = new Dictionary<long, int>();
        public static bool CheckLocation(string location) {
            if(session == null) {
                LocationQueue.Enqueue(location);
                return false;
            }

            if(!HasReceivedBiome(Utils.GetBiomeNameFromID(MetaStateService.GameConditions.biome))) {
                GameMB.NewsService.PublishNews("Forbidden biome!", "Your settlement is somehow in a biome you haven't unlocked through AP. No locations will be checked here!", AlertSeverity.Critical);
                return false;
            }

            var locId = session.Locations.GetLocationIdFromName(Constants.AP_GAME_NAME, location);
            if (locId < 0) {
                if (locationsAlreadySent.ContainsKey(locId)) {
                    locationsAlreadySent[locId]++;
                    if (locationsAlreadySent[locId] == 10) {
                        Plugin.Log($"Silencing: {location}");
                    } else if (locationsAlreadySent[locId] < 10) {
                        Plugin.Log($"Location with unknown id: {location}");
                    }
                } else {
                    locationsAlreadySent[locId] = 1;
                    Plugin.Log($"Location with unknown id: {location}");
                }
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

            Plugin.Log($"Sending AP Location: {location} (id: {locId})");
            session.Locations.CompleteLocationChecksAsync(locId);

            if(ShowLocationAlerts) {
                UnityLambdaQueue.Add(() => {
                    GameMB.NewsService.PublishNews("Location Checked", $"You just sent an item from the location: {location}", AlertSeverity.Info);
                });
            }
            return true;
        }

        public static bool HasReceivedGuardianPart(string part) {
            if (!RequiredGuardianParts) {
                return true;
            }

            return session.Items.AllItemsReceived.Any(itemInfo => itemInfo.ItemDisplayName == part);
        }

        public static int TotalGroveExpeditionLocationsCount() {
            if (session == null) {
                return 0;
            }

            int result = 0;
            foreach (var loc in session.Locations.AllLocations) {
                var location = session.Locations.GetLocationNameFromId(loc);

                Match match = new Regex(@"Coastal Grove - \d\d?\w\w Expedition").Match(location);
                if (match.Success) {
                    result++;
                }
            }
            return result;
        }

        public static int CheckedGroveExpeditionLocationsCount() {
            if (session == null) {
                return 0;
            }

            int result = 0;
            foreach (var loc in session.Locations.AllLocationsChecked) {
                var location = session.Locations.GetLocationNameFromId(loc);

                Match match = new Regex(@"Coastal Grove - \d\d?\w\w Expedition").Match(location);
                if (match.Success) {
                    result++;
                }
            }
            return result;
        }

        public static int GetNextUncheckedGroveExpedition() {
            int lowestExpedition = -1;

            foreach (var loc in session.Locations.AllMissingLocations) {
                var location = session.Locations.GetLocationNameFromId(loc);

                Match match = new Regex(@"Coastal Grove - (\d\d?)\w\w Expedition").Match(location);
                if (match.Success) {
                    int expeditionNumber = Int32.Parse(match.Groups[1].ToString());
                    if (lowestExpedition < 0) {
                        lowestExpedition = expeditionNumber;
                    } else {
                        lowestExpedition = Math.Min(lowestExpedition, expeditionNumber);
                    }
                }
            }

            return lowestExpedition;
        }

        public static int GetNextUncheckedCornerstoneForge() {
            int lowestForge = -1;

            foreach (var loc in session.Locations.AllMissingLocations) {
                var location = session.Locations.GetLocationNameFromId(loc);

                Match match = new Regex(@"Ashen Thicket - Forge (\d)\w\w Cornerstone").Match(location);
                if (match.Success) {
                    int forgeNumber = Int32.Parse(match.Groups[1].ToString());
                    if (lowestForge < 0) {
                        lowestForge = forgeNumber;
                    } else {
                        lowestForge = Math.Min(lowestForge, forgeNumber);
                    }
                }
            }

            return lowestForge;
        }

        public static bool HasReceivedItem(string item) {
            item = Constants.ITEM_DICT.ContainsKey(item) ? Constants.ITEM_DICT[item].ToName() : item;

            return !StateService.Effects.rawGoodsProductionBonus.ContainsKey(item) || StateService.Effects.rawGoodsProductionBonus[item] > -Constants.PRODUCTIVITY_MODIFIER / 2;
        }

        public static bool HasReceivedBiome(string biome) {
            if (!EnableBiomeKeys) {
                return true;
            }

            return session.Items.AllItemsReceived.Any(itemInfo => itemInfo.ItemDisplayName == biome);
        }

        private static void HandleItemReceived(string itemName) {
            if (!GameMB.IsGameActive) {
                return;
            }

            // Guardian Part
            if (itemName.StartsWith("Guardian ")) {
                // No need to handle reception, we just check for it when opening the seal menu
                return;
            }

            // Filler
            if (itemName == "Survivor Bonding") {
                UnityLambdaQueue.Add(() => {
                    GameMB.Settings.GetEffect("AncientGate_Hardships").Apply();
                });
                return;
            }
            Match match = new Regex(@"(\d+) Starting (.+)").Match(itemName);
            if (match.Success) {
                var fillerQty = Int32.Parse(match.Groups[1].ToString());
                var fillerType = match.Groups[2].ToString();
                if (fillerType == "Villagers") {
                    VillagersToSpawn += fillerQty;
                } else {
                    GameMB.StorageService.Store(new Good(Constants.ITEM_DICT[fillerType].ToName(), fillerQty), StorageOperationType.Other);
                    UnityLambdaQueue.Add(() => {
                        GameMB.NewsService.PublishNews($"{fillerQty} {fillerType} received from AP!", $"{fillerQty} {fillerType} received. You will also receive this bonus in all future settlements.", AlertSeverity.Info, GameMB.Settings.GetGoodIcon(Constants.ITEM_DICT[fillerType].ToName()));
                    });
                }

                return;
            }

            // Good unlock
            if (Constants.ITEM_DICT.ContainsKey(itemName)) {
                string itemId = Constants.ITEM_DICT[itemName].ToName();
                if (!HasReceivedItem(itemId)) {
                    if (OriginalGoodIcons.Keys.Contains(itemId)) {
                        GameMB.Settings.GetGood(itemId).icon = OriginalGoodIcons[itemId];
                        if (Constants.SERVICE_MAPPING.ContainsKey(itemName)) {
                            GameMB.Settings.Needs.FirstOrDefault(need => need.Name == Constants.SERVICE_MAPPING[itemName]).presentation.overrideIcon = OriginalGoodIcons[itemId];
                        }
                    }
                    SO.EffectsService.GrantRawGoodProduction(itemId, Constants.PRODUCTIVITY_MODIFIER);
                    UnityLambdaQueue.Add(() => {
                        GameMB.NewsService.PublishNews($"{itemName} unlocked!", $"{itemName} received from AP. You can now produce, gather, and obtain {itemName}.", AlertSeverity.Info, GameMB.Settings.GetGoodIcon(Constants.ITEM_DICT[itemName].ToName()));
                    });
                }
                return;
            }

            // Blueprint
            // Convert the visible AP name to the in game id, where only these few are different
            string buildingID = Utils.GetIDFromWorkshopName(itemName);
            if (GameMB.Settings.ContainsBuilding(buildingID)) {
                GameMB.GameContentService.Unlock(GameMB.Settings.GetBuilding(buildingID));
                UnityLambdaQueue.Add(() => {
                    GameMB.NewsService.PublishNews($"{itemName} BP received from AP!", $"{itemName} received. You can now build this blueprint in this and all future settlements.", AlertSeverity.Info, null);
                });
                return;
            }

            if(new string[] { "Royal Woodlands", "Coral Forest", "The Marshlands", "Scarlet Orchard", "Cursed Royal Woodlands", "Coastal Grove", "Ashen Thicket", "Bamboo Flats", "Rocky Ravine", "Sealed Forest" }.Contains(itemName) && GameMB.IsGameActive) {
                UnityLambdaQueue.Add(() => {
                    GameMB.NewsService.PublishNews($"{itemName} unlocked from AP!", $"You received the key for {itemName} from AP. You can now check locations in that biome!", AlertSeverity.Info, null);
                });
                return;
            }

            Plugin.Log("Warning: Received unknown item " + itemName + " from AP!");
            foreach (var b in GameMB.Settings.Buildings) {
                Plugin.Log(b.name);
            }
        }

        private static void HandleOrderRewards(OrderState order) {
            int orderIndex = GameMB.StateService.Orders.currentOrders.FindIndex(o => o.id == order.id) + 1;
            CheckLocation("Completed Order - " + orderIndex + Utils.GetOrdinalSuffix(orderIndex) + " Pack");
        }

        private static void HandleGameResult(bool gameWon) {
            PlayerPrefs.SetInt("ap.previouslyProcessedLength", 0);
            if (!gameWon) {
                return;
            }

            // Biome Victory
            var biome = Utils.GetBiomeNameFromID(GameMB.MetaStateService.GameConditions.biome);
            if (biome == "Coastal Grove" || biome == "Ashen Thicket") {
                if(EnabledKeepersDLC) {
                    CheckLocation("Victory - " + biome);
                }
            } else if(biome == "Bamboo Flats" || biome == "Rocky Ravine") {
                if (EnabledNightwatchersDLC) {
                    CheckLocation("Victory - " + biome);
                }
            } else {
                CheckLocation("Victory - " + biome);
            }

            // Sealed Forest (Goal) Victory
            if(GameMB.GameSealService.IsSealedBiome() && GameMB.GameSealService.IsSealCompleted()) {
                session.SetGoalAchieved();
            }
        }

        private static void HandleHubLevelUp(Hearth hearth) {
            var hearthLevel = hearth.state.hubIndex + 1;
            CheckLocation($"Upgraded Hearth - {hearthLevel + Utils.GetOrdinalSuffix(hearthLevel)} Tier");
        }

        public static void HandleRelicResolve(Relic relic) {
            if(relic.model.dangerLevel == DangerLevel.Dangerous) {
                CheckLocation("Complete a Dangerous Glade Event");
            }
            if(relic.model.dangerLevel == DangerLevel.Forbidden) {
                CheckLocation("Complete a Forbidden Glade Event");
            }

            string decisionTag = relic.GetCurrentDecision()?.decisionTag?.displayName?.key;
            if (decisionTag == "DecisionTag_Corruption") {
                CheckLocation("Complete a Glade Event with a Corruption tag");
            }
            if (decisionTag == "DecisionTag_Empathy") {
                CheckLocation("Complete a Glade Event with an Empathy tag");
            }
            if (decisionTag == "DecisionTag_Loyalty") {
                CheckLocation("Complete a Glade Event with a Loyalty tag");
            }

            if (relic.model.name.StartsWith("Angry Ghost")) {
                CheckLocation("Cursed Royal Woodlands - Appease an Angry Ghost");
            }
            if(relic.model.name.StartsWith("Calm Ghost")) {
                CheckLocation("Cursed Royal Woodlands - Appease a Calm Ghost");
            }

            if (relic.model.name == "Spider 3") {
                CheckLocation("Scarlet Orchard - Reconstruct the Sealed Spider");
            }
            if (relic.model.name == "Snake 3") {
                CheckLocation("Scarlet Orchard - Reconstruct the Sea Snake");
            }
            if (relic.model.name == "Scorpion 3") {
                CheckLocation("Scarlet Orchard - Reconstruct the Smoldering Scorpion");
            }
        }

        private static void HandleSlowUpdate() {
            if(!GameMB.IsGameActive) {
                return;
            }

            try {
                // I don't know why I have to do this, but the exception is distracting in the console, so here we are
                GameMB.RacesService.HasRace("Human");
            } catch (NullReferenceException e) {
                Plugin.Log("Caught HasRace NRE, skipping SlowUpdate");
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
            if (EnabledKeepersDLC && GameMB.RacesService.HasRace("Frog") && GameMB.ResolveService.GetReputationGainFor("Frog") >= 1) {
                CheckLocation("First Reputation through Resolve - Frogs");
            }
            if (EnabledNightwatchersDLC && GameMB.RacesService.HasRace("Bat") && GameMB.ResolveService.GetReputationGainFor("Bat") >= 1) {
                CheckLocation("First Reputation through Resolve - Bats");
            }

            // Overall Reputation for biome
            var repGained = GameMB.ReputationService.GetReputationGainedFrom(ReputationChangeSource.Order) + GameMB.ReputationService.GetReputationGainedFrom(ReputationChangeSource.Other) + GameMB.ReputationService.GetReputationGainedFrom(ReputationChangeSource.Relics) + GameMB.ReputationService.GetReputationGainedFrom(ReputationChangeSource.Resolve);
            var biome = Utils.GetBiomeNameFromID(GameMB.MetaStateService.GameConditions.biome);
            if (biome == "Coastal Grove" || biome == "Ashen Thicket") {
                if (EnabledKeepersDLC) {
                    foreach (int repIndex in ReputationLocationIndices) {
                        if (repGained >= repIndex) {
                            CheckLocation($"{repIndex}{Utils.GetOrdinalSuffix(repIndex)} Reputation - {biome}");
                        }
                    }
                }
            } else if (biome == "Bamboo Flats" || biome == "Rocky Ravine") {
                if (EnabledNightwatchersDLC) {
                    foreach (int repIndex in ReputationLocationIndices) {
                        if (repGained >= repIndex) {
                            CheckLocation($"{repIndex}{Utils.GetOrdinalSuffix(repIndex)} Reputation - {biome}");
                        }
                    }
                }
            } else {
                foreach (int repIndex in ReputationLocationIndices) {
                    if (repGained >= repIndex) {
                        CheckLocation($"{repIndex}{Utils.GetOrdinalSuffix(repIndex)} Reputation - {biome}");
                    }
                }
            }

            // 50 Resolve check
            if (GameMB.RacesService.HasRace("Human") && GameMB.ResolveService.GetResolveFor("Human") >= 49.5) {
                CheckLocation("50 Resolve - Humans");
            }
            if (GameMB.RacesService.HasRace("Lizard") && GameMB.ResolveService.GetResolveFor("Lizard") >= 49.5) {
                CheckLocation("50 Resolve - Lizards");
            }
            if (GameMB.RacesService.HasRace("Beaver") && GameMB.ResolveService.GetResolveFor("Beaver") >= 49.5) {
                CheckLocation("50 Resolve - Beavers");
            }
            if (GameMB.RacesService.HasRace("Harpy") && GameMB.ResolveService.GetResolveFor("Harpy") >= 49.5) {
                CheckLocation("50 Resolve - Harpies");
            }
            if (GameMB.RacesService.HasRace("Foxes") && GameMB.ResolveService.GetResolveFor("Foxes") >= 49.5) {
                CheckLocation("50 Resolve - Foxes");
            }
            if (EnabledKeepersDLC && GameMB.RacesService.HasRace("Frog") && GameMB.ResolveService.GetResolveFor("Frog") >= 49.5) {
                CheckLocation("50 Resolve - Frogs");
            }
            if (EnabledNightwatchersDLC && GameMB.RacesService.HasRace("Bat") && GameMB.ResolveService.GetResolveFor("Bat") >= 49.5) {
                CheckLocation("50 Resolve - Bats");
            }

            // 20 Housed Villagers
            if (GameMB.RacesService.HasRace("Human") && Utils.GetFullyUpgradedHousedAmount("Human") >= 20) {
                CheckLocation("Have 20 Villagers in fully upgraded Housing - Humans");
            }
            if (GameMB.RacesService.HasRace("Beaver") && Utils.GetFullyUpgradedHousedAmount("Beaver") >= 20) {
                CheckLocation("Have 20 Villagers in fully upgraded Housing - Beavers");
            }
            if (GameMB.RacesService.HasRace("Lizard") && Utils.GetFullyUpgradedHousedAmount("Lizard") >= 20) {
                CheckLocation("Have 20 Villagers in fully upgraded Housing - Lizards");
            }
            if (GameMB.RacesService.HasRace("Harpy") && Utils.GetFullyUpgradedHousedAmount("Harpy") >= 20) {
                CheckLocation("Have 20 Villagers in fully upgraded Housing - Harpies");
            }
            if (GameMB.RacesService.HasRace("Foxes") && Utils.GetFullyUpgradedHousedAmount("Foxes") >= 20) {
                CheckLocation("Have 20 Villagers in fully upgraded Housing - Foxes");
            }
            if (EnabledKeepersDLC && GameMB.RacesService.HasRace("Frog") && Utils.GetFullyUpgradedHousedAmount("Frog") >= 20) {
                CheckLocation("Have 20 Villagers in fully upgraded Housing - Frogs");
            }
            if (EnabledNightwatchersDLC && GameMB.RacesService.HasRace("Bat") && Utils.GetFullyUpgradedHousedAmount("Bat") >= 20) {
                CheckLocation("Have 20 Villagers in fully upgraded Housing - Bats");
            }

            // Handle filler villagers received mid game in a game thread, as ItemReceived causes crashes
            if (VillagersToSpawn > 0) {
                GameMB.NewsService.PublishNews($"{VillagersToSpawn} Villagers arrived!", $"{VillagersToSpawn} extra Villagers received from AP!", AlertSeverity.Info);
                while(VillagersToSpawn > 0) {
                    GameMB.VillagersService.SpawnNewVillager(SO.RacesService.GetRandom());
                    VillagersToSpawn--;
                }
            }

            while(UnityLambdaQueue.Any()) {
                UnityLambdaQueue[0].Invoke();
                UnityLambdaQueue.RemoveAt(0);
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
    }
}
