import { useState, useEffect, useRef } from "react";
import * as ReactDOM from "react-dom";
import { bindValue, useValue, trigger } from "cs2/api";
import { getModule } from "cs2/modding";
import { FloatingButton } from "cs2/ui";
import { useT } from "mods/i18n";
import { FONT, TEXT, panelTitle, sectionTitle } from "mods/ui-tokens";
import ICON from "../wm-icon.svg";

// Realistic Funding & Management: Welfare Benefits UI — the benefits panel (fund toggle + 3 sliders + welfare-office gate warning), the toolbar
// button, and the Budget "Citizen Benefits" detail sub-item + hover box.
const G = "WMParams";
const TAX_SLIDER_PATH = "game-ui/game/components/economy-panel/taxation-page/tax-slider.tsx";

type P = { key: string; label: string };
const BENEFITS: P[] = [
    { key: "pension", label: "Pension" },
    { key: "unemployment", label: "Unemployment Benefit" },
    { key: "family", label: "Family Allowance" },
];
const MIN = 0, MAX = 200, STEP = 5;

const pct$: Record<string, any> = {};
const cost$: Record<string, any> = {};
const val$: Record<string, any> = {};
for (const p of BENEFITS) {
    pct$[p.key] = bindValue<number>(G, "pct_" + p.key, 100);
    cost$[p.key] = bindValue<number>(G, "cost_" + p.key, 0);
    val$[p.key] = bindValue<number>(G, "val_" + p.key, 0);
}
const funded$ = bindValue<boolean>(G, "funded", false);
const totalCost$ = bindValue<number>(G, "cost_total", 0);
const welfareOffices$ = bindValue<number>(G, "welfareOffices", 0);
const benefitsGated$ = bindValue<boolean>(G, "benefitsGated", false);
const hoursPerMonth$ = bindValue<number>(G, "hoursPerMonth", 24);

const fmt = (n: number) => Math.round(Math.abs(n)).toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");

// ---- toolbar button + panel plumbing --------------------------------------------------------------------------
const PANEL_ID = "welfare";
let _open = false;
const _subs = new Set<() => void>();
function _notify() { _subs.forEach((f) => f()); }
// Registered in a shared page-window registry so opening THIS panel closes the OTHER mods' floating panels
// (native CS2 = one toolbar panel at a time). _forceClose only touches this panel (no re-broadcast).
function _forceClose() { if (_open) { _open = false; _notify(); } }
(function () { const w = window as any; if (!w.__csFloatingPanels) w.__csFloatingPanels = {}; w.__csFloatingPanels[PANEL_ID] = _forceClose; })();
function setOpen(v: boolean) {
    if (v) { const reg = (window as any).__csFloatingPanels; if (reg) { for (const id in reg) { if (id !== PANEL_ID) { try { reg[id](); } catch { } } } } }
    if (_open !== v) { _open = v; _notify(); }
}
function useOpen() {
    const [, force] = useState(0);
    useEffect(() => { const f = () => force((x) => x + 1); _subs.add(f); return () => { _subs.delete(f); }; }, []);
    return _open;
}
const CloseGlyph = ({ onClick }: { onClick: () => void }) => (
    <button onClick={onClick} style={{ cursor: "pointer", width: "24rem", height: "24rem", border: "none", background: "transparent", padding: 0, pointerEvents: "auto" } as any}>
        <div style={{
            width: "24rem", height: "24rem", margin: "auto", backgroundColor: "var(--textColor)",
            maskImage: "url(Media/Glyphs/Close.svg)", WebkitMaskImage: "url(Media/Glyphs/Close.svg)",
            maskSize: "contain", WebkitMaskSize: "contain", maskRepeat: "no-repeat", WebkitMaskRepeat: "no-repeat",
            maskPosition: "center", WebkitMaskPosition: "center",
        } as any} />
    </button>
);

let _slider: any, _tried = false;
function nativeSlider(): any {
    if (!_tried) { _tried = true; try { _slider = getModule(TAX_SLIDER_PATH, "TaxSlider"); } catch { _slider = null; } }
    return _slider;
}

const BenefitRow = ({ p, funded }: { p: P; funded: boolean }) => {
    const pct = useValue(pct$[p.key]) as number;
    const cost = useValue(cost$[p.key]) as number;
    const val = useValue(val$[p.key]) as number;
    const hpm = (useValue(hoursPerMonth$) as number) || 24;
    const [local, setLocal] = useState<number | null>(null);
    const timer = useRef<any>(null);
    useEffect(() => { if (local !== null && Math.round(pct) === local) setLocal(null); }, [pct, local]);
    const display = local !== null ? local : Math.round(pct);
    const commit = (v: number) => {
        if (!isFinite(v)) return; // a mis-measured slider can emit NaN/Infinity — never forward it
        const snapped = Math.max(MIN, Math.min(MAX, Math.round(v / STEP) * STEP));
        setLocal(snapped);
        if (timer.current) clearTimeout(timer.current);
        timer.current = setTimeout(() => trigger(G, "setPct", p.key, snapped), 120);
    };
    const TaxSlider = nativeSlider();
    const income = funded ? -Math.round(cost / hpm) : 0;
    const t = useT();
    return (
        <div style={{ display: "flex", alignItems: "center", padding: "6rem 14rem" }}>
            <div style={{ width: "160rem", flexShrink: 0 }}>
                <div style={{ fontSize: "14rem" }}>{t(p.key, p.label)}</div>
                <div style={{ fontSize: "11rem", opacity: 0.55 }}>{t("perRecipient", "₡{v}/mo per recipient", { v: fmt(val) })}</div>
            </div>
            {/* Explicit width: a flex:1 track can measure 0 in cohtml at drag time, which made the native slider emit
                NaN and overflow the benefit. A fixed width gives it a deterministic track. */}
            <div style={{ width: "290rem", flexShrink: 0 }}>
                {TaxSlider
                    ? <TaxSlider min={MIN} max={MAX} rate={display} income={income} onValueChanged={commit} />
                    : <span style={{ fontSize: "13rem" }}>{display}%</span>}
            </div>
        </div>
    );
};

export const WelfareButton = () => {
    const t = useT();
    return <FloatingButton src={ICON} tooltipLabel={t("buttonTooltip", "Realistic Funding & Management: Welfare Benefits")} onSelect={() => setOpen(!_open)} />;
};

export const WelfarePanelHost = () => {
    const open = useOpen();
    const funded = useValue(funded$);
    const totalCost = useValue(totalCost$) as number;
    const hpm = useValue(hoursPerMonth$) as number;
    const gated = useValue(benefitsGated$);
    const offices = useValue(welfareOffices$) as number;
    const t = useT();
    if (!open) return null;
    return (
        <div style={{
            position: "fixed", top: "90rem", right: "56rem", width: "480rem", zIndex: 99999, pointerEvents: "auto",
            background: "rgba(13, 21, 33, 0.97)", borderRadius: "6rem", display: "flex", flexDirection: "column",
            color: TEXT, boxShadow: "0 4rem 24rem rgba(0,0,0,0.5)",
        }}>
            <div style={{ display: "flex", alignItems: "center", padding: "10rem 14rem", borderBottom: "1rem solid rgba(255,255,255,0.12)" }}>
                <div style={{ flex: 1, ...panelTitle }}>{t("panelTitle", "Welfare Benefits")}</div>
                <CloseGlyph onClick={() => setOpen(false)} />
            </div>
            <div style={{ padding: "8rem 0 10rem", maxHeight: "860rem", overflowY: "auto" }}>
                <div style={{ ...sectionTitle, padding: "4rem 14rem 6rem", opacity: 0.9 }}>{t("benefitsHeader", "CITIZEN BENEFITS")}</div>
                <div style={{ display: "flex", alignItems: "center", padding: "4rem 14rem 10rem" }}>
                    <button
                        onClick={() => trigger(G, "setFunded", !funded)}
                        style={{ cursor: "pointer", padding: "5rem 12rem", borderRadius: "4rem", fontSize: "13rem", color: "white", background: funded ? "rgba(60, 160, 90, 0.9)" : "rgba(120, 120, 120, 0.6)" }}
                    >
                        {funded ? t("fundedOn", "Funded by treasury: ON") : t("fundedOff", "Funded by treasury: OFF")}
                    </button>
                    <div style={{ flex: 1 }} />
                    <div style={{ fontSize: "13rem", color: funded && totalCost > 0 ? "rgb(232, 110, 110)" : "rgba(255,255,255,0.6)" }}>
                        {funded && totalCost > 0 ? "-₡" + fmt(totalCost / (hpm || 24)) + " /h" : t("noCost", "no cost")}
                    </div>
                </div>
                <div style={{ padding: "0 14rem 8rem", fontSize: "12rem", opacity: 0.6 }}>
                    {t("benefitsNote", "% of vanilla. With funding on, the treasury cost is shown per hour (matching the money trend).")}
                </div>
                {funded && gated ? (
                    <div style={{ margin: "0 14rem 8rem", padding: "6rem 10rem", borderRadius: "4rem", background: "rgba(220, 160, 60, 0.18)", border: "1rem solid rgba(220, 160, 60, 0.5)", fontSize: "12rem", color: "rgb(240, 190, 110)" }}>
                        {t("welfareWarning", "No welfare office in the city — benefits are paid by the base game (free) and not charged to the treasury. Build a Welfare Office to fund them from the treasury.")}
                    </div>
                ) : null}
                {funded && !gated && offices > 0 ? (
                    <div style={{ padding: "0 14rem 6rem", fontSize: "11rem", opacity: 0.45 }}>
                        {t("welfareAdmin", "administered by {n} welfare office(s)", { n: offices })}
                    </div>
                ) : null}
                {BENEFITS.map((p) => <BenefitRow key={p.key} p={p} funded={funded} />)}
            </div>
        </div>
    );
};

// ---- budget detail sub-item (Subsidies -> Citizen Benefits) ---------------------------------------------------
export const BudgetDetailInject = ({ Original, detailProps }: { Original: any; detailProps: any }) => {
    const funded = useValue(funded$);
    const benefitCost = useValue(totalCost$) as number; // monthly, positive

    const item = detailProps && detailProps.item;
    const id = item && item.id;
    if (!item || id !== "Subsidies" || !Array.isArray(item.sources) || !Array.isArray(detailProps.values))
        return <Original {...detailProps} />;

    try {
        const values = detailProps.values.slice();
        const sources = item.sources.slice();
        let residual = 0;
        if (funded && benefitCost > 0) {
            const idx = values.length;
            values.push(-benefitCost); // expenses are negative in the detail values
            sources.push({ __Type: "Game.UI.InGame.BudgetSource", id: "WMCitizenBenefits", index: idx });
            residual += -benefitCost;
        }
        if (residual !== 0 && values.length > 0) values[0] = (values[0] || 0) - residual;
        return <Original {...detailProps} item={{ ...item, sources }} values={values} />;
    } catch {
        return <Original {...detailProps} />;
    }
};

// ---- hover box for the injected "Citizen Benefits" budget row --------------------------------------------------
export const BudgetRowHoverLayer = () => {
    const t = useT();
    const [box, setBox] = useState<{ row: any; left: number; top: number; width: number } | null>(null);
    useEffect(() => {
        const rows = [
            { label: t("benefitsRow", "Citizen Benefits"), icon: "Media/Game/Icons/GovernmentSubsidies.svg", title: t("benefitsHeader", "CITIZEN BENEFITS"), text: t("hoverBenefits", "Vanilla creates pension, unemployment and family-allowance money out of thin air. With treasury funding on, the mod counts each recipient every in-game day — the elderly (pension), jobless adults still inside the unemployment allowance window, and children (family allowance) — multiplies them by the current benefit amounts, and deducts the total from your city balance as this expense.") },
        ];
        const findRow = (el: any) => {
            let c = el, best: any = null;
            for (let i = 0; i < 5 && c; i++) {
                const tx = ((c.textContent || "") as string).trim();
                for (const r of rows) {
                    if (r.label && tx.indexOf(r.label) === 0 && tx.length < r.label.length + 26) {
                        if (!best || tx.length > best.len) best = { row: r, el: c, len: tx.length };
                    }
                }
                c = c.parentElement;
            }
            return best;
        };
        let infoSel: string | null = null;
        const infoColumn = () => {
            if (infoSel === null) {
                try {
                    const cls: any = getModule("game-ui/game/components/economy-panel/budget-page/budget-page.module.scss", "classes");
                    infoSel = cls && cls.infoColumn ? "." + String(cls.infoColumn).split(" ")[0] : "";
                } catch { infoSel = ""; }
            }
            return infoSel;
        };
        const onOver = (e: any) => {
            try {
                const hit = findRow(e.target);
                if (!hit) { setBox((b) => (b ? null : b)); return; }
                let vh = 1080, vw = 1920;
                try { vh = (window as any).innerHeight || 1080; vw = (window as any).innerWidth || 1920; } catch { }
                let left = Math.round(vw * 0.655), top = Math.round(vh * 0.27), width = 360;
                try {
                    const sel = infoColumn();
                    const col = sel ? document.querySelector(sel) : null;
                    if (col) { const cr = (col as any).getBoundingClientRect(); left = Math.round(cr.left + 10); top = Math.round(cr.top + 8); width = Math.max(240, Math.round(cr.width - 20)); }
                } catch { }
                setBox({ row: hit.row, left, top, width });
            } catch { }
        };
        document.addEventListener("mouseover", onOver, true);
        return () => document.removeEventListener("mouseover", onOver, true);
    }, []);

    if (!box) return null;
    const row = box.row;
    const el = (
        <div style={{
            position: "fixed", left: box.left + "px", top: box.top + "px", width: box.width + "rem",
            boxSizing: "border-box", zIndex: 99999, pointerEvents: "none",
            display: "flex", flexDirection: "column", alignItems: "stretch",
        } as any}>
            <div style={{ width: "96rem", height: "96rem", padding: "4rem", boxSizing: "border-box", backgroundColor: "var(--panelColorDark)", borderRadius: "8rem", marginBottom: "6rem" } as any}>
                <img src={row.icon} style={{ width: "100%", height: "100%" }} />
            </div>
            <div style={{ fontSize: "var(--fontSizeXL)", fontWeight: "bold", textTransform: "uppercase", color: "var(--accentColorLight)", lineHeight: "1.2", margin: "12rem 0", wordWrap: "break-word" } as any}>{row.title}</div>
            <div style={{ fontSize: FONT.m, lineHeight: "1.4", color: TEXT, wordWrap: "break-word" } as any}>{row.text}</div>
        </div>
    );
    const portal = (ReactDOM as any) && (ReactDOM as any).createPortal;
    return portal ? portal(el, document.body) : el;
};
