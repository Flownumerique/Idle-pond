# Design: "Lagon Vif" Phase 2 — candy rebrand of Profil / Corail / Perles

**Date:** 2026-06-25
**Status:** Approved
**Follows:** `2026-06-24-lagon-vif-desktop-shell-design.md` (Phase 1)

## Problem

Phase 1 rebranded the desktop shell + Shop to the candy "Lagon Vif" DA but left the
other dock panels on a dark-teal compatible surface with their original dark-Tailwind
styling. This phase rebrands three of them — **Profil** (Stats + Achievements +
Challenges), **Corail** (Research), and **Perles** (PearlMarket + PrestigeUpgrades) — to
the candy DA.

Out of scope: Journal (Lore/Guide) and the Améliorations tab (run upgrades) stay on the
dark surface for a later pass. No game-logic changes.

## Decisions

- These three panels move from the dark `.lg-dock-body:not(.is-light)` surface to the
  light candy surface (`#EFF9FA`). The Phase-1 `is-shop` flag is generalized to `is-light`,
  set for: shop+fish, research, pearls (both tabs), profile. Shop+upgrades (Améliorations)
  and journal remain dark.
- Markup/classNames only — every store action, prereq check, reward calc, and handler is
  preserved byte-for-byte.

## Shared candy kit (new CSS in `src/index.css`)

- `.lg-card` — white, radius 18px, soft teal shadow `0 6px 16px rgba(11,85,102,.1)`,
  padding 14px. Modifiers: `--gold` (claimable/affordable highlight: gold gradient +
  `#FFC23C` border), `--done` (owned/claimed → muted gray-teal, lowered opacity),
  `--locked` (faded).
- `.lg-card-title` / `.lg-card-desc` / `.lg-card-flavour` — Baloo 2 title (#0B5566),
  Nunito desc (#6E97A0), italic flavour.
- `.lg-section-eyebrow` — small uppercase teal sub-section label.
- `.cbtn--sm` (compact), `.cbtn--teal`, `.cbtn--emerald` — added button variants for the
  reward/claim/unlock actions (3D candy look consistent with Phase 1).

## Per-panel treatment

**Corail (`Research.tsx`)** — 5 branch groups become light tinted cards keeping their
branch hue accents (biologie/emerald, géologie/amber, alchimie/violet, mystique/cyan,
oceanologie/teal): tinted group header with coloured title + `n/total` count; each node a
`.lg-card`, unlocked → `--done` with ✓, buyable → small candy unlock button `N 💎`,
locked/prereq → `--locked`.

**Perles (`PearlMarket.tsx`, `PrestigeUpgrades.tsx`)** — market upgrades and prestige
upgrades as `.lg-card`s; owned → `--done`, affordable → highlighted, locked → `--locked`;
buy buttons `.cbtn--violet`/`--sm` showing `N 💎` (market) or `N 🪸` (prestige). Tier
labels (I/II/III) as coloured `.lg-section-eyebrow`. The "do a prestige first" empty state
restyled as a candy note card.

**Profil (`Stats.tsx`, `Achievements.tsx`, `Challenges.tsx`)** —
- Stats → candy hero: mana headline (Baloo 2, ink) + `+x/s` (green, boost ×2 in gold) over
  a 2×2 stat grid of `.lg-card` cells (gemmes/perles/poissons/profondeur) with the
  Phase-1 currency colours.
- Achievements → claimable as `.lg-card--gold` with a `.cbtn--emerald --sm` "+N 💎"; the
  header pill `claimed/total`; claimed/locked as `--done`/`--locked`.
- Challenges → `.lg-card`s, met-but-unclaimed highlighted (coral), completed `--done`;
  progress bar in teal→coral; "Réclamer/En cours" candy button; daily reset timer kept.

## Files changed

| File | Change |
|------|--------|
| `src/index.css` | Append shared candy kit + per-panel candy classes |
| `src/App.tsx` | `isShopFish` → `isLight` (covers shop-fish, research, pearls, profile) |
| `src/components/Research.tsx` | Candy markup |
| `src/components/PearlMarket.tsx` | Candy markup |
| `src/components/PrestigeUpgrades.tsx` | Candy markup |
| `src/components/Stats.tsx` | Candy markup |
| `src/components/Achievements.tsx` | Candy markup |
| `src/components/Challenges.tsx` | Candy markup |

## Verification

`npm run build` (`tsc -b && vite build`) passes after each panel; visual check of each
panel on the light surface. Committed per panel (Corail, Perles, Profil) atop the shared
kit commit.
