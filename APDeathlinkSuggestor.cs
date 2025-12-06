using QFSW.QC;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ryguy9999.ATS.ATSForAP {
    public struct APDeathlinkSuggestionTag : IQcSuggestorTag {

    }

    public sealed class APDeathlinkSuggestionAttribute : SuggestorTagAttribute {
        private readonly IQcSuggestorTag[] _tags = { new APDeathlinkSuggestionTag() };

        public override IQcSuggestorTag[] GetSuggestorTags() {
            return _tags;
        }
    }

    public class APDeathlinkSuggestionSuggestor : BasicCachedQcSuggestor<string> {
        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options) {
            return context.HasTag<APDeathlinkSuggestionTag>();
        }

        protected override IQcSuggestion ItemToSuggestion(string abilityName) {
            return new RawSuggestion(abilityName, true);
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options) {
            return new string[]
            {
                "off",
                "death_only",
                "leave_and_death",
            };
        }
    }
}
