using System.Collections.Generic;
using Colossal;

namespace WelfareManagement
{
    // One instance per CS2 locale. Provides the Options-page entries (ids generated from the settings instance). Strings come
    // from Translations (English only at launch; falls back to English for any locale/key that isn't translated).
    public class LocaleSource : IDictionarySource
    {
        private readonly WelfareManagementSetting m_S;
        private readonly string m_Locale;

        public LocaleSource(WelfareManagementSetting setting, string locale)
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
                { s.GetOptionTabLocaleID(WelfareManagementSetting.Section), T("tab.main") },
                { s.GetOptionGroupLocaleID(WelfareManagementSetting.GroupBenefits), T("group.benefits") },
                { s.GetOptionGroupLocaleID(WelfareManagementSetting.GroupGeneral), T("group.general") },

                // Label for the mod's folded-in benefit cost in the vanilla budget DETAIL breakdown (hover Subsidies).
                { "EconomyPanel.BUDGET_SUB_ITEM[WMCitizenBenefits]", "Citizen Benefits" },
            };

            Opt(d, nameof(WelfareManagementSetting.Enabled), "enabled");
            Opt(d, nameof(WelfareManagementSetting.BenefitsFundedByTreasury), "benefitsFunded");
            d[s.GetOptionWarningLocaleID(nameof(WelfareManagementSetting.BenefitsFundedByTreasury))] = T("benefitsFunded.W");
            Opt(d, nameof(WelfareManagementSetting.PensionPercent), "pension");
            Opt(d, nameof(WelfareManagementSetting.UnemploymentBenefitPercent), "unemployment");
            Opt(d, nameof(WelfareManagementSetting.FamilyAllowancePercent), "family");

            d["WelfareManagement.ui.perRecipient"] = T("ui.perRecipient");
            d["WelfareManagement.ui.buttonTooltip"] = T("ui.buttonTooltip");
            d["WelfareManagement.ui.panelTitle"] = T("ui.panelTitle");
            d["WelfareManagement.ui.benefitsHeader"] = T("ui.benefitsHeader");
            d["WelfareManagement.ui.fundedOn"] = T("ui.fundedOn");
            d["WelfareManagement.ui.fundedOff"] = T("ui.fundedOff");
            d["WelfareManagement.ui.noCost"] = T("ui.noCost");
            d["WelfareManagement.ui.benefitsNote"] = T("ui.benefitsNote");
            d["WelfareManagement.ui.welfareWarning"] = T("ui.welfareWarning");
            d["WelfareManagement.ui.welfareAdmin"] = T("ui.welfareAdmin");
            d["WelfareManagement.ui.benefitsRow"] = T("ui.benefitsRow");
            d["WelfareManagement.ui.hoverBenefits"] = T("ui.hoverBenefits");

            return d;
        }

        public void Unload() { }
    }
}
