using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;

namespace WelfareManagement
{
    [FileLocation(nameof(WelfareManagement))]
    public class Setting : ModSetting
    {
        public const string Section = "Main";

        public const string GroupBenefits = "Benefits";
        public const string GroupGeneral = "General";

        public Setting(IMod mod) : base(mod) { }

        // NOTE: every property carries a C# initializer matching SetDefaults(). This is the failsafe for settings
        // migration — an older .coc predating a property keeps this initializer value (e.g. 100% = vanilla) instead
        // of falling back to 0, which would silently scale a benefit to zero.

        // ---- Citizen benefits (percent of vanilla; 100% = unchanged) ----
        // Opt-IN: OFF by default (safe = pure vanilla, the game mints benefits free). When ON, the real benefit outlay
        // is deducted from the city treasury (shown as a budget cost) — but only once a WELFARE OFFICE is present to
        // administer it. With this ON and NO welfare office, benefits are NOT charged to the treasury: they fall back to
        // the base-game default (paid, minted free) and a warning prompts you to build an office. (v1.22: this used to
        // ZERO benefits with no office, which collapsed immigration and could deadlock a new city — fixed.)
        [SettingsUISection(Section, GroupBenefits)]
        [SettingsUIWarning(typeof(Setting), nameof(NeedsWelfareOffice))]
        public bool BenefitsFundedByTreasury { get; set; } = false;

        // Options-page warning condition: treasury funding is on but the city has no welfare office to administer it, so
        // funding isn't actually happening (benefits fall back to free base-game minting). Reads the live office count
        // that EconomySystem publishes. Returns true => the warning shows on the toggle.
        public bool NeedsWelfareOffice() => BenefitsFundedByTreasury && EconomySystem.LiveWelfareOfficeCount == 0;

        [SettingsUISlider(min = 0f, max = 200f, step = 5f, unit = "percentage")]
        [SettingsUISection(Section, GroupBenefits)]
        public float PensionPercent { get; set; } = 100f;

        [SettingsUISlider(min = 0f, max = 200f, step = 5f, unit = "percentage")]
        [SettingsUISection(Section, GroupBenefits)]
        public float UnemploymentBenefitPercent { get; set; } = 100f;

        [SettingsUISlider(min = 0f, max = 200f, step = 5f, unit = "percentage")]
        [SettingsUISection(Section, GroupBenefits)]
        public float FamilyAllowancePercent { get; set; } = 100f;

        // ---- Achievements ----
        // The game disables achievements whenever any mod is active; this re-enables them (user choice). Idempotent,
        // so it is safe to have this on in more than one of the split mods at once.
        [SettingsUISection(Section, GroupGeneral)]
        public bool EnableAchievements { get; set; } = true;

        public override void SetDefaults()
        {
            BenefitsFundedByTreasury = false;
            PensionPercent = 100f;
            UnemploymentBenefitPercent = 100f;
            FamilyAllowancePercent = 100f;
            EnableAchievements = true;
        }
    }
}
