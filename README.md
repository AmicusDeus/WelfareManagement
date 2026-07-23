# Welfare Management

A Cities: Skylines II mod that turns citizen benefits (pension, unemployment, family allowance) into a real cost to the city treasury and lets you scale them, with treasury funding administered by welfare offices.

## Features
- **Treasury-funded benefits** — vanilla mints benefits for free; this deducts the real daily outlay from the treasury and shows it as a budget expense (Subsidies). Off = vanilla behaviour.
- **Scale each benefit 0–200%** (pension, unemployment, family allowance) from an in-game panel or the Options page.
- **Welfare-office gate** — with funding on, benefits are charged to the treasury only while the city has a welfare office to administer them. No office = benefits fall back to the base game (still paid, minted free, not charged to the treasury), so your city keeps growing normally, and a warning reminds you to build a welfare office to actually fund them from the treasury.

## The welfare-office gate
"Fund benefits from the treasury" is **off by default**. When you turn it **on**, treasury funding is administered by a **welfare office**. If your city has **no welfare office yet**, the mod does **not** charge the treasury and does **not** change anything for citizens — benefits are simply paid by the base game (free), so immigration and growth are unaffected, and a settings/panel warning reminds you to build a welfare office. Building one switches funding over to the treasury.

(Earlier versions, up to v1.21, instead *zeroed* benefits when funding was on with no office — that starved non-working households and could stall a new city's growth. v1.22 replaced that hard-zero with the free-fallback-plus-warning above.)

## How it works (transparency)
Recipient counts mirror the game's own rule exactly (`EconomyUtils.GetHouseholdIncome`): each living, non-working member of a resident household — children/teens draw family allowance, the elderly draw pension, out-of-work adults draw unemployment while still inside the allowance window. The mod counts them once per in-game day, multiplies by the current (%-scaled) benefit amounts, and folds the total into the game's own budget as a real `SubsidyResidential` expense — so the native budget system deducts it from the treasury. It never mints money or pokes `PlayerMoney` out of band. A small Harmony postfix keeps the budget panel from flickering (falls back safely if it can't install). Treasury funding is opt-in (off by default); with it off the mod is pure vanilla.

## Part of a set
This is one of three mods that replace the older all-in-one **Economy Tweaks**. The others are **Private Schools & Hospitals** and **Service & Sanitation Levy**. Don't run Economy Tweaks alongside these (they would double-apply).

## Credits
Made with Claude Code's Opus 4.8 and Fable 5 models.

## License
MIT — see [LICENSE](LICENSE).
