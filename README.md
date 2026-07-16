# Welfare Management

A Cities: Skylines II mod that turns citizen benefits (pension, unemployment, family allowance) into a real cost to the city treasury, lets you scale them, and gates them behind welfare offices.

## Features
- **Treasury-funded benefits** — vanilla mints benefits for free; this deducts the real daily outlay from the treasury and shows it as a budget expense (Subsidies). Off = vanilla behaviour.
- **Scale each benefit 0–200%** (pension, unemployment, family allowance) from an in-game panel or the Options page.
- **Welfare-office gate** — with funding on, benefits are only paid if the city has at least one welfare office. No office = benefits suspended (citizens receive nothing; the treasury pays nothing). No minting.

## ⚠️ Important — the welfare-office gate can shrink your population
"Fund benefits from the treasury" is **off by default**. When you turn it **on**, benefits become the responsibility of a **welfare office** — and if your city has **no welfare office**, benefits are gated to **zero**: pensioners, out-of-work adults and families receive **nothing** (the treasury doesn't pay it either). Those non-working households then run out of money and leave, so your **population will shrink**.

This is intended (welfare requires welfare infrastructure), but it's easy to hit by accident. So: **build at least one welfare office before enabling treasury funding, or leave the setting off.** With it off, the mod does nothing and the game behaves exactly like vanilla.

## How it works (transparency)
Recipient counts mirror the game's own rule exactly (`EconomyUtils.GetHouseholdIncome`): each living, non-working member of a resident household — children/teens draw family allowance, the elderly draw pension, out-of-work adults draw unemployment while still inside the allowance window. The mod counts them once per in-game day, multiplies by the current (%-scaled) benefit amounts, and folds the total into the game's own budget as a real `SubsidyResidential` expense — so the native budget system deducts it from the treasury. It never mints money or pokes `PlayerMoney` out of band. A small Harmony postfix keeps the budget panel from flickering (falls back safely if it can't install). Treasury funding is opt-in (off by default); with it off the mod is pure vanilla.

## Part of a set
This is one of three mods that replace the older all-in-one **Economy Tweaks**. The others are **Private Schools & Hospitals** and **Service & Sanitation Levy**. Don't run Economy Tweaks alongside these (they would double-apply).

## Credits
Made with Claude Code's Opus 4.8 and Fable 5 models.

## License
MIT — see [LICENSE](LICENSE).
