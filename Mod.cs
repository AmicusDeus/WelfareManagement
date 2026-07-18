using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;

namespace WelfareManagement
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(WelfareManagement)}.{nameof(Mod)}").SetShowsErrorsInUI(false);

        public static Setting ActiveSetting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            // Harmony: budget display-sync (Economy>Budget flicker fix). Falls back safely to TEMP-only injection
            // (money still moves) if the patch can't install.
            HarmonyPatcher.Apply();

            // Settings + in-game options page.
            ActiveSetting = new Setting(this);
            ActiveSetting.RegisterInOptionsUI();

            // Register our strings for every language CS2 supports (English source; English fallback for all locales).
            var lm = GameManager.instance.localizationManager;
            foreach (var locale in lm.GetSupportedLocales())
                lm.AddSource(locale, new LocaleSource(ActiveSetting, locale));

            AssetDatabase.global.LoadSettings(nameof(WelfareManagement), ActiveSetting, new Setting(this));
            // Persist every settings change to disk the moment it is applied (survives a crash / non-clean exit).
            ActiveSetting.onSettingsApplied += OnSettingsApplied;

            // Systems that apply the settings in-game.
            updateSystem.UpdateAt<EconomySystem>(SystemUpdatePhase.GameSimulation);        // scale + welfare-office gate
            updateSystem.UpdateAt<BenefitCostSystem>(SystemUpdatePhase.GameSimulation);    // count recipients + cost
            updateSystem.UpdateAt<AchievementEnablerSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<BudgetInjectSystem>(SystemUpdatePhase.GameSimulation);   // fold cost into treasury
            updateSystem.UpdateAt<DiagnosticsSystem>(SystemUpdatePhase.GameSimulation);    // [SelfTest] log lines

            // UI bridge — the benefits panel + budget breakdown.
            updateSystem.UpdateAt<WelfareUISystem>(SystemUpdatePhase.UIUpdate);

            log.Info("Realistic Funding & Management: Welfare Benefits loaded.");
        }

        // Persist a settings change to disk as soon as it is applied (guard: ApplyAndSave re-raises onSettingsApplied).
        private static bool s_savingReentrant;
        private static void OnSettingsApplied(Game.Settings.Setting setting)
        {
            if (s_savingReentrant)
                return;
            s_savingReentrant = true;
            try { ActiveSetting?.ApplyAndSave(); }
            finally { s_savingReentrant = false; }
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));

            HarmonyPatcher.Remove();

            if (ActiveSetting != null)
            {
                ActiveSetting.onSettingsApplied -= OnSettingsApplied;
                ActiveSetting.UnregisterInOptionsUI();
                ActiveSetting = null;
            }
        }
    }
}
