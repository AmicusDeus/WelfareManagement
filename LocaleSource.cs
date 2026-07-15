using System.Collections.Generic;
using Colossal;

namespace WelfareManagement
{
    // One instance per CS2 locale. Provides the Options-page entries (ids generated from the Setting). Strings come
    // from Translations (English only at launch; falls back to English for any locale/key that isn't translated).
    public class LocaleSource : IDictionarySource
    {
        private readonly Setting m_S;
        private readonly string m_Locale;

        public LocaleSource(Setting setting, string locale)
        {
            m_S = setting;
            m_Locale = locale;
        }

        private string T(string key) => Translations.Get(key, m_Locale);

        private void Opt(Dictionary<string, string> d, string prop, string key)
        {
            d[m_S.GetOptionLabelLocaleID(prop)] = T(key + ".L");
            d[m_S.GetOptionDescLocaleID(prop)] = T(key + ".D");
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            var s = m_S;
            var d = new Dictionary<string, string>
            {
                { s.GetSettingsLocaleID(), T("mod.name") },
                { s.GetOptionTabLocaleID(Setting.Section), T("tab.main") },
                { s.GetOptionGroupLocaleID(Setting.GroupBenefits), T("group.benefits") },
                { s.GetOptionGroupLocaleID(Setting.GroupGeneral), T("group.general") },

                // Label for the mod's folded-in benefit cost in the vanilla budget DETAIL breakdown (hover Subsidies).
                { "EconomyPanel.BUDGET_SUB_ITEM[WMCitizenBenefits]", "Citizen Benefits" },
            };

            Opt(d, nameof(Setting.BenefitsFundedByTreasury), "benefitsFunded");
            Opt(d, nameof(Setting.PensionPercent), "pension");
            Opt(d, nameof(Setting.UnemploymentBenefitPercent), "unemployment");
            Opt(d, nameof(Setting.FamilyAllowancePercent), "family");
            Opt(d, nameof(Setting.EnableAchievements), "achievements");

            return d;
        }

        public void Unload() { }
    }
}
