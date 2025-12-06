using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using ATS_API.Helpers;
using ATS_API.Localization;
using Eremite;
using Eremite.Buildings;
using Eremite.Buildings.UI.Ports;
using Eremite.Buildings.UI.Seals;
using Eremite.Characters.Behaviours;
using Eremite.Characters.Villagers;
using Eremite.Controller.Generator;
using Eremite.Model;
using Eremite.Model.Sound;
using Eremite.Model.State;
using Eremite.Services;
using Eremite.View;
using Eremite.View.HUD;
using Eremite.View.HUD.Reputation;
using Eremite.View.HUD.TradeRoutes;
using Eremite.View.Menu;
using Eremite.View.Menu.Pick;
using Eremite.WorldMap;
using Eremite.WorldMap.UI;
using Eremite.WorldMap.UI.CustomGames;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;

namespace Ryguy9999.ATS.ATSForAP {
    class GamePatches {
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
            foreach (var recipe in __instance.state.recipes) {
                recipe.active = recipe.active && ArchipelagoService.HasReceivedItem(GameMB.Settings.GetRecipe(recipe.model).GetProducedGood());
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FishingHut), nameof(FishingHut.SetUp))]
        [HarmonyPatch(new Type[] { typeof(FishingHutModel), typeof(FishingHutState) })]
        private static void FishingHutSetUpPostfix(GathererHut __instance, FishingHutModel model, FishingHutState state) {
            foreach (var recipe in __instance.state.recipes) {
                recipe.active = recipe.active && ArchipelagoService.HasReceivedItem(GameMB.Settings.GetRecipe(recipe.model).GetProducedGood());
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Farm), nameof(Farm.SetUp))]
        [HarmonyPatch(new Type[] { typeof(FarmModel), typeof(FarmState) })]
        private static void FarmSetUpPostfix(Farm __instance, FarmModel model, FarmState state) {
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
            if (ArchipelagoService.PreventNaturalBPSelection) {
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
            for (int i = 0; i < 4 && i < __instance.all.Count; i++) {
                __instance.picked.Add(__instance.all[i]);
            }
        }

        private static string GetRelevantPart() {
            SealState sealState = Serviceable.StateService.Buildings.seals.Count > 0 ? Serviceable.StateService.Buildings.seals.First<SealState>() : null;
            int currentSealLevel = 0;
            while (currentSealLevel < sealState.kits.Length && sealState.kits[currentSealLevel].completedIndex >= 0) {
                currentSealLevel += 1;
            }
            switch (currentSealLevel) {
            case 0:
                return "Guardian Heart";
            case 1:
                return "Guardian Blood";
            case 2:
                return "Guardian Feathers";
            case 3:
                return "Guardian Essence";
            }
            return "";
        }

        private static bool HasCompletedEnoughOrders() {
            SealState sealState = Serviceable.StateService.Buildings.seals.Count > 0 ? Serviceable.StateService.Buildings.seals.First<SealState>() : null;
            int currentSealLevel = 0;
            while (currentSealLevel < sealState.kits.Length && sealState.kits[currentSealLevel].completedIndex >= 0) {
                currentSealLevel += 1;
            }
            int completedOrders = 0;
            for (int i = 0; i < sealState.kits[currentSealLevel].orders.Length; i++) {
                if (GameMB.OrdersService.CanComplete(sealState.kits[currentSealLevel].orders[i], GameMB.Settings.GetOrder(sealState.kits[currentSealLevel].orders[i].model))) {
                    completedOrders++;
                }
            }
            return completedOrders >= ArchipelagoService.RequiredSealTasks;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PartSlot), nameof(PartSlot.StartButtons))]
        private static void PartSlotButtonsPostfix(PartSlot __instance) {
            __instance.completeButton.OnFailedClick.Subscribe(() => {
                if(!ArchipelagoService.HasReceivedGuardianPart(GetRelevantPart())) {
                    GameMB.NewsService.PublishNews("Can't complete seal part!", $"\"Seal Parts\" option is on for this AP slot, meaning you need to receive \"{GetRelevantPart()}\" before you can complete this phase of the Seal.", Eremite.Services.Monitors.AlertSeverity.Warning);
                } else if(!HasCompletedEnoughOrders()) {
                    GameMB.NewsService.PublishNews("Can't complete seal part!", $"\"Required Seal Tasks\" is set to {ArchipelagoService.RequiredSealTasks} for this AP slot, meaning you need to fulfill the requirements for that many tasks before you can complete this phase of the Seal.", Eremite.Services.Monitors.AlertSeverity.Warning);
                }
            });
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PartSlot), nameof(PartSlot.UpdateButton))]
        private static void PartSlotUpdateButtonPostfix(PartSlot __instance) {
            __instance.completeButton.CanInteract = __instance.completeButton.CanInteract && HasCompletedEnoughOrders() && (!ArchipelagoService.RequiredGuardianParts || ArchipelagoService.HasReceivedGuardianPart(GetRelevantPart()));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VersionText), nameof(VersionText.Start))]
        private static void VersionTextStartPostfix(VersionText __instance) {
            __instance.text.text += " + APv" + PluginInfo.PLUGIN_VERSION;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TownOfferSlot), nameof(TownOfferSlot.SetUpSlots))]
        private static bool SetUpTownOfferSlotPostfix(TownOfferSlot __instance) {
            string scoutedKey = ArchipelagoService.LocationScouts.Keys.FirstOrDefault(scout => {
                Match match = new Regex(@"Trade - (\d+) (.+)").Match(scout);
                int tradeQty = Int32.Parse(match.Groups[1].ToString());
                string tradeItem = match.Groups[2].ToString();
                
                if (Constants.ITEM_DICT.ContainsKey(tradeItem)) {
                    tradeItem = Constants.ITEM_DICT[tradeItem].ToName();
                } else if (tradeItem.Contains("Water")) {
                    tradeItem = "[Water] " + tradeItem;
                }

                return __instance.state.good.name == tradeItem && __instance.state.good.amount == tradeQty;
            });
            if (__instance.state.townId == Constants.TRADE_TOWN_ID && scoutedKey != null) {
                ScoutedItemInfo scoutInfo = ArchipelagoService.LocationScouts[scoutedKey];
                var playerName = scoutInfo.Player.Alias;
                var itemScouted = scoutInfo.ItemDisplayName;
                var itemType = scoutInfo.Flags == ItemFlags.Trap ? "trap" : scoutInfo.Flags == ItemFlags.NeverExclude ? "useful" : scoutInfo.Flags == ItemFlags.Advancement ? "progression" : "filler";

                __instance.good.SetUp(GameMB.TradeRoutesService.GetFullGood(__instance.state));
                __instance.price.SetUp(new Good($"{Constants.SCOUT_STRING_PREFIX}||{itemType}||{itemScouted}||{playerName}", 0));
                __instance.fuel.SetUp(GameMB.TradeRoutesService.GetFullFuel(__instance.state));
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GoodSlot), nameof(GoodSlot.SetUpIcon))]
        private static bool SetUpSlotIconPrefix(GoodSlot __instance) {
            if (__instance.good.name.StartsWith(Constants.SCOUT_STRING_PREFIX)) {
                __instance.icon.sprite = TextureHelper.GetImageAsSprite("ap-icon.png", TextureHelper.SpriteType.EffectIcon);
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GoodSlot), nameof(GoodSlot.SetUpCounter))]
        private static bool SetUpSlotCounterPrefix(GoodSlot __instance) {
            if (__instance.good.name.StartsWith(Constants.SCOUT_STRING_PREFIX)) {
                __instance.counter.transform.parent.SetActive(false);
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GoodTooltip), nameof(GoodTooltip.Show))]
        [HarmonyPatch(new Type[] { typeof(RectTransform), typeof(TooltipSettings), typeof(Good), typeof(GoodTooltipMode), typeof(string) })]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Method Declaration", "Harmony003:Harmony non-ref patch parameters modified", Justification = "In fact not modifying")]
        private static bool GoodTooltipShowPrefix(GoodTooltip __instance, RectTransform target, TooltipSettings settings, Good good, GoodTooltipMode mode, string footnote = null) {
            if (good.name.StartsWith(Constants.SCOUT_STRING_PREFIX)) {
                Match match = new Regex(@"^\|\|(.*)\|\|(.*)\|\|(.*)$").Match(good.name.Substring(Constants.SCOUT_STRING_PREFIX.Length));
                var playerName = match.Groups[3].ToString();
                var itemScouted = match.Groups[2].ToString();
                var itemType = match.Groups[1].ToString();

                //__instance.model = model;
                //__instance.amount = amount;
                __instance.mode = mode;
                __instance.nameText.text = $"{playerName}'s {itemScouted}";
                __instance.descText.text = $"You can trade for {playerName}'s {itemScouted}! {itemType.Replace("trap", "They might not be too happy about it though...").Replace("filler", "It's uncertain how much they'll use it though.").Replace("useful", "They'll probably get some use out of this.").Replace("progression", "It seems pretty important to them!")}";
                __instance.labelText.text = "Archipelago Item";
                __instance.storageParent.SetActive(false);
                if (__instance.icon != null) {
                    __instance.icon.sprite = TextureHelper.GetImageAsSprite("ap-icon.png", TextureHelper.SpriteType.EffectIcon); ;
                }
                __instance.footnoteParent.SetActive(false);
                __instance.AnimateShow(target, settings);

                return false;
            }

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HarvestDeposit), nameof(HarvestDeposit.OnComplete))]
        private static void HarvestCompletePostfix(HarvestDeposit __instance) {
            var name = __instance.deposit.name;
            if (name.Contains("Marshlands Infinite")) {
                if (name.Contains("Grain")) {
                    ArchipelagoService.CheckLocation("The Marshlands - Harvest from an Ancient Proto Wheat");
                }
                if (name.Contains("Meat")) {
                    ArchipelagoService.CheckLocation("The Marshlands - Harvest from a Dead Leviathan");
                }
                if (name.Contains("Mushroom")) {
                    ArchipelagoService.CheckLocation("The Marshlands - Harvest from a Giant Proto Fungus");
                }
            }
        }

        private static BuildingCategoryModel apExpeditionCategoryModel = new BuildingCategoryModel {
            name = "AP Checks",
            icon = TextureHelper.GetImageAsSprite("ap-category-icon.png", TextureHelper.SpriteType.EffectIcon),
            isActive = true,
            isOnHUD = false,
            isDebugOnHUD = false,
            shortName = LocalizationManager.ToLocaText("ATSForAP_GameUI_APExpeditionCategory"),
            displayName = LocalizationManager.ToLocaText("ATSForAP_GameUI_APExpeditionCategory")
        };
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PortRewardsPickPanel), nameof(PortRewardsPickPanel.GetCategories))]
        private static void GetCategoriesPostfix(ref List<BuildingCategoryModel> __result) {
            if(ArchipelagoService.TotalGroveExpeditionLocationsCount() > 0) {
                __result.Add(apExpeditionCategoryModel);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildingCategoryRadialSlot), nameof(BuildingCategoryRadialSlot.CountOwned))]
        private static bool PortMenuSlotCountOwnedPrefix(BuildingCategoryRadialSlot __instance, ref int __result) {
            if (__instance.model.displayName.Text == "AP Checks") {
                __result = ArchipelagoService.CheckedGroveExpeditionLocationsCount();
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildingCategoryRadialSlot), nameof(BuildingCategoryRadialSlot.CountMax))]
        private static bool PortMenuSlotCountMaxPrefix(BuildingCategoryRadialSlot __instance, ref int __result) {
            if (__instance.model.displayName.Text == "AP Checks") {
                __result = ArchipelagoService.TotalGroveExpeditionLocationsCount();
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PortRewardsPickPanel), nameof(PortRewardsPickPanel.GetCurrentCategory))]
        private static bool PortRewardPanelGetCategoryPrefix(PortRewardsPickPanel __instance, ref BuildingCategoryModel __result) {
            Plugin.Log(__instance.port.state.pickedCategory);
            if(__instance.port.state.pickedCategory == "AP Checks") {
                __result = apExpeditionCategoryModel;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PortRewardsGenerator), nameof(PortRewardsGenerator.SetMainReward))]
        private static bool PortRewardGeneratorMainRewardPrefix(PortRewardsGenerator __instance) {
            if(__instance.port.state.pickedCategory == "AP Checks") {
                int expeditionNumber = ArchipelagoService.GetNextUncheckedGroveExpedition();
                if(expeditionNumber < 0) {
                    return true;
                }
                ArchipelagoService.CheckLocation($"Coastal Grove - {expeditionNumber}{Utils.GetOrdinalSuffix(expeditionNumber)} Expedition");
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PortRewardsPanel), nameof(PortRewardsPanel.HasBlueprintReward))]
        private static bool PortRewardsBPRewardPrefix(PortRewardsPanel __instance, ref bool __result) {
            if (__instance.port.state.pickedCategory == "AP Checks") {
                __result = false;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PerkCrafter), nameof(PerkCrafter.CreateCurrentPerk))]
        private static void PerkCraftingCreatePrefix() {
            int forgeNumber = ArchipelagoService.GetNextUncheckedCornerstoneForge();
            if(forgeNumber < 0) {
                return;
            }
            ArchipelagoService.CheckLocation($"Ashen Thicket - Forge {forgeNumber}{Utils.GetOrdinalSuffix(forgeNumber)} Cornerstone");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AchivsService), nameof(AchivsService.SendAchivs))]
        private static bool SendAchievementPrefix() {
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Villager), nameof(Villager.Die))]
        [HarmonyPatch(new Type[] { typeof(VillagerLossType), typeof(string), typeof(bool), typeof(float), typeof(SoundModel) })]
        private static void DiePostfix(VillagerLossType lossType, string reasonKey, bool showDying = true, float duration = 25f, SoundModel extraSound = null) {
            ArchipelagoService.HandleVillagerDeath(lossType, reasonKey);
        }

        private static CustomGamePopup customGamePopupInstance;
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CustomGamePopup), nameof(CustomGamePopup.Show))]
        private static void CustomGameShowPostfix(CustomGamePopup __instance) {
            customGamePopupInstance = __instance;
            __instance.embarkButton.interactable = ArchipelagoService.HasReceivedBiome(Utils.GetBiomeNameFromID(__instance.biomePanel.GetBiome().Name));
            __instance.biomePanel.dropdown.onValueChanged.AddListener(new UnityAction<int>(OnBiomeValueChanged));
        }
        private static void OnBiomeValueChanged(int value) {
            customGamePopupInstance.embarkButton.interactable = ArchipelagoService.HasReceivedBiome(Utils.GetBiomeNameFromID(customGamePopupInstance.biomePanel.GetBiome().Name));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomGameBiomePanel), nameof(CustomGameBiomePanel.CanBiomeBePicked))]
        [HarmonyPatch(new Type[] { typeof(BiomeModel) })]
        private static bool CustomGameBiomeCanBePickedPrefix(CustomGameBiomePanel __instance, BiomeModel biome, ref bool __result) {
            if(!ArchipelagoService.HasReceivedBiome(Utils.GetBiomeNameFromID(biome.Name))) {
                __result = false;
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildingsPickScreen), nameof(BuildingsPickScreen.UpdateConfirmButton))]
        private static bool EmbarkScreenShowPostfix(BuildingsPickScreen __instance) {
            bool HasUnlocked = ArchipelagoService.HasReceivedBiome(Utils.GetBiomeNameFromID(__instance.field.Biome.Name));
            if(!HasUnlocked) {
                __instance.confirmButton.CanInteract = false;
                return false;
            }

            return true;
        }
    }
}
