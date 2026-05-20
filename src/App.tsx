import React, { useEffect, useReducer, useRef, useState } from 'react';
import { BIOMES, FISH, ACHIEVEMENTS, NARRATIVE_EVENTS } from './gameData';
import type { Fish, ResearchNode, RunUpgrade, PearlUpgrade, PrestigeUpgrade } from './gameData';
import { PondCanvas } from './components/PondCanvas';
import {
  fmt, Shop, ResearchTree, ProfilePanel, JournalPanel,
  PrestigeModal, WelcomeBackModal, RunUpgradesTab, PearlsPanel,
  ChallengesSection, BestiaryPanel, BoostButton, Onboarding,
} from './components/Panels';

export interface Stats {
  sessionMs: number;
  totalPlayMs: number;
  offlineMs: number;
  totalManaEarned: number;
}

export interface GameState {
  mana: number;
  gemmes: number;
  perles: number;
  prestiges: number;
  depth: number;
  fishOwned: Record<string, number>;
  fishLevel: Record<string, number>;
  research: string[];
  achievements: string[];
  runUpgrades: string[];
  pearlUpgrades: string[];
  prestigeUpgrades: string[];
  boostUntil: number;
}

export type GameAction =
  | { type: 'tick'; amount: number }
  | { type: 'buy_fish'; fish: Fish; cost: number }
  | { type: 'level_fish'; fish: Fish; cost: number }
  | { type: 'dig' }
  | { type: 'unlock_research'; node: ResearchNode }
  | { type: 'buy_run_upgrade'; upgrade: RunUpgrade }
  | { type: 'buy_pearl_upgrade'; upgrade: PearlUpgrade }
  | { type: 'buy_prestige_upgrade'; upgrade: PrestigeUpgrade }
  | { type: 'award_achievement'; id: string }
  | { type: 'prestige' }
  | { type: 'boost' }
  | { type: 'demo_offline'; amount: number }
  | { type: 'demo_gemmes'; amount: number };

const INITIAL: GameState = {
  mana: 0,
  gemmes: 12,
  perles: 0,
  prestiges: 0,
  depth: 0,
  fishOwned: {},
  fishLevel: {},
  research: [],
  achievements: [],
  runUpgrades: [],
  pearlUpgrades: [],
  prestigeUpgrades: [],
  boostUntil: 0,
};

function reducer(state: GameState, action: GameAction): GameState {
  switch (action.type) {
    case 'tick':
      return { ...state, mana: state.mana + action.amount };
    case 'buy_fish': {
      if (state.mana < action.cost) return state;
      const owned = (state.fishOwned[action.fish.id] || 0) + 1;
      return {
        ...state,
        mana: state.mana - action.cost,
        fishOwned: { ...state.fishOwned, [action.fish.id]: owned },
        fishLevel: { ...state.fishLevel, [action.fish.id]: state.fishLevel[action.fish.id] || 1 },
      };
    }
    case 'level_fish': {
      if (state.mana < action.cost) return state;
      const lvl = (state.fishLevel[action.fish.id] || 1) + 1;
      return {
        ...state,
        mana: state.mana - action.cost,
        fishLevel: { ...state.fishLevel, [action.fish.id]: lvl },
      };
    }
    case 'dig': {
      const next = BIOMES[state.depth + 1];
      if (!next) return state;
      if (state.mana < next.upgradeCost) return state;
      return { ...state, mana: state.mana - next.upgradeCost, depth: state.depth + 1 };
    }
    case 'unlock_research': {
      if (state.gemmes < action.node.cost) return state;
      if (state.research.includes(action.node.id)) return state;
      return {
        ...state,
        gemmes: state.gemmes - action.node.cost,
        research: [...state.research, action.node.id],
      };
    }
    case 'buy_run_upgrade': {
      if (state.mana < action.upgrade.cost) return state;
      if (state.runUpgrades.includes(action.upgrade.id)) return state;
      return {
        ...state,
        mana: state.mana - action.upgrade.cost,
        runUpgrades: [...state.runUpgrades, action.upgrade.id],
      };
    }
    case 'buy_pearl_upgrade': {
      if (state.perles < action.upgrade.cost) return state;
      if (state.pearlUpgrades.includes(action.upgrade.id)) return state;
      return {
        ...state,
        perles: state.perles - action.upgrade.cost,
        pearlUpgrades: [...state.pearlUpgrades, action.upgrade.id],
      };
    }
    case 'buy_prestige_upgrade': {
      if (state.perles < action.upgrade.cost) return state;
      if (state.prestigeUpgrades.includes(action.upgrade.id)) return state;
      return {
        ...state,
        perles: state.perles - action.upgrade.cost,
        prestigeUpgrades: [...state.prestigeUpgrades, action.upgrade.id],
      };
    }
    case 'award_achievement': {
      if (state.achievements.includes(action.id)) return state;
      const ach = ACHIEVEMENTS.find(a => a.id === action.id);
      return {
        ...state,
        achievements: [...state.achievements, action.id],
        gemmes: state.gemmes + (ach?.reward || 0),
      };
    }
    case 'prestige': {
      if (state.depth < 2) return state;
      const earned = Math.max(1, Math.ceil(state.depth * 5 + Math.log10(Math.max(state.mana, 10))));
      return {
        ...INITIAL,
        gemmes: state.gemmes,
        perles: state.perles + earned,
        prestiges: state.prestiges + 1,
        research: state.research,
        achievements: state.achievements,
        pearlUpgrades: state.pearlUpgrades,
        prestigeUpgrades: state.prestigeUpgrades,
      };
    }
    case 'boost': {
      if (state.gemmes < 10) return state;
      return { ...state, gemmes: state.gemmes - 10, boostUntil: Date.now() + 5 * 60 * 1000 };
    }
    case 'demo_offline':
      return { ...state, mana: state.mana + action.amount };
    case 'demo_gemmes':
      return { ...state, gemmes: state.gemmes + action.amount };
    default:
      return state;
  }
}

function computeIncomeRate(state: GameState): number {
  let researchMult = 1;
  if (state.research.includes('bio_1')) researchMult += 0.15;
  if (state.research.includes('bio_2')) researchMult += 0.25;
  if (state.research.includes('bio_3')) researchMult += 0.40;
  if (state.research.includes('bio_5')) researchMult += 0.75;
  if (state.research.includes('ocean_2')) researchMult += 0.20;

  let total = 0;
  for (const fish of FISH) {
    const owned = state.fishOwned[fish.id] || 0;
    if (owned === 0) continue;
    const level = state.fishLevel[fish.id] || 1;
    let rate = fish.base * Math.pow(1.5, level - 1) * owned;
    if (fish.depth >= 4 && state.research.includes('ocean_3')) rate *= 1.5;
    total += rate;
  }
  total *= researchMult;
  if (state.boostUntil > Date.now()) total *= 2;
  return total;
}

// ─── HUD components ──────────────────────────────────────────

function TopHUD({ state }: { state: GameState }) {
  const biome = BIOMES[Math.min(state.depth, BIOMES.length - 1)];
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
              <div className="value">{fmt(state.mana)}</div>
            </div>
          </div>
          <div className="currency gemme">
            <div className="dot">◇</div>
            <div>
              <div className="label">Gemmes</div>
              <div className="value">{state.gemmes}</div>
            </div>
          </div>
          {state.perles > 0 && (
            <div className="currency perle">
              <div className="dot">○</div>
              <div>
                <div className="label">Pearls</div>
                <div className="value">{state.perles}</div>
              </div>
            </div>
          )}
        </div>
        <div className="depth-chip">
          <span className="num">{state.depth}</span>
          <div className="biome-name"><em>—</em>{biome.name}</div>
          {state.prestiges > 0 && (
            <span style={{ marginLeft: 8, padding: '2px 8px', background: 'oklch(1 0 0 / 0.1)', borderRadius: 999, fontSize: 10 }}>
              ✦ {state.prestiges} renewal{state.prestiges === 1 ? '' : 's'}
            </span>
          )}
        </div>
      </div>
    </div>
  );
}

function BottomHUD({ state, rate, dispatch }: { state: GameState; rate: number; dispatch: React.Dispatch<GameAction> }) {
  const next = BIOMES[state.depth + 1];
  const digCost = next ? next.upgradeCost : Infinity;
  const canDig = !!next && state.mana >= digCost;
  const totalFish = Object.values(state.fishOwned).reduce((a, b) => a + b, 0);
  const progress = next ? Math.min(state.mana / digCost, 1) : 1;
  const boosted = state.boostUntil > Date.now();
  const boostLeft = boosted ? Math.ceil((state.boostUntil - Date.now()) / 1000) : 0;

  return (
    <div className="hud-bottom">
      <div className="hud-bottom-inner">
        <div className="readout">
          <div className="col">
            <span className="lab">Income</span>
            <span className="val mono">{fmt(rate)}<span style={{ fontSize: 11, marginLeft: 4, opacity: 0.7 }}>/s</span></span>
            {boosted && <span className="sub" style={{ color: 'var(--sun)' }}>boost ×2 · {boostLeft}s</span>}
          </div>
          <div className="col">
            <span className="lab">Pond</span>
            <span className="val">{totalFish} <span style={{ fontSize: 12, opacity: 0.6 }}>fish</span></span>
            <span className="sub">{Object.keys(state.fishOwned).length} species</span>
          </div>
          <BoostButton state={state} dispatch={dispatch} />
        </div>
        <button className="dig-pill" disabled={!canDig} onClick={() => dispatch({ type: 'dig' })}>
          <div className="icon">⛏</div>
          <div>
            <span className="label">{next ? `Dig to ${next.name}` : 'Deepest reached'}</span>
            <span className="cost">{next ? `${fmt(digCost)} mana` : '—'}</span>
          </div>
        </button>
        <div className="progress-cluster">
          <span className="lab">Next biome</span>
          <div className="progress-bar"><i style={{ transform: `scaleX(${progress})` }} /></div>
          <span className="hint">{next ? `${Math.round(progress * 100)}% to ${next.name}` : 'you have reached the Nexus'}</span>
        </div>
      </div>
    </div>
  );
}

// ─── Dock ─────────────────────────────────────────────────────

type PanelId = 'shop' | 'research' | 'pearls' | 'profile' | 'journal';

const DOCK_ITEMS: { id: PanelId; label: string; icon: string }[] = [
  { id: 'shop',     label: 'Shop',    icon: '𓆝' },
  { id: 'research', label: 'Coral',   icon: '❋' },
  { id: 'pearls',   label: 'Pearls',  icon: '○' },
  { id: 'profile',  label: 'Profile', icon: '◉' },
  { id: 'journal',  label: 'Journal', icon: '✎' },
];

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
      <button onClick={onPrestige} title="Prestige" style={prestigeReady ? { color: 'var(--coral)' } : undefined}>
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

interface Toast {
  id: number;
  text: string;
  kind?: 'event' | 'unlock';
}

const PANEL_META: Record<PanelId, { title: string; sub: string }> = {
  shop:     { title: 'Boutique',          sub: 'Buy and raise the inhabitants of your pond.' },
  research: { title: 'Coral of Prestige', sub: 'Permanent bonuses paid in gemmes.' },
  pearls:   { title: 'Pearl Market',      sub: 'Mother-of-pearl, traded for what endures.' },
  profile:  { title: 'Profile',           sub: 'Your record in the Pond of Wonders.' },
  journal:  { title: 'Journal',           sub: 'Pages and creatures of the deep.' },
};

const TABS_BY_PANEL: Partial<Record<PanelId, { id: string; label: string }[]>> = {
  shop:    [{ id: 'fish', label: 'Fish' },     { id: 'upgrades', label: 'Run upgrades' }],
  pearls:  [{ id: 'market', label: 'Market' }, { id: 'persist', label: 'Prestige upgrades' }],
  journal: [{ id: 'lore', label: 'Lore' },     { id: 'bestiary', label: 'Bestiary' }],
};

export default function App() {
  const [state, dispatch] = useReducer(reducer, INITIAL);
  const [activePanel, setActivePanel] = useState<PanelId | null>(null);
  const [panelTab, setPanelTab] = useState<Record<string, string>>({
    shop: 'fish',
    pearls: 'market',
    journal: 'lore',
  });
  const [showPrestige, setShowPrestige] = useState(false);
  const [showWelcome, setShowWelcome] = useState<{ minutes: number; mana: number } | null>(null);
  const [showOnboarding, setShowOnboarding] = useState(true);
  const [toasts, setToasts] = useState<Toast[]>([]);

  const sessionStartRef = useRef(Date.now());
  const [stats, setStats] = useState<Stats>({
    sessionMs: 0,
    totalPlayMs: 0,
    offlineMs: 0,
    totalManaEarned: 0,
  });

  // Stats tick
  useEffect(() => {
    const id = setInterval(() => {
      setStats(prev => ({
        ...prev,
        sessionMs: Date.now() - sessionStartRef.current,
        totalPlayMs: prev.totalPlayMs + 1000,
      }));
    }, 1000);
    return () => clearInterval(id);
  }, []);

  // Idle loop
  useEffect(() => {
    const id = setInterval(() => {
      const rate = computeIncomeRate(state);
      if (rate > 0) {
        const gained = rate * 0.1;
        dispatch({ type: 'tick', amount: gained });
        setStats(prev => ({ ...prev, totalManaEarned: prev.totalManaEarned + gained }));
      }
    }, 100);
    return () => clearInterval(id);
  }, [state]);

  // Achievement watcher
  useEffect(() => {
    const ctx = {
      mana: state.mana,
      depth: state.depth,
      prestiges: state.prestiges,
      researchCount: state.research.length,
      totalFish: Object.values(state.fishOwned).reduce((a, b) => a + b, 0),
      maxLevel: Math.max(0, ...Object.values(state.fishLevel)),
    };
    for (const ach of ACHIEVEMENTS) {
      if (!state.achievements.includes(ach.id) && ach.check(ctx)) {
        dispatch({ type: 'award_achievement', id: ach.id });
        setToasts(prev => [...prev, { id: Date.now() + Math.random(), text: `✦ ${ach.name} — +${ach.reward} gemmes` }]);
      }
    }
  }, [state.mana, state.depth, state.prestiges, state.research.length, state.fishOwned, state.fishLevel]);

  // Ambient narrative events
  useEffect(() => {
    const id = setInterval(() => {
      const pool = NARRATIVE_EVENTS.filter(e => state.depth >= e.minDepth);
      if (pool.length === 0) return;
      const event = pool[Math.floor(Math.random() * pool.length)];
      setToasts(prev => [...prev, { id: Date.now() + Math.random(), text: event.text, kind: 'event' }]);
    }, 35000 + Math.random() * 20000);
    return () => clearInterval(id);
  }, [state.depth]);

  // Biome unlock toast
  const prevDepth = useRef(0);
  useEffect(() => {
    if (state.depth > prevDepth.current) {
      const biome = BIOMES[state.depth];
      if (biome) {
        setToasts(prev => [...prev, { id: Date.now() + Math.random(), text: `✦ Unlocked — ${biome.name}`, kind: 'unlock' }]);
      }
    }
    prevDepth.current = state.depth;
  }, [state.depth]);

  // Toast auto-dismiss
  useEffect(() => {
    if (toasts.length === 0) return;
    const oldest = toasts[0];
    const tm = setTimeout(() => setToasts(prev => prev.filter(t => t.id !== oldest.id)), 4500);
    return () => clearTimeout(tm);
  }, [toasts]);

  // ESC closes panels/modals
  useEffect(() => {
    const h = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setActivePanel(null);
        setShowPrestige(false);
        setShowWelcome(null);
      }
    };
    window.addEventListener('keydown', h);
    return () => window.removeEventListener('keydown', h);
  }, []);

  const rate = computeIncomeRate(state);
  const prestigeReady = state.depth >= 2;

  return (
    <div className="app">
      <PondCanvas depth={state.depth} fishCounts={state.fishOwned} fishIconStyle="placeholder" />

      <TopHUD state={state} />
      <BottomHUD state={state} rate={rate} dispatch={dispatch} />

      <Dock
        active={activePanel}
        setActive={setActivePanel}
        onPrestige={() => setShowPrestige(true)}
        prestigeReady={prestigeReady}
      />

      <MobileTabs active={activePanel} setActive={setActivePanel} onPrestige={() => setShowPrestige(true)} />

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
            {TABS_BY_PANEL[activePanel] && (
              <div className="panel-tabs">
                {TABS_BY_PANEL[activePanel]!.map(tab => (
                  <button
                    key={tab.id}
                    className={panelTab[activePanel] === tab.id ? 'active' : ''}
                    onClick={() => setPanelTab({ ...panelTab, [activePanel]: tab.id })}
                  >{tab.label}</button>
                ))}
              </div>
            )}
            <div className="panel-body">
              {activePanel === 'shop' && panelTab.shop === 'fish'     && <Shop state={state} dispatch={dispatch} />}
              {activePanel === 'shop' && panelTab.shop === 'upgrades' && <RunUpgradesTab state={state} dispatch={dispatch} />}
              {activePanel === 'research' && <ResearchTree state={state} dispatch={dispatch} />}
              {activePanel === 'pearls' && (
                <PearlsPanel
                  state={state}
                  dispatch={dispatch}
                  tab={panelTab.pearls}
                  setTab={tab => setPanelTab({ ...panelTab, pearls: tab })}
                />
              )}
              {activePanel === 'profile' && (
                <>
                  <ProfilePanel state={state} stats={stats} />
                  <ChallengesSection />
                </>
              )}
              {activePanel === 'journal' && panelTab.journal === 'lore'     && <JournalPanel state={state} />}
              {activePanel === 'journal' && panelTab.journal === 'bestiary' && <BestiaryPanel state={state} />}
            </div>
          </div>
        </>
      )}

      {showPrestige && (
        <PrestigeModal
          state={state}
          onClose={() => setShowPrestige(false)}
          onConfirm={() => { dispatch({ type: 'prestige' }); setShowPrestige(false); }}
        />
      )}

      {showWelcome && (
        <WelcomeBackModal
          minutes={showWelcome.minutes}
          mana={showWelcome.mana}
          onClose={() => {
            dispatch({ type: 'demo_offline', amount: showWelcome!.mana });
            setStats(prev => ({
              ...prev,
              offlineMs: prev.offlineMs + showWelcome!.minutes * 60 * 1000,
              totalManaEarned: prev.totalManaEarned + showWelcome!.mana,
            }));
            setShowWelcome(null);
          }}
        />
      )}

      <div className="toast-rail">
        {toasts.map(t => <div key={t.id} className={`toast ${t.kind || ''}`}>{t.text}</div>)}
      </div>

      {showOnboarding && (
        <Onboarding onDismiss={() => setShowOnboarding(false)} />
      )}

      {/* Demo buttons (hidden, accessible via tweaks) */}
      <div style={{ display: 'none' }}>
        <button onClick={() => setShowWelcome({ minutes: 47, mana: Math.max(rate * 60 * 25, 5000) })}>Welcome-back</button>
        <button onClick={() => setShowPrestige(true)}>Prestige</button>
        <button onClick={() => dispatch({ type: 'demo_offline', amount: 1_000_000 })}>+1M mana</button>
        <button onClick={() => dispatch({ type: 'demo_gemmes', amount: 50 })}>+50 gemmes</button>
      </div>
    </div>
  );
}
