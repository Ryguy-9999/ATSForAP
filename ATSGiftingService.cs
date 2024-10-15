using Archipelago.Gifting.Net.Gifts.Versions.Current;
using Archipelago.Gifting.Net.Service;
using Archipelago.Gifting.Net.Traits;
using Archipelago.Gifting.Net.Utilities.CloseTraitParser;
using Archipelago.MultiClient.Net;
using QFSW.QC;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ryguy9999.ATS.ATSForAP {
    class ATSGiftingService {
        static ICloseTraitParser<string> giftTraitParser = new BKTreeCloseTraitParser<string>();
        static GiftingService giftingService;

        public static void InitializeGifting(ArchipelagoSession session) {
            giftingService = new GiftingService(session);
            giftingService.OpenGiftBox(); // TODO figure out list of tags
            giftingService.SubscribeToNewGifts(new Action<Dictionary<string, Gift>>(HandleNewGifts));
            HandleNewGifts(giftingService.GetAllGiftsAndEmptyGiftbox());

            giftTraitParser.RegisterAvailableGift("Wood", new GiftTrait[] { new GiftTrait("Wood", 1, 1), new GiftTrait("Material", 1, 1) });
            giftTraitParser.RegisterAvailableGift("Berry", new GiftTrait[] { new GiftTrait("Berry", 1, 1), new GiftTrait("Fruit", 1, 1), new GiftTrait("Food", 1, 1) });
            giftTraitParser.RegisterAvailableGift("Egg", new GiftTrait[] { new GiftTrait("Egg", 1, 1), new GiftTrait("Food", 1, 1) });
            giftTraitParser.RegisterAvailableGift("Insect", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Meat", new GiftTrait[] { new GiftTrait("Meat", 1, 1), new GiftTrait("Food", 1, 1) });
            giftTraitParser.RegisterAvailableGift("Mushroom", new GiftTrait[] { new GiftTrait("Mushroom", 1, 1), new GiftTrait("Food", 1, 1) });
            giftTraitParser.RegisterAvailableGift("Root", new GiftTrait[] { }); // TODO Ginger?
            giftTraitParser.RegisterAvailableGift("Vegetable", new GiftTrait[] { new GiftTrait("Vegetable", 1, 1), new GiftTrait("Food", 1, 1) });
            giftTraitParser.RegisterAvailableGift("Biscuit", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Jerky", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Pickled Good", new GiftTrait[] { new GiftTrait("Pickle", 1, 1), new GiftTrait("Food", 1, 2) });
            giftTraitParser.RegisterAvailableGift("Pie", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Porridge", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Skewer", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Coat", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Brick", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Fabric", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Plank", new GiftTrait[] { new GiftTrait("Lumber", 1, 1), new GiftTrait("Wood", 1, 2), new GiftTrait("Material", 1, 2) });
            giftTraitParser.RegisterAvailableGift("Pipe", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Part", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Wildfire Essence", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Ale", new GiftTrait[] { }); // TODO Alcohol?
            giftTraitParser.RegisterAvailableGift("Incense", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Scroll", new GiftTrait[] { new GiftTrait("Scroll", 1, 1) });
            giftTraitParser.RegisterAvailableGift("Tea", new GiftTrait[] { }); // TODO Coffee?
            giftTraitParser.RegisterAvailableGift("Training Gear", new GiftTrait[] { }); // TODO Weapon?
            giftTraitParser.RegisterAvailableGift("Wine", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Clay", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Copper Ore", new GiftTrait[] { new GiftTrait("Ore", 1, 1), new GiftTrait("Copper", 1, 0.5), new GiftTrait("Metal", 1, 0.5), new GiftTrait("Material", 1, 1) });
            giftTraitParser.RegisterAvailableGift("Crystallized Dew", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Grain", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Herb", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Leather", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Plant Fiber", new GiftTrait[] { new GiftTrait("Fiber", 1, 1), new GiftTrait("Material", 1, 1) });
            giftTraitParser.RegisterAvailableGift("Reed", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Resin", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Stone", new GiftTrait[] { new GiftTrait("Stone", 1, 1), new GiftTrait("Material", 1, 1) });
            giftTraitParser.RegisterAvailableGift("Barrel", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Copper Bar", new GiftTrait[] { new GiftTrait("Metal", 1, 1), new GiftTrait("Copper", 1, 1), new GiftTrait("Material", 1, 1) });
            giftTraitParser.RegisterAvailableGift("Flour", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Dye", new GiftTrait[] { new GiftTrait("Dye", 1, 1), new GiftTrait("Material", 1, 1) });
            giftTraitParser.RegisterAvailableGift("Pottery", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Waterskin", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Amber", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Pack of Building Materials", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Pack of Crops", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Pack of Luxury Goods", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Pack of Provisions", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Pack of Trade Goods", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Ancient Tablet", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Coal", new GiftTrait[] { new GiftTrait("Coal", 1, 1) });
            giftTraitParser.RegisterAvailableGift("Oil", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Purging Fire", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Sea Marrow", new GiftTrait[] { }); // TODO
            giftTraitParser.RegisterAvailableGift("Tool", new GiftTrait[] { new GiftTrait("Tool", 1, 1) });
            giftTraitParser.RegisterAvailableGift("Drizzle Water", new GiftTrait[] { });
            giftTraitParser.RegisterAvailableGift("Clearance Water", new GiftTrait[] { });
            giftTraitParser.RegisterAvailableGift("Storm Water", new GiftTrait[] { });
        }

        private static void HandleNewGifts(Dictionary<string, Gift> gifts) {
            foreach (var gift in gifts) {
                HandleGift(gift.Key, gift.Value);
            }
        }

        private static void HandleGift(string giftId, Gift gift) {

        }

        [Command("ap.gift", "Sends a gift to the specified player.", Platform.AllPlatforms, MonoTargetType.Single)]
        public static void SendGift(string player, string resourceToGift, string quantity) {
        }
    }
}
