using ATS_API.Helpers;
using Eremite;
using Eremite.Buildings;
using Eremite.Buildings.UI;
using Eremite.Model;
using Eremite.Model.State;
using Eremite.Services;
using Eremite.View.HUD;
using Eremite.View.HUD.Reputation;
using Eremite.View.HUD.TradeRoutes;
using Eremite.WorldMap.UI.CustomGames;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace Ryguy9999.ATS.ATSForAP {
    class StoragePatch {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StorageService), nameof(StorageService.Store))]
        [HarmonyPatch(new Type[] { typeof(Good), typeof(string), typeof(int), typeof(StorageOperationType) })]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Method Declaration", "Harmony003:Harmony non-ref patch parameters modified", Justification = "In fact not modifying")]
        private static bool StorePrefix(Good good, string ownerModel, int ownerId, StorageOperationType type) {
            bool isItemTrackedByAP = false;
            foreach(KeyValuePair<string, GoodsTypes> pair in Constants.ITEM_DICT) { 
                if(good.name == pair.Value.ToName()) {
                    isItemTrackedByAP = true;
                    break;
                }
            }

            var permittedOperations = new StorageOperationType[] { StorageOperationType.InitialCaravan, StorageOperationType.Other, StorageOperationType.ManualIngredientsReturn, StorageOperationType.IngredientsReturn, StorageOperationType.BuildingRemoval, StorageOperationType.BuildingRefund, StorageOperationType.BuildingMove };
            if(!isItemTrackedByAP || ArchipelagoService.HasReceivedItem(good.name) || Array.IndexOf(permittedOperations, type) > -1) {
                // True allows item through
                return true;
            }
            // False prevents original method from executing
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WorkshopRecipeState), MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(WorkshopRecipeModel), typeof(int), typeof(bool), typeof(bool), typeof(bool) })]
        private static void WorkshopRecipeStatePostfix(WorkshopRecipeState __instance, WorkshopRecipeModel model, int limit, bool firstIngredientEnabled, bool secondaryIngredientsEnabled, bool allRecipesEnabled) {
            __instance.active = __instance.active && ArchipelagoService.HasReceivedItem(model.producedGood.Name);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GathererHut), nameof(GathererHut.SetUp))]
        [HarmonyPatch(new Type[] { typeof(GathererHutModel), typeof(GathererHutState) })]
        private static void CampSetUpPostfix(GathererHut __instance, GathererHutModel model, GathererHutState state) {
            foreach(var recipe in __instance.state.recipes) {
                recipe.active = recipe.active && ArchipelagoService.HasReceivedItem(GameMB.Settings.GetRecipe(recipe.model).GetProducedGood());
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TradeRoutesGenerator), nameof(TradeRoutesGenerator.RegenerateOffersFor))]
        [HarmonyPatch(new Type[] { typeof(TradeTownState) })]
        private static bool TradeRoutesRegenerateForPrefix(TradeTownState town) {
            if (town.id == Constants.TRADE_TOWN_ID) {
                // Prevent service from regenerating special town for handling trade AP locations
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ExtendOffersSlot), nameof(ExtendOffersSlot.CanBuy))]
        private static bool TradeTownExtendPrefix(ExtendOffersSlot __instance, ref bool __result) {
            if (__instance.state.id == Constants.TRADE_TOWN_ID) {
                // Cannot extend AP town offers
                __result = false;
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ReputationRewardButton), nameof(ReputationRewardButton.UpdateCounter))]
        [HarmonyPatch(new Type[] { typeof(int) })]
        private static void ReputationBlueprintCounterPrefix(ref int amount) {
            // Prevent natural blueprint selection when receiving them from AP
            if (ArchipelagoService.TreatBlueprintsAsItems) {
                amount = GameMB.EffectsService.GetWildcardPicksLeft();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CalendarDisplay), nameof(CalendarDisplay.SelfUpdate))]
        private static bool CalendarUpdatePrefix(CalendarDisplay __instance) {
            __instance.UpdateCalendar(GameMB.CalendarService.GetCurrentSeasonProgress());
            __instance.CheckForStormSound();
            // Never run the original, it listens for DevNextSeason which we are trying to disable here
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Relic), nameof(Relic.AddAllRewards))]
        private static void RelicAllRewardsPostfix(Relic __instance) {
            // After a completed event calculates its rewards, we come in and strip all the items not unlocked
            // through AP, just so scouts don't spend a long time hauling goods that won't store
            foreach(Good good in __instance.state.rewards.ToList()) {
                if(!ArchipelagoService.HasReceivedItem(good.name)) {
                    __instance.state.rewards.Remove(good);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CustomGameTradeTownsPanel), nameof(CustomGameTradeTownsPanel.PrepareAll))]
        private static void CustomGameTradeTownSetUpPostfix(CustomGameTradeTownsPanel __instance) {
            // Start the first 4 trade towns as selected, as that's probably the better default
            Plugin.Log(__instance.ToString());
            Plugin.Log(__instance.picked.ToString());
            Plugin.Log(__instance.picked.Count.ToString());
            Plugin.Log(__instance.all.ToString());
            Plugin.Log(__instance.all.Count.ToString());
            for (int i = 0; i < 4 && i < __instance.all.Count; i++) {
                Plugin.Log(__instance.all[i].faction);
                __instance.picked.Add(__instance.all[i]);
            }
        }
    }
}
