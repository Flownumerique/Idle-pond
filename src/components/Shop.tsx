import { useGameStore, getPondUpgradeCost, getPondSize } from '../store/useGameStore';
import { formatNumber } from '../utils/formatNumber';
import Decimal from 'break_infinity.js';

interface FishType {
  type: string;
  baseIncome: number;
  baseCost: number;
  name: string;
  desc: string;
  requiredDepth: number;
  emoji: string;
  depthLabel: string;
}

const FISH_TYPES: FishType[] = [
  {
    type: 'gold',
    baseIncome: 1,
    baseCost: 10,
    name: 'Poisson Or',
    desc: 'Produit 1 Mana/s',
    requiredDepth: 0,
    emoji: '🐟',
    depthLabel: 'Eaux peu profondes',
  },
  {
    type: 'ruby',
    baseIncome: 10,
    baseCost: 150,
    name: 'Poisson Rubis',
    desc: 'Produit 10 Mana/s',
    requiredDepth: 1,
    emoji: '🐠',
    depthLabel: 'Eaux intermédiaires',
  },
  {
    type: 'diamond',
    baseIncome: 100,
    baseCost: 5000,
    name: 'Poisson Diamant',
    desc: 'Produit 100 Mana/s',
    requiredDepth: 2,
    emoji: '🐡',
    depthLabel: 'Eaux profondes',
  },
  {
    type: 'abyssal',
    baseIncome: 1000,
    baseCost: 100000,
    name: 'Poisson Abyssal',
    desc: 'Produit 1 000 Mana/s',
    requiredDepth: 3,
    emoji: '🦑',
    depthLabel: 'Abysses',
  },
];

const DEPTH_NAMES = ['Peu profond', 'Standard', 'Profond', 'Abyssal', 'Maximum'];

export const Shop = () => {
  const mana = useGameStore(state => state.mana);
  const poissons = useGameStore(state => state.poissons);
  const pondDepth = useGameStore(state => state.pondDepth);
  const buyFish = useGameStore(state => state.buyFish);
  const upgradeFish = useGameStore(state => state.upgradeFish);
  const upgradePond = useGameStore(state => state.upgradePond);

  const pondSize = getPondSize(pondDepth);
  const maxDepth = 4;
  const isMaxDepth = pondDepth >= maxDepth;
  const pondUpgradeCost = !isMaxDepth ? getPondUpgradeCost(pondDepth) : null;
  const canUpgradePond = pondUpgradeCost ? mana.gte(pondUpgradeCost) : false;
  const nextDepth = pondDepth + 1;
  const nextFish = FISH_TYPES.find(f => f.requiredDepth === nextDepth);

  const handleBuy = (fish: FishType) => {
    const ownedCount = poissons.filter(f => f.type === fish.type).length;
    const cost = new Decimal(fish.baseCost).mul(new Decimal(1.15).pow(ownedCount));
    buyFish(fish.type, fish.baseIncome, cost);
  };

  const handleUpgrade = (id: string, currentLevel: number, type: string) => {
    const baseCost = FISH_TYPES.find(t => t.type === type)?.baseCost || 10;
    const upgradeCost = new Decimal(baseCost).mul(2).mul(new Decimal(1.5).pow(currentLevel));
    upgradeFish(id, upgradeCost);
  };

  return (
    <div className="bg-slate-900/60 backdrop-blur-md p-6 rounded-xl border border-white/10 shadow-xl pointer-events-auto h-full max-h-[calc(100vh-2rem)] overflow-y-auto w-80 lg:w-96 flex flex-col gap-6">
      <h2 className="text-xl font-bold text-white border-b border-white/10 pb-2">Boutique & Améliorations</h2>

      {/* Section amélioration de l'étang */}
      <div className="flex flex-col gap-3">
        <h3 className="text-sm font-semibold text-teal-400 uppercase tracking-wider">Améliorer l'Étang</h3>
        <div className="bg-teal-900/20 rounded-lg p-4 border border-teal-700/30">
          <div className="flex items-center justify-between mb-3">
            <div>
              <span className="text-xs text-teal-400 uppercase tracking-wider">Profondeur actuelle</span>
              <div className="font-bold text-teal-200 text-lg">
                Niv. {pondDepth} — {DEPTH_NAMES[pondDepth] ?? 'Maximum'}
              </div>
            </div>
            <div className="text-3xl">🏊</div>
          </div>

          <div className="flex justify-between text-xs text-gray-400 mb-3">
            <span>Capacité : {poissons.length} / {pondSize} poissons</span>
          </div>

          {!isMaxDepth ? (
            <>
              <div className="text-xs text-gray-300 mb-2 bg-black/20 rounded p-2 border border-white/5">
                <div className="font-semibold text-white mb-1">Prochaine amélioration — Niv. {nextDepth}</div>
                <div className="text-gray-400">
                  • Capacité : {getPondSize(nextDepth)} poissons
                </div>
                {nextFish && (
                  <div className="text-yellow-300 mt-1">
                    • Débloque : {nextFish.emoji} {nextFish.name} ({nextFish.depthLabel})
                  </div>
                )}
              </div>
              <button
                onClick={upgradePond}
                disabled={!canUpgradePond}
                className={`w-full py-2.5 rounded-lg font-bold text-sm transition-all shadow flex items-center justify-center gap-2 ${
                  canUpgradePond
                    ? 'bg-gradient-to-r from-teal-600 to-cyan-600 hover:from-teal-500 hover:to-cyan-500 text-white hover:scale-[1.02]'
                    : 'bg-gray-800 text-gray-500 cursor-not-allowed opacity-70'
                }`}
              >
                ⛏️ Creuser plus profond ({formatNumber(pondUpgradeCost!)} Mana)
              </button>
            </>
          ) : (
            <div className="text-center text-xs text-gray-500 italic py-2">
              Profondeur maximale atteinte
            </div>
          )}
        </div>
      </div>

      {/* Section achat de poissons */}
      <div className="flex flex-col gap-4">
        <h3 className="text-sm font-semibold text-gray-400 uppercase tracking-wider">Acheter des poissons</h3>
        {FISH_TYPES.map((fish) => {
          const unlocked = pondDepth >= fish.requiredDepth;
          const ownedCount = poissons.filter(f => f.type === fish.type).length;
          const currentCost = new Decimal(fish.baseCost).mul(new Decimal(1.15).pow(ownedCount));
          const canAfford = mana.gte(currentCost) && poissons.length < pondSize;

          if (!unlocked) {
            return (
              <div key={fish.type} className="bg-white/[0.03] rounded-lg p-4 border border-white/5 opacity-60">
                <div className="flex justify-between items-start mb-2">
                  <div>
                    <h4 className="font-bold text-gray-500 flex items-center gap-2">
                      <span className="grayscale">{fish.emoji}</span>
                      {fish.name}
                      <span className="text-[10px] bg-gray-800 text-gray-500 px-1.5 py-0.5 rounded border border-gray-700">🔒</span>
                    </h4>
                    <p className="text-xs text-gray-600">{fish.desc}</p>
                  </div>
                </div>
                <div className="text-xs text-center text-gray-600 bg-black/20 rounded py-1.5 border border-dashed border-gray-700">
                  Déverrouillé à la profondeur Niv. {fish.requiredDepth} — {DEPTH_NAMES[fish.requiredDepth]}
                </div>
              </div>
            );
          }

          return (
            <div key={fish.type} className="bg-white/5 rounded-lg p-4 border border-white/5 transition hover:bg-white/10">
              <div className="flex justify-between items-start mb-2">
                <div>
                  <h4 className="font-bold text-blue-200 flex items-center gap-2">
                    {fish.emoji} {fish.name}
                  </h4>
                  <p className="text-xs text-gray-400">{fish.desc}</p>
                </div>
                <div className="text-xs bg-black/30 px-2 py-1 rounded text-gray-300 border border-white/10">
                  {ownedCount} possédés
                </div>
              </div>
              <button
                onClick={() => handleBuy(fish)}
                disabled={!canAfford}
                className={`w-full py-2 rounded font-semibold text-sm transition-all duration-200 shadow flex items-center justify-center gap-2 ${
                  canAfford
                    ? 'bg-blue-600 hover:bg-blue-500 text-white shadow-blue-900/50 hover:shadow-blue-500/50'
                    : 'bg-gray-800 text-gray-500 cursor-not-allowed opacity-70'
                }`}
              >
                Acheter ({formatNumber(currentCost)} Mana)
              </button>
            </div>
          );
        })}
      </div>

      {/* Section poissons possédés */}
      <div className="flex flex-col gap-4">
        <h3 className="text-sm font-semibold text-gray-400 uppercase tracking-wider">Vos Poissons</h3>
        {poissons.length === 0 ? (
          <p className="text-sm text-gray-500 italic text-center py-4 bg-black/20 rounded-lg border border-dashed border-white/10">
            Votre bassin est vide.
          </p>
        ) : (
          <div className="space-y-3">
            {poissons.map(fish => {
              const baseTypeInfo = FISH_TYPES.find(t => t.type === fish.type);
              const baseCost = baseTypeInfo?.baseCost || 10;
              const upgradeCost = new Decimal(baseCost).mul(2).mul(new Decimal(1.5).pow(fish.level));
              const canAfford = mana.gte(upgradeCost);
              const fishEmoji = baseTypeInfo?.emoji || '🐟';
              const fishName = baseTypeInfo?.name || fish.type;
              const multiplier = new Decimal(1.5).pow(fish.level - 1);
              const currentIncome = new Decimal(fish.baseIncome).mul(multiplier);

              return (
                <div key={fish.id} className="bg-black/40 rounded-lg p-3 border border-white/5 flex items-center justify-between gap-2">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-1">
                      <span>{fishEmoji}</span>
                      <h4 className="font-bold text-sm text-gray-200">{fishName}</h4>
                      <span className="text-[10px] bg-indigo-900/50 text-indigo-300 px-1.5 py-0.5 rounded border border-indigo-700/50">Lvl {fish.level}</span>
                    </div>
                    <p className="text-xs text-blue-300/70">{formatNumber(currentIncome)} Mana/s</p>
                  </div>
                  <button
                    onClick={() => handleUpgrade(fish.id, fish.level, fish.type)}
                    disabled={!canAfford}
                    className={`px-3 py-1.5 rounded text-xs font-semibold shadow transition-all ${
                      canAfford
                        ? 'bg-indigo-600 hover:bg-indigo-500 text-white shadow-indigo-900/50'
                        : 'bg-gray-800 text-gray-500 cursor-not-allowed'
                    }`}
                  >
                    Améliorer<br/>({formatNumber(upgradeCost)})
                  </button>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
};
