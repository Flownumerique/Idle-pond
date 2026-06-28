# Lagon Vif Desktop Shell Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebrand the Idle Pond desktop shell to the bright "Lagon Vif" aesthetic — a three-column layout (left rail · live Phaser pond · permanently docked right shop) with candy buttons, restyled HUD/action bar, candy Shop cards, and a candy Renouveau modal.

**Architecture:** A new `.lagon` flex-row root replaces the old absolutely-positioned `.app` shell. The center column hosts the existing transparent Phaser canvas over a CSS teal water gradient, with the HUD and action bar overlaid. The right dock is always visible and swaps its body by the active left-rail item; non-Shop panels keep their existing dark-Tailwind look on a dark-teal "compatible" surface (Phase 2 rebrands them). Only the Shop fish cards and the Renouveau modal are restyled to candy in this phase.

**Tech Stack:** React 19, Zustand, Phaser 3, Tailwind v4 (utility classes in panel bodies), hand-written CSS in `src/index.css`. Build: `tsc -b && vite build`. No unit-test framework exists, so each task is verified by a passing type-check/build plus a described visual check in the dev server.

## Global Constraints

- **No game-logic changes.** Do not touch `useGameStore`, `GameLoopManager`, `OfflineManager`, `src/data/*`, or `PondScene`. UI/markup/CSS only.
- **Preserve all existing hook calls and computations** when moving code (manager init, the 1 Hz `now` timer, `computeBonuses`, `computeIncomePerSec`, boost/dig disabled logic).
- **Verification is build + visual** (no unit tests in repo). Every task ends with `npm run build` passing and a commit.
- **Palette (exact hex, from the mockup):** teal `#16C8C4`→`#0E9CAB`, ink `#0B5566`, ink-soft `#5A8A92`, coral `#FF8A63`→`#FF6B3D` (shadow `#D24E27`), gold `#FFD56A`→`#FFB72E` (shadow `#E59412`, text `#7A4E00`), violet `#8C6BFF`→`#6B47F0` (shadow `#4F30C2`), mana `#7C5CFF`, gemme `#3FC97E`, perle `#FF7AB8`, surface `#EFF9FA`.
- **Fonts:** Baloo 2 (display/numbers), Nunito (body).
- **Branch:** all work on `feat/lagon-vif-desktop-shell` (already checked out).

## File Structure

| File | Responsibility |
|------|----------------|
| `index.html` | Load Baloo 2 + Nunito |
| `src/index.css` | Candy tokens, fonts, `.cbtn` primitive, `.lagon` shell/rail/dock/HUD/action-bar classes, candy Shop-card classes, candy modal, < 1024px fallback |
| `src/App.tsx` | New three-column shell: `LeftRail`, `TopHUD`, `BottomActionBar`, `RightDock`, candy `PrestigeModal`; persistent-dock nav state |
| `src/components/Shop.tsx` | Candy `FishCard` (legendary/owned/buyable variants) |
| `src/components/PhaserContainer.tsx` | One-line: drop `bg-gray-900` so the teal gradient shows through the transparent canvas |

---

### Task 1: Aesthetic foundation — fonts, tokens, candy button

Adds the new fonts and the candy design tokens + `.cbtn` button primitive. Additions only — no layout change yet, so the app still renders as before but with the new typography.

**Files:**
- Modify: `index.html` (font link, line 11 area)
- Modify: `src/index.css` (`:root` font vars near top; append candy section at end)

**Interfaces:**
- Produces (CSS classes/tokens later tasks consume): `.cbtn`, `.cbtn--coral`, `.cbtn--gold`, `.cbtn--violet`, `.cbtn--ghost`; the `--lg-*` custom properties scoped to `.lagon` (defined in Task 2 but the `.cbtn` rules use literal hex so they work standalone).

- [ ] **Step 1: Add the fonts to `index.html`**

Add this line directly after the existing Google Fonts `<link ... rel="stylesheet" />` (line 11):

```html
    <link href="https://fonts.googleapis.com/css2?family=Baloo+2:wght@500;600;700;800&family=Nunito:wght@600;700;800;900&display=swap" rel="stylesheet" />
```

- [ ] **Step 2: Point the font tokens at the new families**

In `src/index.css`, replace these two lines inside `:root` (currently lines 20-21):

```css
  --font-display: "Newsreader", "Spectral", Georgia, serif;
  --font-ui:      "Inter Tight", "Inter", system-ui, sans-serif;
```

with:

```css
  --font-display: "Baloo 2", "Newsreader", Georgia, serif;
  --font-ui:      "Nunito", "Inter Tight", system-ui, sans-serif;
```

- [ ] **Step 3: Append the candy tokens + button primitive**

Append to the **end** of `src/index.css`:

```css
/* ════════════════════════════════════════════════════════════
   LAGON VIF — candy desktop shell
   ════════════════════════════════════════════════════════════ */

/* ─── Candy button primitive ───────────────────────────────── */
.cbtn {
  border: 0;
  cursor: pointer;
  font-family: "Baloo 2", sans-serif;
  font-weight: 800;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  border-radius: 14px;
  padding: 12px 16px;
  color: #fff;
  line-height: 1.05;
  transition: transform 0.1s ease, filter 0.15s ease;
}
.cbtn:hover:not(:disabled) { transform: translateY(-1px); filter: brightness(1.04); }
.cbtn:active:not(:disabled) { transform: translateY(2px); box-shadow: none !important; }
.cbtn:disabled { opacity: 0.5; cursor: not-allowed; filter: grayscale(0.3); }

.cbtn--coral  { background: linear-gradient(180deg,#FF8A63,#FF6B3D); box-shadow: 0 4px 0 #D24E27; }
.cbtn--gold   { background: linear-gradient(180deg,#FFD56A,#FFB72E); box-shadow: 0 4px 0 #E59412; color: #7A4E00; }
.cbtn--violet { background: linear-gradient(180deg,#8C6BFF,#6B47F0); box-shadow: 0 4px 0 #4F30C2; }
.cbtn--ghost  { background: #EDF3F4; box-shadow: 0 4px 0 #D4E4E6; color: #5A8A92; }
```

- [ ] **Step 4: Verify the build passes**

Run: `npm run build`
Expected: completes with no errors (exit 0). Tailwind + tsc succeed.

- [ ] **Step 5: Commit**

```bash
git add index.html src/index.css
git commit -m "feat(lagon): add Baloo 2/Nunito fonts and candy button primitive"
```

---

### Task 2: Three-column desktop shell

Replaces the old `.app` shell with the `.lagon` three-column layout: left rail, center pond (gradient + transparent Phaser + candy HUD + candy action bar), and an always-visible right dock that swaps its body by active rail item. All panels (including Shop) render on the dark-teal compatible surface for now — Task 3 flips Shop to the light surface together with the candy cards, so every intermediate state stays legible.

**Files:**
- Modify: `src/App.tsx` (full rewrite of the shell; keep imports, `DEPTH_NAMES`, manager init, notifications)
- Modify: `src/components/PhaserContainer.tsx:60` (remove `bg-gray-900`)
- Modify: `src/index.css` (append shell classes)

**Interfaces:**
- Consumes: `useGameStore`, `computeBonuses`, `computeIncomePerSec`, `formatNumber`, `getPondUpgradeCost`, `calcPrestigeReward`, `GameLoopManager`, `OfflineManager`, `PhaserContainer`, and the panel components — all unchanged signatures.
- Produces: `PanelId` type, `RAIL_ITEMS`, `PANEL_META`, `TABS_BY_PANEL`, and the components `LeftRail`, `TopHUD`, `BottomActionBar`, `RightDock`, `PanelBody`, `PrestigeModal` (PrestigeModal is restyled in Task 4 — keep the existing implementation here so the build stays green).

- [ ] **Step 1: Remove the opaque Phaser background**

In `src/components/PhaserContainer.tsx`, change line 60 from:

```tsx
      className="absolute top-0 left-0 w-full h-full z-0 overflow-hidden bg-gray-900"
```

to:

```tsx
      className="absolute top-0 left-0 w-full h-full z-0 overflow-hidden"
```

- [ ] **Step 2: Rewrite `src/App.tsx`**

Replace the **entire** contents of `src/App.tsx` with:

```tsx
import { useEffect, useMemo, useState } from 'react';
import {
  useGameStore,
  calcPrestigeReward,
  getPondUpgradeCost,
} from './store/useGameStore';
import { computeBonuses } from './utils/bonuses';
import { computeIncomePerSec } from './utils/incomeCalc';
import { formatNumber } from './utils/formatNumber';
import { GameLoopManager } from './managers/GameLoopManager';
import { OfflineManager } from './managers/OfflineManager';
import { PhaserContainer } from './components/PhaserContainer';
import { Shop } from './components/Shop';
import { Research } from './components/Research';
import { Ameliorations } from './components/Ameliorations';
import { PearlMarket } from './components/PearlMarket';
import { PrestigeUpgrades } from './components/PrestigeUpgrades';
import { Achievements } from './components/Achievements';
import { Challenges } from './components/Challenges';
import { Stats } from './components/Stats';
import { Lore } from './components/Lore';
import { Guide } from './components/Guide';
import { EventNotification } from './components/EventNotification';
import { UnlockNotification } from './components/UnlockNotification';
import { WelcomeBackNotification } from './components/WelcomeBackNotification';

// ─── Constants ────────────────────────────────────────────────

type PanelId = 'shop' | 'research' | 'pearls' | 'profile' | 'journal';

const RAIL_ITEMS: { id: PanelId; label: string; icon: string }[] = [
  { id: 'shop',     label: 'Boutique', icon: '𓆝' },
  { id: 'research', label: 'Corail',   icon: '❋' },
  { id: 'pearls',   label: 'Perles',   icon: '○' },
  { id: 'profile',  label: 'Profil',   icon: '◉' },
  { id: 'journal',  label: 'Journal',  icon: '✎' },
];

const PANEL_META: Record<PanelId, { title: string; sub: string }> = {
  shop:     { title: 'Boutique',           sub: "Achetez et élevez les habitants de l'étang" },
  research: { title: 'Corail de Prestige', sub: 'Bonus permanents payés en gemmes' },
  pearls:   { title: 'Marché des Perles',  sub: 'Nacre échangée contre ce qui perdure' },
  profile:  { title: 'Profil',             sub: "Votre parcours dans l'Étang des Merveilles" },
  journal:  { title: 'Journal',            sub: 'Pages et créatures des profondeurs' },
};

const TABS_BY_PANEL: Partial<Record<PanelId, { id: string; label: string }[]>> = {
  shop:    [{ id: 'fish', label: 'Poissons' }, { id: 'upgrades', label: 'Améliorations' }],
  pearls:  [{ id: 'market', label: 'Marché' }, { id: 'prestige', label: 'Prestige' }],
  journal: [{ id: 'lore', label: 'Lore' }, { id: 'guide', label: 'Guide' }],
};

const DEPTH_NAMES = [
  'Lac de Surface', 'Rivière Souterraine', 'Récif Corallien', 'Océan des Profondeurs',
  'Abysses', 'Zone Hydrothermale', 'Plaine Abyssale', 'Fosse des Origines',
  'Nexus de Mana', 'Cœur Volcanique', 'Royaume Céleste', 'Dimension Quantique',
];

// ─── Left rail ────────────────────────────────────────────────

function LeftRail({ active, setActive, onPrestige, prestigeReady }: {
  active: PanelId;
  setActive: (id: PanelId) => void;
  onPrestige: () => void;
  prestigeReady: boolean;
}) {
  return (
    <div className="lg-rail">
      <div className="lg-rail-logo">𓆟</div>
      <div className="lg-rail-sep" />
      <div className="lg-rail-nav">
        {RAIL_ITEMS.map(item => (
          <button
            key={item.id}
            className={`lg-rail-btn${active === item.id ? ' active' : ''}`}
            onClick={() => setActive(item.id)}
          >
            <span className="ic">{item.icon}</span>
            <span className="lb">{item.label}</span>
          </button>
        ))}
      </div>
      <button
        className={`lg-rail-renew${prestigeReady ? ' ready' : ''}`}
        onClick={onPrestige}
      >
        <span className="ic">𓆟</span>
        <span className="lb">Renew</span>
      </button>
    </div>
  );
}

// ─── Top HUD ──────────────────────────────────────────────────

function TopHUD() {
  const mana      = useGameStore(s => s.mana);
  const gemmes    = useGameStore(s => s.gemmes);
  const perles    = useGameStore(s => s.perles);
  const pondDepth = useGameStore(s => s.pondDepth);

  return (
    <div className="lg-hud">
      <div className="lg-depth-chip">
        <span className="num">{pondDepth}</span>
        <span className="nm">{DEPTH_NAMES[pondDepth] ?? 'Dimension Quantique'}</span>
      </div>
      <div className="lg-res-group">
        <div className="lg-res mana">
          <div className="coin" />
          <div><div className="lab">MANA</div><div className="val">{formatNumber(mana)}</div></div>
        </div>
        <div className="lg-res gemme">
          <div className="coin" />
          <div><div className="lab">GEMMES</div><div className="val">{gemmes}</div></div>
        </div>
        {perles > 0 && (
          <div className="lg-res perle">
            <div className="coin" />
            <div><div className="lab">PERLES</div><div className="val">{perles}</div></div>
          </div>
        )}
      </div>
    </div>
  );
}

// ─── Bottom action bar ────────────────────────────────────────

function BottomActionBar() {
  const poissons                 = useGameStore(s => s.poissons);
  const mana                     = useGameStore(s => s.mana);
  const gemmes                   = useGameStore(s => s.gemmes);
  const pondDepth                = useGameStore(s => s.pondDepth);
  const boostActiveUntil         = useGameStore(s => s.boostActiveUntil);
  const researchUnlocked         = useGameStore(s => s.researchUnlocked);
  const pearlUpgradesUnlocked    = useGameStore(s => s.pearlUpgradesUnlocked);
  const prestigeUpgradesUnlocked = useGameStore(s => s.prestigeUpgradesUnlocked);
  const runUpgradesOwned         = useGameStore(s => s.runUpgradesOwned);
  const upgradePond              = useGameStore(s => s.upgradePond);
  const activateBoost            = useGameStore(s => s.activateBoost);

  const bonuses = useMemo(
    () => computeBonuses(researchUnlocked, pearlUpgradesUnlocked, prestigeUpgradesUnlocked, runUpgradesOwned),
    [researchUnlocked, pearlUpgradesUnlocked, prestigeUpgradesUnlocked, runUpgradesOwned]
  );

  const incomePerSec = computeIncomePerSec(poissons, bonuses, boostActiveUntil);

  const pondCost = getPondUpgradeCost(pondDepth);
  const canDig   = pondDepth < 11 && mana.gte(pondCost);
  const progress = pondDepth < 11 ? Math.min(mana.div(pondCost).toNumber(), 1) : 1;

  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, []);
  const boosted   = boostActiveUntil > now;
  const boostLeft = boosted ? Math.max(0, Math.ceil((boostActiveUntil - now) / 1000)) : 0;

  const totalFish = poissons.length;
  const species   = new Set(poissons.map(f => f.type)).size;

  return (
    <div className="lg-actionbar">
      <div className="lg-ab-revenue">
        <div className="lab">REVENU</div>
        <div className="val">{formatNumber(incomePerSec)}<span className="per">/s</span></div>
        {boosted && <div className="boost">boost ×{bonuses.boostMultiplier} · {boostLeft}s</div>}
      </div>
      <div className="lg-ab-fish">
        <div className="big">{totalFish}</div>
        <div className="sub">poissons · {species} esp.</div>
      </div>
      <div className="lg-ab-progress">
        <div className="row">
          <span>{pondDepth < 11 ? `PROCHAIN BIOME · ${(DEPTH_NAMES[pondDepth + 1] ?? '').toUpperCase()}` : 'NEXUS ATTEINT'}</span>
          <span>{Math.round(progress * 100)}%</span>
        </div>
        <div className="bar"><i style={{ width: `${progress * 100}%` }} /></div>
      </div>
      <button
        className="cbtn cbtn--gold lg-ab-boost"
        disabled={boosted || gemmes < bonuses.boostCost}
        onClick={activateBoost}
        title={boosted ? 'Boost actif' : `×${bonuses.boostMultiplier} revenus`}
      >
        <span className="ic">⚡</span>
        <span className="tx">{boosted ? `Boost · ${boostLeft}s` : `Boost · ${bonuses.boostCost}💎`}</span>
      </button>
      <button className="cbtn cbtn--coral lg-ab-dig" disabled={!canDig} onClick={upgradePond}>
        <span className="t1">⛏ {pondDepth < 11 ? 'Creuser' : 'Max'}</span>
        <span className="t2">{pondDepth < 11 ? `${formatNumber(pondCost)} mana` : '—'}</span>
      </button>
    </div>
  );
}

// ─── Panel body ───────────────────────────────────────────────

function PanelBody({ panel, tab }: { panel: PanelId; tab: string }) {
  if (panel === 'shop')     return tab === 'fish' ? <Shop /> : <Ameliorations />;
  if (panel === 'research') return <Research />;
  if (panel === 'pearls')   return tab === 'market' ? <PearlMarket /> : <PrestigeUpgrades />;
  if (panel === 'profile')  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16, padding: 16 }}>
      <Stats />
      <Achievements />
      <Challenges />
    </div>
  );
  if (panel === 'journal')  return tab === 'lore' ? <Lore /> : <Guide />;
  return null;
}

// ─── Right dock ───────────────────────────────────────────────

function RightDock({ panel, tab, setTab }: {
  panel: PanelId;
  tab: string;
  setTab: (id: string) => void;
}) {
  const meta = PANEL_META[panel];
  const tabs = TABS_BY_PANEL[panel];
  const isShopFish = panel === 'shop' && tab === 'fish';

  return (
    <div className="lg-dock">
      <div className="lg-dock-head">
        <div className="ttl">{meta.title}</div>
        <div className="sub">{meta.sub}</div>
        {tabs && (
          <div className="lg-dock-tabs">
            {tabs.map(t => (
              <button
                key={t.id}
                className={`lg-dock-tab${tab === t.id ? ' active' : ''}`}
                onClick={() => setTab(t.id)}
              >{t.label}</button>
            ))}
          </div>
        )}
      </div>
      <div className={`lg-dock-body${isShopFish ? ' is-shop' : ''}`}>
        <PanelBody panel={panel} tab={tab} />
      </div>
    </div>
  );
}

// ─── Prestige modal (restyled in Task 4) ──────────────────────

function PrestigeModal({ onClose }: { onClose: () => void }) {
  const manaRunHigh              = useGameStore(s => s.manaRunHigh);
  const pondDepth                = useGameStore(s => s.pondDepth);
  const researchUnlocked         = useGameStore(s => s.researchUnlocked);
  const pearlUpgradesUnlocked    = useGameStore(s => s.pearlUpgradesUnlocked);
  const prestigeUpgradesUnlocked = useGameStore(s => s.prestigeUpgradesUnlocked);
  const runUpgradesOwned         = useGameStore(s => s.runUpgradesOwned);
  const prestige                 = useGameStore(s => s.prestige);

  const bonuses    = computeBonuses(researchUnlocked, pearlUpgradesUnlocked, prestigeUpgradesUnlocked, runUpgradesOwned);
  const baseReward = calcPrestigeReward(manaRunHigh);
  const earned     = Math.ceil(baseReward * bonuses.prestigePearlMult);
  const canConfirm = pondDepth >= 2;

  return (
    <>
      <div className="lg-modal-scrim" onClick={onClose} />
      <div className="lg-modal-card">
        <div className="lg-modal-sigil">𓆟</div>
        <div className="lg-modal-title">Renouveau</div>
        <div className="lg-modal-sub">Réinitialisez votre partie et gagnez des perles</div>
        <div className="lg-modal-reward">
          <div className="lab">Vous gagnerez</div>
          <div className="big">{earned} 🪸</div>
          {bonuses.prestigeKeepFishPercent > 0 && (
            <div className="keep">✦ {bonuses.prestigeKeepFishPercent}% de vos poissons seront conservés</div>
          )}
        </div>
        <div className="lg-modal-note">
          Mana et poissons seront perdus. Recherche, succès et améliorations persistent.
          {!canConfirm && <><br />Profondeur ≥ 2 requise pour renouveler.</>}
        </div>
        <div className="lg-modal-actions">
          <button className="cbtn cbtn--ghost" style={{ flex: 1 }} onClick={onClose}>Annuler</button>
          <button
            className="cbtn cbtn--coral"
            style={{ flex: 1.6 }}
            disabled={!canConfirm}
            onClick={() => { prestige(); onClose(); }}
          >Renouveler</button>
        </div>
      </div>
    </>
  );
}

// ─── Root ─────────────────────────────────────────────────────

export default function App() {
  const [activePanel, setActivePanel] = useState<PanelId>('shop');
  const [panelTab, setPanelTab]       = useState<Record<string, string>>({
    shop: 'fish', pearls: 'market', journal: 'lore',
  });
  const [showPrestige, setShowPrestige] = useState(false);

  useEffect(() => {
    useGameStore.getState().checkDailyReset();
    OfflineManager.getInstance().calculateOfflineGain();
    GameLoopManager.getInstance().start();
    return () => GameLoopManager.getInstance().stop();
  }, []);

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setShowPrestige(false);
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, []);

  const pondDepth     = useGameStore(s => s.pondDepth);
  const prestigeReady = pondDepth >= 2;
  const tabs          = TABS_BY_PANEL[activePanel];
  const currentTab    = panelTab[activePanel] ?? tabs?.[0]?.id ?? '';
  const setTab        = (id: string) => setPanelTab(prev => ({ ...prev, [activePanel]: id }));

  return (
    <div className="lagon">
      <LeftRail
        active={activePanel}
        setActive={setActivePanel}
        onPrestige={() => setShowPrestige(true)}
        prestigeReady={prestigeReady}
      />

      <div className="lg-pond">
        <div className="lg-rays">
          <span className="lg-ray" style={{ left: 120, width: 120, height: 520, transform: 'rotate(16deg)' }} />
          <span className="lg-ray" style={{ left: 420, width: 90, height: 460, transform: 'rotate(-12deg)', animationDelay: '1s' }} />
          <span className="lg-ray" style={{ left: 680, width: 80, height: 420, transform: 'rotate(10deg)', animationDelay: '.5s' }} />
        </div>
        <div className="lg-bubbles">
          <span style={{ left: 160, bottom: 120, width: 10, height: 10 }} />
          <span style={{ left: 380, bottom: 100, width: 7, height: 7, animationDelay: '2s' }} />
          <span style={{ left: 600, bottom: 140, width: 12, height: 12, animationDelay: '3.5s' }} />
          <span style={{ left: 760, bottom: 90, width: 7, height: 7, animationDelay: '1.4s' }} />
        </div>
        <PhaserContainer />
        <TopHUD />
        <BottomActionBar />
      </div>

      <RightDock panel={activePanel} tab={currentTab} setTab={setTab} />

      {showPrestige && <PrestigeModal onClose={() => setShowPrestige(false)} />}

      <EventNotification />
      <UnlockNotification />
      <WelcomeBackNotification />
    </div>
  );
}
```

- [ ] **Step 3: Append the shell CSS**

Append to the **end** of `src/index.css`:

```css
/* ─── Shell tokens + root ──────────────────────────────────── */
.lagon {
  --lg-teal: #16C8C4; --lg-teal-d: #0E9CAB;
  --lg-ink: #0B5566;  --lg-ink-soft: #5A8A92;
  --lg-coral: #FF6B3D; --lg-gold: #FFB72E;
  --lg-surface: #EFF9FA;
  position: relative;
  width: 100%; height: 100%;
  display: flex;
  background: var(--lg-surface);
  font-family: "Nunito", sans-serif;
  overflow: hidden;
  isolation: isolate;
}
.lagon img { user-select: none; -webkit-user-drag: none; }

@keyframes lg-shimmer { 0%,100% { opacity: .2 } 50% { opacity: .55 } }
@keyframes lg-bubble  { 0% { transform: translateY(0); opacity: 0 } 15% { opacity: .6 } 100% { transform: translateY(-260px); opacity: 0 } }

/* ─── Left rail ────────────────────────────────────────────── */
.lg-rail {
  width: 96px; flex-shrink: 0;
  background: #fff;
  box-shadow: 4px 0 24px rgba(11,85,102,.08);
  display: flex; flex-direction: column; align-items: center;
  padding: 18px 0; z-index: 5;
}
.lg-rail-logo {
  width: 52px; height: 52px; border-radius: 16px;
  background: linear-gradient(135deg,#FF8A63,#FFC23C);
  box-shadow: 0 6px 14px rgba(255,122,89,.4);
  display: grid; place-items: center; color: #fff; font-size: 26px;
  font-family: "Baloo 2";
}
.lg-rail-sep { width: 36px; height: 1px; background: #E4F1F3; margin: 18px 0; }
.lg-rail-nav { display: flex; flex-direction: column; gap: 8px; align-items: center; flex: 1; }
.lg-rail-btn {
  display: flex; flex-direction: column; align-items: center; gap: 3px;
  width: 68px; padding: 11px 0; border-radius: 16px;
  background: transparent; color: #8AAEB4;
  transition: background .15s, color .15s;
}
.lg-rail-btn .ic { font-size: 21px; font-family: "Baloo 2"; }
.lg-rail-btn .lb { font-family: "Baloo 2"; font-weight: 700; font-size: 9px; }
.lg-rail-btn:hover { background: #F1FAFB; }
.lg-rail-btn.active {
  background: linear-gradient(180deg,#16C8C4,#0E9CAB);
  box-shadow: 0 6px 14px rgba(14,156,171,.35);
}
.lg-rail-btn.active .lb, .lg-rail-btn.active .ic { color: #fff; }
.lg-rail-renew {
  display: flex; flex-direction: column; align-items: center; gap: 3px;
  width: 68px; padding: 12px 0; border-radius: 16px; cursor: pointer;
  background: #FFF1EC; border: 1.5px solid #FFD3C2; color: #FF6B3D;
}
.lg-rail-renew .ic { font-size: 21px; font-family: "Baloo 2"; }
.lg-rail-renew .lb { font-family: "Baloo 2"; font-weight: 800; font-size: 9px; }
.lg-rail-renew.ready { animation: boost-pulse 2.5s ease-in-out infinite; }

/* ─── Center pond ──────────────────────────────────────────── */
.lg-pond {
  flex: 1; position: relative; overflow: hidden;
  background: linear-gradient(180deg,#D6F4F8 0%,#9FE6EF 26%,#4FBDD2 62%,#1E8AA8 100%);
}
.lg-rays { position: absolute; inset: 0; pointer-events: none; z-index: 1; }
.lg-ray {
  position: absolute; top: 0;
  background: linear-gradient(180deg,rgba(255,255,255,.46),transparent);
  filter: blur(13px);
  animation: lg-shimmer 7s ease-in-out infinite;
}
.lg-bubbles { position: absolute; inset: 0; pointer-events: none; z-index: 1; }
.lg-bubbles span {
  position: absolute; border-radius: 50%;
  background: rgba(255,255,255,.65);
  animation: lg-bubble 10s ease-in infinite;
}

/* ─── Top HUD ──────────────────────────────────────────────── */
.lg-hud {
  position: absolute; top: 0; left: 0; right: 0; z-index: 4;
  display: flex; align-items: flex-start; justify-content: space-between;
  padding: 20px 24px;
}
.lg-depth-chip {
  display: flex; align-items: center; gap: 8px;
  background: rgba(255,255,255,.8); padding: 9px 15px; border-radius: 16px;
  box-shadow: 0 4px 12px rgba(11,85,102,.12);
}
.lg-depth-chip .num { font-family: "Baloo 2"; font-weight: 800; font-size: 20px; color: #15A0AE; }
.lg-depth-chip .nm  { font-family: "Nunito"; font-weight: 800; font-size: 11px; color: #0B5566; line-height: 1.1; max-width: 120px; }
.lg-res-group { display: flex; gap: 10px; }
.lg-res {
  display: flex; align-items: center; gap: 9px;
  background: rgba(255,255,255,.85); padding: 9px 14px; border-radius: 16px;
  box-shadow: 0 4px 12px rgba(11,85,102,.12);
}
.lg-res .coin { width: 24px; height: 24px; flex-shrink: 0; }
.lg-res .lab { font-family: "Nunito"; font-weight: 800; font-size: 9px; letter-spacing: .04em; }
.lg-res .val { font-family: "Baloo 2"; font-weight: 800; font-size: 18px; line-height: 1; }
.lg-res.mana  .coin { border-radius: 50%; background: radial-gradient(circle at 35% 30%,#B9A6FF,#7C5CFF); box-shadow: 0 0 10px rgba(124,92,255,.5); }
.lg-res.mana  .lab { color: #8C7BC0; } .lg-res.mana .val { color: #3A2E78; }
.lg-res.gemme .coin { width: 22px; height: 22px; border-radius: 6px; transform: rotate(45deg); background: radial-gradient(circle at 35% 30%,#9CF5C4,#3FC97E); box-shadow: 0 0 10px rgba(63,201,126,.5); }
.lg-res.gemme .lab { color: #3FA570; } .lg-res.gemme .val { color: #1B6B43; }
.lg-res.perle .coin { width: 22px; height: 22px; border-radius: 50%; background: radial-gradient(circle at 35% 30%,#FFD0E6,#FF7AB8); box-shadow: 0 0 10px rgba(255,122,184,.5); }
.lg-res.perle .lab { color: #D86AA0; } .lg-res.perle .val { color: #A83A72; }

/* ─── Bottom action bar ────────────────────────────────────── */
.lg-actionbar {
  position: absolute; left: 24px; right: 24px; bottom: 22px; z-index: 4;
  background: rgba(255,255,255,.92); border-radius: 24px;
  padding: 16px 20px; box-shadow: 0 12px 30px rgba(11,85,102,.22);
  backdrop-filter: blur(8px);
  display: flex; align-items: center; gap: 22px;
}
.lg-ab-revenue .lab { font-family: "Nunito"; font-weight: 800; font-size: 10px; color: #15A0AE; letter-spacing: .06em; }
.lg-ab-revenue .val { font-family: "Baloo 2"; font-weight: 800; font-size: 30px; color: #0B5566; line-height: 1; }
.lg-ab-revenue .per { font-size: 15px; color: #7AB; }
.lg-ab-revenue .boost { font-family: "Nunito"; font-weight: 800; font-size: 11px; color: #E59412; }
.lg-ab-fish { text-align: center; padding: 0 22px; border-left: 1px solid #DCEFF2; border-right: 1px solid #DCEFF2; }
.lg-ab-fish .big { font-family: "Baloo 2"; font-weight: 800; font-size: 24px; color: #0B5566; line-height: 1; }
.lg-ab-fish .sub { font-family: "Nunito"; font-weight: 700; font-size: 11px; color: #7AB; }
.lg-ab-progress { flex: 1; }
.lg-ab-progress .row { display: flex; justify-content: space-between; font-family: "Nunito"; font-weight: 800; font-size: 10px; color: #7AB; margin-bottom: 6px; }
.lg-ab-progress .bar { height: 10px; border-radius: 6px; background: #E4F1F3; overflow: hidden; }
.lg-ab-progress .bar > i { display: block; height: 100%; border-radius: 6px; background: linear-gradient(90deg,#16C8C4,#3FC97E); transition: width .4s ease; }
.lg-ab-boost { flex-direction: column; gap: 1px; padding: 12px 18px; }
.lg-ab-boost .ic { font-size: 20px; }
.lg-ab-boost .tx { font-size: 11px; }
.lg-ab-dig { flex-direction: column; gap: 1px; padding: 15px 26px; }
.lg-ab-dig .t1 { font-size: 15px; }
.lg-ab-dig .t2 { font-family: "Nunito"; font-weight: 800; font-size: 11px; opacity: .92; }

/* ─── Right dock ───────────────────────────────────────────── */
.lg-dock {
  width: 392px; flex-shrink: 0;
  background: var(--lg-surface);
  box-shadow: -6px 0 28px rgba(11,85,102,.1);
  display: flex; flex-direction: column; z-index: 5;
}
.lg-dock-head { padding: 22px 22px 16px; background: linear-gradient(180deg,#16C8C4,#0E9CAB); }
.lg-dock-head .ttl { font-family: "Baloo 2"; font-weight: 800; font-size: 26px; color: #fff; }
.lg-dock-head .sub { font-family: "Nunito"; font-weight: 700; font-size: 12px; color: rgba(255,255,255,.85); }
.lg-dock-tabs { display: flex; gap: 8px; margin-top: 14px; }
.lg-dock-tab {
  padding: 8px 20px; border-radius: 13px; cursor: pointer;
  background: rgba(255,255,255,.22); color: #fff;
  font-family: "Baloo 2"; font-weight: 700; font-size: 13px;
}
.lg-dock-tab.active { background: #fff; color: #0E9CAB; font-weight: 800; }
.lg-dock-body { flex: 1; overflow-y: auto; padding: 16px; }
.lg-dock-body.is-shop { background: var(--lg-surface); }
/* Compatible dark-teal surface for not-yet-rebranded panels (Phase 2) */
.lg-dock-body:not(.is-shop) { background: #0B3D47; }
```

- [ ] **Step 4: Verify the build passes**

Run: `npm run build`
Expected: completes with no errors (exit 0).

- [ ] **Step 5: Visual smoke check**

Run: `npm run dev`, open the served URL at ≥1280px width. Confirm:
- Three columns: white rail (left), teal-gradient pond with Phaser fish swimming over it (center), dock (right).
- Top HUD shows depth chip + mana/gemmes pills; bottom action bar shows REVENU / fish count / progress / Boost / Creuser candy buttons.
- Clicking rail items (Corail, Perles, Profil, Journal) swaps the dock body; those panels render legibly on the dark surface. Boutique shows the (still-dark) Shop on the dark surface for now.
- Clicking **Renew** opens the prestige modal; Escape closes it.

- [ ] **Step 6: Commit**

```bash
git add src/App.tsx src/index.css src/components/PhaserContainer.tsx
git commit -m "feat(lagon): three-column desktop shell with rail, pond HUD, and right dock"
```

---

### Task 3: Candy Shop fish cards

Rebuilds `FishCard` to the mockup's three candy variants and flips the Shop dock body to the light surface (already wired in Task 2 via `is-shop`). Game logic (buy/upgrade/cost/milestone/quantity selector/locked rows) is untouched.

**Files:**
- Modify: `src/components/Shop.tsx` (the `Shop` wrapper's outer markup + the `FishCard` render JSX; keep all calculation code)
- Modify: `src/index.css` (append Shop-card classes)

**Interfaces:**
- Consumes: `.cbtn` variants (Task 1); the `.lg-dock-body.is-shop` light surface (Task 2).
- Produces: nothing other tasks depend on.

- [ ] **Step 1: Append the Shop-card CSS**

Append to the **end** of `src/index.css`:

```css
/* ─── Candy Shop cards ─────────────────────────────────────── */
.lg-shop { display: flex; flex-direction: column; gap: 13px; }
.lg-shop-section { display: flex; flex-direction: column; gap: 13px; }
.lg-shop-eyebrow { font-family: "Baloo 2"; font-weight: 800; font-size: 9px; letter-spacing: .12em; color: #8AA; }

.lg-fish {
  background: #fff; border-radius: 20px; padding: 15px;
  box-shadow: 0 6px 16px rgba(11,85,102,.1);
  border: 2px solid transparent;
}
.lg-fish--legend {
  background: linear-gradient(180deg,#FFF8E8,#FFF1CF);
  border-color: #FFC23C; box-shadow: 0 6px 16px rgba(229,148,18,.15);
}
.lg-fish--owned { border-color: #16C8C4; }
.lg-fish--locked { opacity: .55; }
.lg-fish-legend-eyebrow { font-family: "Baloo 2"; font-weight: 800; font-size: 9px; color: #C98A0F; letter-spacing: .12em; margin-bottom: 9px; }

.lg-fish-head { display: flex; align-items: center; gap: 13px; }
.lg-fish-sprite {
  width: 58px; height: 58px; border-radius: 16px; flex-shrink: 0;
  background: linear-gradient(135deg,#E6FBFB,#C3F0F2);
  display: grid; place-items: center;
}
.lg-fish--legend .lg-fish-sprite { background: #fff; box-shadow: 0 3px 8px rgba(229,148,18,.18); }
.lg-fish-sprite img { width: 48px; object-fit: contain; }
.lg-fish-info { flex: 1; min-width: 0; }
.lg-fish-row { display: flex; justify-content: space-between; align-items: center; gap: 8px; }
.lg-fish-name { font-family: "Baloo 2"; font-weight: 800; font-size: 15px; color: #0B5566; }
.lg-fish--legend .lg-fish-name { color: #8A5A0A; }
.lg-fish-badge { font-family: "Baloo 2"; font-weight: 800; font-size: 9px; color: #fff; background: #16C8C4; padding: 3px 9px; border-radius: 8px; flex-shrink: 0; }
.lg-fish--legend .lg-fish-badge { background: #E5A50F; }
.lg-fish-income { font-family: "Baloo 2"; font-weight: 800; font-size: 13px; color: #3FC97E; }
.lg-fish-desc { font-family: "Nunito"; font-weight: 600; font-size: 11px; color: #9AB; line-height: 1.3; }

.lg-fish-miles { display: flex; gap: 5px; margin: 11px 0 9px; }
.lg-fish-chip {
  flex: 1; text-align: center; padding: 4px; border-radius: 8px;
  font-family: "Baloo 2"; font-weight: 800; font-size: 10px;
  background: #EEF4F5; color: #BCC;
}
.lg-fish-chip.hit  { background: #FFE9A8; color: #A87A12; }
.lg-fish-chip.next { background: #E4F1F3; color: #16C8C4; border: 1.5px dashed #16C8C4; }

.lg-fish .cbtn { width: 100%; margin-top: 12px; }
.lg-fish-qty { display: flex; gap: 5px; margin-bottom: 9px; }
.lg-fish-qty button {
  flex: 1; padding: 5px 0; border-radius: 8px; cursor: pointer;
  font-family: "Baloo 2"; font-weight: 800; font-size: 10px;
  background: #EEF4F5; color: #7AA1A8; border: 1.5px solid transparent;
}
.lg-fish-qty button.active { background: #6B47F0; color: #fff; }
.lg-fish-max { text-align: center; font-family: "Nunito"; font-weight: 700; font-size: 11px; color: #C98A0F; padding: 10px; background: #FFF8E8; border-radius: 12px; margin-top: 12px; }
.lg-fish-locked-note { font-family: "Nunito"; font-weight: 600; font-size: 11px; color: #9AB; margin-top: 8px; background: #F1FAFB; border-radius: 10px; padding: 8px 10px; }
```

- [ ] **Step 2: Replace the `Shop` wrapper markup**

In `src/components/Shop.tsx`, replace the `return (...)` block of the `Shop` component (currently lines 75-151, the `<div className="flex flex-col gap-6 ...">` … `</div>`) with:

```tsx
  return (
    <div className="lg-shop">
      {/* Poissons légendaires */}
      {legendaryFish.map(fish => {
        const unlocked = prestiges >= (fish.requiredPrestiges ?? 0) && pondDepth >= fish.requiredDepth;
        if (!unlocked) return null;
        const instance = poissons.find(f => f.type === fish.type) ?? null;
        const atMax = instance !== null && fish.maxOwned !== undefined;
        const buyCost = new Decimal(fish.baseCost).mul(bonuses.celestialCostMult);
        const canAffordBuy = !atMax && mana.gte(buyCost);

        return (
          <FishCard
            key={fish.type}
            fish={fish}
            instance={instance}
            mana={mana}
            bonuses={bonuses}
            onBuy={() => handleBuy(fish)}
            onUpgrade={(qty) => instance && handleUpgrade(fish, instance, qty)}
            canAffordBuy={canAffordBuy}
            buyCost={buyCost}
            variant="legendary"
          />
        );
      })}

      {/* Poissons normaux */}
      <div className="lg-shop-section">
        {normalFish.map(fish => {
          const unlocked = pondDepth >= fish.requiredDepth;
          const instance = poissons.find(f => f.type === fish.type) ?? null;
          const buyCost = new Decimal(fish.baseCost).mul(bonuses.fishCostMult);
          const canAffordBuy = instance === null && mana.gte(buyCost);

          if (!unlocked) {
            return (
              <div key={fish.type} className="lg-fish lg-fish--locked">
                <div className="lg-fish-head">
                  <div className="lg-fish-sprite">
                    {fish.sprite
                      ? <img src={fish.sprite} alt={fish.name} style={{ filter: 'grayscale(1)' }} />
                      : <span style={{ fontSize: 28, filter: 'grayscale(1)' }}>{fish.emoji}</span>}
                  </div>
                  <div className="lg-fish-info">
                    <div className="lg-fish-name">{fish.name} 🔒</div>
                    <div className="lg-fish-locked-note">
                      Profondeur Niv. {fish.requiredDepth} requise — {DEPTH_NAMES[fish.requiredDepth]}
                    </div>
                  </div>
                </div>
              </div>
            );
          }

          return (
            <FishCard
              key={fish.type}
              fish={fish}
              instance={instance}
              mana={mana}
              bonuses={bonuses}
              onBuy={() => handleBuy(fish)}
              onUpgrade={(qty) => instance && handleUpgrade(fish, instance, qty)}
              canAffordBuy={canAffordBuy}
              buyCost={buyCost}
              variant="normal"
            />
          );
        })}
      </div>
    </div>
  );
```

- [ ] **Step 3: Replace the `FishCard` render block**

In `src/components/Shop.tsx`, replace the `return (...)` block of `FishCard` (currently lines 209-328, the `<div className={...bgClass...}>` … closing `</div>`) with the candy markup below. **Keep everything above the return** (the `useState`, `owned`, `levelMult`, milestone, `maxN`, `upgradeCount`, `canAffordUpgrade`, `nextMilestone` calculations) exactly as-is. Delete the now-unused `borderClass`/`bgClass` consts.

```tsx
  const cardClass = variant === 'legendary'
    ? 'lg-fish lg-fish--legend'
    : owned ? 'lg-fish lg-fish--owned' : 'lg-fish';

  return (
    <div className={cardClass}>
      {variant === 'legendary' && (
        <div className="lg-fish-legend-eyebrow">★ POISSON LÉGENDAIRE</div>
      )}

      <div className="lg-fish-head">
        <div className="lg-fish-sprite">
          {fish.sprite
            ? <img src={fish.sprite} alt={fish.name} />
            : <span style={{ fontSize: 28 }}>{fish.emoji}</span>}
        </div>
        <div className="lg-fish-info">
          <div className="lg-fish-row">
            <span className="lg-fish-name">{fish.name}</span>
            {owned && (
              <span className="lg-fish-badge">{isMaxLevel ? 'MAX' : `NIV. ${instance.level}`}</span>
            )}
          </div>
          {owned
            ? <div className="lg-fish-income">{formatNumber(currentIncome)} Mana/s</div>
            : <div className="lg-fish-desc">{fish.desc}</div>}
        </div>
      </div>

      {owned && (
        <div className="lg-fish-miles">
          {MILESTONE_LEVELS.map(lvl => {
            const milestone = fish.milestones.find(m => m.level === lvl);
            const effectiveLevel = Math.max(1, lvl - bonuses.milestoneLevelReduction);
            const reached = instance.level >= effectiveLevel;
            const isNext = nextMilestone?.level === lvl;
            return (
              <div
                key={lvl}
                className={`lg-fish-chip${reached ? ' hit' : isNext ? ' next' : ''}`}
                title={milestone
                  ? `Niv. ${effectiveLevel} — ${milestone.label} (${milestoneBonus(milestone.selfMultiplier, milestone.globalBonus)})`
                  : `Niv. ${effectiveLevel}`}
              >{effectiveLevel}</div>
            );
          })}
        </div>
      )}

      {!owned ? (
        <button className="cbtn cbtn--coral" onClick={onBuy} disabled={!canAffordBuy}>
          Acheter — {formatNumber(buyCost)} Mana
        </button>
      ) : isMaxLevel ? (
        <div className="lg-fish-max">✦ Niveau maximum atteint</div>
      ) : (
        <>
          <div className="lg-fish-qty">
            {UPGRADE_QTYS.map(q => (
              <button
                key={q}
                className={upgradeQty === q ? 'active' : ''}
                onClick={() => setUpgradeQty(q)}
              >{q === 'max' ? 'Max' : `×${q}`}</button>
            ))}
          </div>
          <button
            className={`cbtn ${variant === 'legendary' ? 'cbtn--gold' : 'cbtn--violet'}`}
            onClick={() => onUpgrade(upgradeQty)}
            disabled={!canAffordUpgrade}
          >
            {canAffordUpgrade
              ? <>Améliorer {upgradeQty === 'max' ? `×${maxN}` : upgradeCount > 0 ? `×${upgradeCount}` : ''} — {formatNumber(upgradeCostTotal)} Mana</>
              : maxN === 0 ? 'Mana insuffisante' : `Améliorer — ${formatNumber(upgradeCostTotal)} Mana`}
          </button>
        </>
      )}
    </div>
  );
```

- [ ] **Step 4: Verify the build passes**

Run: `npm run build`
Expected: completes with no errors (exit 0). If tsc flags an unused `borderClass`/`bgClass`, confirm both consts were deleted in Step 3.

- [ ] **Step 5: Visual check**

`npm run dev`, open the app, click **Boutique** (Poissons tab). Confirm:
- Cards are light/candy on the light dock surface; owned cards show teal `NIV.` badge + green income + milestone chips; buyable cards show coral "Acheter"; the legendary card (once unlocked) is gold.
- Buy a fish and upgrade it — costs deduct and level rises (logic still works). The ×1/×5/×10/×25/Max selector toggles.

- [ ] **Step 6: Commit**

```bash
git add src/components/Shop.tsx src/index.css
git commit -m "feat(lagon): candy Shop fish cards (legendary/owned/buyable)"
```

---

### Task 4: Candy Renouveau modal

Restyles the prestige modal to the mockup. The `PrestigeModal` markup was already written in Task 2; this task only adds its CSS (it currently renders unstyled-ish because the `.lg-modal-*` classes don't exist yet). Splitting it here keeps Task 2's diff focused on layout.

**Files:**
- Modify: `src/index.css` (append modal classes)

**Interfaces:**
- Consumes: the `PrestigeModal` markup + `.cbtn` variants already in place.

- [ ] **Step 1: Append the modal CSS**

Append to the **end** of `src/index.css`:

```css
/* ─── Candy Renouveau modal ────────────────────────────────── */
.lg-modal-scrim {
  position: absolute; inset: 0; z-index: 30;
  background: rgba(8,60,72,.45); backdrop-filter: blur(3px);
  animation: scrim-in .2s ease;
}
.lg-modal-card {
  position: absolute; z-index: 31;
  top: 50%; left: 50%; transform: translate(-50%,-50%);
  width: 420px; max-width: calc(100vw - 48px);
  background: #fff; border-radius: 28px; padding: 28px;
  box-shadow: 0 24px 60px rgba(0,0,0,.32); text-align: center;
  animation: panel-in .3s cubic-bezier(.3,.8,.2,1);
}
.lg-modal-sigil {
  width: 74px; height: 74px; border-radius: 50%; margin: 0 auto 14px;
  background: linear-gradient(135deg,#FF8A63,#FFC23C);
  display: grid; place-items: center; font-size: 34px; color: #fff;
  font-family: "Baloo 2"; box-shadow: 0 10px 24px rgba(255,122,89,.42);
}
.lg-modal-title { font-family: "Baloo 2"; font-weight: 800; font-size: 26px; color: #0B5566; }
.lg-modal-sub { font-family: "Nunito"; font-weight: 700; font-size: 12px; color: #7AB; margin-top: 2px; }
.lg-modal-reward {
  background: linear-gradient(135deg,#FFF3F8,#FDE6F0);
  border-radius: 20px; padding: 18px; margin: 18px 0; border: 1.5px solid #FFC7E0;
}
.lg-modal-reward .lab { font-family: "Nunito"; font-weight: 700; font-size: 12px; color: #C13E81; }
.lg-modal-reward .big { font-family: "Baloo 2"; font-weight: 800; font-size: 38px; color: #C13E81; line-height: 1.1; }
.lg-modal-reward .keep { font-family: "Nunito"; font-weight: 700; font-size: 11px; color: #D86AA0; margin-top: 4px; }
.lg-modal-note { font-family: "Nunito"; font-weight: 600; font-size: 11px; color: #9AB; line-height: 1.45; margin-bottom: 18px; }
.lg-modal-actions { display: flex; gap: 11px; }
.lg-modal-actions .cbtn { padding: 14px; border-radius: 16px; font-size: 14px; }
```

- [ ] **Step 2: Verify the build passes**

Run: `npm run build`
Expected: completes with no errors (exit 0).

- [ ] **Step 3: Visual check**

`npm run dev`, click **Renew** in the rail. Confirm the modal matches the mockup: teal blurred backdrop, white rounded card, coral sigil, "Renouveau" title, pink reward box with `N 🪸`, Annuler (ghost) + Renouveler (coral) buttons. With pond depth < 2 the Renouveler button is disabled and the depth note shows; Escape and Annuler close it.

- [ ] **Step 4: Commit**

```bash
git add src/index.css
git commit -m "feat(lagon): candy Renouveau modal styling"
```

---

### Task 5: Responsive fallback (< 1024px)

Below 1024px the three-column shell collapses: the rail becomes a bottom tab bar, the right dock becomes a slide-over overlay toggled by the tabs, and the action bar wraps. Keeps the app usable on narrow widths with the new tokens.

**Files:**
- Modify: `src/App.tsx` (add `dockOpenMobile` state, a `MobileTabs` component, a dock scrim, and an `is-open` class hook on the dock)
- Modify: `src/index.css` (append the media query)

**Interfaces:**
- Consumes: `RAIL_ITEMS`, `PanelId`, `activePanel`/`setActivePanel` from Task 2.

- [ ] **Step 1: Add the `MobileTabs` component**

In `src/App.tsx`, add this component immediately after the `LeftRail` component definition:

```tsx
function MobileTabs({ active, setActive, onPrestige }: {
  active: PanelId;
  setActive: (id: PanelId) => void;
  onPrestige: () => void;
}) {
  return (
    <div className="lg-mtabs">
      {RAIL_ITEMS.map(item => (
        <button
          key={item.id}
          className={active === item.id ? 'active' : ''}
          onClick={() => setActive(item.id)}
        >
          <span className="ic">{item.icon}</span>
          <span>{item.label}</span>
        </button>
      ))}
      <button onClick={onPrestige}>
        <span className="ic">𓆟</span>
        <span>Renew</span>
      </button>
    </div>
  );
}
```

- [ ] **Step 2: Wire mobile dock open/close state in `App`**

In the `App` component, add the state and a combined rail handler. Replace the `const [showPrestige, setShowPrestige] = useState(false);` line with:

```tsx
  const [showPrestige, setShowPrestige] = useState(false);
  const [dockOpenMobile, setDockOpenMobile] = useState(false);

  const openPanel = (id: PanelId) => { setActivePanel(id); setDockOpenMobile(true); };
```

- [ ] **Step 3: Use the mobile handlers in the render tree**

In `App`'s returned JSX:

1. Add `MobileTabs` and a dock scrim. Replace the `<RightDock ... />` line with:

```tsx
      <div className={`lg-dock-scrim${dockOpenMobile ? ' open' : ''}`} onClick={() => setDockOpenMobile(false)} />
      <div className={`lg-dock-wrap${dockOpenMobile ? ' open' : ''}`}>
        <RightDock panel={activePanel} tab={currentTab} setTab={setTab} />
      </div>
      <MobileTabs active={activePanel} setActive={openPanel} onPrestige={() => setShowPrestige(true)} />
```

2. The desktop `LeftRail` already calls `setActivePanel`; leave it. (On desktop the dock is always shown; `dockOpenMobile` is ignored by CSS.)

- [ ] **Step 4: Append the responsive CSS**

Append to the **end** of `src/index.css`:

```css
/* ─── Lagon mobile tabs + dock wrap (desktop defaults) ─────── */
.lg-dock-wrap { display: contents; }
.lg-dock-scrim { display: none; }
.lg-mtabs { display: none; }

@media (max-width: 1023px) {
  .lagon { display: block; }
  .lg-rail { display: none; }
  .lg-pond { position: absolute; inset: 0; }
  .lg-actionbar { left: 12px; right: 12px; bottom: calc(64px + 12px); flex-wrap: wrap; gap: 12px; padding: 12px 14px; }
  .lg-ab-progress { flex-basis: 100%; order: 5; }

  .lg-dock-wrap {
    display: block; position: absolute; z-index: 22;
    top: 0; bottom: 64px; right: 0; width: min(392px, 88vw);
    transform: translateX(101%); transition: transform .25s cubic-bezier(.3,.8,.2,1);
  }
  .lg-dock-wrap.open { transform: translateX(0); }
  .lg-dock { width: 100%; height: 100%; }
  .lg-dock-scrim {
    display: block; position: absolute; inset: 0; z-index: 21;
    background: rgba(8,60,72,.4); opacity: 0; pointer-events: none;
    transition: opacity .2s ease;
  }
  .lg-dock-scrim.open { opacity: 1; pointer-events: auto; }

  .lg-mtabs {
    display: flex; position: absolute; bottom: 0; left: 0; right: 0; height: 64px; z-index: 23;
    background: #fff; box-shadow: 0 -4px 18px rgba(11,85,102,.12);
  }
  .lg-mtabs button {
    flex: 1; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 2px;
    background: transparent; color: #8AAEB4;
    font-family: "Baloo 2"; font-weight: 700; font-size: 9px;
  }
  .lg-mtabs button .ic { font-size: 19px; font-family: "Baloo 2"; }
  .lg-mtabs button.active { color: #0E9CAB; }
}
```

- [ ] **Step 5: Verify the build passes**

Run: `npm run build`
Expected: completes with no errors (exit 0).

- [ ] **Step 6: Visual check**

`npm run dev`, resize the browser to ~600px wide (or device toolbar). Confirm:
- Rail is replaced by a bottom tab bar; pond fills the screen; action bar wraps above the tabs.
- Tapping a tab slides the dock in from the right over a scrim; tapping the scrim closes it.
- Renew still opens the modal.
- Resize back to ≥1024px: the three-column desktop layout returns and the dock is always visible.

- [ ] **Step 7: Commit**

```bash
git add src/App.tsx src/index.css
git commit -m "feat(lagon): responsive fallback with bottom tabs and slide-over dock"
```

---

## Self-Review

**Spec coverage:**
- §1 tokens/fonts/cbtn → Task 1 ✓
- §2 three-column shell + TopHUD + BottomActionBar + keep Phaser → Task 2 ✓ (incl. PhaserContainer bg fix)
- §3 persistent dock nav + mobile fallback → Task 2 (nav) + Task 5 (fallback) ✓
- §4 dock body surface switching (light Shop / dark others) → Task 2 (`is-shop` / `:not(.is-shop)`) ✓
- §5 candy Shop cards (3 variants) → Task 3 ✓
- §6 candy Renouveau modal → Task 2 (markup) + Task 4 (CSS) ✓
- §7 files changed: index.html, index.css, App.tsx, Shop.tsx (+ PhaserContainer noted) → covered ✓
- §8 out of scope (other panels stay dark) → enforced by `:not(.is-shop)` surface, no edits to those components ✓
- §9 verification build + visual → each task ✓

**Placeholder scan:** No TBD/TODO; every code step shows full code. Locked-fish row and legendary path included.

**Type/name consistency:** `PanelId`, `RAIL_ITEMS`, `PANEL_META`, `TABS_BY_PANEL`, `PanelBody`, `RightDock` props (`panel`/`tab`/`setTab`), `FishCard` props (unchanged), `openPanel`, `dockOpenMobile` — consistent across tasks. Class names (`.lg-dock-body.is-shop`, `.cbtn--*`, `.lg-fish*`) consistent between the CSS and the JSX that uses them. The Renouveau modal markup (Task 2) and its CSS (Task 4) use the same `.lg-modal-*` names.
