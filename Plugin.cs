using BepInEx;
using HarmonyLib;
using Eremite;
using Eremite.Controller;
using Eremite.Model;
using ATS_API.Effects;
using Eremite.Model.Effects;
using System;
using Eremite.Controller.Effects;
using System.Reflection;
using ATS_API.Helpers;
using Eremite.Services;
using Newtonsoft.Json;
using ATS_API.Localization;

namespace Ryguy9999.ATS.ATSForAP
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        private Harmony harmony;

        private void Awake()
        {
            Instance = this;
            harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
            harmony.PatchAll(typeof(StoragePatch));
            harmony.PatchAll(typeof(VillagerPatch));
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} loaded.");
        }

        public static void Log(object o) {
            Log(o.ToString());
        }

        public static void Log(string s) {
            Instance.Logger.LogInfo(s);
        }

        public static void Logify(object o, int maxDepth = 10) {
            Log(JsonConvert.SerializeObject(o, Formatting.None, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, MaxDepth = maxDepth }));
        }

        [HarmonyPatch(typeof(MainController), nameof(MainController.OnServicesReady))]
        [HarmonyPostfix]
        private static void HookMainControllerSetup() {
            // This method will run after game load (Roughly on entering the main menu)
            // At this point a lot of the game's data will be available.
            // Main entry point to access this data will be `Serviceable.Settings` or `MainController.Instance.Settings`
            MB.Controller.Build.type = Eremite.Model.Configs.BuildType.Development;

            LocalizationManager.AddString("ATSForAP_GameUI_APExpeditionCategory", "AP Checks");

            // Setup defaults for the Training Expedition Menu
            MB.Settings.customGameConfig.reputationDefaultIndex = 2; // P1 14 -> 18 Reputation
            MB.Settings.customGameConfig.stormDurationDefaultIndex = 3; // P2 Storm 02:00 -> 04:00
            MB.Settings.customGameConfig.initialPositiveEffectsAmount = 1; // Viceroy 1 | 4 Mysteries
            MB.Settings.customGameConfig.blightFootprintDefaultIndex = 2; // P11 x2 blightrot
            // Default selection of embarkation goods
            foreach(var good in MB.Settings.customGameConfig.embarkGoods) {
                if (good.Name == GoodsTypes.Mat_Raw_Wood.ToName()) {
                    good.amount = 70;
                } else if (good.Name == GoodsTypes.Crafting_Coal.ToName()) {
                    good.amount = 28;
                } else if (good.Name == GoodsTypes.Hearth_Parts.ToName()) {
                    good.amount = 6;
                } else if (good.Name == GoodsTypes.Mat_Processed_Parts.ToName()) {
                    good.amount = 28;
                } else if (good.Name == GoodsTypes.Mat_Processed_Pipe.ToName()) {
                    good.amount = 14;
                } else if (good.Name == GoodsTypes.Food_Raw_Eggs.ToName()) {
                    good.amount = 42;
                } else if (good.Name == GoodsTypes.Food_Raw_Roots.ToName()) {
                    good.amount = 28;
                } else if (good.Name == GoodsTypes.Food_Raw_Vegetables.ToName()) {
                    good.amount = 28;
                } else if (good.Name == GoodsTypes.Food_Raw_Meat.ToName()) {
                    good.amount = 28;
                } else if (good.Name == GoodsTypes.Food_Raw_Mushrooms.ToName()) {
                    good.amount = 28;
                } else if (good.Name == GoodsTypes.Food_Raw_Insects.ToName()) {
                    good.amount = 28;
                } else if (good.Name == GoodsTypes.Food_Raw_Berries.ToName()) {
                    good.amount = 28;
                } else if (good.Name == GoodsTypes.Food_Raw_Fish.ToName()) {
                    good.amount = 28;
                } else if (good.Name == GoodsTypes.Mat_Processed_Planks.ToName()) {
                    good.amount = 7;
                } else if (good.Name == GoodsTypes.Mat_Processed_Fabric.ToName()) {
                    good.amount = 7;
                } else if (good.Name == GoodsTypes.Mat_Processed_Bricks.ToName()) {
                    good.amount = 7;
                }
            }
        }

        [HarmonyPatch(typeof(MainController), nameof(MainController.InitReferences))]
        [HarmonyPostfix]
        private static void PostSetupMainController() {
            //ArchipelagoService.RandomizeBuildingRecipes();

            EffectBuilder<ArchipelagoEffectModel> builder = new(PluginInfo.PLUGIN_GUID, "Archipelago", "ap-icon.png");
            builder.SetRarity(EffectRarity.Legendary);
            builder.SetPositive(false);
            builder.SetLabel("Core Archipelago Effect");
            builder.SetDisplayName("Aura of the Archipelago");
            builder.SetDescription("The storm's flooding has given us these islands. Resources behave in unusual and scarce ways here...");



            // Build a unique, hidden effect that blocks production of Most items in the game, for use by the hooked effects
            //foreach (KeyValuePair<string, GoodsTypes> pair in Constants.ITEM_DICT) {
            //    EffectBuilder<GoodsRawProductionEffectModel> productionBuilder = new(PluginInfo.PLUGIN_GUID, pair.Key + Constants.AP_PROD_EFFECT_NAME, null);
            //    productionBuilder.EffectModel.good = new GoodRef() { good = MB.Settings.GetGood(pair.Value.ToName()), amount = -Constants.PRODUCTIVITY_MODIFIER };


            //    APItemReceivedHook apItemReceivedHook = Activator.CreateInstance<APItemReceivedHook>();
            //    apItemReceivedHook.itemName = pair.Key;

            //    HookedEffectBuilder productionBlockerBuilder = new(PluginInfo.PLUGIN_GUID, pair.Key + Constants.AP_PROD_EFFECT_CONTROL_NAME, null);

            //    //string name = productionBuilder.EffectModel.name;
            //    productionBlockerBuilder.AddInstantEffect(productionBuilder.EffectModel);
            //    //productionBuilder.EffectModel.name = name;

            //    //GameTimePassedHook gameTimePassedHook = Activator.CreateInstance<GameTimePassedHook>();
            //    //gameTimePassedHook.startWithCurrentValue = false;
            //    //gameTimePassedHook.seconds = 10f;
            //    //productionBlockerBuilder.AddRemovalHook(gameTimePassedHook);
            //    productionBlockerBuilder.AddRemovalHook(apItemReceivedHook);

            //    // This successfully prevents the Effect spam. Disabling just for debugging purposes
            //    //productionBlockerBuilder.EffectModel.forceHideOnHUD = true;
            //}
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.StartGame))]
        [HarmonyPostfix]
        private static void HookEveryGameStart() {
            //var isNewGame = MB.GameSaveService.IsNewGame();
            //if (isNewGame) {
            //EffectModel effectModel = SO.Settings.GetEffect($"{PluginInfo.PLUGIN_GUID}_Archipelago");
            //effectModel.Apply();
            ////    ArchipelagoService.SetupNewGame();
            //}

            ArchipelagoService.EnterGame();
        }

        // Deprecated: for deprecated approach of adding hooked effects
        // We need this patch for our injected hook to not cause errors, since the base game has a big switch() statement
        // over every existing HookType
        static APItemReceivedMonitor apItemReceivedMonitor = new APItemReceivedMonitor();
        [HarmonyPatch(typeof(HookedEffectsController), nameof(HookedEffectsController.GetMonitorFor))]
        [HarmonyFinalizer]
        private static Exception HookedEffectsController_GetMonitorFor_Finalizer(Exception __exception, HookLogicType type, ref IHookMonitor __result) {
            if (__exception is NotImplementedException) {
                // Check custom types
                switch (type) {
                case ((HookLogicType)Constants.CustomHookType.APItemReceived):
                    __result = apItemReceivedMonitor;
                    return null; // do not throw exception
                default:
                    throw __exception;
                }
            }

            return __exception;
        }
    }
}
