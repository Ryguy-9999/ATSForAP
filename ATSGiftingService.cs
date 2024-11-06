using Archipelago.Gifting.Net.Gifts;
using Archipelago.Gifting.Net.Gifts.Versions.Current;
using Archipelago.Gifting.Net.Service;
using Archipelago.Gifting.Net.Traits;
using Archipelago.Gifting.Net.Utilities.CloseTraitParser;
using Archipelago.MultiClient.Net;
using ATS_API.Helpers;
using Eremite;
using Eremite.Model;
using QFSW.QC;
using QFSW.QC.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ryguy9999.ATS.ATSForAP {
    class ATSGiftingService {
        static ICloseTraitParser<string> giftTraitParser = new BKTreeCloseTraitParser<string>();
        static GiftingService giftingService = null;
        static Dictionary<string, GiftTrait[]> giftTagDictionary = new Dictionary<string, GiftTrait[]> {
            ["Wood"] = new GiftTrait[] { new GiftTrait("Wood", 1, 1), new GiftTrait("Material", 1, 1), new GiftTrait("Fuel", 1, 1) },
            ["Berry"] = new GiftTrait[] { new GiftTrait("Berry", 1, 1), new GiftTrait("Fruit", 1, 1), new GiftTrait("Food", 1, 1) },
            ["Egg"] = new GiftTrait[] { new GiftTrait("Egg", 1, 1), new GiftTrait("Food", 1, 1) },
            ["Insect"] = new GiftTrait[] { new GiftTrait("Insect", 1, 1), new GiftTrait("Food", 1, 1) },
            ["Meat"] = new GiftTrait[] { new GiftTrait("Meat", 1, 1), new GiftTrait("Food", 1, 1) },
            ["Mushroom"] = new GiftTrait[] { new GiftTrait("Mushroom", 1, 1), new GiftTrait("Food", 1, 1) },
            ["Root"] = new GiftTrait[] { new GiftTrait("Root", 1, 1), new GiftTrait("Food", 1, 1) },
            ["Vegetable"] = new GiftTrait[] { new GiftTrait("Vegetable", 1, 1), new GiftTrait("Food", 1, 1) },
            ["Fish"] = new GiftTrait[] { new GiftTrait("Fish", 1, 1), new GiftTrait("Food", 1, 1) },
            ["Biscuit"] = new GiftTrait[] { new GiftTrait("Cooking", 1, 1), new GiftTrait("Food", 1, 2) },
            ["Jerky"] = new GiftTrait[] { new GiftTrait("Meat", 1, 1), new GiftTrait("Cooking", 1, 1), new GiftTrait("Food", 1, 2) },
            ["Pickled Good"] = new GiftTrait[] { new GiftTrait("Pickle", 1, 1), new GiftTrait("Cooking", 1, 1), new GiftTrait("Food", 1, 2) },
            ["Pie"] = new GiftTrait[] { new GiftTrait("Cooking", 1, 1), new GiftTrait("Food", 1, 2) },
            ["Porridge"] = new GiftTrait[] { new GiftTrait("Cooking", 1, 1), new GiftTrait("Food", 1, 2) },
            ["Skewer"] = new GiftTrait[] { new GiftTrait("Cooking", 1, 1), new GiftTrait("Food", 1, 2) },
            ["Paste"] = new GiftTrait[] { new GiftTrait("Cooking", 1, 1), new GiftTrait("Food", 1, 2) },
            ["Coat"] = new GiftTrait[] { new GiftTrait("Cloth", 1, 1), new GiftTrait("Clothing", 1, 1) },
            ["Boot"] = new GiftTrait[] { new GiftTrait("Leather", 1, 1), new GiftTrait("Clothing", 1, 1) },
            ["Brick"] = new GiftTrait[] { new GiftTrait("Stone", 1, 2), new GiftTrait("Material", 1, 2) },
            ["Fabric"] = new GiftTrait[] { new GiftTrait("Cloth", 1, 1), new GiftTrait("Material", 1, 2) },
            ["Plank"] = new GiftTrait[] { new GiftTrait("Lumber", 1, 1), new GiftTrait("Wood", 1, 2), new GiftTrait("Material", 1, 2) },
            ["Pipe"] = new GiftTrait[] { new GiftTrait("Pipe", 1, 1), new GiftTrait("Metal", 1, 1) },
            ["Part"] = new GiftTrait[] { new GiftTrait("Gear", 1, 3) },
            ["Wildfire Essence"] = new GiftTrait[] { new GiftTrait("Fire", 1, 3) },
            ["Ale"] = new GiftTrait[] { new GiftTrait("Alcohol", 1, 1), new GiftTrait("Root", 1, 1), new GiftTrait("Luxury", 1, 1) },
            ["Incense"] = new GiftTrait[] { new GiftTrait("Luxury", 1, 1) },
            ["Scroll"] = new GiftTrait[] { new GiftTrait("Scroll", 1, 1), new GiftTrait("Luxury", 1, 1) },
            ["Tea"] = new GiftTrait[] { new GiftTrait("Coffee", 1, 1), new GiftTrait("Luxury", 1, 1) },
            ["Training Gear"] = new GiftTrait[] { new GiftTrait("Weapon", 1, 1), new GiftTrait("Luxury", 1, 1) },
            ["Wine"] = new GiftTrait[] { new GiftTrait("Alcohol", 1, 1), new GiftTrait("Fruit", 1, 1), new GiftTrait("Luxury", 1, 1) },
            ["Clay"] = new GiftTrait[] { new GiftTrait("Clay", 1, 1), new GiftTrait("Material", 1, 1) },
            ["Copper Ore"] = new GiftTrait[] { new GiftTrait("Ore", 1, 1), new GiftTrait("Copper", 1, 0.5), new GiftTrait("Metal", 1, 0.5), new GiftTrait("Material", 1, 1) },
            ["Scale"] = new GiftTrait[] { new GiftTrait("Material", 1, 1), new GiftTrait("Ore", 1, 0.1), new GiftTrait("Copper", 1, 0.1) },
            ["Crystallized Dew"] = new GiftTrait[] { new GiftTrait("Metal", 1, 1), new GiftTrait("Material", 1, 1) },
            ["Grain"] = new GiftTrait[] { new GiftTrait("Grain", 1, 1) },
            ["Herb"] = new GiftTrait[] { new GiftTrait("Herb", 1, 1) },
            ["Leather"] = new GiftTrait[] { new GiftTrait("Leather", 1, 1), new GiftTrait("Material", 1, 1) },
            ["Plant Fiber"] = new GiftTrait[] { new GiftTrait("Fiber", 1, 1), new GiftTrait("Material", 1, 1) },
            ["Algae"] = new GiftTrait[] { new GiftTrait("Algae", 1, 1), new GiftTrait("Material", 1, 1) },
            ["Reed"] = new GiftTrait[] { new GiftTrait("Material", 1, 1) },
            ["Resin"] = new GiftTrait[] { new GiftTrait("Material", 1, 1), new GiftTrait("Amber", 1, 0.1) },
            ["Stone"] = new GiftTrait[] { new GiftTrait("Stone", 1, 1), new GiftTrait("Material", 1, 1) },
            ["Barrel"] = new GiftTrait[] { new GiftTrait("Container", 1, 1) },
            ["Copper Bar"] = new GiftTrait[] { new GiftTrait("Metal", 1, 1), new GiftTrait("Copper", 1, 1), new GiftTrait("Material", 1, 1) },
            ["Flour"] = new GiftTrait[] { new GiftTrait("Flour", 1, 1) },
            ["Dye"] = new GiftTrait[] { new GiftTrait("Dye", 1, 1), new GiftTrait("Material", 1, 1) },
            ["Pottery"] = new GiftTrait[] { new GiftTrait("Container", 1, 1) },
            ["Waterskin"] = new GiftTrait[] { new GiftTrait("Container", 1, 1) },
            ["Amber"] = new GiftTrait[] { new GiftTrait("Amber", 1, 1), new GiftTrait("Currency", 1, 1), new GiftTrait("Gem", 1, 1) },
            ["Pack of Building Materials"] = new GiftTrait[] { new GiftTrait("Pack", 1, 1) },
            ["Pack of Crops"] = new GiftTrait[] { new GiftTrait("Pack", 1, 1) },
            ["Pack of Luxury Goods"] = new GiftTrait[] { new GiftTrait("Pack", 1, 1) },
            ["Pack of Provisions"] = new GiftTrait[] { new GiftTrait("Pack", 1, 1) },
            ["Pack of Trade Goods"] = new GiftTrait[] { new GiftTrait("Pack", 1, 1) },
            ["Ancient Tablet"] = new GiftTrait[] { new GiftTrait("Ancient", 1, 1), new GiftTrait("Artifact", 1, 1) },
            ["Coal"] = new GiftTrait[] { new GiftTrait("Coal", 1, 1), new GiftTrait("Fuel", 1, 3) },
            ["Oil"] = new GiftTrait[] { new GiftTrait("Oil", 1, 1), new GiftTrait("Fuel", 1, 2) },
            ["Purging Fire"] = new GiftTrait[] { new GiftTrait("Fire", 1, 1) },
            ["Sea Marrow"] = new GiftTrait[] { new GiftTrait("Fossil", 1, 1), new GiftTrait("Fuel", 1, 3) },
            ["Tool"] = new GiftTrait[] { new GiftTrait("Tool", 1, 1) },
            ["Drizzle Water"] = new GiftTrait[] { new GiftTrait("Water", 1, 1), new GiftTrait("Green", 1, 0.1) },
            ["Clearance Water"] = new GiftTrait[] { new GiftTrait("Water", 1, 1), new GiftTrait("Yellow", 1, 0.1) },
            ["Storm Water"] = new GiftTrait[] { new GiftTrait("Water", 1, 1), new GiftTrait("Blue", 1, 0.1) },
        };
        static Dictionary<string, string> giftNames = new Dictionary<string, string> {
            [GoodsTypes.Mat_Raw_Wood.ToName()] = "Wood",
            [GoodsTypes.Food_Raw_Berries.ToName()] = "Berry",
            [GoodsTypes.Food_Raw_Eggs.ToName()] = "Egg",
            [GoodsTypes.Food_Raw_Insects.ToName()] = "Insect",
            [GoodsTypes.Food_Raw_Meat.ToName()] = "Meat",
            [GoodsTypes.Food_Raw_Mushrooms.ToName()] = "Mushroom",
            [GoodsTypes.Food_Raw_Roots.ToName()] = "Root",
            [GoodsTypes.Food_Raw_Vegetables.ToName()] = "Vegetable",
            [GoodsTypes.Food_Raw_Fish.ToName()] = "Fish",
            [GoodsTypes.Food_Processed_Biscuits.ToName()] = "Biscuit",
            [GoodsTypes.Food_Processed_Jerky.ToName()] = "Jerky",
            [GoodsTypes.Food_Processed_Pickled_Goods.ToName()] = "Pickled Good",
            [GoodsTypes.Food_Processed_Pie.ToName()] = "Pie",
            [GoodsTypes.Food_Processed_Porridge.ToName()] = "Porridge",
            [GoodsTypes.Food_Processed_Skewers.ToName()] = "Skewer",
            [GoodsTypes.Food_Processed_Paste.ToName()] = "Paste",
            [GoodsTypes.Crafting_Coal.ToName()] = "Coat",
            [GoodsTypes.Needs_Boots.ToName()] = "Boot",
            [GoodsTypes.Mat_Processed_Bricks.ToName()] = "Brick",
            [GoodsTypes.Mat_Processed_Fabric.ToName()] = "Fabric",
            [GoodsTypes.Mat_Processed_Planks.ToName()] = "Plank",
            [GoodsTypes.Mat_Processed_Pipe.ToName()] = "Pipe",
            [GoodsTypes.Mat_Processed_Parts.ToName()] = "Part",
            [GoodsTypes.Hearth_Parts.ToName()] = "Wildfire Essence",
            [GoodsTypes.Needs_Ale.ToName()] = "Ale",
            [GoodsTypes.Needs_Incense.ToName()] = "Incense",
            [GoodsTypes.Needs_Scrolls.ToName()] = "Scroll",
            [GoodsTypes.Needs_Tea.ToName()] = "Tea",
            [GoodsTypes.Needs_Training_Gear.ToName()] = "Training Gear",
            [GoodsTypes.Mat_Raw_Clay.ToName()] = "Wine",
            [GoodsTypes.Mat_Raw_Clay.ToName()] = "Clay",
            [GoodsTypes.Metal_Copper_Ore.ToName()] = "Copper Ore",
            [GoodsTypes.Mat_Raw_Scales.ToName()] = "Scale",
            [GoodsTypes.Metal_Crystalized_Dew.ToName()] = "Crystallized Dew",
            [GoodsTypes.Food_Raw_Grain.ToName()] = "Grain",
            [GoodsTypes.Food_Raw_Herbs.ToName()] = "Herb",
            [GoodsTypes.Mat_Raw_Leather.ToName()] = "Leather",
            [GoodsTypes.Mat_Raw_Plant_Fibre.ToName()] = "Plant Fiber",
            [GoodsTypes.Mat_Raw_Algae.ToName()] = "Algae",
            [GoodsTypes.Mat_Raw_Reeds.ToName()] = "Reed",
            [GoodsTypes.Mat_Raw_Resin.ToName()] = "Resin",
            [GoodsTypes.Mat_Raw_Stone.ToName()] = "Stone",
            [GoodsTypes.Vessel_Barrels.ToName()] = "Barrel",
            [GoodsTypes.Metal_Copper_Bar.ToName()] = "Copper Bar",
            [GoodsTypes.Crafting_Flour.ToName()] = "Flour",
            [GoodsTypes.Crafting_Dye.ToName()] = "Dye",
            [GoodsTypes.Vessel_Pottery.ToName()] = "Pottery",
            [GoodsTypes.Vessel_Waterskin.ToName()] = "Waterskin",
            [GoodsTypes.Valuable_Amber.ToName()] = "Amber",
            [GoodsTypes.Packs_Pack_Of_Building_Materials.ToName()] = "Pack of Building Materials",
            [GoodsTypes.Packs_Pack_Of_Crops.ToName()] = "Pack of Crops",
            [GoodsTypes.Packs_Pack_Of_Luxury_Goods.ToName()] = "Pack of Luxury Goods",
            [GoodsTypes.Packs_Pack_Of_Provisions.ToName()] = "Pack of Provisions",
            [GoodsTypes.Packs_Pack_Of_Trade_Goods.ToName()] = "Pack of Trade Goods",
            [GoodsTypes.Valuable_Ancient_Tablet.ToName()] = "Ancient Tablet",
            [GoodsTypes.Crafting_Coal.ToName()] = "Coal",
            [GoodsTypes.Crafting_Oil.ToName()] = "Oil",
            [GoodsTypes.Blight_Fuel.ToName()] = "Purging Fire",
            [GoodsTypes.Crafting_Sea_Marrow.ToName()] = "Sea Marrow",
            [GoodsTypes.Tools_Simple_Tools.ToName()] = "Tool",
            [GoodsTypes.Water_Drizzle_Water.ToName()] = "Drizzle Water",
            [GoodsTypes.Water_Clearance_Water.ToName()] = "Clearance Water",
            [GoodsTypes.Water_Storm_Water.ToName()] = "Storm Water",
        };

        public static void InitializeGifting(ArchipelagoSession session) {
            giftingService = new GiftingService(session);
            giftingService.OpenGiftBox();
            giftingService.SubscribeToNewGifts(new Action<Dictionary<string, Gift>>(HandleNewGifts));
            if(GameMB.IsGameActive) {
                HandleNewGifts(giftingService.GetAllGiftsAndEmptyGiftbox());
            }

            // Created traits
            // Berry, Insect, Root, Oil, Fuel, Water, Amber, Clay, Leather, Container, Cloth, Clothing, Luxury, Gear, Pipe, Pack, Grain, Flour, Herb
            foreach (var item in giftTagDictionary) {
                giftTraitParser.RegisterAvailableGift(item.Key, item.Value);
            }
        }

        public static void EnterGame() {
            if(giftingService != null) {
                HandleNewGifts(giftingService.GetAllGiftsAndEmptyGiftbox());
            }
        }

        private static void HandleNewGifts(Dictionary<string, Gift> gifts) {
            if(!GameMB.IsGameActive) {
                return;
            }

            foreach (var gift in gifts) {
                HandleGift(gift.Key, gift.Value);
            }
        }

        private static void HandleGift(string giftId, Gift gift) {
            Plugin.Log($"Incoming AP gift: \"{gift.ItemName}\" ({gift.Amount}). id: {giftId}");

            string goodsGiftedId = null;
            goodsGiftedId = HandleSpecialCaseGift(gift);
            if (goodsGiftedId != null) {
                ReceiveGoods(goodsGiftedId, gift);
                return;
            }
            goodsGiftedId = HandleStringContainsGift(gift);
            if (goodsGiftedId != null) {
                ReceiveGoods(goodsGiftedId, gift);
                return;
            }
            goodsGiftedId = HandleGiftTagGift(gift);
            if (goodsGiftedId != null) {
                ReceiveGoods(goodsGiftedId, gift);
                return;
            }

            Plugin.Log($"Failed to process gift \"{gift.ItemName}\" ({gift.Amount}) with tags [{String.Join(", ", gift.Traits.Select(trait => trait.Trait))}]");
            giftingService.RefundGift(gift);
        }

        private static void ReceiveGoods(string goodsId, Gift gift) {
            ArchipelagoService.ItemsForNews.Add((null, "You received a gift through AP!", $"{gift.Amount} {gift.ItemName} received from {ArchipelagoService.session.Players.GetPlayerAlias(gift.SenderSlot)}!"));
            GameMB.StorageService.Store(new Good(goodsId, gift.Amount), StorageOperationType.Other);
            giftingService.RemoveGiftFromGiftBox(gift.ID);
        }

        private static string HandleSpecialCaseGift(Gift gift) {
            return null;
        }

        private static string HandleStringContainsGift(Gift gift) {
            string giftName = " " + gift.ItemName + " ";

            foreach(KeyValuePair<string, string> pair in giftNames) {
                if(giftName.Contains(pair.Value + " ") || giftName.Contains(" " + pair.Value)) {
                    Plugin.Log("Matched gift on gift name: " + pair.Value);
                    return pair.Key;
                }
            }

            return null;
        }

        private static string HandleGiftTagGift(Gift gift) {
            var parsedGiftOptions = giftTraitParser.FindClosestAvailableGift(gift.Traits);

            if(parsedGiftOptions.Count > 0) {
                Plugin.Log($"Matched gift using gift tags: {String.Join(", ", (object[])gift.Traits)}. Result: {String.Join(", ", parsedGiftOptions)}");
                return giftNames.FirstOrDefault(pair => pair.Value == parsedGiftOptions[0]).Key;
            }

            return null;
        }

        [Command("ap.gift", "Sends a gift to the specified player.", Platform.AllPlatforms, MonoTargetType.Single)]
        public static IEnumerator<ICommandAction> SendGift(string player, string resourceToGift, int quantity) {
            if(!GameMB.IsGameActive) {
                yield return new Value("You must be currently in a settlement to send/receive gifts.");
                yield break;
            }

            if(giftingService == null) {
                yield return new Value("Not connected to archipelago, cannot send gift.");
                yield break;
            }

            string resourceId;
            if (!giftNames.ContainsKey(resourceToGift)) {
                if(resourceToGift == "Wood") {
                    resourceId = GoodsTypes.Mat_Raw_Wood.ToName();
                } else if (resourceToGift == "Drizzle Water") {
                    resourceId = GoodsTypes.Water_Drizzle_Water.ToName();
                } else if (resourceToGift == "Clearance Water") {
                    resourceId = GoodsTypes.Water_Clearance_Water.ToName();
                } else if (resourceToGift == "Storm Water") {
                    resourceId = GoodsTypes.Water_Storm_Water.ToName();
                } else if (Constants.ITEM_DICT.ContainsKey(resourceToGift)) {
                    resourceId = Constants.ITEM_DICT[resourceToGift].ToName();
                } else {
                    yield return new Value($"Could not understand resource \"{resourceToGift}\".");
                    yield break;
                }
            } else {
                resourceId = resourceToGift;
            }

            if(!giftingService.CanGiftToPlayer(player, giftTagDictionary[giftNames[resourceId]].Select(trait => trait.Trait))) {
                yield return new Value($"Cannot gift to {player}. Maybe their giftbox isn't open or their game can't accept {resourceToGift}.");
                yield break;
            }


            if(GameMB.StorageService.GetAmount(resourceId) < quantity) {
                yield return new Value($"Insufficient resources in warehouse to send {quantity} of \"{resourceToGift}\".");
                yield break;
            }

            giftingService.SendGift(new GiftItem(giftNames[resourceId], quantity, 0), giftTagDictionary[giftNames[resourceId]], player);
            GameMB.StorageService.Remove(new Good(resourceId, quantity), StorageOperationType.Other);
            yield return new Value("Gift sent!");
        }
    }
}
