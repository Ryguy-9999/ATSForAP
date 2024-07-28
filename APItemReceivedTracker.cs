using Eremite;
using Eremite.Controller.Effects;
using Eremite.Model;
using Eremite.Model.Effects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ryguy9999.ATS.ATSForAP {
    class APItemReceivedTracker : HookTracker<APItemReceivedHook> {
        public APItemReceivedTracker(HookState hookState, APItemReceivedHook model, HookedEffectModel effectModel, HookedEffectState effectState)
            : base(hookState, model, effectModel, effectState) {
        }

        public void SetAmount(int amount) {
            this.Update(amount - this.hookState.totalAmount);
        }

        public void Update(int amount) {
            this.hookState.totalAmount += amount;
            this.hookState.currentAmount += amount;
            while (this.hookState.currentAmount >= this.model.amount) {
                base.Fire();
                this.hookState.currentAmount -= this.model.amount;
            }
        }
    }
}