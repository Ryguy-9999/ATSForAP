using QFSW.QC;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ryguy9999.ATS.ATSForAP {
    public struct APUrlSuggestionTag : IQcSuggestorTag {

    }

    public sealed class APUrlSuggestionAttribute : SuggestorTagAttribute {
        private readonly IQcSuggestorTag[] _tags = { new APUrlSuggestionTag() };

        public override IQcSuggestorTag[] GetSuggestorTags() {
            return _tags;
        }
    }

    public class APUrlSuggestionSuggestor : BasicCachedQcSuggestor<string> {
        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options) {
            return context.HasTag<APUrlSuggestionTag>();
        }

        protected override IQcSuggestion ItemToSuggestion(string abilityName) {
            return new RawSuggestion(abilityName, true);
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options) {
            return new string[]
            {
                "archipelago.gg:",
            };
        }
    }
}
