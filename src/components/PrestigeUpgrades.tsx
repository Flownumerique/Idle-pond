import { useGameStore } from '../store/useGameStore';
import { PRESTIGE_UPGRADES, type PrestigeUpgradeDef } from '../data/prestigeUpgrades';

const TIER_META = {
  1: { label: 'Palier I — Qualité de vie', hex: '#6B47F0' },
  2: { label: 'Palier II — Impactant',     hex: '#C13E81' },
  3: { label: 'Palier III — Puissant',     hex: '#E5484D' },
} as const;

function UpgradeCard({ p, owned, canBuy }: {
  p: PrestigeUpgradeDef;
  owned: boolean;
  canBuy: boolean;
}) {
  const buy = useGameStore(s => s.buyPrestigeUpgrade);

  return (
    <div className={`lg-card${owned ? ' lg-card--done' : canBuy ? ' lg-card--hot' : ''}`}>
      <div className="lg-card-row">
        <div style={{ flex: 1, minWidth: 0 }}>
          <div className="lg-card-title">{owned ? '✓ ' : ''}{p.name}</div>
          <div className="lg-card-desc">{p.description}</div>
          <div className="lg-card-flavour">{p.flavour}</div>
          {p.requires && !owned && (
            <div className="lg-card-prereq">
              Prérequis : {PRESTIGE_UPGRADES.find(x => x.id === p.requires)?.name}
            </div>
          )}
        </div>
        {!owned && (
          <button
            onClick={() => buy(p.id)}
            disabled={!canBuy}
            className="cbtn cbtn--violet cbtn--xs"
            style={{ flexShrink: 0 }}
          >
            {p.cost} 🪸
          </button>
        )}
      </div>
    </div>
  );
}

export const PrestigeUpgrades = () => {
  const perles = useGameStore(s => s.perles);
  const prestigeUpgradesUnlocked = useGameStore(s => s.prestigeUpgradesUnlocked);
  const prestiges = useGameStore(s => s.prestiges);

  const tiers = ([1, 2, 3] as const).map(tier => ({
    tier,
    items: PRESTIGE_UPGRADES.filter(p => p.tier === tier),
  }));

  return (
    <div className="lg-panel">
      <div className="lg-panel-head">
        <h3>Améliorations de Prestige</h3>
        <span className="lg-chip" style={{ color: '#A83A72', background: '#FCE3EE' }}>{perles} 🪸</span>
      </div>

      {prestiges === 0 ? (
        <div className="lg-card" style={{ textAlign: 'center' }}>
          <div style={{ fontSize: 30, marginBottom: 6 }}>🪸</div>
          <p className="lg-card-desc">
            Effectuez votre premier Prestige pour débloquer ces améliorations permanentes.
          </p>
        </div>
      ) : (
        <p className="lg-panel-note">
          Ces améliorations survivent à tous les Prestiges et rendent chaque run plus puissant.
        </p>
      )}

      {prestiges > 0 && tiers.map(({ tier, items }) => {
        const meta = TIER_META[tier];
        return (
          <div key={tier} className="lg-panel" style={{ gap: 8 }}>
            <div className="lg-section-eyebrow" style={{ color: meta.hex }}>{meta.label}</div>
            {items.map(p => {
              const owned = prestigeUpgradesUnlocked.includes(p.id);
              const prereqMet = !p.requires || prestigeUpgradesUnlocked.includes(p.requires);
              const canBuy = !owned && prereqMet && perles >= p.cost;
              return (
                <UpgradeCard key={p.id} p={p} owned={owned} canBuy={canBuy} />
              );
            })}
          </div>
        );
      })}
    </div>
  );
};
