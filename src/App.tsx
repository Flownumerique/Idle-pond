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

const DOCK_ITEMS: { id: PanelId; label: string; icon: string }[] = [
  { id: 'shop',     label: 'Shop',    icon: '𓆝' },
  { id: 'research', label: 'Coral',   icon: '❋' },
  { id: 'pearls',   label: 'Pearls',  icon: '○' },
  { id: 'profile',  label: 'Profile', icon: '◉' },
  { id: 'journal',  label: 'Journal', icon: '✎' },
];

const PANEL_META: Record<PanelId, { title: string; sub: string }> = {
  shop:     { title: 'Boutique',          sub: 'Buy and raise the inhabitants of your pond.' },
  research: { title: 'Coral of Prestige', sub: 'Permanent bonuses paid in gemmes.' },
  pearls:   { title: 'Pearl Market',      sub: 'Mother-of-pearl, traded for what endures.' },
  profile:  { title: 'Profile',           sub: 'Your record in the Pond of Wonders.' },
  journal:  { title: 'Journal',           sub: 'Pages and creatures of the deep.' },
};

const TABS_BY_PANEL: Partial<Record<PanelId, { id: string; label: string }[]>> = {
  shop:    [{ id: 'fish', label: 'Fish' }, { id: 'upgrades', label: 'Run upgrades' }],
  pearls:  [{ id: 'market', label: 'Market' }, { id: 'prestige', label: 'Prestige upgrades' }],
  journal: [{ id: 'lore', label: 'Lore' }, { id: 'guide', label: 'Guide' }],
};

const DEPTH_NAMES = [
  'Lac de Surface', 'Rivière Souterraine', 'Récif Corallien', 'Océan des Profondeurs',
  'Abysses', 'Zone Hydrothermale', 'Plaine Abyssale', 'Fosse des Origines',
  'Nexus de Mana', 'Cœur Volcanique', 'Royaume Céleste', 'Dimension Quantique',
];

// ─── HUD ──────────────────────────────────────────────────────

function TopHUD() {
  const mana      = useGameStore(s => s.mana);
  const gemmes    = useGameStore(s => s.gemmes);
  const perles    = useGameStore(s => s.perles);
  const pondDepth = useGameStore(s => s.pondDepth);
  const prestiges = useGameStore(s => s.prestiges);

  return (
    <div className="hud-top">
      <div className="hud-top-inner">
        <div className="identity">
          <div className="crest" />
          <div className="name"><em>Étang des</em> Merveilles</div>
        </div>
        <div className="currencies">
          <div className="currency mana">
            <div className="dot">◐</div>
            <div>
              <div className="label">Mana</div>
              <div className="value">{formatNumber(mana)}</div>
            </div>
          </div>
          <div className="currency gemme">
            <div className="dot">◇</div>
            <div>
              <div className="label">Gemmes</div>
              <div className="value">{gemmes}</div>
            </div>
          </div>
          {perles > 0 && (
            <div className="currency perle">
              <div className="dot">○</div>
              <div>
                <div className="label">Pearls</div>
                <div className="value">{perles}</div>
              </div>
            </div>
          )}
        </div>
        <div className="depth-chip">
          <span className="num">{pondDepth}</span>
          <div className="biome-name"><em>—</em>{DEPTH_NAMES[pondDepth] ?? 'Dimension Quantique'}</div>
          {prestiges > 0 && (
            <span style={{ marginLeft: 8, padding: '2px 8px', background: 'oklch(1 0 0 / 0.1)', borderRadius: 999, fontSize: 10 }}>
              ✦ {prestiges} renewal{prestiges === 1 ? '' : 's'}
            </span>
          )}
        </div>
      </div>
    </div>
  );
}

function BottomHUD() {
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

  // Not memoized: recomputes on every render. BottomHUD re-renders frequently
  // (mana changes 10×/s), so this is always fresh, including after boost expires.
  const incomePerSec = computeIncomePerSec(poissons, bonuses, boostActiveUntil);

  const pondCost = getPondUpgradeCost(pondDepth);
  const canDig   = pondDepth < 11 && mana.gte(pondCost);
  const progress = pondDepth < 11 ? Math.min(mana.div(pondCost).toNumber(), 1) : 1;

  const boosted = boostActiveUntil > Date.now();
  const [boostLeft, setBoostLeft] = useState(0);
  useEffect(() => {
    if (!boosted) { setBoostLeft(0); return; }
    const id = setInterval(
      () => setBoostLeft(Math.max(0, Math.ceil((boostActiveUntil - Date.now()) / 1000))),
      1000
    );
    return () => clearInterval(id);
  }, [boosted, boostActiveUntil]);

  const totalFish = poissons.length;
  const species   = new Set(poissons.map(f => f.type)).size;

  return (
    <div className="hud-bottom">
      <div className="hud-bottom-inner">
        <div className="readout">
          <div className="col">
            <span className="lab">Income</span>
            <span className="val mono">
              {formatNumber(incomePerSec)}
              <span style={{ fontSize: 11, marginLeft: 4, opacity: 0.7 }}>/s</span>
            </span>
            {boosted && (
              <span className="sub" style={{ color: 'var(--sun)' }}>
                boost ×{bonuses.boostMultiplier} · {boostLeft}s
              </span>
            )}
          </div>
          <div className="col">
            <span className="lab">Pond</span>
            <span className="val">{totalFish} <span style={{ fontSize: 12, opacity: 0.6 }}>fish</span></span>
            <span className="sub">{species} species</span>
          </div>
          <button
            className="dig-pill"
            style={{ fontSize: 12 }}
            disabled={boosted || gemmes < bonuses.boostCost}
            onClick={activateBoost}
            title={boosted ? 'Boost active' : `×${bonuses.boostMultiplier} income for ${Math.round(bonuses.boostDurationMs / 60_000)} min`}
          >
            <div className="icon">⚡</div>
            <div>
              <span className="label">{boosted ? `Boost · ${boostLeft}s` : 'Boost'}</span>
              <span className="cost">{boosted ? 'active' : `${bonuses.boostCost} 💎`}</span>
            </div>
          </button>
        </div>
        <button className="dig-pill" disabled={!canDig} onClick={upgradePond}>
          <div className="icon">⛏</div>
          <div>
            <span className="label">
              {pondDepth < 11 ? `Dig to ${DEPTH_NAMES[pondDepth + 1] ?? '…'}` : 'Deepest reached'}
            </span>
            <span className="cost">{pondDepth < 11 ? `${formatNumber(pondCost)} mana` : '—'}</span>
          </div>
        </button>
        <div className="progress-cluster">
          <span className="lab">Next biome</span>
          <div className="progress-bar"><i style={{ transform: `scaleX(${progress})` }} /></div>
          <span className="hint">
            {pondDepth < 11
              ? `${Math.round(progress * 100)}% to ${DEPTH_NAMES[pondDepth + 1]}`
              : 'Nexus reached'}
          </span>
        </div>
      </div>
    </div>
  );
}

// ─── Navigation ───────────────────────────────────────────────

function Dock({ active, setActive, onPrestige, prestigeReady }: {
  active: PanelId | null;
  setActive: (id: PanelId | null) => void;
  onPrestige: () => void;
  prestigeReady: boolean;
}) {
  return (
    <div className="dock">
      {DOCK_ITEMS.map(item => (
        <button
          key={item.id}
          className={active === item.id ? 'active' : ''}
          onClick={() => setActive(active === item.id ? null : item.id)}
          title={item.label}
        >
          <div className="ico" style={{ fontFamily: 'var(--font-display)', fontSize: 18 }}>{item.icon}</div>
          <span className="lab">{item.label}</span>
        </button>
      ))}
      <div className="sep" />
      <button
        onClick={onPrestige}
        title="Prestige"
        style={prestigeReady ? { color: 'var(--coral)' } : undefined}
      >
        <div className="ico" style={{ fontFamily: 'var(--font-display)', fontSize: 18 }}>𓆟</div>
        <span className="lab">Renew</span>
      </button>
    </div>
  );
}

function MobileTabs({ active, setActive, onPrestige }: {
  active: PanelId | null;
  setActive: (id: PanelId | null) => void;
  onPrestige: () => void;
}) {
  return (
    <div className="mobile-tabs">
      {DOCK_ITEMS.map(item => (
        <button
          key={item.id}
          className={active === item.id ? 'active' : ''}
          onClick={() => setActive(active === item.id ? null : item.id)}
        >
          <div className="ico" style={{ fontFamily: 'var(--font-display)', fontSize: 18 }}>{item.icon}</div>
          <span>{item.label}</span>
        </button>
      ))}
      <button onClick={onPrestige}>
        <div className="ico" style={{ fontFamily: 'var(--font-display)', fontSize: 18 }}>𓆟</div>
        <span>Renew</span>
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

// ─── Prestige modal ───────────────────────────────────────────

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
      <div className="panel-scrim" onClick={onClose} />
      <div className="panel" style={{ maxWidth: 420, top: '50%', transform: 'translateX(-50%) translateY(-50%)' }}>
        <div className="panel-head">
          <div>
            <h2>Renewal</h2>
            <span className="sub">Reset your run and earn pearls.</span>
          </div>
          <button className="close" onClick={onClose}>×</button>
        </div>
        <div className="panel-body" style={{ padding: 24, display: 'flex', flexDirection: 'column', gap: 16 }}>
          <p style={{ margin: 0 }}>
            You will earn <strong>{earned} 🪸 pearls</strong>.
          </p>
          <p style={{ margin: 0, opacity: 0.65, fontSize: 13 }}>
            Mana and fish will be lost. Research, achievements, and pearl upgrades persist.
          </p>
          {bonuses.prestigeKeepFishPercent > 0 && (
            <p style={{ margin: 0, color: 'var(--coral)', fontSize: 13 }}>
              ✦ {bonuses.prestigeKeepFishPercent}% of your fish will be kept.
            </p>
          )}
          {!canConfirm && (
            <p style={{ margin: 0, color: 'oklch(0.7 0.15 30)', fontSize: 13 }}>
              Requires pond depth ≥ 2 to renew.
            </p>
          )}
          <div style={{ display: 'flex', gap: 12, justifyContent: 'flex-end', marginTop: 8 }}>
            <button className="dig-pill" style={{ opacity: 0.7 }} onClick={onClose}>
              <div className="icon">✕</div>
              <div><span className="label">Cancel</span></div>
            </button>
            <button
              className="dig-pill"
              disabled={!canConfirm}
              onClick={() => { prestige(); onClose(); }}
              style={canConfirm ? { color: 'var(--coral)' } : {}}
            >
              <div className="icon">𓆟</div>
              <div>
                <span className="label">Renew</span>
                <span className="cost">{earned} pearls</span>
              </div>
            </button>
          </div>
        </div>
      </div>
    </>
  );
}

// ─── Root ─────────────────────────────────────────────────────

export default function App() {
  const [activePanel, setActivePanel]   = useState<PanelId | null>(null);
  const [panelTab, setPanelTab]         = useState<Record<string, string>>({
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
      if (e.key === 'Escape') {
        setActivePanel(null);
        setShowPrestige(false);
      }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, []);

  const pondDepth = useGameStore(s => s.pondDepth);

  const prestigeReady = pondDepth >= 2;
  const tabs       = activePanel ? TABS_BY_PANEL[activePanel] : undefined;
  const currentTab = activePanel ? (panelTab[activePanel] ?? tabs?.[0]?.id ?? '') : '';

  return (
    <div className="app">
      <PhaserContainer />
      <TopHUD />
      <BottomHUD />

      <Dock
        active={activePanel}
        setActive={setActivePanel}
        onPrestige={() => setShowPrestige(true)}
        prestigeReady={prestigeReady}
      />
      <MobileTabs
        active={activePanel}
        setActive={setActivePanel}
        onPrestige={() => setShowPrestige(true)}
      />

      {activePanel && (
        <>
          <div className="panel-scrim" onClick={() => setActivePanel(null)} />
          <div className="panel">
            <div className="panel-head">
              <div>
                <h2>{PANEL_META[activePanel].title}</h2>
                <span className="sub">{PANEL_META[activePanel].sub}</span>
              </div>
              <button className="close" onClick={() => setActivePanel(null)}>×</button>
            </div>
            {tabs && (
              <div className="panel-tabs">
                {tabs.map(tab => (
                  <button
                    key={tab.id}
                    className={currentTab === tab.id ? 'active' : ''}
                    onClick={() => setPanelTab(prev => ({ ...prev, [activePanel]: tab.id }))}
                  >{tab.label}</button>
                ))}
              </div>
            )}
            <div className="panel-body">
              <PanelBody panel={activePanel} tab={currentTab} />
            </div>
          </div>
        </>
      )}

      {showPrestige && <PrestigeModal onClose={() => setShowPrestige(false)} />}

      <EventNotification />
      <UnlockNotification />
      <WelcomeBackNotification />
    </div>
  );
}
