using System;
using Colossal.UI.Binding;
using Game.Prefabs;
using Game.UI;
using Unity.Entities;

namespace WelfareManagement
{
    // Backs the Welfare panel: a percentage per benefit (from Setting), the LIVE per-recipient value (from
    // EconomyParameterData), and the per-day treasury COST (from BenefitCostSystem), plus the "fund benefits" toggle
    // and the welfare-office gate state. Bindings (group "WMParams") consumed by UI/src/mods/welfare.tsx.
    public partial class WelfareUISystem : UISystemBase
    {
        private const string Group = "WMParams";

        private sealed class Def
        {
            public string Key;
            public Func<Setting, float> GetPct;
            public Action<Setting, float> SetPct;
            public Func<EconomyParameterData, int> GetLive;
            public Func<BenefitCostSystem, int> GetCost;
            public GetterValueBinding<float> PctBinding;
            public GetterValueBinding<int> ValBinding;
            public GetterValueBinding<int> CostBinding;
            public int LiveValue;
            public int CostValue;
        }

        private Def[] m_Defs;
        private EntityQuery m_EconQuery;
        private EntityQuery m_TimeSettingsQuery;
        private BenefitCostSystem m_CostSystem;
        private EconomySystem m_EconomySystem;
        private GetterValueBinding<bool> m_Funded;
        private GetterValueBinding<int> m_WelfareOffices;
        private GetterValueBinding<bool> m_BenefitsGated;
        private GetterValueBinding<int> m_TotalCost;
        private GetterValueBinding<int> m_HoursPerMonth;
        private int m_TotalCostValue;
        private int m_Tick;

        private static Setting S => Mod.ActiveSetting;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EconQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            m_TimeSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<TimeSettingsData>());
            m_CostSystem = World.GetOrCreateSystemManaged<BenefitCostSystem>();
            m_EconomySystem = World.GetOrCreateSystemManaged<EconomySystem>();

            m_Defs = new[]
            {
                Make("pension",      s => s.PensionPercent,             (s, v) => s.PensionPercent = v,             d => d.m_Pension,            c => c.PensionCost),
                Make("unemployment", s => s.UnemploymentBenefitPercent, (s, v) => s.UnemploymentBenefitPercent = v, d => d.m_UnemploymentBenefit, c => c.UnemploymentCost),
                Make("family",       s => s.FamilyAllowancePercent,     (s, v) => s.FamilyAllowancePercent = v,     d => d.m_FamilyAllowance,     c => c.FamilyCost),
            };

            foreach (var def in m_Defs)
            {
                Def dd = def;
                dd.PctBinding = new GetterValueBinding<float>(Group, "pct_" + dd.Key, () => S != null ? dd.GetPct(S) : 100f);
                dd.ValBinding = new GetterValueBinding<int>(Group, "val_" + dd.Key, () => dd.LiveValue);
                dd.CostBinding = new GetterValueBinding<int>(Group, "cost_" + dd.Key, () => dd.CostValue);
                AddBinding(dd.PctBinding);
                AddBinding(dd.ValBinding);
                AddBinding(dd.CostBinding);
            }

            m_Funded = new GetterValueBinding<bool>(Group, "funded", () => S != null && S.BenefitsFundedByTreasury);
            AddBinding(m_Funded);
            m_WelfareOffices = new GetterValueBinding<int>(Group, "welfareOffices", () => m_EconomySystem.WelfareOfficeCount);
            m_BenefitsGated = new GetterValueBinding<bool>(Group, "benefitsGated", () => m_EconomySystem.BenefitsGatedOff);
            AddBinding(m_WelfareOffices);
            AddBinding(m_BenefitsGated);
            m_HoursPerMonth = new GetterValueBinding<int>(Group, "hoursPerMonth", HoursPerMonth);
            AddBinding(m_HoursPerMonth);
            m_TotalCost = new GetterValueBinding<int>(Group, "cost_total", () => m_TotalCostValue);
            AddBinding(m_TotalCost);

            AddBinding(new TriggerBinding<string, float>(Group, "setPct", (key, pct) =>
            {
                if (S == null) return;
                if (float.IsNaN(pct) || float.IsInfinity(pct)) return; // reject a corrupt slider value outright
                pct = Math.Max(0f, Math.Min(200f, pct));               // clamp to the valid 0-200% range
                foreach (var def in m_Defs)
                {
                    if (def.Key == key)
                    {
                        if (def.GetPct(S) != pct) { def.SetPct(S, pct); S.ApplyAndSave(); }
                        break;
                    }
                }
            }));
            AddBinding(new TriggerBinding<bool>(Group, "setFunded", v =>
            {
                if (S != null && S.BenefitsFundedByTreasury != v) { S.BenefitsFundedByTreasury = v; S.ApplyAndSave(); }
            }));

            if (S != null)
                S.onSettingsApplied += OnSettingsApplied;
        }

        private static Def Make(string key, Func<Setting, float> get, Action<Setting, float> set, Func<EconomyParameterData, int> live, Func<BenefitCostSystem, int> cost)
            => new Def { Key = key, GetPct = get, SetPct = set, GetLive = live, GetCost = cost };

        private int HoursPerMonth()
        {
            if (m_TimeSettingsQuery.IsEmptyIgnoreFilter)
                return 24;
            int dpm = m_TimeSettingsQuery.GetSingleton<TimeSettingsData>().m_DaysPerYear / 12;
            if (dpm < 1) dpm = 1;
            return dpm * 24;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (++m_Tick % 32 == 0)
                Refresh();
        }

        private void Refresh()
        {
            if (!m_EconQuery.IsEmptyIgnoreFilter)
            {
                try
                {
                    EconomyParameterData d = m_EconQuery.GetSingleton<EconomyParameterData>();
                    foreach (var def in m_Defs) { def.LiveValue = def.GetLive(d); def.ValBinding.Update(); }
                }
                catch { /* singleton not ready */ }
            }
            if (m_CostSystem != null)
            {
                foreach (var def in m_Defs) { def.CostValue = def.GetCost(m_CostSystem); def.CostBinding.Update(); }
                m_TotalCostValue = m_CostSystem.TotalCost;
                m_TotalCost.Update();
            }
            m_HoursPerMonth.Update();
            m_WelfareOffices.Update();
            m_BenefitsGated.Update();
        }

        private void OnSettingsApplied(Game.Settings.Setting setting)
        {
            foreach (var def in m_Defs) def.PctBinding.Update();
            m_Funded.Update();
            Refresh();
        }

        protected override void OnDestroy()
        {
            if (S != null)
                S.onSettingsApplied -= OnSettingsApplied;
            base.OnDestroy();
        }
    }
}
