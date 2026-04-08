import { useGameStore, getPondUpgradeCost, MAX_FISH_LEVEL } from '../store/useGameStore';
import { FISH_TYPES, getNextMilestone } from '../data/fishTypes';
import { computeBonuses } from '../utils/bonuses';
import { formatNumber } from '../utils/formatNumber';
import Decimal from 'break_infinity.js';

const DEPTH_NAMES = [
  'Lac de Surface',
  'Rivière Souterraine',
  'Récif Corallien',
  'Abysses',
  'Zone Hydrothermale',
  'Plaine Abyssale',
  'Fosse des Origines',
  'Nexus de Mana',
  'Cœur Volcanique',
  'Royaume Céleste',
  'Dimension Quantique',
];
const MAX_DEPTH = 10;

/** Nombre de prestiges recommandés AVANT de débloquer cette profondeur */
const DEPTH_PRESTIGE_HINT: Partial<Record<number, number>> = {
  2: 1, 3: 2, 4: 3, 5: 5,
  6: 7, 7: 10, 8: 13, 9: 17, 10: 22,
};
const MILESTONE_LEVELS = [10, 25, 50, 100];

const calcBulkCost = (baseCost: number, fishCostMult: number, alreadyOwned: number, count: number): Decimal => {
  let total = new Decimal(0);
  for (let i = 0; i < count; i++) {
    total = total.plus(new Decimal(baseCost).mul(fishCostMult).mul(new Decimal(1.15).pow(alreadyOwned + i)));
  }
  return total;
};

const calcMaxBuyable = (baseCost: number, fishCostMult: number, alreadyOwned: number, mana: Decimal): number => {
  let count = 0;
  let total = new Decimal(0);
  while (true) {
    const next = new Decimal(baseCost).mul(fishCostMult).mul(new Decimal(1.15).pow(alreadyOwned + count));
    if (total.plus(next).gt(mana)) break;
    total = total.plus(next);
    count++;
    if (count >= 9999) break;
  }
  return count;
};

export const Shop = () => {
  const mana = useGameStore(s => s.mana);
  const poissons = useGameStore(s => s.poissons);
  const pondDepth = useGameStore(s => s.pondDepth);
  const perles = useGameStore(s => s.perles);
  const prestiges = useGameStore(s => s.prestiges);
  const researchUnlocked = useGameStore(s => s.researchUnlocked);
  const pearlUpgradesUnlocked = useGameStore(s => s.pearlUpgradesUnlocked);
  const buyFish = useGameStore(s => s.buyFish);
  const buyFishBulk = useGameStore(s => s.buyFishBulk);
  const upgradeFish = useGameStore(s => s.upgradeFish);
  const upgradePond = useGameStore(s => s.upgradePond);
  const prestige = useGameStore(s => s.prestige);

  const prestigeUpgradesUnlocked = useGameStore(s => s.prestigeUpgradesUnlocked);
  const bonuses = computeBonuses(researchUnlocked, pearlUpgradesUnlocked, prestigeUpgradesUnlocked);
  const isMaxDepth = pondDepth >= MAX_DEPTH;
  const pondUpgradeCost = !isMaxDepth
    ? getPondUpgradeCost(pondDepth).mul(bonuses.pondCostMult)
    : null;
  const canUpgradePond = pondUpgradeCost ? mana.gte(pondUpgradeCost) : false;
  const nextDepth = pondDepth + 1;
  const nextFish = FISH_TYPES.filter(f => f.requiredDepth === nextDepth && !f.requiredPrestiges);
  const canPrestige = pondDepth >= 2;

  const handleBuy = (fish: typeof FISH_TYPES[0]) => {
    const ownedCount = poissons.filter(f => f.type === fish.type).length;
    const cost = new Decimal(fish.baseCost).mul(bonuses.fishCostMult).mul(new Decimal(1.15).pow(ownedCount));
    buyFish(fish.type, fish.baseIncome, cost);
  };

  const handleUpgrade = (id: string, currentLevel: number, type: string) => {
    const baseCost = FISH_TYPES.find(t => t.type === type)?.baseCost || 10;
    upgradeFish(id, new Decimal(baseCost).mul(2).mul(new Decimal(1.5).pow(currentLevel)));
  };

  // Poissons normaux (hors légendaires)
  const normalFish = FISH_TYPES.filter(f => !f.requiredPrestiges);
  // Poissons légendaires
  const legendaryFish = FISH_TYPES.filter(f => f.requiredPrestiges);

  return (
    <div className="flex flex-col gap-6 pointer-events-auto">
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
                <div className="font-semibold text-white mb-1">Prochaine zone — {DEPTH_NAMES[nextDepth]}</div>
                {nextFish.length > 0 && (
                  <div className="text-yellow-300 mt-1 leading-5">
                    {nextFish.map(f => `${f.emoji} ${f.name}`).join('  ·  ')}
                  </div>
                )}
                {bonuses.pondCostMult < 1 && (
                  <div className="text-teal-400 text-[10px] mt-1">
                    Réduction Géologie : -{Math.round((1 - bonuses.pondCostMult) * 100)}%
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
                ⛏️ Creuser ({formatNumber(pondUpgradeCost!)} Mana)
              </button>
              {DEPTH_PRESTIGE_HINT[nextDepth] !== undefined && (() => {
                const hint = DEPTH_PRESTIGE_HINT[nextDepth]!;
                const met = prestiges >= hint;
                return (
                  <div className={`text-xs mt-2 text-center py-1 px-2 rounded border ${
                    met ? 'text-emerald-400/80 border-transparent' : 'text-amber-300/90 bg-amber-950/30 border-amber-700/30'
                  }`}>
                    {met
                      ? `✓ Prêt — ${hint} prestige${hint > 1 ? 's' : ''} recommandé${hint > 1 ? 's' : ''} pour cette zone`
                      : `💡 Recommandé : ${hint} prestige${hint > 1 ? 's' : ''} avant cette zone (vous : ${prestiges})`
                    }
                  </div>
                );
              })()}
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

      {/* Poissons légendaires */}
      {legendaryFish.map(fish => {
        const unlocked = prestiges >= (fish.requiredPrestiges ?? 0) && pondDepth >= fish.requiredDepth;
        const owned = poissons.filter(f => f.type === fish.type).length;
        const atMax = fish.maxOwned !== undefined && owned >= fish.maxOwned;
        const cost = new Decimal(fish.baseCost).mul(bonuses.celestialCostMult);
        const canAfford = !atMax && mana.gte(cost);

        if (!unlocked) return null;

        return (
          <div key={fish.type} className="flex flex-col gap-2">
            <h3 className="text-sm font-semibold text-yellow-400 uppercase tracking-wider">Poisson Légendaire</h3>
            <div className="bg-yellow-900/20 rounded-lg p-4 border border-yellow-700/30">
              <div className="flex justify-between items-start mb-3">
                <div>
                  <h4 className="font-bold text-yellow-200 text-lg flex items-center gap-2">
                    {fish.emoji} {fish.name}
                  </h4>
                  <p className="text-xs text-gray-300">{fish.desc}</p>
                  {atMax && <p className="text-xs text-yellow-500 mt-1">Déjà possédé (unique)</p>}
                </div>
                <span className="text-xs bg-yellow-900/50 text-yellow-300 px-2 py-1 rounded border border-yellow-700/50">
                  {owned} / {fish.maxOwned}
                </span>
              </div>
              <button
                onClick={() => handleBuy(fish)}
                disabled={!canAfford}
                className={`w-full py-2.5 rounded-lg font-bold text-sm transition-all ${
                  canAfford
                    ? 'bg-gradient-to-r from-yellow-600 to-amber-600 hover:from-yellow-500 hover:to-amber-500 text-white'
                    : 'bg-gray-800 text-gray-500 cursor-not-allowed opacity-70'
                }`}
              >
                Acquérir ({formatNumber(cost)} Mana)
              </button>
            </div>
          </div>
        );
      })}

      {/* Acheter des poissons normaux */}
      <div className="flex flex-col gap-4">
        <h3 className="text-sm font-semibold text-gray-400 uppercase tracking-wider">Acheter des poissons</h3>
        {normalFish.map((fish) => {
          const unlocked = pondDepth >= fish.requiredDepth;
          const ownedCount = poissons.filter(f => f.type === fish.type).length;
          const cost1 = new Decimal(fish.baseCost).mul(bonuses.fishCostMult).mul(new Decimal(1.15).pow(ownedCount));
          const cost10 = calcBulkCost(fish.baseCost, bonuses.fishCostMult, ownedCount, 10);
          const maxCount = calcMaxBuyable(fish.baseCost, bonuses.fishCostMult, ownedCount, mana);
          const costMax = calcBulkCost(fish.baseCost, bonuses.fishCostMult, ownedCount, maxCount);

          if (!unlocked) {
            return (
              <div key={fish.type} className="bg-white/[0.03] rounded-lg p-4 border border-white/5 opacity-60">
                <h4 className="font-bold text-gray-500 flex items-center gap-2 mb-1">
                  <span className="grayscale">{fish.emoji}</span>
                  {fish.name}
                  <span className="text-[10px] bg-gray-800 text-gray-500 px-1.5 py-0.5 rounded border border-gray-700">🔒</span>
                </h4>
                <div className="text-xs text-center text-gray-600 bg-black/20 rounded py-1.5 border border-dashed border-gray-700">
                  Profondeur Niv. {fish.requiredDepth} requise — {DEPTH_NAMES[fish.requiredDepth]}
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

              {/* Jalons */}
              <div className="flex gap-1 mb-3">
                {fish.milestones.map(m => (
                  <div
                    key={m.level}
                    title={`Niv. ${m.level} — ${m.label}${m.globalBonus > 0 ? ` | +${m.globalBonus}% global` : ''}`}
                    className={`flex-1 text-center text-[9px] font-bold py-0.5 rounded border ${
                      ownedCount > 0 ? 'text-gray-400 border-gray-700 bg-gray-800/40' : 'text-gray-600 border-gray-800'
                    }`}
                  >
                    {m.level}
                  </div>
                ))}
              </div>

              <div className="flex gap-1.5">
                {[
                  { label: 'x1',  count: 1,        cost: cost1,   canAff: mana.gte(cost1) },
                  { label: 'x10', count: 10,       cost: cost10,  canAff: mana.gte(cost10) },
                  { label: `max (${maxCount})`, count: maxCount, cost: costMax, canAff: maxCount > 0 },
                ].map(({ label, count, cost, canAff }) => (
                  <button
                    key={label}
                    onClick={() => count === 1 ? handleBuy(fish) : buyFishBulk(fish.type, fish.baseIncome, fish.baseCost, count)}
                    disabled={!canAff}
                    className={`flex-1 py-1.5 rounded font-semibold text-xs transition-all ${
                      canAff ? 'bg-blue-600 hover:bg-blue-500 text-white' : 'bg-gray-800 text-gray-500 cursor-not-allowed opacity-70'
                    }`}
                  >
                    {label}<br/>
                    <span className="text-[9px] opacity-80">{count > 0 ? formatNumber(cost) : '–'}</span>
                  </button>
                ))}
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
              const achievedMilestones = (baseTypeInfo?.milestones ?? []).filter(
                m => fish.level >= Math.max(1, m.level - bonuses.milestoneLevelReduction)
              );
              const milestoneMult = achievedMilestones.reduce((acc, m) => acc * m.selfMultiplier, 1);
              const currentIncome = new Decimal(fish.baseIncome).mul(levelMult).mul(milestoneMult);
              const nextM = getNextMilestone(fish, bonuses.milestoneLevelReduction);

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
                      const effectiveLevel = Math.max(1, lvl - bonuses.milestoneLevelReduction);
                      const reached = fish.level >= effectiveLevel;
                      const isNext = nextM?.level === lvl;
                      return (
                        <div
                          key={lvl}
                          title={milestone
                            ? `Niv. ${effectiveLevel} — ${milestone.label}${milestone.globalBonus > 0 ? ` | +${milestone.globalBonus}% global` : ''}`
                            : `Niv. ${lvl}`}
                          className={`flex-1 text-center text-[9px] font-bold py-0.5 rounded border transition-all ${
                            reached
                              ? 'bg-yellow-600/30 text-yellow-300 border-yellow-600/40'
                              : isNext
                                ? 'bg-indigo-800/30 text-indigo-400 border-indigo-600/30 animate-pulse'
                                : 'bg-gray-800/30 text-gray-600 border-gray-700/30'
                          }`}
                        >
                          {effectiveLevel}
                        </div>
                      );
                    })}
                  </div>
                  {nextM && (
                    <div className="text-[10px] text-gray-500 mt-1">
                      Prochain : Niv. {Math.max(1, nextM.level - bonuses.milestoneLevelReduction)} — {nextM.label}
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
