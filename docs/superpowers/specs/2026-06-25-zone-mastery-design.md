# Design: Zone Mastery bonuses + Succès tab

**Date:** 2026-06-25
**Status:** Approved

## Problem

The game has no reward for fully developing a biome. The player wants a
permanent "zone mastery" bonus — when every normal species of a zone reaches
its final milestone (level 100) — and a place to see the bonuses they can earn.
Achievements currently only grant gems and live stacked inside the Profil panel.

## Decisions (locked with user)

1. **Mastery condition:** a zone is mastered when, for every *normal* species of
   that depth, the player owns at least one instance at **level 100** (the final
   milestone). Legendary species (`requiredPrestiges`) are excluded from rosters.
2. **Bonus:** each mastered zone grants **+5 % income to that zone's fish only**
   (local, not global). Value lives in a tunable constant.
3. **Persistence:** mastery is **permanent** — recorded once and kept across
   prestige (which resets fish).
4. **Surfacing:** Profil gains sub-tabs **Stats / Succès / Défis**; the Succès
   tab shows the existing achievements plus a new Zone Mastery section.

## Architecture

### Data — `src/data/zones.ts` (new)

- `ZONE_MASTERY_BONUS_PERCENT = 5`
- `MASTERY_ZONES: number[]` — sorted unique `requiredDepth` values that have at
  least one normal (non-`requiredPrestiges`) species.
- `getZoneRoster(depth): FishType[]` — normal species with `requiredDepth === depth`.
- `getZoneMasteryProgress(depth, poissons): { done: number; total: number }` —
  `done` = roster species that have an owned instance with `level >= 100`;
  `total` = roster length.
- `isZoneMastered(depth, poissons): boolean` — `total > 0 && done === total`.
- `computeNewlyMasteredZones(poissons, mastered: number[]): number[]` — returns
  the input array plus any `MASTERY_ZONES` not already present that are now
  mastered (append-only; stable reference if nothing new).

`MAX_FISH_LEVEL` (100) is imported from the store. Zone display names come from
the existing `DEPTH_NAMES` table (already in `App.tsx`); the component receives
them or imports a shared copy — see §UI.

### Income — `src/utils/incomeCalc.ts`

- Signature becomes
  `computeIncomePerSec(poissons, bonuses, boostActiveUntil, masteredZones: number[] = [])`.
- Build a module-level `FISH_DEPTH = new Map(FISH_TYPES.map(f => [f.type, f.requiredDepth]))`.
- In the per-fish loop, multiply that fish's income by
  `masteredZones.includes(FISH_DEPTH.get(fish.type) ?? -1) ? 1 + ZONE_MASTERY_BONUS_PERCENT/100 : 1`.
- The default `[]` keeps existing call sites valid until updated.
- The bonus is keyed purely by `requiredDepth`: any fish whose depth is a
  mastered zone receives the +5 % — including a legendary fish sharing that
  depth, even though legendaries don't count toward the mastery *condition*.

### Store — `src/store/useGameStore.ts`

- New state field `masteredZones: number[]` (initial `[]`). Persisted
  automatically (the `partialize` spreads the whole state). `prestige()` returns
  an explicit partial that does **not** include `masteredZones`, so it is
  preserved → permanent. No change to `prestige()` needed.
- `upgradeFishN` (the only place a fish reaches level 100) computes
  `computeNewlyMasteredZones(updated, s.masteredZones)` and includes
  `masteredZones` in its returned partial.
- New action `evaluateZoneMastery(): void` running the same computation against
  current `poissons`/`masteredZones`; called once on app init to backfill
  existing saves.

### Income callers

- `GameLoopManager.tick()` passes `state.masteredZones` to `computeIncomePerSec`.
- `BottomActionBar` reads `masteredZones` from the store and passes it too.

### UI — Profil sub-tabs + Zone Mastery

- `TABS_BY_PANEL.profile = [{id:'stats',label:'Stats'},{id:'succes',label:'Succès'},{id:'defis',label:'Défis'}]`.
  The dock already renders `TABS_BY_PANEL` tabs.
- `panelTab` initial state gains `profile: 'stats'`.
- `PanelBody` for `profile` routes by tab:
  - `stats` → `<Stats/>`
  - `succes` → `<Achievements/>` followed by `<ZoneMastery/>`
  - `defis` → `<Challenges/>`
  (The current single stacked `<Stats/><Achievements/><Challenges/>` block is replaced.)
- New `src/components/ZoneMastery.tsx` (candy `.lg-*` kit): a `.lg-panel` with a
  `<h3>Maîtrise des zones</h3>` head, then one card per `MASTERY_ZONES` entry:
  - title = `DEPTH_NAMES[depth]`, bonus line `+5 % revenus de la zone`.
  - mastered → `.lg-card--gold` with ✓ and "Maîtrisée".
  - in progress → `.lg-card` showing `done/total` species at niv. 100 and the
    list of remaining species names.
  Reads `poissons` and `masteredZones` from the store; pure display, no actions.

`DEPTH_NAMES` is currently duplicated in several components. `ZoneMastery` reuses
the same list; no refactor of the existing duplicates (out of scope).

## Out of scope

- No global (cross-zone) bonus; the bonus is strictly local to each zone.
- No change to existing achievements, their gem rewards, or game balance
  elsewhere.
- No de-duplication of the existing `DEPTH_NAMES` copies.
- No mobile-specific layout work beyond what the existing dock/sub-tabs already do.

## Verification

No test framework in the repo. Verify with `npm run build` (`tsc -b && vite
build`) passing, then in the dev server: the Profil panel shows Stats/Succès/Défis
sub-tabs; the Succès tab lists achievements + the Zone Mastery cards with correct
progress; bringing a zone's species to level 100 marks it mastered and the
income for those fish rises by 5 %; the mastered state survives a prestige.
