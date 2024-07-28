using BepInEx;
using HarmonyLib;
using Eremite;
using Eremite.Controller;
using Eremite.Model;
using ATS_API.Effects;
using Eremite.Model.Effects;
using System;
using Eremite.Controller.Effects;

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

        public static void Log(string s) {
            Instance.Logger.LogInfo(s);
        }

        [HarmonyPatch(typeof(MainController), nameof(MainController.OnServicesReady))]
        [HarmonyPostfix]
        private static void HookMainControllerSetup()
        { 
            // This method will run after game load (Roughly on entering the main menu)
            // At this point a lot of the game's data will be available.
            // Main entry point to access this data will be `Serviceable.Settings` or `MainController.Instance.Settings`
            MB.Controller.Build.type = Eremite.Model.Configs.BuildType.Development;
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
        private static void HookEveryGameStart()
        {
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
