using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using ATS_API.Effects;
using ATS_API.Helpers;
using Eremite;
using Eremite.Buildings;
using Eremite.Model;
using Eremite.Model.Effects;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Ryguy9999.ATS.ATSForAP {
    class ArchipelagoEffectModel : EffectModel {
		public override bool IsPerk {
			get {
				return true;
			}
		}

		public override bool IsPositive {
			get {
				return false;
			}
		}

		public override string FormatedDescription {
			get {
				return base.TryFormat(this.description.Text, "<sprite name=\"" + GoodsTypes.Mat_Raw_Plant_Fibre.ToName().ToLowerInvariant() + "\"> " + this.displayName.Text, this.GetRawAmountText());
			}
		}

		public override string GetDescriptionInfo {
			get {
				return "{0} - good name, {1} - amount";
			}
		}

		public override void OnApply(EffectContextType contextType, string contextModel, int contextId) {
		}

		public override void OnRemove(EffectContextType contextType, string contextModel, int contextId) {
			Plugin.Log("Cannot remove Archipelago Effect from run once added! How did we even get here?");
		}

		public override string GetAmountText() {
			return null;
		}

		public override string GetRawAmountText() {
			return null;
		}

		public override int GetIntAmount() {
			return 0;
		}

		public override Sprite GetDefaultIcon() {
			return TextureHelper.GetImageAsSprite("ap-icon.png", TextureHelper.SpriteType.EffectIcon);
		}

		public override Color GetTypeColor() {
			return SO.Settings.RewardColorCommonNegative;
		}

		public override bool HasImpactOn(BuildingModel building) {
			foreach (KeyValuePair<string, GoodsTypes> pair in Constants.ITEM_DICT) {
				if (SO.RecipesService.IsBuildingProducing(building.Name, pair.Value.ToName())) {
					return true;
                }
			}
			return false;
		}
	}
}
