using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;

namespace WelfareManagement
{
    // NOTE: the class SIMPLE NAME must stay unique across all of this author's mods. ModSetting.ApplyAndSave() resolves
    // the target .coc via AssetDatabase.SaveSpecificSetting(GetType().Name) and breaks on the FIRST match, so two mods
    // sharing a settings class name silently write each other's file. Nothing on disk derives from this name — the
    // [FileLocation] value below and the LoadSettings(...) name in Mod.cs are the on-disk identity: neither may change.
    [FileLocation(nameof(WelfareManagement))]
    public class WelfareManagementSetting : ModSetting
    {
        public const string Section = "Main";

        public const string GroupBenefits = "Benefits";
        public const string GroupGeneral = "General";

        public WelfareManagementSetting(IMod mod) : base(mod) { }

        // NOTE: every property carries a C# initializer matching SetDefaults(). This is the failsafe for settings
        // migration — an older .coc predating a property keeps this initializer value (e.g. 100% = vanilla) instead
        // of falling back to 0, which would silently scale a benefit to zero.

        // ---- Master switch ----
        // Turn the whole mod on/off without uninstalling. OFF = exact vanilla: benefit amounts are restored to their
        // base-game values and the treasury is charged nothing; your other settings below are preserved. Default ON.
        [SettingsUISection(Section, GroupBenefits)]
        public bool Enabled { get; set; } = true;

        // ---- Citizen benefits (percent of vanilla; 100% = unchanged) ----
        // Opt-IN: OFF by default (safe = pure vanilla, the game mints benefits free). When ON, the real benefit outlay
        // is deducted from the city treasury (shown as a budget cost) — but only once a WELFARE OFFICE is present to
        // administer it. With this ON and NO welfare office, benefits are NOT charged to the treasury: they fall back to
        // the base-game default (paid, minted free) and a warning prompts you to build an office. (v1.22: this used to
        // ZERO benefits with no office, which collapsed immigration and could deadlock a new city — fixed.)
        [SettingsUISection(Section, GroupBenefits)]
        [SettingsUIWarning(typeof(WelfareManagementSetting), nameof(NeedsWelfareOffice))]
        public bool BenefitsFundedByTreasury { get; set; } = false;

        // Options-page warning condition: treasury funding is on but the city has no welfare office to administer it, so
        // funding isn't actually happening (benefits fall back to free base-game minting). Reads the live office count
        // that EconomySystem publishes. Returns true => the warning shows on the toggle.
        public bool NeedsWelfareOffice() => Enabled && BenefitsFundedByTreasury && EconomySystem.LiveWelfareOfficeCount == 0;

        [SettingsUISlider(min = 0f, max = 200f, step = 5f, unit = "percentage")]
        [SettingsUISection(Section, GroupBenefits)]
        public float PensionPercent { get; set; } = 100f;

        [SettingsUISlider(min = 0f, max = 200f, step = 5f, unit = "percentage")]
        [SettingsUISection(Section, GroupBenefits)]
        public float UnemploymentBenefitPercent { get; set; } = 100f;

        [SettingsUISlider(min = 0f, max = 200f, step = 5f, unit = "percentage")]
        [SettingsUISection(Section, GroupBenefits)]
        public float FamilyAllowancePercent { get; set; } = 100f;

        public override void SetDefaults()
        {
            Enabled = true;
            BenefitsFundedByTreasury = false;
            PensionPercent = 100f;
            UnemploymentBenefitPercent = 100f;
            FamilyAllowancePercent = 100f;
        }
    }
}
