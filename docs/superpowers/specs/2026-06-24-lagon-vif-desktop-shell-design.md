# Design: "Lagon Vif" desktop shell — Phase 1

**Date:** 2026-06-24
**Status:** Approved
**Source mockup:** `Lagon Vif Desktop.dc.html` (claude.ai/design project "UI design pour Idle Pond")

## Problem

The shipped app uses an editorial "paper" aesthetic — serif `Newsreader` fonts, muted
`oklch` ink/paper tokens, a floating left dock, and panels that slide in as overlays. The
`Lagon Vif Desktop` mockup proposes a different desktop shell: a bright candy-lagoon look
with a fixed three-column layout (left rail · center pond · permanently docked right shop).

This spec covers **Phase 1**: implement the desktop shell and the parts the mockup actually
specifies, fully rebranded. Phase 2 (deferred) rebrands the remaining panels.

## Decisions (locked with user)

1. **Full desktop rebrand** to the Lagon Vif aesthetic (new fonts, teal+coral palette, 3D
   candy buttons), not a layout-only change and not a coexisting alternate view.
2. **Persistent right dock that swaps content** — the 392px right panel is always visible on
   desktop; clicking a left-rail item swaps its body. Replaces the slide-over overlay on desktop.
3. **Keep the live Phaser pond** in the center column. The mockup's floating PNG fish were a
   static stand-in.
4. **Ship Shop now, rebrand the rest later** — this pass fully implements the candy shell, rail,
   HUD, action bar, Shop fish cards, and Renouveau modal. The other dock sections (Améliorations,
   Coral/Research, Pearls, Profile, Journal) stay functional on a compatible surface and get their
   candy rebrand in a Phase 2 spec.

---

## Architecture

### 1. Aesthetic tokens (foundation)

- **Fonts:** add **Baloo 2** (weights 500–800) and **Nunito** (600–900) to the Google Fonts
  link in `index.html`. In `index.css`: `--font-display: 'Baloo 2'` (headings/numbers),
  `--font-ui: 'Nunito'` (body). These cascade globally, so even the deferred dark panels pick
  up the new typography; color-sensitive surfaces are handled in §4.
- **Palette** (matching the mockup's hex values):
  - teal `#16C8C4` → `#0E9CAB`, deep ink `#0B5566`, muted ink `#5A8A92`
  - coral `#FF8A63` → `#FF6B3D` (shadow `#D24E27`)
  - gold `#FFD56A` → `#FFB72E` (shadow `#E59412`, text `#7A4E00`)
  - violet `#8C6BFF` → `#6B47F0` (shadow `#4F30C2`)
  - mana-violet `#7C5CFF`, gemme-green `#3FC97E`, perle-pink `#FF7AB8`
- **Candy button primitive** `.cbtn`: the 3D look (`border:0; box-shadow: 0 4px 0 <darker>`),
  with `.cbtn--coral / --gold / --violet / --ghost` variants and a disabled state.
- **Radii** 14–24px; **shadows** soft teal (`rgba(11,85,102,.1–.32)`).

The new shell classes are namespaced under a `.lagon` root so they are self-contained and do
not collide with the existing editorial classes that the deferred panels still rely on.

### 2. Layout — three-column desktop shell (`App.tsx`)

Root becomes a flex row filling the viewport:

```
.lagon ───────────────────────────────────────────────
│ LeftRail │        CenterPond           │  RightDock  │
│  96px    │         flex:1              │   392px     │
│ logo     │  ┌ water gradient + rays ─┐ │ ┌ teal hdr ┐│
│ Boutique │  │ PhaserContainer (kept) │ │ │ title+tab││
│ Corail   │  │ TopHUD (depth · res.)  │ │ ├──────────┤│
│ Perles   │  │ Phaser fish            │ │ │  body    ││
│ Profil   │  │ BottomActionBar        │ │ │ (swaps)  ││
│ Journal  │  └────────────────────────┘ │ └──────────┘│
│ …Renew   │                             │             │
```

- **LeftRail** (96px, solid white): logo tile, nav items (Boutique / Corail / Perles / Profil /
  Journal), and a Renew tile pinned to the bottom. Active item uses the teal-gradient pill.
- **CenterPond** (`flex:1`, relative, overflow hidden): a CSS teal water gradient background +
  animated rays/bubbles, the transparent `PhaserContainer` layered over it, and the candy
  **TopHUD** and **BottomActionBar** overlaid on top. The Phaser game config is unchanged; it
  reflows into the column instead of full-bleed.
- **RightDock** (392px): teal-gradient header (title + sub + sub-tabs) over a swappable body.

#### TopHUD

Reads `mana`, `gemmes`, `perles`, `pondDepth` from `useGameStore`. Left: depth chip
(`{pondDepth}` + biome name). Right: three resource pills (mana / gemmes / perles) with the
mockup's radial-gradient coin icons. `formatNumber` for values.

#### BottomActionBar

Reads the same fields `BottomHUD` uses today (`poissons`, `mana`, `gemmes`, `pondDepth`,
`boostActiveUntil`, bonus inputs) and computes income via `computeIncomePerSec` exactly as now.
Layout per mockup: REVENU `…/s` · fish count + species · next-biome progress bar · Boost candy
button (gold) · Creuser candy button (coral). Boost/dig disabled logic and the 1 Hz `now` timer
are carried over unchanged.

### 3. Navigation model — persistent dock

- `activePanel` is **always set** on desktop (default `'shop'`); the right dock is always
  visible. Clicking a rail item sets `activePanel`; the active rail item is highlighted.
- Sub-tabs (`shop`: fish/upgrades, `pearls`: market/prestige, `journal`: lore/guide) render in
  the dock header, driven by the existing `panelTab` state and `PanelBody` routing.
- **Renew** opens the Renouveau **modal** (overlay), not a dock section.
- **Mobile / < 1024px fallback:** the rail collapses to the existing bottom tab bar; the right
  dock detaches into a slide-over overlay (reusing today's overlay/scrim mechanics); the action
  bar stacks. `activePanel` may be `null` on mobile (dock hidden) as today. The working mobile
  UX is preserved, restyled with the new tokens.

### 4. Right dock body + deferred-panel surface

The dock body switches its surface class based on the active content:

- **Shop fish list →** light candy surface (`#EFF9FA`) hosting the rebuilt cards (§5).
- **All other content** (Améliorations, Coral/Research, Pearls, Profile, Journal) **→** a
  **dark-teal compatible surface** so the existing dark-Tailwind components remain legible and
  untouched until Phase 2. No changes to those components in this pass.

`PanelBody` routing is unchanged; only the wrapping surface differs.

### 5. Shop cards — candy (`Shop.tsx`)

Rebuild `FishCard` into the mockup's three variants, replacing its dark Tailwind classes with
candy classes. **All game logic is preserved** (buy / upgrade / cost / milestone math, the
×1/×5/×10/×25/max quantity selector, locked-fish rows); only markup and class names change.

- **Legendary:** gold gradient card, "★ POISSON LÉGENDAIRE" eyebrow, sprite tile, name + `NIV.`
  badge, green income, gold candy "Améliorer" button.
- **Owned:** white card / teal border, teal `NIV.` badge, green income, milestone chips row
  (10·25·50·100 with hit / next / locked states), violet candy "Améliorer" button.
- **Buyable:** white card, sprite tile, name + description, coral candy "Acheter" button.

The redundant inner `<h2>Boutique</h2>` is dropped — the dock header owns the title and tabs.

### 6. Renouveau modal (`App.tsx`)

Restyle `PrestigeModal` to the mockup: teal blurred backdrop, white rounded card, coral circular
sigil, "Renouveau" title, pink reward box showing `{earned} 🪸`, the kept-fish line (when
`prestigeKeepFishPercent > 0`), body copy, and **Annuler** (ghost candy) + **Renouveler** (coral
candy) buttons. Existing reward computation and the `pondDepth >= 2` confirm gate are unchanged.

---

## Files changed

### Modified
| File | Change |
|------|--------|
| `index.html` | Add Baloo 2 + Nunito to the Google Fonts link |
| `src/index.css` | Candy tokens + fonts; new `.lagon` shell / rail / dock / HUD / action-bar / `.cbtn` / candy-modal classes; < 1024px fallback rules. Existing editorial classes left intact for deferred panels. |
| `src/App.tsx` | Rewrite root shell into LeftRail / CenterPond / RightDock + candy TopHUD, BottomActionBar, Renouveau modal; persistent-dock nav state. Manager init, Escape handling, and notification mounts preserved. |
| `src/components/Shop.tsx` | Candy `FishCard` variants; drop redundant inner header |

### Created
| File | Purpose |
|------|---------|
| (none required) | New CSS lives in `index.css`; new shell sub-components live in `App.tsx` alongside the existing HUD components |

---

## Out of scope (Phase 2)

- Candy rebrand of Coral/Research, Pearls (`PearlMarket` / `PrestigeUpgrades`), Profile
  (`Stats` / `Achievements` / `Challenges`), Journal (`Lore` / `Guide`), and `Améliorations`.
  These keep working on the dark-teal compatible dock surface.
- Any changes to `useGameStore`, game balance, the `GameLoopManager`/`OfflineManager`, or the
  Phaser `PondScene` internals.
- The mockup's static floating-PNG fish (the live Phaser pond is kept instead).

## Verification

No test framework exists in the repo (no vitest/jest). Verification is build + manual visual:

1. `npm run build` (`tsc -b && vite build`) must pass with no type errors.
2. `npm run dev`, then via Chrome DevTools MCP at 1440×900 confirm against the mockup:
   resources/HUD render, the left rail swaps the dock body, Shop buy/upgrade works, the
   Renouveau modal opens and confirms, and the < 1024px fallback (bottom tabs + overlay dock)
   behaves.
