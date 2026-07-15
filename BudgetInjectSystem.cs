using Game;
using Game.City;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace WelfareManagement
{
    // Folds the mod's benefit cost into the GAME'S OWN treasury math (a real EXPENSE), instead of poking PlayerMoney
    // out of band (which BudgetApplySystem's continuous recompute swamps, so the money never moved).
    //
    // Vanilla flow: CityServiceBudgetSystem re-zeros+fills m_ExpensesTemp every sim tick; BudgetApplySystem then does
    // PlayerMoney += (Σincome − Σexpenses) / 1024. GetExpenseArray hands back that very array. We run BETWEEN the two
    // systems, adding our benefit figure into a real slot each tick, so the game natively deducts it.
    //
    // FALLBACK ROLE: this writes only the TEMP array (BudgetApplySystem reads it, so the treasury moves), but the UI
    // reads the COMMITTED array and would flicker. The Harmony display-sync postfix writes BOTH buffers atomically;
    // when it is live this system STANDS DOWN to avoid a double temp-write. It only runs if that patch failed.
    [UpdateAfter(typeof(CityServiceBudgetSystem))]
    [UpdateBefore(typeof(BudgetApplySystem))]
    public partial class BudgetInjectSystem : GameSystemBase
    {
        private CityServiceBudgetSystem m_Budget;
        private BenefitCostSystem m_Benefits;

        // Exposed for diagnostics: what we injected this tick.
        public int InjectedExpense { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Budget = World.GetOrCreateSystemManaged<CityServiceBudgetSystem>();
            m_Benefits = World.GetOrCreateSystemManaged<BenefitCostSystem>();
        }

        protected override void OnUpdate()
        {
            Setting s = Mod.ActiveSetting;
            if (s == null)
                return;

            // Benefit cost -> a real EXPENSE. Folded into SubsidyResidential (benefits are a resident subsidy, and
            // that slot is otherwise 0 in most cities so it reads cleanly in the budget's Subsidies line).
            int benefitDaily = s.BenefitsFundedByTreasury ? m_Benefits.TotalCost : 0;
            InjectedExpense = benefitDaily;

            // When the Harmony display-sync postfix is installed it owns BOTH budget buffers; writing temp here too
            // would DOUBLE the injection. Stand down — this is only the fallback that keeps the money moving.
            if (HarmonyPatcher.BudgetDisplaySyncActive)
                return;
            if (benefitDaily == 0)
                return;

            NativeArray<int> expenses = m_Budget.GetExpenseArray(out JobHandle dExp);
            // Completing the array's dependency (the recompute job) makes the main-thread write race-free.
            dExp.Complete();
            expenses[(int)ExpenseSource.SubsidyResidential] += benefitDaily;
        }
    }
}
