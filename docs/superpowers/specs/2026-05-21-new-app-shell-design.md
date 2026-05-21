# Design: Wire Zustand system as the single source of truth

**Date:** 2026-05-21  
**Status:** Approved

## Problem

The codebase has two parallel implementations that have diverged:

- **Old system** (currently rendered): `App.tsx` → `Panels.tsx` → `gameData.ts`. Uses `useReducer` with plain numbers. No persistence. Self-contained idle loop via `setInterval` inside a `useEffect`.
- **New system** (built but never mounted): `useGameStore.ts` + individual panel components + `GameLoopManager` + `OfflineManager` + `src/data/`. Uses Zustand with `persist`, `Decimal` big numbers, offline gains, save/load, achievements, challenges, prestige upgrades, and pearl upgrades.

`main.tsx` still renders the old `App.tsx`. The new system is never initialized.

## Goal

Remove the old system entirely. Build a new `App.tsx` shell that initializes the managers and routes to the new components. Single source of truth: `useGameStore`.

---

## Architecture

### New `App.tsx` — thin shell only

Responsibilities:
1. **Manager init on mount**: `checkDailyReset()`, `OfflineManager.getInstance().calculateOfflineGain()`, `GameLoopManager.getInstance().start()`. Stop loop on unmount.
2. **HUD chrome**: `TopHUD` reads from `useGameStore` (mana, gemmes, perles, pondDepth, prestiges). `BottomHUD` reads the same fields plus `boostActiveUntil`; income-per-second is computed inline via `useMemo` using `computeIncomePerSec`.
3. **Panel navigation state**: `activePanel: PanelId | null`, `panelTab: Record<string, string>`.
4. **Always-on notification mounts**: `<EventNotification />`, `<UnlockNotification />`, `<WelcomeBackNotification />` — these components self-manage via store flags; just mount them unconditionally.
5. **Prestige modal**: shown when `showPrestige` local state is true; reads `manaRunHigh` + `calcPrestigeReward` to preview reward, calls `useGameStore.getState().prestige()` on confirm.

No game logic lives in `App.tsx`. It is only layout, routing, and initialization.

### Income rate utility

`GameLoopManager` and the HUD both need to compute income-per-second from the same formula. Extract into `src/utils/incomeCalc.ts`:

```ts
export function computeIncomePerSec(state: Pick<GameState, 'poissons' | 'researchUnlocked' | 'pearlUpgradesUnlocked' | 'prestigeUpgradesUnlocked' | 'runUpgradesOwned' | 'boostActiveUntil'>): Decimal
```

`GameLoopManager.tick()` calls this instead of its inline version. The HUD calls it via a `useMemo` keyed on the fields that affect income.

---

## Panel routing

The dock has 5 tabs + prestige button. Two tabs have sub-tabs.

| PanelId    | Sub-tab key  | Component rendered          |
|------------|--------------|-----------------------------|
| `shop`     | `fish`       | `<Shop />`                  |
| `shop`     | `upgrades`   | `<Ameliorations />`         |
| `research` | —            | `<Research />`              |
| `pearls`   | `market`     | `<PearlMarket />`           |
| `pearls`   | `prestige`   | `<PrestigeUpgrades />`      |
| `profile`  | —            | `<Stats />` + `<Achievements />` + `<Challenges />` |
| `journal`  | `lore`       | `<Lore />`                  |
| `journal`  | `guide`      | `<Guide />`                 |

Panel frame (header, tabs bar, close button, scrim overlay) uses existing CSS classes from `index.css`. The Tailwind-based panel body components drop in as children.

---

## Visual pond

`PondCanvas.tsx` (React SVG) is deleted. `PhaserContainer` replaces it. It is already connected to `useGameStore` for `poissons` (array of `PoissonInstance`) and `pondDepth`, handles fish creation/destruction, animated swimming, and camera scroll (wheel + pointer drag).

---

## Prestige modal (inline in App.tsx)

~40 lines. Reads `manaRunHigh` and `pondDepth` from the store. Shows:
- Pearl reward preview via `calcPrestigeReward(manaRunHigh)` × prestige bonus
- Warning: pond resets, fish lost (except kept % from upgrades)
- The dock prestige button is highlighted when `pondDepth >= 2` (prestige ready). The modal can always be opened.
- The confirm button inside the modal is disabled if `pondDepth < 2`.

Confirm calls `useGameStore.getState().prestige()` then closes the modal.

---

## Files changed

### Deleted
| File | Reason |
|------|--------|
| `src/components/Panels.tsx` | All panels replaced by individual new components |
| `src/components/PondCanvas.tsx` | Replaced by `PhaserContainer` |
| `src/gameData.ts` | Replaced by `src/data/*.ts` |

### Created
| File | Purpose |
|------|---------|
| `src/utils/incomeCalc.ts` | Shared income-per-second computation |

### Modified
| File | Change |
|------|--------|
| `src/App.tsx` | Full rewrite as new shell |
| `src/managers/GameLoopManager.ts` | Use shared `computeIncomePerSec` from incomeCalc |

---

## Data flow summary

```
main.tsx
  └── App.tsx (shell)
        ├── init: GameLoopManager.start() ← ticks useGameStore every 100ms
        ├── init: OfflineManager.calculateOfflineGain() ← one-shot on mount
        ├── TopHUD ← useGameStore (mana, gemmes, perles, pondDepth, prestiges)
        ├── BottomHUD ← useGameStore + computeIncomePerSec
        ├── PhaserContainer ← useGameStore (poissons, pondDepth)
        ├── Panel overlay
        │     └── Shop / Research / Ameliorations / PearlMarket / PrestigeUpgrades
        │         Achievements / Challenges / Stats / Lore / Guide
        │         (each reads useGameStore independently)
        ├── EventNotification ← useGameStore.pendingNarrativeEvent
        ├── UnlockNotification ← useGameStore.pendingUnlock
        ├── WelcomeBackNotification ← useGameStore.pendingWelcomeBack
        └── PrestigeModal (local state) ← useGameStore.manaRunHigh, calcPrestigeReward
```

---

## Out of scope

- Visual redesign of any existing panel component
- Changes to `useGameStore` actions or game balance
- Changes to `GameLoopManager` logic beyond the incomeCalc extraction
- Mobile-specific layout changes beyond preserving the existing mobile tabs
