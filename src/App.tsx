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
