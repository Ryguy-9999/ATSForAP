using Eremite.Characters.Villagers;
using Eremite.Model.Sound;
using HarmonyLib;
using System;

namespace Ryguy9999.ATS.ATSForAP {
    [HarmonyPatch(typeof(Villager))]
    class VillagerPatch {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Villager.Die))]
        [HarmonyPatch(new Type[] { typeof(VillagerLossType), typeof(string), typeof(bool), typeof(float), typeof(SoundModel) })]
        private static void DiePostfix(VillagerLossType lossType, string reasonKey, bool showDying = true, float duration = 25f, SoundModel extraSound = null) {
            ArchipelagoService.HandleVillagerDeath(lossType, reasonKey);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Villager.Leave))]
        //[HarmonyPatch(new Type[] { typeof(string) })]
        private static void LeavePostfix() {
            ArchipelagoService.HandleVillagerDeath(VillagerLossType.Leave, "");
        }
    }
}
