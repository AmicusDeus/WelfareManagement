using System;
using Game;
using Game.Common;
using Game.Simulation;
using Unity.Entities;

namespace WelfareManagement
{
    // Self-test telemetry: once per in-game day (and once right after load), dumps structured [SelfTest] lines with
    // the current settings and benefit figures to WelfareManagement.Mod.log so behaviour can be verified by reading
    // the log — no screenshots needed. Read-only: never mutates game state.
    public partial class DiagnosticsSystem : GameSystemBase
    {
        private SimulationSystem m_Sim;
        private BenefitCostSystem m_Benefits;
        private EconomySystem m_Econ;
        private EntityQuery m_TimeQuery;
        private int m_LastDay = int.MinValue;
        private bool m_Primed;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Sim = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_Benefits = World.GetOrCreateSystemManaged<BenefitCostSystem>();
            m_Econ = World.GetOrCreateSystemManaged<EconomySystem>();
            m_TimeQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 256;

        protected override void OnUpdate()
        {
            if (m_TimeQuery.IsEmptyIgnoreFilter)
                return;
            int day = TimeSystem.GetDay(m_Sim.frameIndex, m_TimeQuery.GetSingleton<TimeData>());
            if (!m_Primed)
            {
                // Skip the very first tick after load: the other systems may not have run yet.
                m_Primed = true;
                m_LastDay = day - 1;
                return;
            }
            if (day == m_LastDay)
                return;
            m_LastDay = day;
            try { Dump(day); }
            catch (Exception ex) { Mod.log.Warn($"[SelfTest] dump failed: {ex.Message}"); }
        }

        private void Dump(int day)
        {
            Setting s = Mod.ActiveSetting;
            if (s == null)
                return;
            Mod.log.Info($"[SelfTest] day={day} settings: BenefitsFunded={s.BenefitsFundedByTreasury} " +
                         $"pension%={s.PensionPercent} unemployment%={s.UnemploymentBenefitPercent} family%={s.FamilyAllowancePercent} " +
                         $"achievements={s.EnableAchievements} budgetSync={(HarmonyPatcher.BudgetDisplaySyncActive ? "ACTIVE" : "FALLBACK")}");
            Mod.log.Info($"[SelfTest] benefits: funded={s.BenefitsFundedByTreasury} welfareOffices={m_Econ.WelfareOfficeCount} gatedOff={m_Econ.BenefitsGatedOff} " +
                         $"pension={m_Benefits.PensionCost} unemployment={m_Benefits.UnemploymentCost} family={m_Benefits.FamilyCost} total={m_Benefits.TotalCost}");
        }
    }
}
