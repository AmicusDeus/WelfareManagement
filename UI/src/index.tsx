import { ModRegistrar } from "cs2/modding";
import { Safe } from "mods/budget-section";
import { WelfareButton, WelfarePanelHost, BudgetDetailInject, BudgetRowHoverLayer } from "mods/welfare";

const register: ModRegistrar = (moduleRegistry) => {
    console.info("[WelfareManagement] register() running");

    // Budget DETAIL breakdown: the benefit cost is folded into the Subsidies total, so we show a "Citizen Benefits"
    // sub-item in the Subsidies line's detail box (hover the line), netted out of the Residential slot.
    try {
        const BUDGET_ITEM_DETAIL = "game-ui/game/components/economy-panel/budget-page/budget-item-detail/budget-item-detail.tsx";
        moduleRegistry.extend(BUDGET_ITEM_DETAIL, "BudgetItemDetail", (Original: any) => (props: any) => (
            <Safe><BudgetDetailInject Original={Original} detailProps={props} /></Safe>
        ));
    } catch (e) { console.info("[WM] extend(BudgetItemDetail) error: " + String(e)); }

    // Toolbar button + floating panel + budget-row hover layer.
    try {
        moduleRegistry.append("GameTopRight", WelfareButton);
        moduleRegistry.append("Game", WelfarePanelHost);
        moduleRegistry.append("Game", BudgetRowHoverLayer);
    } catch (e) { console.info("[WM] panel error: " + String(e)); }
};

export default register;
