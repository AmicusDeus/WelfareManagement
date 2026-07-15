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
        // Opt-out: ON by default so the mod is active on install. When on, the real benefit outlay is deducted from
        // the city treasury (and shown as a budget cost). Turn OFF for vanilla (the game mints them free).
        [SettingsUISection(Section, GroupBenefits)]
        public bool BenefitsFundedByTreasury { get; set; } = true;

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
            BenefitsFundedByTreasury = true;
            PensionPercent = 100f;
            UnemploymentBenefitPercent = 100f;
            FamilyAllowancePercent = 100f;
            EnableAchievements = true;
        }
    }
}
