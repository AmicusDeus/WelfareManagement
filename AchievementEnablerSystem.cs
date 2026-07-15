using Colossal.PSI.Common;
using Game;
using Unity.Entities;

namespace WelfareManagement
{
    // Re-enables platform achievements in modded sessions (the game unconditionally disables them whenever any mod
    // is in the save's usedMods list — see AchievementTriggerSystem). The vanilla check keeps re-disabling, so this
    // simply re-asserts the flag periodically while the toggle is on. Idempotent across mods.
    public partial class AchievementEnablerSystem : GameSystemBase
    {
        public override int GetUpdateInterval(SystemUpdatePhase phase) => 256;

        protected override void OnUpdate()
        {
            Setting s = Mod.ActiveSetting;
            if (s != null && s.EnableAchievements && PlatformManager.instance != null)
                PlatformManager.instance.achievementsEnabled = true;
        }
    }
}
