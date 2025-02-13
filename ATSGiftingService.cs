using Archipelago.Gifting.Net.Service;
using Archipelago.Gifting.Net.Traits;
using Archipelago.Gifting.Net.Utilities.CloseTraitParser;
using Archipelago.Gifting.Net.Versioning.Gifts;
using Archipelago.Gifting.Net.Versioning.Gifts.Current;
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
            ["Wood"] = new GiftTrait[] { new GiftTrait("Wood"), new GiftTrait("Material"), new GiftTrait("Fuel") },
            ["Berry"] = new GiftTrait[] { new GiftTrait("Berry"), new GiftTrait("Fruit"), new GiftTrait("Food") },
            ["Egg"] = new GiftTrait[] { new GiftTrait("Egg"), new GiftTrait("Food") },
            ["Insect"] = new GiftTrait[] { new GiftTrait("Insect"), new GiftTrait("Food") },
            ["Meat"] = new GiftTrait[] { new GiftTrait("Meat"), new GiftTrait("Food") },
            ["Mushroom"] = new GiftTrait[] { new GiftTrait("Mushroom"), new GiftTrait("Food") },
            ["Root"] = new GiftTrait[] { new GiftTrait("Root"), new GiftTrait("Food") },
            ["Vegetable"] = new GiftTrait[] { new GiftTrait("Vegetable"), new GiftTrait("Food") },
            ["Fish"] = new GiftTrait[] { new GiftTrait("Fish"), new GiftTrait("Food") },
            ["Biscuit"] = new GiftTrait[] { new GiftTrait("Cooking"), new GiftTrait("Food", 2) },
            ["Jerky"] = new GiftTrait[] { new GiftTrait("Meat"), new GiftTrait("Cooking"), new GiftTrait("Food", 2) },
            ["Pickled Good"] = new GiftTrait[] { new GiftTrait("Pickle"), new GiftTrait("Cooking"), new GiftTrait("Food", 2) },
            ["Pie"] = new GiftTrait[] { new GiftTrait("Cooking"), new GiftTrait("Food", 2) },
            ["Porridge"] = new GiftTrait[] { new GiftTrait("Cooking"), new GiftTrait("Food", 2) },
            ["Skewer"] = new GiftTrait[] { new GiftTrait("Cooking"), new GiftTrait("Food", 2) },
            ["Paste"] = new GiftTrait[] { new GiftTrait("Cooking"), new GiftTrait("Food", 2) },
            ["Coat"] = new GiftTrait[] { new GiftTrait("Cloth"), new GiftTrait("Clothing") },
            ["Boot"] = new GiftTrait[] { new GiftTrait("Leather"), new GiftTrait("Clothing") },
            ["Brick"] = new GiftTrait[] { new GiftTrait("Stone", 2), new GiftTrait("Material", 2) },
            ["Fabric"] = new GiftTrait[] { new GiftTrait("Cloth"), new GiftTrait("Material", 2) },
            ["Plank"] = new GiftTrait[] { new GiftTrait("Lumber"), new GiftTrait("Wood", 2), new GiftTrait("Material", 2) },
            ["Pipe"] = new GiftTrait[] { new GiftTrait("Pipe"), new GiftTrait("Metal") },
            ["Part"] = new GiftTrait[] { new GiftTrait("Gear", 3) },
            ["Wildfire Essence"] = new GiftTrait[] { new GiftTrait("Fire", 3) },
            ["Ale"] = new GiftTrait[] { new GiftTrait("Alcohol"), new GiftTrait("Root"), new GiftTrait("Luxury") },
            ["Incense"] = new GiftTrait[] { new GiftTrait("Luxury") },
            ["Scroll"] = new GiftTrait[] { new GiftTrait("Scroll"), new GiftTrait("Luxury") },
            ["Tea"] = new GiftTrait[] { new GiftTrait("Coffee"), new GiftTrait("Luxury") },
            ["Training Gear"] = new GiftTrait[] { new GiftTrait("Weapon"), new GiftTrait("Luxury") },
            ["Wine"] = new GiftTrait[] { new GiftTrait("Alcohol"), new GiftTrait("Fruit"), new GiftTrait("Luxury") },
            ["Clay"] = new GiftTrait[] { new GiftTrait("Clay"), new GiftTrait("Material") },
            ["Copper Ore"] = new GiftTrait[] { new GiftTrait("Ore"), new GiftTrait("Copper", 0.5), new GiftTrait("Metal", 0.5), new GiftTrait("Material") },
            ["Scale"] = new GiftTrait[] { new GiftTrait("Material"), new GiftTrait("Ore", 0.1), new GiftTrait("Copper", 0.1) },
            ["Crystallized Dew"] = new GiftTrait[] { new GiftTrait("Metal"), new GiftTrait("Material") },
            ["Grain"] = new GiftTrait[] { new GiftTrait("Grain") },
            ["Herb"] = new GiftTrait[] { new GiftTrait("Herb") },
            ["Leather"] = new GiftTrait[] { new GiftTrait("Leather"), new GiftTrait("Material") },
            ["Plant Fiber"] = new GiftTrait[] { new GiftTrait("Fiber"), new GiftTrait("Material") },
            ["Algae"] = new GiftTrait[] { new GiftTrait("Algae"), new GiftTrait("Material") },
            ["Reed"] = new GiftTrait[] { new GiftTrait("Material") },
            ["Resin"] = new GiftTrait[] { new GiftTrait("Material"), new GiftTrait("Amber", 0.1) },
            ["Stone"] = new GiftTrait[] { new GiftTrait("Stone"), new GiftTrait("Material") },
            ["Salt"] = new GiftTrait[] { new GiftTrait("Salted"), new GiftTrait("Mineral"), new GiftTrait("Material") },
            ["Barrel"] = new GiftTrait[] { new GiftTrait("Container") },
            ["Copper Bar"] = new GiftTrait[] { new GiftTrait("Metal"), new GiftTrait("Copper"), new GiftTrait("Material") },
            ["Flour"] = new GiftTrait[] { new GiftTrait("Flour") },
            ["Dye"] = new GiftTrait[] { new GiftTrait("Dye"), new GiftTrait("Material") },
            ["Pottery"] = new GiftTrait[] { new GiftTrait("Ceramic"), new GiftTrait("Container") },
            ["Waterskin"] = new GiftTrait[] { new GiftTrait("Container") },
            ["Amber"] = new GiftTrait[] { new GiftTrait("Amber"), new GiftTrait("Currency"), new GiftTrait("Gem") },
            ["Pack of Building Materials"] = new GiftTrait[] { new GiftTrait("Pack") },
            ["Pack of Crops"] = new GiftTrait[] { new GiftTrait("Pack") },
            ["Pack of Luxury Goods"] = new GiftTrait[] { new GiftTrait("Pack") },
            ["Pack of Provisions"] = new GiftTrait[] { new GiftTrait("Pack") },
            ["Pack of Trade Goods"] = new GiftTrait[] { new GiftTrait("Pack") },
            ["Ancient Tablet"] = new GiftTrait[] { new GiftTrait("Ancient"), new GiftTrait("Artifact") },
            ["Coal"] = new GiftTrait[] { new GiftTrait("Coal"), new GiftTrait("Fuel", 3) },
            ["Oil"] = new GiftTrait[] { new GiftTrait("Oil"), new GiftTrait("Fuel", 2) },
            ["Purging Fire"] = new GiftTrait[] { new GiftTrait("Fire") },
            ["Sea Marrow"] = new GiftTrait[] { new GiftTrait("Fossil"), new GiftTrait("Fuel", 3) },
            ["Tool"] = new GiftTrait[] { new GiftTrait("Tool") },
            ["Drizzle Water"] = new GiftTrait[] { new GiftTrait("Water"), new GiftTrait("Green", 0.1) },
            ["Clearance Water"] = new GiftTrait[] { new GiftTrait("Water"), new GiftTrait("Yellow", 0.1) },
            ["Storm Water"] = new GiftTrait[] { new GiftTrait("Water"), new GiftTrait("Blue", 0.1) },
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
            [GoodsTypes.Crafting_Salt.ToName()] = "Salt",
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
            giftingService.OnNewGift += HandleNewGifts;
            if (GameMB.IsGameActive) {
                HandleNewGifts(giftingService.GetAllGiftsAndEmptyGiftBox());
            }

            foreach (var item in giftTagDictionary) {
                giftTraitParser.RegisterAvailableGift(item.Key, item.Value);
            }
        }

        public static void EnterGame() {
            if(giftingService != null) {
                HandleNewGifts(giftingService.GetAllGiftsAndEmptyGiftBox());
            }
        }

        private static void HandleNewGifts(Dictionary<string, Gift> gifts) {
            if (!GameMB.IsGameActive) {
                return;
            }

            foreach (var gift in gifts) {
                HandleGift(gift.Key, gift.Value);
            }
        }

        private static void HandleNewGifts(Gift gift) {
            if (!GameMB.IsGameActive) {
                return;
            }

            HandleGift(gift);
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

        private static void HandleGift(Gift gift) {
            Plugin.Log($"Incoming AP gift: \"{gift.ItemName}\" ({gift.Amount}).");

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

            if(!giftingService.CanGiftToPlayer(player, giftTagDictionary[giftNames[resourceId]].Select(trait => trait.Trait)).CanGift) {
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
