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

            // Systems that apply the settings in-game.
            updateSystem.UpdateAt<EconomySystem>(SystemUpdatePhase.GameSimulation);        // scale + welfare-office gate
            updateSystem.UpdateAt<BenefitCostSystem>(SystemUpdatePhase.GameSimulation);    // count recipients + cost
            updateSystem.UpdateAt<AchievementEnablerSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<BudgetInjectSystem>(SystemUpdatePhase.GameSimulation);   // fold cost into treasury
            updateSystem.UpdateAt<DiagnosticsSystem>(SystemUpdatePhase.GameSimulation);    // [SelfTest] log lines

            // UI bridge — the benefits panel + budget breakdown.
            updateSystem.UpdateAt<WelfareUISystem>(SystemUpdatePhase.UIUpdate);

            log.Info("Welfare Management loaded.");
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));

            HarmonyPatcher.Remove();

            if (ActiveSetting != null)
            {
                ActiveSetting.UnregisterInOptionsUI();
                ActiveSetting = null;
            }
        }
    }
}
