import { useGameStore } from '../store/useGameStore';
import { PRESTIGE_UPGRADES, type PrestigeUpgradeDef } from '../data/prestigeUpgrades';

const TIER_META = {
  1: { label: 'Palier I — Qualité de vie', color: 'text-purple-300', bg: 'bg-purple-900/15', border: 'border-purple-700/25' },
  2: { label: 'Palier II — Impactant',     color: 'text-pink-300',   bg: 'bg-pink-900/15',   border: 'border-pink-700/25'   },
  3: { label: 'Palier III — Puissant',     color: 'text-rose-300',   bg: 'bg-rose-900/15',   border: 'border-rose-700/25'   },
} as const;

function UpgradeCard({ p, owned, canBuy }: {
  p: PrestigeUpgradeDef;
  owned: boolean;
  canBuy: boolean;
}) {
  const buy = useGameStore(s => s.buyPrestigeUpgrade);
  const tier = TIER_META[p.tier];

  return (
    <div className={`rounded-lg p-3 border ${owned ? 'opacity-55' : tier.bg} ${tier.border}`}>
      <div className="flex items-start justify-between gap-2">
        <div className="flex-1">
          <div className={`font-semibold text-sm ${owned ? 'text-gray-400' : 'text-white'}`}>
            {owned ? '✓ ' : ''}{p.name}
          </div>
          <div className="text-xs text-gray-300 mt-0.5">{p.description}</div>
          <div className="text-[10px] text-gray-600 mt-1 italic">{p.flavour}</div>
          {p.requires && !owned && (
            <div className="text-[10px] text-gray-600 mt-0.5">
              Prérequis : {PRESTIGE_UPGRADES.find(x => x.id === p.requires)?.name}
            </div>
          )}
        </div>
        {!owned && (
          <button
            onClick={() => buy(p.id)}
            disabled={!canBuy}
            className={`shrink-0 px-2.5 py-1.5 rounded text-xs font-bold transition-all ${
              canBuy
                ? 'bg-gradient-to-br from-purple-600 to-pink-600 hover:from-purple-500 hover:to-pink-500 text-white'
                : 'bg-gray-800 text-gray-500 cursor-not-allowed opacity-60'
            }`}
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
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-white uppercase tracking-wider">Améliorations de Prestige</h3>
        <span className="text-xs text-purple-300 font-bold">{perles} 🪸</span>
      </div>

      {prestiges === 0 ? (
        <div className="bg-black/20 rounded-lg p-4 border border-dashed border-white/10 text-center">
          <div className="text-2xl mb-2">🪸</div>
          <p className="text-sm text-gray-500">
            Effectuez votre premier Prestige pour débloquer ces améliorations permanentes.
          </p>
        </div>
      ) : (
        <p className="text-xs text-gray-500 -mt-2">
          Ces améliorations survivent à tous les Prestiges et rendent chaque run plus puissant.
        </p>
      )}

      {prestiges > 0 && tiers.map(({ tier, items }) => {
        const meta = TIER_META[tier];
        return (
          <div key={tier}>
            <div className={`text-xs font-bold uppercase tracking-widest ${meta.color} mb-2`}>{meta.label}</div>
            <div className="flex flex-col gap-2">
              {items.map(p => {
                const owned = prestigeUpgradesUnlocked.includes(p.id);
                const prereqMet = !p.requires || prestigeUpgradesUnlocked.includes(p.requires);
                const canBuy = !owned && prereqMet && perles >= p.cost;
                return (
                  <UpgradeCard key={p.id} p={p} owned={owned} canBuy={canBuy} />
                );
              })}
            </div>
          </div>
        );
      })}
    </div>
  );
};
