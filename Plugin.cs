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
            harmony.PatchAll(typeof(GamePatches));
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
            MB.Controller.Build.type = Eremite.Model.Configs.BuildType.Development; // Dev mode to enable console

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

        [HarmonyPatch(typeof(GameController), nameof(GameController.StartGame))]
        [HarmonyPostfix]
        private static void HookEveryGameStart() {
            ArchipelagoService.EnterGame();
        }
    }
}
