using System.Collections.Generic;

namespace WelfareManagement
{
    // English strings for the Options page. Key convention: "<name>.L" = label, "<name>.D" = description.
    // English only at launch; Get() falls back to English (the key's value) for every locale.
    public static class Translations
    {
        public static string Get(string key, string locale) => En.TryGetValue(key, out var v) ? v : key;

        public static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            { "mod.name", "Realistic Funding & Management: Welfare Benefits" },
            { "tab.main", "Main" },
            { "group.benefits", "Citizen benefits" },
            { "group.general", "General" },

            { "benefitsFunded.L", "Fund benefits from the treasury" },
            { "benefitsFunded.D", "Vanilla mints pensions, unemployment and family allowance for free. When on, the real daily outlay is deducted from the city treasury and shown as a budget expense (Subsidies) — but only while a welfare office is present to administer it. With this ON and NO welfare office, benefits are NOT charged to the treasury: they fall back to the base-game default (still paid, minted free), so your city keeps growing normally. Build a welfare office to actually fund benefits from the treasury. Off = vanilla behaviour." },
            { "benefitsFunded.W", "No welfare office in your city yet, so benefits aren't being funded from the treasury — they're paid by the base game (free) instead. Build a welfare office to fund them from the treasury." },

            { "pension.L", "Pension amount" },
            { "pension.D", "Scales the pension paid to each elderly non-worker. 100% = vanilla." },

            { "unemployment.L", "Unemployment benefit amount" },
            { "unemployment.D", "Scales the unemployment benefit paid to each out-of-work adult (only while inside the allowance window). 100% = vanilla." },

            { "family.L", "Family allowance amount" },
            { "family.D", "Scales the family allowance paid for each child or teen. 100% = vanilla." },

            { "achievements.L", "Keep achievements enabled" },
            { "achievements.D", "The game disables achievements while any mod is active; this re-enables them. Safe to leave on even if you run more than one of the split mods." },
        };
    }
}
