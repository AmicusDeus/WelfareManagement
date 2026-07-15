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
    // WELFARE-OFFICE GATE (user-designed): while the mod owns benefits (BenefitsFundedByTreasury on), benefits are
    // only paid if at least one welfare office exists in the city — no office => the amounts are zeroed, so citizens
    // genuinely receive nothing (no vanilla minting either) and the treasury pays nothing. Funding OFF = pure
    // vanilla, no gating. Two detection layers: the runtime WelfareOffice component, plus a prefab-data scan
    // (WelfareOfficeData) in case the runtime archetype differs.
    public partial class EconomySystem : GameSystemBase
    {
        private EntityQuery m_Query;
        private EntityQuery m_WelfareQuery;
        private EntityQuery m_WelfareScanQuery;
        private bool m_Captured;
        private int m_Pension, m_Unemployment, m_Family;
        private int m_Tick;

        // For diagnostics: number of welfare offices, and whether benefits are currently gated OFF by it.
        public int WelfareOfficeCount { get; private set; }
        public bool BenefitsGatedOff { get; private set; }

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

            // Gate: with treasury funding on and no welfare office, nobody administers benefits => nothing is paid.
            BenefitsGatedOff = s.BenefitsFundedByTreasury && WelfareOfficeCount == 0;
            float gate = BenefitsGatedOff ? 0f : 1f;

            d.m_Pension = Scale(m_Pension, SanePct(s.PensionPercent) * gate);
            d.m_UnemploymentBenefit = Scale(m_Unemployment, SanePct(s.UnemploymentBenefitPercent) * gate);
            d.m_FamilyAllowance = Scale(m_Family, SanePct(s.FamilyAllowancePercent) * gate);

            EntityManager.SetComponentData(entity, d);
        }

        // Guard against a corrupt / NaN / out-of-range percent (e.g. from a mis-measured UI slider) ever scaling a
        // benefit into integer overflow — which would mint garbage money to households. Clamp to the slider range.
        private static float SanePct(float p) => (float.IsNaN(p) || float.IsInfinity(p)) ? 100f : math.clamp(p, 0f, 200f);

        private static int Scale(int original, float percent) => (int)math.round(original * (percent / 100f));
    }
}
