import React from 'react';
import {
  FISH, RESEARCH_BRANCHES, ACHIEVEMENTS, JOURNAL, BIOMES,
  RUN_UPGRADES, PEARL_UPGRADES, PRESTIGE_UPGRADES, DAILY_CHALLENGES, BESTIARY,
} from '../gameData';
import type { GameState, GameAction, Stats } from '../App';

export function fmt(n: number): string {
  if (n === Infinity) return '∞';
  if (n < 1000) return Math.floor(n).toLocaleString();
  const u = ['', 'K', 'M', 'B', 'T', 'Qa', 'Qi', 'Sx', 'Sp', 'Oc', 'No', 'De'];
  let i = 0;
  let v = n;
  while (v >= 1000 && i < u.length - 1) { v /= 1000; i++; }
  return v.toFixed(v < 10 ? 2 : v < 100 ? 1 : 0) + u[i];
}

const fishCost = (fish: typeof FISH[0], owned: number) =>
  Math.ceil(fish.cost * Math.pow(1.15, owned));
const levelCost = (fish: typeof FISH[0], level: number) =>
  Math.ceil(fish.cost * 5 * Math.pow(1.6, level));

export function Shop({ state, dispatch }: { state: GameState; dispatch: React.Dispatch<GameAction> }) {
  const visible = FISH.filter(f =>
    f.depth <= state.depth &&
    (!f.requiredPrestiges || state.prestiges >= f.requiredPrestiges)
  );

  return (
    <div>
      {visible.map(fish => {
        const owned = state.fishOwned[fish.id] || 0;
        const level = state.fishLevel[fish.id] || 1;
        const buyCost = fishCost(fish, owned);
        const lvlCost = levelCost(fish, level);
        const affordable = state.mana >= buyCost;
        const lvlAffordable = state.mana >= lvlCost && owned > 0;
        const milestones = [10, 25, 50, 100];
        const ratePerOne = fish.base * Math.pow(1.5, level - 1);
        const totalRate = ratePerOne * owned;

        return (
          <div key={fish.id} className={`fish-card ${affordable ? 'affordable' : ''}`}>
            <div className="placeholder" title={fish.name}><span>FISH<br />ART</span></div>
            <div className="meta">
              <div className="name">{fish.name}</div>
              <div className="desc">{fish.desc}</div>
              <div className="stats">
                <div className="stat owned"><span>Owned</span><b>{owned}</b></div>
                <div className="stat"><span>Level</span><b>{level}</b></div>
                <div className="stat"><span>Yield</span><b>{fmt(totalRate)}/s</b></div>
              </div>
              {owned > 0 && (
                <div className="milestone-track" title="Milestones at L10/25/50/100">
                  {milestones.map(m => {
                    const hit = level >= m;
                    const next = !hit && milestones.find(x => level < x) === m;
                    return <div key={m} className={`pip ${hit ? 'hit' : ''} ${next ? 'next' : ''}`} />;
                  })}
                  <span style={{ fontSize: 9, color: 'var(--ink-muted)', fontFamily: 'var(--font-mono)', marginLeft: 4 }}>
                    L{level}
                  </span>
                </div>
              )}
            </div>
            <div className="actions">
              <button
                className="btn"
                disabled={!affordable || !!(fish.maxOwned && owned >= fish.maxOwned)}
                onClick={() => dispatch({ type: 'buy_fish', fish, cost: buyCost })}
              >
                Buy <span className="cost">{fmt(buyCost)}</span>
              </button>
              {owned > 0 && (
                <button
                  className="btn ghost btn-sm"
                  disabled={!lvlAffordable}
                  onClick={() => dispatch({ type: 'level_fish', fish, cost: lvlCost })}
                >
                  Level → <span className="cost">{fmt(lvlCost)}</span>
                </button>
              )}
            </div>
          </div>
        );
      })}
      {visible.length === 0 && (
        <div style={{ padding: '30px 4px', textAlign: 'center', color: 'var(--ink-muted)', fontStyle: 'italic', fontFamily: 'var(--font-display)' }}>
          Dig deeper to discover new species.
        </div>
      )}
    </div>
  );
}

export function ResearchTree({ state, dispatch }: { state: GameState; dispatch: React.Dispatch<GameAction> }) {
  return (
    <div className="research-canvas">
      {RESEARCH_BRANCHES.map(branch => (
        <div key={branch.id} className="branch-section" style={{ '--branch-h': branch.hue } as React.CSSProperties}>
          <div className="branch-head">
            <span className="dot" style={{ background: `oklch(0.62 0.13 ${branch.hue})` }} />
            <h3>{branch.name}</h3>
            <span className="desc">— {branch.desc}</span>
          </div>
          <div className="branch-nodes">
            {branch.nodes.map((node, idx) => {
              const unlocked = state.research.includes(node.id);
              const prereqOk = !node.requires || state.research.includes(node.requires);
              const affordable = state.gemmes >= node.cost && prereqOk && !unlocked;
              return (
                <React.Fragment key={node.id}>
                  {idx > 0 && (
                    <div className={`branch-line ${unlocked ? 'unlocked' : ''}`} style={{ '--branch-h': branch.hue } as React.CSSProperties} />
                  )}
                  <button
                    className={`node ${unlocked ? 'unlocked' : prereqOk ? '' : 'locked'}`}
                    style={{ '--branch-h': branch.hue } as React.CSSProperties}
                    disabled={unlocked || !prereqOk || !affordable}
                    onClick={() => dispatch({ type: 'unlock_research', node })}
                  >
                    <div className="n-label">{node.label}</div>
                    <div className="n-effect">{node.effect}</div>
                    {!unlocked && <div className="n-cost">{node.cost} 💎</div>}
                  </button>
                </React.Fragment>
              );
            })}
          </div>
        </div>
      ))}
      <div style={{ padding: '12px 14px', background: 'var(--paper-soft)', borderRadius: 14, marginTop: 8 }}>
        <div style={{ fontSize: 11, color: 'var(--ink-muted)', fontStyle: 'italic', fontFamily: 'var(--font-display)' }}>
          Research nodes are permanent. Coral grows once, blooms forever.
        </div>
      </div>
    </div>
  );
}

export function AchievementsPanel({ state }: { state: GameState }) {
  return (
    <div>
      <div className="ach-grid">
        {ACHIEVEMENTS.map(ach => {
          const unlocked = state.achievements.includes(ach.id);
          return (
            <div key={ach.id} className={`ach-cell ${unlocked ? 'unlocked' : 'locked'}`}>
              <div className="seal" style={!unlocked ? { background: 'oklch(0.22 0.025 230 / 0.08)', color: 'var(--ink-muted)' } : undefined}>
                {unlocked ? '✦' : '·'}
              </div>
              <div>
                <div className="ach-name">{unlocked ? ach.name : 'Locked'}</div>
                <div className="ach-desc">{unlocked ? ach.desc : '—'}</div>
                <div className="ach-reward">+{ach.reward} 💎</div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

function formatDuration(ms: number): string {
  const s = Math.floor(ms / 1000);
  const h = Math.floor(s / 3600);
  const m = Math.floor((s % 3600) / 60);
  const sec = s % 60;
  if (h > 0) return `${h}h ${String(m).padStart(2, '0')}m`;
  if (m > 0) return `${m}m ${String(sec).padStart(2, '0')}s`;
  return `${sec}s`;
}

export function ProfilePanel({ state, stats }: { state: GameState; stats: Stats }) {
  const totalFish = Object.values(state.fishOwned).reduce((a, b) => a + b, 0);
  const speciesOwned = Object.keys(state.fishOwned).length;
  const unlockedAch = state.achievements.length;
  const totalAch = ACHIEVEMENTS.length;
  const biome = BIOMES[Math.min(state.depth, BIOMES.length - 1)];

  return (
    <div>
      <div className="profile-hero">
        <div className="profile-crest">
          <div className="crest-glow" />
          <div className="crest-sigil">𓆟</div>
        </div>
        <div className="profile-titles">
          <div className="profile-rank">Pond keeper</div>
          <div className="profile-name">{
            state.prestiges >= 5 ? 'Nexus-Touched'
            : state.prestiges >= 1 ? 'Tide-Reader'
            : state.depth >= 3 ? 'Abyss-Walker'
            : state.depth >= 1 ? 'Reedsinger'
            : 'Newcomer'
          }</div>
          <div className="profile-where">Currently in <em>{biome.name}</em></div>
        </div>
      </div>

      <div className="profile-stats">
        <div className="ps-cell">
          <div className="ps-lab">Session</div>
          <div className="ps-val">{formatDuration(stats.sessionMs)}</div>
          <div className="ps-sub">Online now</div>
        </div>
        <div className="ps-cell">
          <div className="ps-lab">Total play</div>
          <div className="ps-val">{formatDuration(stats.totalPlayMs)}</div>
          <div className="ps-sub">Across all sessions</div>
        </div>
        <div className="ps-cell">
          <div className="ps-lab">Offline gains</div>
          <div className="ps-val">{formatDuration(stats.offlineMs)}</div>
          <div className="ps-sub">Pond fished alone</div>
        </div>
        <div className="ps-cell">
          <div className="ps-lab">Mana earned</div>
          <div className="ps-val mono">{fmt(stats.totalManaEarned)}</div>
          <div className="ps-sub">Lifetime</div>
        </div>
        <div className="ps-cell">
          <div className="ps-lab">Pond size</div>
          <div className="ps-val">{totalFish}</div>
          <div className="ps-sub">{speciesOwned} species</div>
        </div>
        <div className="ps-cell">
          <div className="ps-lab">Renewals</div>
          <div className="ps-val">{state.prestiges}</div>
          <div className="ps-sub">{state.perles} pearls 🪸</div>
        </div>
        <div className="ps-cell">
          <div className="ps-lab">Deepest</div>
          <div className="ps-val">{state.depth}</div>
          <div className="ps-sub">{biome.name}</div>
        </div>
        <div className="ps-cell">
          <div className="ps-lab">Research</div>
          <div className="ps-val">
            {state.research.length}
            <span style={{ fontSize: 12, color: 'var(--ink-muted)', marginLeft: 4 }}>/22</span>
          </div>
          <div className="ps-sub">nodes grown</div>
        </div>
      </div>

      <div className="trophies-head">
        <div>
          <h3>Trophies</h3>
          <span className="th-sub">Things the pond has remembered</span>
        </div>
        <div className="th-count">
          <span className="thc-num">{unlockedAch}</span>
          <span className="thc-of">/ {totalAch}</span>
        </div>
      </div>
      <AchievementsPanel state={state} />
    </div>
  );
}

export function JournalPanel({ state }: { state: GameState }) {
  return (
    <div>
      {JOURNAL.map(entry => {
        const unlocked = state.depth >= entry.depth;
        return (
          <div key={entry.depth} className={`journal-entry ${unlocked ? '' : 'je-locked'}`}>
            <div className="je-head">
              <span className="je-depth">DEPTH · {String(entry.depth).padStart(2, '0')}</span>
              <span className="je-name">{unlocked ? entry.name : 'Sealed'}</span>
            </div>
            <div className="je-body">
              {unlocked ? entry.text : 'Dig further to recover this page.'}
            </div>
          </div>
        );
      })}
    </div>
  );
}

export function PrestigeModal({ state, onClose, onConfirm }: {
  state: GameState;
  onClose: () => void;
  onConfirm: () => void;
}) {
  const pearls = Math.max(1, Math.ceil(state.depth * 5 + Math.log10(Math.max(state.mana, 10))));
  const canPrestige = state.depth >= 2;
  return (
    <div className="modal-scrim" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()}>
        <div className="hero">
          <div className="sigil">𓆟</div>
        </div>
        <div className="body">
          <h3>Renew the Pond</h3>
          <p className="lede">
            Drain the waters. Plant new reeds. The Coral remembers what the fish forget — your research, gemmes and pearls persist, and the bottom of the pond is closer to you each time.
          </p>
          <div className="stat-row">
            <div className="cell">
              <div className="lab">You'll lose</div>
              <div className="val">{fmt(state.mana)} <span style={{ fontSize: 11, color: 'var(--ink-muted)' }}>mana</span></div>
            </div>
            <div className="cell">
              <div className="lab">You'll earn</div>
              <div className="val" style={{ color: 'var(--coral)' }}>{pearls} <span style={{ fontSize: 11, color: 'var(--ink-muted)' }}>pearls 🪸</span></div>
            </div>
          </div>
          <div className="actions">
            <button className="btn ghost" onClick={onClose}>Not yet</button>
            <button className="btn gold" disabled={!canPrestige} onClick={onConfirm}>
              {canPrestige ? 'Renew the pond' : 'Reach depth 2 first'}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

export function WelcomeBackModal({ minutes, mana, onClose }: { minutes: number; mana: number; onClose: () => void }) {
  return (
    <div className="modal-scrim" onClick={onClose}>
      <div className="modal" style={{ maxWidth: 460 }} onClick={e => e.stopPropagation()}>
        <div className="hero" style={{ height: 140, background: 'radial-gradient(circle at 50% 60%, oklch(0.86 0.07 88), oklch(0.62 0.06 200) 70%, oklch(0.4 0.06 220))' }}>
          <div className="sigil" style={{ fontSize: 36 }}>☼</div>
        </div>
        <div className="body">
          <h3>Welcome back</h3>
          <p className="lede">
            The pond kept fishing while you were gone. The reeds say it was <b style={{ fontStyle: 'normal', color: 'var(--ink)' }}>{minutes} minute{minutes === 1 ? '' : 's'}</b>; the surface still hasn't settled.
          </p>
          <div className="stat-row" style={{ gridTemplateColumns: '1fr' }}>
            <div className="cell" style={{ textAlign: 'center' }}>
              <div className="lab">Mana collected</div>
              <div className="val" style={{ fontSize: 32, color: 'var(--mana)' }}>+{fmt(mana)}</div>
            </div>
          </div>
          <div className="actions">
            <button className="btn" style={{ flex: 1, justifyContent: 'center' }} onClick={onClose}>Collect</button>
          </div>
        </div>
      </div>
    </div>
  );
}

// ─── Run upgrades ─────────────────────────────────────────────
function UpgradeCard({ upgrade, owned, prereqMet, currency, affordable, onClick, accent }: {
  upgrade: { id: string; name: string; desc: string; cost: number; icon: string };
  owned: boolean;
  prereqMet: boolean;
  currency: string;
  affordable: boolean;
  onClick: () => void;
  accent: string;
}) {
  return (
    <div
      className={`upg-card ${owned ? 'unlocked' : ''} ${affordable && !owned ? 'affordable' : ''} ${!prereqMet && !owned ? 'locked' : ''}`}
      style={{ '--accent': accent } as React.CSSProperties}
    >
      <div className="upg-icon">{upgrade.icon || '✦'}</div>
      <div className="upg-meta">
        <div className="upg-name">{upgrade.name}</div>
        <div className="upg-desc">{upgrade.desc}</div>
      </div>
      <div className="upg-action">
        {owned ? (
          <span className="upg-pill owned">Owned</span>
        ) : (
          <button className="btn btn-sm" disabled={!affordable || !prereqMet} onClick={onClick}>
            <span className="cost">{fmt(upgrade.cost)}</span> {currency}
          </button>
        )}
      </div>
    </div>
  );
}

export function RunUpgradesTab({ state, dispatch }: { state: GameState; dispatch: React.Dispatch<GameAction> }) {
  return (
    <div>
      <div className="tab-intro">
        <span className="ti-eye">Temporary</span>
        <span className="ti-text">These bloom for one run. Renewing the pond returns them to soil.</span>
      </div>
      <div className="upg-list">
        {RUN_UPGRADES.map(u => {
          const owned = state.runUpgrades.includes(u.id);
          const prereqMet = !u.requires || state.runUpgrades.includes(u.requires);
          const affordable = state.mana >= u.cost;
          return (
            <UpgradeCard
              key={u.id}
              upgrade={u}
              owned={owned}
              prereqMet={prereqMet}
              currency="mana"
              affordable={affordable}
              accent="var(--leaf)"
              onClick={() => dispatch({ type: 'buy_run_upgrade', upgrade: u })}
            />
          );
        })}
      </div>
    </div>
  );
}

export function PearlsPanel({ state, dispatch, tab, setTab }: {
  state: GameState;
  dispatch: React.Dispatch<GameAction>;
  tab: string;
  setTab: (t: string) => void;
}) {
  void setTab;
  return (
    <div>
      <div className="pearl-banner">
        <div className="pb-stat">
          <span className="pb-lab">Pearls available</span>
          <span className="pb-val">{state.perles} <span style={{ fontSize: 18, opacity: 0.7 }}>🪸</span></span>
        </div>
        <div className="pb-stat">
          <span className="pb-lab">Earned over</span>
          <span className="pb-val">{state.prestiges} <span style={{ fontSize: 11, opacity: 0.6, marginLeft: 4 }}>renewals</span></span>
        </div>
      </div>
      {tab === 'market' && (
        <div>
          <div className="tab-intro">
            <span className="ti-eye">Permanent · pearls</span>
            <span className="ti-text">Tides remember the wares. Each purchase persists across all renewals.</span>
          </div>
          <div className="upg-list">
            {PEARL_UPGRADES.map(u => {
              const owned = state.pearlUpgrades.includes(u.id);
              const prereqMet = !u.requires || state.pearlUpgrades.includes(u.requires);
              const affordable = state.perles >= u.cost;
              return (
                <UpgradeCard
                  key={u.id}
                  upgrade={u}
                  owned={owned}
                  prereqMet={prereqMet}
                  currency="🪸"
                  affordable={affordable}
                  accent="var(--perle)"
                  onClick={() => dispatch({ type: 'buy_pearl_upgrade', upgrade: u })}
                />
              );
            })}
          </div>
        </div>
      )}
      {tab === 'persist' && (
        <div>
          <div className="tab-intro">
            <span className="ti-eye">Deepest · pearls</span>
            <span className="ti-text">Carry pieces of a run into the next: starting mana, depth, fish.</span>
          </div>
          <div className="upg-list">
            {PRESTIGE_UPGRADES.map(u => {
              const owned = state.prestigeUpgrades.includes(u.id);
              const prereqMet = !u.requires || state.prestigeUpgrades.includes(u.requires);
              const affordable = state.perles >= u.cost;
              return (
                <UpgradeCard
                  key={u.id}
                  upgrade={u}
                  owned={owned}
                  prereqMet={prereqMet}
                  currency="🪸"
                  affordable={affordable}
                  accent="var(--coral)"
                  onClick={() => dispatch({ type: 'buy_prestige_upgrade', upgrade: u })}
                />
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}

export function ChallengesSection() {
  return (
    <div className="challenges">
      <div className="ch-head">
        <div>
          <h3>Daily Challenges</h3>
          <span className="th-sub">Three new tasks at dawn. Each rewards a pearl.</span>
        </div>
        <div className="ch-timer">
          <span className="ct-lab">Next dawn in</span>
          <span className="ct-val">12h 38m</span>
        </div>
      </div>
      <div className="ch-list">
        {DAILY_CHALLENGES.map(ch => {
          const done = ch.progress >= 1;
          return (
            <div key={ch.id} className={`ch-card ${done ? 'done' : ''}`}>
              <div className="ch-icon">{done ? '✓' : '◌'}</div>
              <div className="ch-body">
                <div className="ch-name">{ch.name}</div>
                <div className="ch-desc">{ch.desc}</div>
                <div className="ch-bar"><i style={{ transform: `scaleX(${ch.progress})` }} /></div>
              </div>
              <div className="ch-reward">
                <span className="cr-pl">+{ch.reward}</span>
                <span className="cr-l">🪸</span>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

export function BestiaryPanel({ state }: { state: GameState }) {
  const owned = state.fishOwned;
  return (
    <div className="bestiary">
      {FISH.map(fish => {
        const has = (owned[fish.id] || 0) > 0;
        const entry = BESTIARY[fish.id];
        return (
          <div key={fish.id} className={`bestiary-card ${has ? '' : 'unknown'}`}>
            <div className="placeholder">
              {has ? <span>FISH<br />ART</span> : <span style={{ color: 'transparent' }}>?</span>}
            </div>
            <div className="meta">
              <div className="depth-tag">DEPTH · {String(fish.depth).padStart(2, '0')}</div>
              <div className="name">{has ? fish.name : 'Unobserved'}</div>
              <div className="entry">{has ? entry : 'Encounter a specimen to learn its lore.'}</div>
            </div>
          </div>
        );
      })}
    </div>
  );
}

export function BoostButton({ state, dispatch }: { state: GameState; dispatch: React.Dispatch<GameAction> }) {
  const now = Date.now();
  const active = state.boostUntil > now;
  const leftMs = active ? state.boostUntil - now : 0;
  const leftMin = Math.floor(leftMs / 60000);
  const leftSec = Math.floor((leftMs % 60000) / 1000);
  const canBoost = state.gemmes >= 10 && !active;

  return (
    <button
      className={`boost-btn ${active ? 'active' : ''}`}
      disabled={!canBoost && !active}
      onClick={() => !active && dispatch({ type: 'boost' })}
      title={active ? 'Boost active' : 'Activate +5 min ×2 boost (10 gemmes)'}
    >
      <div className="bb-icon">⚡</div>
      <div className="bb-body">
        <span className="bb-lab">{active ? 'Boost ×2' : 'Boost'}</span>
        <span className="bb-sub">
          {active
            ? `${leftMin}:${String(leftSec).padStart(2, '0')} left`
            : '10 💎 · 5 min'}
        </span>
      </div>
    </button>
  );
}

export function Onboarding({ onDismiss }: { onDismiss: () => void }) {
  return (
    <div className="onboarding">
      <div className="ob-card">
        <div className="ob-sigil">𓆟</div>
        <div className="ob-eyebrow">L'Étang des Merveilles</div>
        <h2>Welcome, keeper.</h2>
        <p>
          The pond is yours. It is also patient. Begin by visiting the <em>Boutique</em> on
          the left to bring fish into the water — they will pay their lodging in mana
          while you watch the surface, or while you sleep.
        </p>
        <div className="ob-steps">
          <div className="ob-step">
            <span className="ob-num">01</span>
            <div><b>Buy fish</b><br /><span>They generate mana automatically.</span></div>
          </div>
          <div className="ob-step">
            <span className="ob-num">02</span>
            <div><b>Dig deeper</b><br /><span>New biomes hold rarer species.</span></div>
          </div>
          <div className="ob-step">
            <span className="ob-num">03</span>
            <div><b>Renew the pond</b><br /><span>Trade depth for pearls — keep what matters.</span></div>
          </div>
        </div>
        <div className="ob-actions">
          <button className="btn gold" onClick={onDismiss}>Begin tending the pond</button>
        </div>
      </div>
    </div>
  );
}
