using Eremite.Buildings;
using Eremite.Model.Effects;
using Eremite.Services;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Ryguy9999.ATS.ATSForAP {
    public class APItemReceivedHook : HookLogic {
        public override HookLogicType Type => (HookLogicType)Constants.CustomHookType.APItemReceived;
        public string itemName;
        public int amount = 1;

        public override bool CanBeDrawn() {
            return false;
        }

        public override string GetAmountText() {
            return "1";
        }

        public override int GetIntAmount() {
            return 1;
        }

        public override bool HasImpactOn(BuildingModel building) {
            return Serviceable.RecipesService.IsBuildingProducing(building.Name, itemName);
        }
    }
}