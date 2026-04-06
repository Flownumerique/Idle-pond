import { useGameStore, getPondUpgradeCost, MAX_FISH_LEVEL } from '../store/useGameStore';
import { FISH_TYPES, getNextMilestone } from '../data/fishTypes';
import { formatNumber } from '../utils/formatNumber';
import Decimal from 'break_infinity.js';

const DEPTH_NAMES = ['Peu profond', 'Standard', 'Profond', 'Abyssal', 'Maximum'];
const MAX_DEPTH = 4;
const MILESTONE_LEVELS = [10, 25, 50, 100];

const calcBulkCost = (baseCost: number, alreadyOwned: number, count: number): Decimal => {
  let total = new Decimal(0);
  for (let i = 0; i < count; i++) {
    total = total.plus(new Decimal(baseCost).mul(new Decimal(1.15).pow(alreadyOwned + i)));
  }
  return total;
};

const calcMaxBuyable = (baseCost: number, alreadyOwned: number, mana: Decimal): number => {
  let count = 0;
  let total = new Decimal(0);
  while (true) {
    const next = new Decimal(baseCost).mul(new Decimal(1.15).pow(alreadyOwned + count));
    if (total.plus(next).gt(mana)) break;
    total = total.plus(next);
    count++;
    if (count >= 9999) break;
  }
  return count;
};

export const Shop = () => {
  const mana = useGameStore(state => state.mana);
  const poissons = useGameStore(state => state.poissons);
  const pondDepth = useGameStore(state => state.pondDepth);
  const perles = useGameStore(state => state.perles);
  const buyFish = useGameStore(state => state.buyFish);
  const buyFishBulk = useGameStore(state => state.buyFishBulk);
  const upgradeFish = useGameStore(state => state.upgradeFish);
  const upgradePond = useGameStore(state => state.upgradePond);
  const prestige = useGameStore(state => state.prestige);

  const isMaxDepth = pondDepth >= MAX_DEPTH;
  const pondUpgradeCost = !isMaxDepth ? getPondUpgradeCost(pondDepth) : null;
  const canUpgradePond = pondUpgradeCost ? mana.gte(pondUpgradeCost) : false;
  const nextDepth = pondDepth + 1;
  const nextFish = FISH_TYPES.find(f => f.requiredDepth === nextDepth);
  const canPrestige = pondDepth >= 2;

  const handleBuy = (fish: typeof FISH_TYPES[0]) => {
    const ownedCount = poissons.filter(f => f.type === fish.type).length;
    const cost = new Decimal(fish.baseCost).mul(new Decimal(1.15).pow(ownedCount));
    buyFish(fish.type, fish.baseIncome, cost);
  };

  const handleBuyBulk = (fish: typeof FISH_TYPES[0], count: number) => {
    buyFishBulk(fish.type, fish.baseIncome, fish.baseCost, count);
  };

  const handleUpgrade = (id: string, currentLevel: number, type: string) => {
    const baseCost = FISH_TYPES.find(t => t.type === type)?.baseCost || 10;
    const upgradeCost = new Decimal(baseCost).mul(2).mul(new Decimal(1.5).pow(currentLevel));
    upgradeFish(id, upgradeCost);
  };

  return (
    <div className="bg-slate-900/60 backdrop-blur-md p-6 rounded-xl border border-white/10 shadow-xl pointer-events-auto h-full max-h-[calc(100vh-2rem)] overflow-y-auto w-80 lg:w-96 flex flex-col gap-6">
      <h2 className="text-xl font-bold text-white border-b border-white/10 pb-2">Boutique & Améliorations</h2>

      {/* Amélioration de l'étang */}
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
          {!isMaxDepth ? (
            <>
              <div className="text-xs text-gray-300 mb-3 bg-black/20 rounded p-2 border border-white/5">
                <div className="font-semibold text-white mb-1">Prochaine amélioration — Niv. {nextDepth}</div>
                {nextFish && <div className="text-yellow-300">• Débloque : {nextFish.emoji} {nextFish.name}</div>}
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
            <div className="text-center text-xs text-gray-500 italic py-2">Profondeur maximale atteinte</div>
          )}
        </div>
      </div>

      {/* Prestige */}
      <div className="flex flex-col gap-3">
        <h3 className="text-sm font-semibold text-purple-400 uppercase tracking-wider">Prestige</h3>
        <div className="bg-purple-900/20 rounded-lg p-4 border border-purple-700/30">
          <div className="flex justify-between items-center mb-2">
            <span className="text-sm text-gray-300">Perles accumulées</span>
            <span className="font-bold text-purple-200">🪸 {perles}</span>
          </div>
          <p className="text-xs text-gray-500 mb-3">
            {canPrestige
              ? 'Réinitialisez votre progression contre des Perles permanentes.'
              : 'Atteignez la profondeur 2 pour débloquer le Prestige.'}
          </p>
          <button
            onClick={prestige}
            disabled={!canPrestige}
            className={`w-full py-2 rounded-lg font-bold text-sm transition-all ${
              canPrestige
                ? 'bg-gradient-to-r from-purple-700 to-pink-700 hover:from-purple-600 hover:to-pink-600 text-white'
                : 'bg-gray-800 text-gray-600 cursor-not-allowed opacity-60'
            }`}
          >
            ✨ Prestige (réinitialiser)
          </button>
        </div>
      </div>

      {/* Acheter des poissons */}
      <div className="flex flex-col gap-4">
        <h3 className="text-sm font-semibold text-gray-400 uppercase tracking-wider">Acheter des poissons</h3>
        {FISH_TYPES.map((fish) => {
          const unlocked = pondDepth >= fish.requiredDepth;
          const ownedCount = poissons.filter(f => f.type === fish.type).length;
          const cost1 = new Decimal(fish.baseCost).mul(new Decimal(1.15).pow(ownedCount));
          const cost10 = calcBulkCost(fish.baseCost, ownedCount, 10);
          const maxCount = calcMaxBuyable(fish.baseCost, ownedCount, mana);
          const costMax = calcBulkCost(fish.baseCost, ownedCount, maxCount);

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
              <div className="flex justify-between items-start mb-3">
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

              {/* Jalons de l'espèce */}
              <div className="flex gap-1 mb-3">
                {fish.milestones.map(m => (
                  <div
                    key={m.level}
                    title={`Niv. ${m.level} — ${m.label}${m.globalBonus > 0 ? ` (+${m.globalBonus}% global)` : ''}`}
                    className={`flex-1 text-center text-[9px] font-bold py-0.5 rounded border ${
                      ownedCount > 0
                        ? 'text-gray-400 border-gray-700 bg-gray-800/40'
                        : 'text-gray-600 border-gray-800 bg-transparent'
                    }`}
                  >
                    {m.level}
                  </div>
                ))}
              </div>

              <div className="flex gap-2">
                <button
                  onClick={() => handleBuy(fish)}
                  disabled={!mana.gte(cost1)}
                  className={`flex-1 py-1.5 rounded font-semibold text-xs transition-all ${
                    mana.gte(cost1) ? 'bg-blue-700 hover:bg-blue-600 text-white' : 'bg-gray-800 text-gray-500 cursor-not-allowed opacity-70'
                  }`}
                >
                  x1<br/><span className="text-[10px] opacity-80">{formatNumber(cost1)}</span>
                </button>
                <button
                  onClick={() => handleBuyBulk(fish, 10)}
                  disabled={!mana.gte(cost10)}
                  className={`flex-1 py-1.5 rounded font-semibold text-xs transition-all ${
                    mana.gte(cost10) ? 'bg-blue-600 hover:bg-blue-500 text-white' : 'bg-gray-800 text-gray-500 cursor-not-allowed opacity-70'
                  }`}
                >
                  x10<br/><span className="text-[10px] opacity-80">{formatNumber(cost10)}</span>
                </button>
                <button
                  onClick={() => handleBuyBulk(fish, maxCount)}
                  disabled={maxCount === 0}
                  className={`flex-1 py-1.5 rounded font-semibold text-xs transition-all ${
                    maxCount > 0 ? 'bg-blue-500 hover:bg-blue-400 text-white' : 'bg-gray-800 text-gray-500 cursor-not-allowed opacity-70'
                  }`}
                >
                  max ({maxCount})<br/><span className="text-[10px] opacity-80">{maxCount > 0 ? formatNumber(costMax) : '–'}</span>
                </button>
              </div>
            </div>
          );
        })}
      </div>

      {/* Poissons possédés */}
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
              const isMaxLevel = fish.level >= MAX_FISH_LEVEL;
              const canAfford = !isMaxLevel && mana.gte(upgradeCost);
              const fishEmoji = baseTypeInfo?.emoji || '🐟';
              const fishName = baseTypeInfo?.name || fish.type;
              const levelMult = new Decimal(1.5).pow(fish.level - 1);

              // Multiplicateur de jalons pour ce poisson
              const achievedMilestones = (baseTypeInfo?.milestones ?? []).filter(m => fish.level >= m.level);
              const milestoneMult = achievedMilestones.reduce((acc, m) => acc * m.selfMultiplier, 1);

              const currentIncome = new Decimal(fish.baseIncome).mul(levelMult).mul(milestoneMult);
              const nextM = getNextMilestone(fish);

              return (
                <div key={fish.id} className="bg-black/40 rounded-lg p-3 border border-white/5">
                  <div className="flex items-center justify-between gap-2 mb-2">
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <span>{fishEmoji}</span>
                        <h4 className="font-bold text-sm text-gray-200">{fishName}</h4>
                        <span className={`text-[10px] px-1.5 py-0.5 rounded border ${
                          isMaxLevel
                            ? 'bg-yellow-900/50 text-yellow-300 border-yellow-700/50'
                            : 'bg-indigo-900/50 text-indigo-300 border-indigo-700/50'
                        }`}>
                          {isMaxLevel ? 'MAX' : `Lvl ${fish.level}`}
                        </span>
                      </div>
                      <p className="text-xs text-blue-300/70">{formatNumber(currentIncome)} Mana/s</p>
                    </div>
                    <button
                      onClick={() => handleUpgrade(fish.id, fish.level, fish.type)}
                      disabled={!canAfford}
                      className={`px-3 py-1.5 rounded text-xs font-semibold shadow transition-all shrink-0 ${
                        isMaxLevel
                          ? 'bg-yellow-900/40 text-yellow-600 cursor-not-allowed border border-yellow-800/30'
                          : canAfford
                            ? 'bg-indigo-600 hover:bg-indigo-500 text-white shadow-indigo-900/50'
                            : 'bg-gray-800 text-gray-500 cursor-not-allowed'
                      }`}
                    >
                      {isMaxLevel ? 'Max' : <>Améliorer<br/>({formatNumber(upgradeCost)})</>}
                    </button>
                  </div>

                  {/* Barre de jalons */}
                  <div className="flex gap-1">
                    {MILESTONE_LEVELS.map(lvl => {
                      const milestone = baseTypeInfo?.milestones.find(m => m.level === lvl);
                      const reached = fish.level >= lvl;
                      const isNext = nextM?.level === lvl;
                      return (
                        <div
                          key={lvl}
                          title={milestone ? `Niv. ${lvl} — ${milestone.label}${milestone.globalBonus > 0 ? ` | +${milestone.globalBonus}% global` : ''}` : `Niv. ${lvl}`}
                          className={`flex-1 text-center text-[9px] font-bold py-0.5 rounded border transition-all ${
                            reached
                              ? 'bg-yellow-600/30 text-yellow-300 border-yellow-600/40'
                              : isNext
                                ? 'bg-indigo-800/30 text-indigo-400 border-indigo-600/30 animate-pulse'
                                : 'bg-gray-800/30 text-gray-600 border-gray-700/30'
                          }`}
                        >
                          {lvl}
                        </div>
                      );
                    })}
                  </div>
                  {nextM && (
                    <div className="text-[10px] text-gray-500 mt-1">
                      Prochain jalon : Niv. {nextM.level} — {nextM.label}
                      {nextM.globalBonus > 0 && <span className="text-teal-500"> +{nextM.globalBonus}% global</span>}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
};
