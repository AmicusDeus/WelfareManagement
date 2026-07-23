using Game;
using Game.Common;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace WelfareManagement
{
    // Scales the citizen-benefit fields (pension, unemployment, family allowance) of EconomyParameterData by the
    // configured percentages. Captures the vanilla values once so 100% always means "unchanged". Wages, minimum
    // earnings and starting money are left untouched.
    //
    // WELFARE-OFFICE GATE: treasury funding (BenefitsFundedByTreasury) is only ADMINISTERED once the city has a welfare
    // office. With funding on but NO office, the mod does NOT zero benefits (v1.21 and earlier did — that starved
    // non-working households of income and could DEADLOCK a new city: only elderly can move in, so it never grows enough
    // to build the office that would un-gate it). Instead this state falls back to base-game behaviour: benefits are
    // still paid at the chosen levels but MINTED FREE (not charged to the treasury) — BenefitCostSystem bills the
    // treasury nothing while gated (it reads BenefitsGatedOff). Building a welfare office switches funding over to the
    // treasury; a settings warning (Setting.NeedsWelfareOffice) prompts for one. Funding OFF = pure vanilla. Two
    // office-detection layers: the runtime WelfareOffice component, plus a prefab-data scan (WelfareOfficeData).
    public partial class EconomySystem : GameSystemBase
    {
        private EntityQuery m_Query;
        private EntityQuery m_WelfareQuery;
        private EntityQuery m_WelfareScanQuery;
        private bool m_Captured;
        private int m_Pension, m_Unemployment, m_Family;
        private int m_Tick;

        // For diagnostics: number of welfare offices, and whether benefits are currently gated (funding on, no office).
        public int WelfareOfficeCount { get; private set; }
        public bool BenefitsGatedOff { get; private set; }

        // Live welfare-office count, exposed statically so the Options-page warning (Setting.NeedsWelfareOffice) can read
        // it without a system reference. Refreshed on the same cadence as WelfareOfficeCount. -1 = "unknown" (no city
        // simulating yet, e.g. the main menu) so the warning stays hidden until a real count is known.
        public static int LiveWelfareOfficeCount { get; private set; } = -1;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Query = GetEntityQuery(ComponentType.ReadWrite<EconomyParameterData>());
            m_WelfareQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<Game.Buildings.WelfareOffice>() },
                None = new[]
                {
                    ComponentType.ReadOnly<Game.Objects.OutsideConnection>(),
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Game.Tools.Temp>(),
                },
            });
            m_WelfareScanQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<Game.Buildings.Building>(), ComponentType.ReadOnly<PrefabRef>() },
                None = new[]
                {
                    ComponentType.ReadOnly<Game.Objects.OutsideConnection>(),
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Game.Tools.Temp>(),
                },
            });
            RequireForUpdate(m_Query);
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 64;

        // Back to the main menu / no city simulating: reset the static count to "unknown" so the Options-page warning
        // doesn't read a previous city's value (RequireForUpdate(m_Query) stops this system when EconomyParameterData
        // goes away on unload).
        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            LiveWelfareOfficeCount = -1;
        }

        // Runtime-component count first (cheap); prefab-data scan only when it finds nothing.
        private void RefreshWelfareOffices()
        {
            int count = m_WelfareQuery.CalculateEntityCount();
            if (count == 0 && !m_WelfareScanQuery.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> blds = m_WelfareScanQuery.ToEntityArray(Allocator.Temp);
                for (int i = 0; i < blds.Length; i++)
                {
                    Entity bld = blds[i];
                    // (1) Standalone welfare-office building: its own prefab carries WelfareOfficeData.
                    Entity prefab = EntityManager.GetComponentData<PrefabRef>(bld).m_Prefab;
                    if (prefab != Entity.Null && EntityManager.HasComponent<WelfareOfficeData>(prefab))
                    {
                        count++;
                        continue;
                    }
                    // (2) Welfare office as an INSTALLED UPGRADE (e.g. a City Hall welfare wing): the upgrade
                    // sub-entity's prefab carries WelfareOfficeData.
                    if (EntityManager.HasBuffer<Game.Buildings.InstalledUpgrade>(bld))
                    {
                        DynamicBuffer<Game.Buildings.InstalledUpgrade> ups =
                            EntityManager.GetBuffer<Game.Buildings.InstalledUpgrade>(bld, isReadOnly: true);
                        for (int u = 0; u < ups.Length; u++)
                        {
                            Entity up = ups[u].m_Upgrade;
                            if (up == Entity.Null || !EntityManager.Exists(up) || !EntityManager.HasComponent<PrefabRef>(up))
                                continue;
                            Entity upPrefab = EntityManager.GetComponentData<PrefabRef>(up).m_Prefab;
                            if (upPrefab != Entity.Null && EntityManager.HasComponent<WelfareOfficeData>(upPrefab))
                            {
                                count++;
                                break; // one welfare upgrade on this building is enough
                            }
                        }
                    }
                }
                blds.Dispose();
            }
            WelfareOfficeCount = count;
            LiveWelfareOfficeCount = count;
        }

        protected override void OnUpdate()
        {
            Setting s = Mod.ActiveSetting;
            if (s == null)
                return;

            // The scan is heavier than the rest of this tick, so refresh every 8th update (~ once per in-game hour).
            if (m_Tick++ % 8 == 0)
                RefreshWelfareOffices();

            Entity entity = m_Query.GetSingletonEntity();
            EconomyParameterData d = EntityManager.GetComponentData<EconomyParameterData>(entity);

            if (!m_Captured)
            {
                m_Captured = true;
                m_Pension = d.m_Pension;
                m_Unemployment = d.m_UnemploymentBenefit;
                m_Family = d.m_FamilyAllowance;
            }

            // Treasury funding on but no welfare office to administer it. Do NOT zero benefits (that collapses
            // non-working immigration and can deadlock a new city) — leave the amounts at the chosen levels and let
            // BenefitCostSystem bill the treasury nothing while gated, i.e. fall back to base-game free minting. A
            // welfare office switches funding over to the treasury.
            BenefitsGatedOff = s.BenefitsFundedByTreasury && WelfareOfficeCount == 0;

            d.m_Pension = Scale(m_Pension, SanePct(s.PensionPercent));
            d.m_UnemploymentBenefit = Scale(m_Unemployment, SanePct(s.UnemploymentBenefitPercent));
            d.m_FamilyAllowance = Scale(m_Family, SanePct(s.FamilyAllowancePercent));

            EntityManager.SetComponentData(entity, d);
        }

        // Guard against a corrupt / NaN / out-of-range percent (e.g. from a mis-measured UI slider) ever scaling a
        // benefit into integer overflow — which would mint garbage money to households. Clamp to the slider range.
        private static float SanePct(float p) => (float.IsNaN(p) || float.IsInfinity(p)) ? 100f : math.clamp(p, 0f, 200f);

        private static int Scale(int original, float percent) => (int)math.round(original * (percent / 100f));
    }
}
