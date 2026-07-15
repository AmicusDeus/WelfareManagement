using Game;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;

namespace WelfareManagement
{
    // Makes citizen benefits actually cost the city. Vanilla CS2 "mints" pensions/unemployment/family allowance
    // straight into households with no budget cost. When BenefitsFundedByTreasury is on, the real daily outlay is
    // charged as a budget EXPENSE (folded into ExpenseSource.SubsidyResidential by BudgetInjectSystem, so the native
    // BudgetApplySystem deducts it from the treasury).
    //
    // Recipient counts mirror vanilla EconomyUtils.GetHouseholdIncome EXACTLY: for each living, NON-working member of
    // a resident household — Child/Teen => family allowance, Elderly => pension, Adult => unemployment benefit but
    // ONLY while still inside the allowance window (m_UnemploymentCounter < m_UnemploymentAllowanceMaxDays * 32,
    // where 32 == PayWageSystem.kUpdatesPerDay).
    //
    // The per-citizen pass is expensive, so counts are cached and only recomputed on an in-game day boundary (or the
    // first tick after enabling). The displayed/charged COST is count * current (%-scaled) amount each tick, so it
    // still tracks the benefit sliders live between recounts. Failsafe: off => charges nothing (pure vanilla).
    public partial class BenefitCostSystem : GameSystemBase
    {
        private SimulationSystem m_Sim;
        private EntityQuery m_TimeQuery;
        private EntityQuery m_TimeSettingsQuery;
        private EntityQuery m_EconQuery;
        private EntityQuery m_HouseholdQuery;

        private int m_LastDay;
        private bool m_WasFunding;

        // Cached recipient counts (recomputed on day-change / first enable).
        private int m_PensionN;
        private int m_FamilyN;
        private int m_UnemploymentN;
        private bool m_HaveCounts;

        // MONTHLY cost figures for the UI/budget (0 when funding is off), scaled from the per-day outlay by
        // days-per-month so they match the budget panel's other (monthly) figures.
        public int PensionCost { get; private set; }
        public int UnemploymentCost { get; private set; }
        public int FamilyCost { get; private set; }
        public int TotalCost => PensionCost + UnemploymentCost + FamilyCost;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Sim = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_TimeQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
            m_TimeSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<TimeSettingsData>());
            m_EconQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            // Resident households only (exclude tourists/commuters), matching who vanilla pays benefits to.
            m_HouseholdQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<Household>(), ComponentType.ReadOnly<HouseholdCitizen>() },
                None = new[]
                {
                    ComponentType.ReadOnly<TouristHousehold>(),
                    ComponentType.ReadOnly<CommuterHousehold>(),
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Game.Tools.Temp>(),
                },
            });
        }

        // Calendar days per in-game month (year/12). The budget's monthly figures use this scale.
        private int DaysPerMonth()
        {
            if (m_TimeSettingsQuery.IsEmptyIgnoreFilter)
                return 1;
            int daysPerYear = m_TimeSettingsQuery.GetSingleton<TimeSettingsData>().m_DaysPerYear;
            int dpm = daysPerYear / 12;
            return dpm < 1 ? 1 : dpm;
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 64;

        protected override void OnUpdate()
        {
            Setting s = Mod.ActiveSetting;
            if (s == null || m_EconQuery.IsEmptyIgnoreFilter || m_TimeQuery.IsEmptyIgnoreFilter)
                return;

            bool funding = s.BenefitsFundedByTreasury;
            EconomyParameterData econ = m_EconQuery.GetSingleton<EconomyParameterData>();
            int day = TimeSystem.GetDay(m_Sim.frameIndex, m_TimeQuery.GetSingleton<TimeData>());

            if (!funding)
            {
                m_WasFunding = false;
                m_HaveCounts = false;
                m_LastDay = day; // keep baseline current so re-enabling never charges a backlog
                PensionCost = 0;
                UnemploymentCost = 0;
                FamilyCost = 0;
                return;
            }

            bool firstRun = !m_WasFunding;

            // Recompute recipient counts on first enable or a new in-game day; otherwise reuse the cache.
            if (firstRun || !m_HaveCounts || day > m_LastDay)
                RecountRecipients(econ);

            // Cost = recipients * current (%-scaled) per-citizen amount. Amounts update live with the sliders.
            int pensionDaily = m_PensionN * econ.m_Pension;
            int unempDaily = m_UnemploymentN * econ.m_UnemploymentBenefit;
            int familyDaily = m_FamilyN * econ.m_FamilyAllowance;

            int dpm = DaysPerMonth();
            PensionCost = pensionDaily * dpm;
            UnemploymentCost = unempDaily * dpm;
            FamilyCost = familyDaily * dpm;

            if (firstRun)
            {
                m_WasFunding = true; // just turned on — baseline, don't charge this run
                m_LastDay = day;
                return;
            }
            if (day <= m_LastDay)
                return;

            // The cost is applied as a real budget EXPENSE by BudgetInjectSystem (folded into SubsidyResidential so
            // the native BudgetApplySystem deducts it). We only advance the day marker so the recount above stays on
            // its once-per-day cadence.
            m_LastDay = day;
        }

        // Count benefit recipients across resident households, exactly like EconomyUtils.GetHouseholdIncome. Also
        // logs a once-per-recount economy summary to the mod log so wages / benefit recipients can be inspected
        // headlessly.
        private void RecountRecipients(EconomyParameterData econ)
        {
            int pension = 0, family = 0, unemp = 0, adultsPastWindow = 0;
            int households = 0, citizens = 0, workers = 0;
            long wageSum = 0;
            float window = econ.m_UnemploymentAllowanceMaxDays * 32f; // 32 == PayWageSystem.kUpdatesPerDay

            if (!m_HouseholdQuery.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> hhs = m_HouseholdQuery.ToEntityArray(Allocator.Temp);
                households = hhs.Length;
                for (int h = 0; h < hhs.Length; h++)
                {
                    if (!EntityManager.HasBuffer<HouseholdCitizen>(hhs[h]))
                        continue;
                    DynamicBuffer<HouseholdCitizen> members = EntityManager.GetBuffer<HouseholdCitizen>(hhs[h], isReadOnly: true);
                    for (int i = 0; i < members.Length; i++)
                    {
                        Entity c = members[i].m_Citizen;
                        if (c == Entity.Null || !EntityManager.Exists(c) || !EntityManager.HasComponent<Citizen>(c))
                            continue;
                        if (CitizenUtils.IsDead(EntityManager, c))
                            continue;
                        citizens++;
                        if (EntityManager.HasComponent<Worker>(c))
                        {
                            workers++; // employed => no benefit
                            wageSum += econ.GetWage(EntityManager.GetComponentData<Worker>(c).m_Level);
                            continue;
                        }

                        Citizen cd = EntityManager.GetComponentData<Citizen>(c);
                        CitizenAge age = cd.GetAge();
                        if (age == CitizenAge.Child || age == CitizenAge.Teen)
                            family++;
                        else if (age == CitizenAge.Elderly)
                            pension++;
                        else if ((float)cd.m_UnemploymentCounter < window) // Adult, still inside the allowance window
                            unemp++;
                        else
                            adultsPastWindow++;
                    }
                }
                hhs.Dispose();
            }

            m_PensionN = pension;
            m_FamilyN = family;
            m_UnemploymentN = unemp;
            m_HaveCounts = true;

            // Daily welfare summary for calibration.
            try
            {
                int avgWage = workers > 0 ? (int)(wageSum / workers) : 0;
                Mod.log.Info(
                    $"[EconomySummary] households={households} citizens={citizens} workers={workers} avgWage={avgWage}/mo | " +
                    $"pension: {pension} x {econ.m_Pension} | unemployment(in-window): {unemp} x {econ.m_UnemploymentBenefit} (past-window adults not billed: {adultsPastWindow}) | " +
                    $"family: {family} x {econ.m_FamilyAllowance}");
            }
            catch { /* logging must never break the sim */ }
        }
    }
}
