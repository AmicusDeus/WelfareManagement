# Welfare Management

A Cities: Skylines II mod that turns citizen benefits (pension, unemployment, family allowance) into a real cost to the city treasury, lets you scale them, and gates them behind welfare offices.

## Features
- **Treasury-funded benefits** — vanilla mints benefits for free; this deducts the real daily outlay from the treasury and shows it as a budget expense (Subsidies). Off = vanilla behaviour.
- **Scale each benefit 0–200%** (pension, unemployment, family allowance) from an in-game panel or the Options page.
- **Welfare-office gate** — with funding on, benefits are only paid if the city has at least one welfare office. No office = benefits suspended (citizens receive nothing; the treasury pays nothing). No minting.

## How it works (transparency)
Recipient counts mirror the game's own rule exactly (`EconomyUtils.GetHouseholdIncome`): each living, non-working member of a resident household — children/teens draw family allowance, the elderly draw pension, out-of-work adults draw unemployment while still inside the allowance window. The mod counts them once per in-game day, multiplies by the current (%-scaled) benefit amounts, and folds the total into the game's own budget as a real `SubsidyResidential` expense — so the native budget system deducts it from the treasury. It never mints money or pokes `PlayerMoney` out of band. A small Harmony postfix keeps the budget panel from flickering (falls back safely if it can't install). Everything is opt-out (on by default); turn the mod off for exact vanilla behaviour.

## Part of a set
This is one of three mods that replace the older all-in-one **Economy Tweaks**. The others are **Private Schools & Hospitals** and **Service & Sanitation Levy**. Don't run Economy Tweaks alongside these (they would double-apply).

## Credits
Made with Claude Code, Anthropic's agentic coding tool.

## License
MIT — see [LICENSE](LICENSE).
