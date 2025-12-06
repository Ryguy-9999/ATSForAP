using ATS_API.Helpers;
using Eremite;
using Eremite.Buildings;
using Eremite.Services;
using QFSW.QC;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ryguy9999.ATS.ATSForAP {
    class Utils {
        public static int GetFullyUpgradedHousedAmount(string speciesName) {
            int count = 0;

            foreach (var pair in GameMB.BuildingsService.Houses) {
                if ((pair.Value.UpgradableState.level >= 2 && speciesName != "Frog" || pair.Value.UpgradableState.level >= 4) &&
                    pair.Value.model.housingRaces.Contains(GameMB.Settings.GetRace(speciesName))) {
                    count += pair.Value.state.residents.Count;
                }
            }

            return count;
        }

        public static string GetOrdinalSuffix(int number) {
            switch (number) {
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

        public static string GetIDFromWorkshopName(string name) {
            return name
                .Replace("Druid's Hut", "Druid")
                .Replace("Flawless Druids Hut", "Flawless Druid")
                .Replace("Alchemist's Hut", "Alchemist Hut")
                .Replace("Teahouse", "Tea House")
                .Replace("Greenhouse", "Greenhouse Workshop")
                .Replace("Leatherworker", "Leatherworks")
                .Replace("Flawless Leatherworker", "Flawless Leatherworks")
                .Replace("Clay Pit", "Clay Pit Workshop")
                .Replace("Advanced Rain Collector", "Advanced Rain Catcher")
                .Replace("Lumber Mill", "Lumbermill")
                .Replace("Forester's Hut", "Grove")
                .Replace("Small Farm", "SmallFarm");
        }

        public static string GetBiomeNameFromID(string biome) {
            return biome
                .Replace("Moorlands", "Scarlet Orchard")
                .Replace("Bay", "Coastal Grove")
                .Replace("Wasteland", "Ashen Thicket")
                .Replace("Cave", "Rocky Ravine")
                .Replace("Poro Biome", "Bamboo Flats")
                .Replace("Sealed Biome", "Sealed Forest");
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
            SO.StaticRecipesService.GetType().GetField("goodsSourcesMap", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(SO.StaticRecipesService, new Dictionary<string, List<BuildingModel>>());
            SO.StaticRecipesService.GetType().GetMethod("MapGoodsSources", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(SO.StaticRecipesService, new object[0]);
        }

        [Command("ap.restoreProduction", "For debugging purposes. Removes the negative production modifiers applied by the AP mod.")]
        public static void RestoreProduction() {
            foreach (KeyValuePair<string, GoodsTypes> pair in Constants.ITEM_DICT) {
                string itemId = pair.Value.ToName();
                if (!ArchipelagoService.HasReceivedItem(itemId)) {
                    SO.EffectsService.GrantRawGoodProduction(itemId, Constants.PRODUCTIVITY_MODIFIER);
                }
            }
        }
    }
}
