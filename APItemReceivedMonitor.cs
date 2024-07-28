using Eremite.Buildings;
using Eremite.Controller.Effects;
using Eremite.Model.Effects;
using System;
using System.Collections.Generic;
using System.Text;
using UniRx;

namespace Ryguy9999.ATS.ATSForAP {
    class APItemReceivedMonitor : HookMonitor<APItemReceivedHook, APItemReceivedTracker> {
        public override void AddHandle(APItemReceivedTracker tracker) {
            tracker.handle.Add(ArchipelagoService.OnAPItemReceived(tracker.model.itemName).Subscribe(new Action<int>(tracker.Update)));
        }

        public override APItemReceivedTracker CreateTracker(HookState state, APItemReceivedHook model, HookedEffectModel effectModel, HookedEffectState effectState) {
            return new APItemReceivedTracker(state, model, effectModel, effectState);
        }

        public override void InitValue(APItemReceivedTracker tracker) {
            tracker.SetAmount(this.GetInitValueFor(tracker.model));
        }
    }
}