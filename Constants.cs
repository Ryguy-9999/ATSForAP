using ATS_API.Helpers;
using System.Collections.Generic;

namespace Ryguy9999.ATS.ATSForAP {
    class Constants {
        public static Dictionary<string, GoodsTypes> ITEM_DICT = new Dictionary<string, GoodsTypes> {
            ["Berries"] = GoodsTypes.Food_Raw_Berries,
            ["Eggs"] = GoodsTypes.Food_Raw_Eggs,
            ["Insects"] = GoodsTypes.Food_Raw_Insects,
            ["Meat"] = GoodsTypes.Food_Raw_Meat,
            ["Mushrooms"] = GoodsTypes.Food_Raw_Mushrooms,
            ["Roots"] = GoodsTypes.Food_Raw_Roots,
            ["Vegetables"] = GoodsTypes.Food_Raw_Vegetables,
            ["Fish"] = GoodsTypes.Food_Raw_Fish,
            ["Biscuits"] = GoodsTypes.Food_Processed_Biscuits,
            ["Jerky"] = GoodsTypes.Food_Processed_Jerky,
            ["Pickled Goods"] = GoodsTypes.Food_Processed_Pickled_Goods,
            ["Pie"] = GoodsTypes.Food_Processed_Pie,
            ["Porridge"] = GoodsTypes.Food_Processed_Porridge,
            ["Skewers"] = GoodsTypes.Food_Processed_Skewers,
            ["Paste"] = GoodsTypes.Food_Processed_Paste,
            ["Coats"] = GoodsTypes.Needs_Coats,
            ["Boots"] = GoodsTypes.Needs_Boots,
            ["Bricks"] = GoodsTypes.Mat_Processed_Bricks,
            ["Fabric"] = GoodsTypes.Mat_Processed_Fabric,
            ["Planks"] = GoodsTypes.Mat_Processed_Planks,
            ["Pipes"] = GoodsTypes.Mat_Processed_Pipe,
            ["Parts"] = GoodsTypes.Mat_Processed_Parts,
            ["Wildfire Essence"] = GoodsTypes.Hearth_Parts,
            ["Ale"] = GoodsTypes.Needs_Ale,
            ["Incense"] = GoodsTypes.Needs_Incense,
            ["Scrolls"] = GoodsTypes.Needs_Scrolls,
            ["Tea"] = GoodsTypes.Needs_Tea,
            ["Training Gear"] = GoodsTypes.Needs_Training_Gear,
            ["Wine"] = GoodsTypes.Needs_Wine,
            ["Clay"] = GoodsTypes.Mat_Raw_Clay,
            ["Copper Ore"] = GoodsTypes.Metal_Copper_Ore,
            ["Scales"] = GoodsTypes.Mat_Raw_Scales,
            ["Crystallized Dew"] = GoodsTypes.Metal_Crystalized_Dew,
            ["Grain"] = GoodsTypes.Food_Raw_Grain,
            ["Herbs"] = GoodsTypes.Food_Raw_Herbs,
            ["Leather"] = GoodsTypes.Mat_Raw_Leather,
            ["Plant Fiber"] = GoodsTypes.Mat_Raw_Plant_Fibre,
            ["Algae"] = GoodsTypes.Mat_Raw_Algae,
            ["Reeds"] = GoodsTypes.Mat_Raw_Reeds,
            ["Resin"] = GoodsTypes.Mat_Raw_Resin,
            ["Stone"] = GoodsTypes.Mat_Raw_Stone,
            ["Salt"] = GoodsTypes.Crafting_Salt,
            ["Barrels"] = GoodsTypes.Vessel_Barrels,
            ["Copper Bars"] = GoodsTypes.Metal_Copper_Bar,
            ["Flour"] = GoodsTypes.Crafting_Flour,
            ["Dye"] = GoodsTypes.Crafting_Dye,
            ["Pottery"] = GoodsTypes.Vessel_Pottery,
            ["Waterskins"] = GoodsTypes.Vessel_Waterskin,
            ["Amber"] = GoodsTypes.Valuable_Amber,
            ["Pack of Building Materials"] = GoodsTypes.Packs_Pack_Of_Building_Materials,
            ["Pack of Crops"] = GoodsTypes.Packs_Pack_Of_Crops,
            ["Pack of Luxury Goods"] = GoodsTypes.Packs_Pack_Of_Luxury_Goods,
            ["Pack of Provisions"] = GoodsTypes.Packs_Pack_Of_Provisions,
            ["Pack of Trade Goods"] = GoodsTypes.Packs_Pack_Of_Trade_Goods,
            ["Ancient Tablet"] = GoodsTypes.Valuable_Ancient_Tablet,
            ["Coal"] = GoodsTypes.Crafting_Coal,
            ["Oil"] = GoodsTypes.Crafting_Oil,
            ["Purging Fire"] = GoodsTypes.Blight_Fuel,
            ["Sea Marrow"] = GoodsTypes.Crafting_Sea_Marrow,
            ["Tools"] = GoodsTypes.Tools_Simple_Tools,
        };
        public static Dictionary<string, List<GoodsTypes>> PROGRESSIVE_GOODS = new Dictionary<string, List<GoodsTypes>> {
            ["Progressive Complex Food"] = new List<GoodsTypes> {
              GoodsTypes.Food_Processed_Porridge,
              GoodsTypes.Food_Processed_Jerky,
              GoodsTypes.Food_Processed_Pie,
              GoodsTypes.Food_Processed_Skewers,
              GoodsTypes.Food_Processed_Paste,
              GoodsTypes.Food_Processed_Pickled_Goods,
              GoodsTypes.Food_Processed_Biscuits,
            },
        };
        public const int PRODUCTIVITY_MODIFIER = 999999;
        public const int TRADE_TOWN_ID = 9999;
        public const string SCOUT_STRING_PREFIX = "[AP Scout]";
        public const string AP_PROD_EFFECT_NAME = "_AP_ProductionBlocker";
        public const string AP_PROD_EFFECT_CONTROL_NAME = "_AP_ProductionControl";
        public const string AP_GAME_NAME = "Against the Storm";
        public const long RECIPE_SHUFFLE_VANILLA = 0;
        public const long RECIPE_SHUFFLE_EXCLUDE_CRUDE_WS_AND_MS_POST = 1;
        public const long RECIPE_SHUFFLE_EXCLUDE_CRUDE_WS = 2;
        public const long RECIPE_SHUFFLE_EXCLUDE_MS_POST = 3;
        public const long RECIPE_SHUFFLE_FULL_SHUFFLE = 4;
        public const long DEATHLINK_OFF = 0;
        public const long DEATHLINK_DEATH_ONLY = 1;
        public const long DEATHLINK_LEAVE_AND_DEATH = 2;
        public const string DEATHLINK_REASON = "AP Deathlink";

        public enum CustomHookType {
            APItemReceived = 9999
        }
    }
}
