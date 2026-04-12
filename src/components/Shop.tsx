import { useState } from 'react';
import { useGameStore, MAX_FISH_LEVEL } from '../store/useGameStore';
import { FISH_TYPES } from '../data/fishTypes';
import { computeBonuses } from '../utils/bonuses';
import { formatNumber } from '../utils/formatNumber';
import Decimal from 'break_infinity.js';

const DEPTH_NAMES = [
  'Lac de Surface', 'Rivière Souterraine', 'Récif Corallien', 'Océan des Profondeurs',
  'Abysses', 'Zone Hydrothermale', 'Plaine Abyssale', 'Fosse des Origines',
  'Nexus de Mana', 'Cœur Volcanique', 'Royaume Céleste', 'Dimension Quantique',
];
const MILESTONE_LEVELS = [10, 25, 50, 100];
const UPGRADE_QTYS = [1, 5, 10, 25, 'max'] as const;
type UpgradeQty = typeof UPGRADE_QTYS[number];

const calcUpgradeCostN = (baseCost: number, currentLevel: number, n: number): Decimal => {
  let total = new Decimal(0);
  for (let i = 0; i < n; i++) {
    if (currentLevel + i >= MAX_FISH_LEVEL) break;
    total = total.plus(new Decimal(baseCost).mul(2).mul(new Decimal(1.5).pow(currentLevel + i)));
  }
  return total;
};

const calcMaxUpgrades = (baseCost: number, currentLevel: number, mana: Decimal): number => {
  let count = 0;
  let total = new Decimal(0);
  while (currentLevel + count < MAX_FISH_LEVEL) {
    const next = new Decimal(baseCost).mul(2).mul(new Decimal(1.5).pow(currentLevel + count));
    if (total.plus(next).gt(mana)) break;
    total = total.plus(next);
    count++;
  }
  return count;
};

const milestoneBonus = (selfMult: number, globalBonus: number): string => {
  const parts: string[] = [];
  if (selfMult > 1) parts.push(`×${selfMult} revenus`);
  if (globalBonus > 0) parts.push(`+${globalBonus}% global`);
  return parts.join(' · ');
};

export const Shop = () => {
  const mana = useGameStore(s => s.mana);
  const poissons = useGameStore(s => s.poissons);
  const pondDepth = useGameStore(s => s.pondDepth);
  const prestiges = useGameStore(s => s.prestiges);
  const researchUnlocked = useGameStore(s => s.researchUnlocked);
  const pearlUpgradesUnlocked = useGameStore(s => s.pearlUpgradesUnlocked);
  const prestigeUpgradesUnlocked = useGameStore(s => s.prestigeUpgradesUnlocked);
  const runUpgradesOwned = useGameStore(s => s.runUpgradesOwned);
  const buyFish = useGameStore(s => s.buyFish);
  const upgradeFishN = useGameStore(s => s.upgradeFishN);

  const bonuses = computeBonuses(researchUnlocked, pearlUpgradesUnlocked, prestigeUpgradesUnlocked, runUpgradesOwned);

  const handleBuy = (fish: typeof FISH_TYPES[0]) => {
    const cost = new Decimal(fish.baseCost).mul(bonuses.fishCostMult);
    buyFish(fish.type, fish.baseIncome, cost);
  };

  const handleUpgrade = (fish: typeof FISH_TYPES[0], instance: { id: string; level: number }, qty: UpgradeQty) => {
    const maxN = calcMaxUpgrades(fish.baseCost, instance.level, mana);
    const n = qty === 'max' ? maxN : Math.min(qty, MAX_FISH_LEVEL - instance.level, maxN > 0 ? qty : 0);
    if (n <= 0) return;
    const totalCost = calcUpgradeCostN(fish.baseCost, instance.level, n);
    upgradeFishN(instance.id, n, totalCost);
  };

  const normalFish = FISH_TYPES.filter(f => !f.requiredPrestiges);
  const legendaryFish = FISH_TYPES.filter(f => f.requiredPrestiges);

  return (
    <div className="flex flex-col gap-6 pointer-events-auto">
      <h2 className="text-xl font-bold text-white border-b border-white/10 pb-2">Boutique</h2>

      {/* Poissons légendaires */}
      {legendaryFish.map(fish => {
        const unlocked = prestiges >= (fish.requiredPrestiges ?? 0) && pondDepth >= fish.requiredDepth;
        if (!unlocked) return null;
        const instance = poissons.find(f => f.type === fish.type) ?? null;
        const atMax = instance !== null && fish.maxOwned !== undefined;
        const buyCost = new Decimal(fish.baseCost).mul(bonuses.celestialCostMult);
        const canAffordBuy = !atMax && mana.gte(buyCost);

        return (
          <div key={fish.type} className="flex flex-col gap-2">
            <h3 className="text-sm font-semibold text-yellow-400 uppercase tracking-wider">Poisson Légendaire</h3>
            <FishCard
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
          </div>
        );
      })}

      {/* Poissons normaux */}
      <div className="flex flex-col gap-3">
        <h3 className="text-sm font-semibold text-gray-400 uppercase tracking-wider">Poissons</h3>
        {normalFish.map(fish => {
          const unlocked = pondDepth >= fish.requiredDepth;
          const instance = poissons.find(f => f.type === fish.type) ?? null;
          const buyCost = new Decimal(fish.baseCost).mul(bonuses.fishCostMult);
          const canAffordBuy = instance === null && mana.gte(buyCost);

          if (!unlocked) {
            return (
              <div key={fish.type} className="bg-white/[0.03] rounded-lg p-4 border border-white/5 opacity-50">
                <div className="flex items-center gap-2">
                  {fish.sprite
                    ? <img src={fish.sprite} alt={fish.name} className="w-6 h-6 object-contain grayscale opacity-50" />
                    : <span className="grayscale text-xl">{fish.emoji}</span>}
                  <div>
                    <span className="font-bold text-gray-500 text-sm">{fish.name}</span>
                    <span className="ml-2 text-[10px] bg-gray-800 text-gray-600 px-1.5 py-0.5 rounded border border-gray-700">🔒</span>
                  </div>
                </div>
                <div className="text-xs text-gray-600 mt-2 bg-black/20 rounded py-1.5 px-2 border border-dashed border-gray-700">
                  Profondeur Niv. {fish.requiredDepth} requise — {DEPTH_NAMES[fish.requiredDepth]}
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
};

// ── Unified fish card ──────────────────────────────────────────────

interface FishCardProps {
  fish: typeof FISH_TYPES[0];
  instance: { id: string; level: number; baseIncome: number } | null;
  mana: Decimal;
  bonuses: ReturnType<typeof computeBonuses>;
  onBuy: () => void;
  onUpgrade: (qty: UpgradeQty) => void;
  canAffordBuy: boolean;
  buyCost: Decimal;
  variant: 'normal' | 'legendary';
}

function FishCard({
  fish, instance, mana, bonuses,
  onBuy, onUpgrade,
  canAffordBuy, buyCost, variant,
}: FishCardProps) {
  const [upgradeQty, setUpgradeQty] = useState<UpgradeQty>(1);

  const owned = instance !== null;
  const isMaxLevel = owned && instance.level >= MAX_FISH_LEVEL;

  const levelMult = owned ? new Decimal(1.5).pow(instance.level - 1) : new Decimal(1);
  const achievedMilestones = owned
    ? fish.milestones.filter(m => instance.level >= Math.max(1, m.level - bonuses.milestoneLevelReduction))
    : [];
  const milestoneMult = achievedMilestones.reduce((acc, m) => acc * m.selfMultiplier, 1);
  const currentIncome = owned
    ? new Decimal(instance.baseIncome).mul(levelMult).mul(milestoneMult)
    : new Decimal(fish.baseIncome);

  const maxN = owned ? calcMaxUpgrades(fish.baseCost, instance.level, mana) : 0;
  const upgradeCount = owned
    ? (upgradeQty === 'max'
        ? maxN
        : Math.min(upgradeQty, MAX_FISH_LEVEL - instance.level, maxN > 0 ? upgradeQty : 0))
    : 0;
  const upgradeCostTotal = owned && upgradeCount > 0
    ? calcUpgradeCostN(fish.baseCost, instance.level, upgradeCount)
    : new Decimal(0);
  const canAffordUpgrade = owned && !isMaxLevel && upgradeCount > 0 && mana.gte(upgradeCostTotal);

  const nextMilestone = owned
    ? fish.milestones.find(m => instance.level < Math.max(1, m.level - bonuses.milestoneLevelReduction))
    : fish.milestones[0];

  const borderClass = variant === 'legendary'
    ? 'border-yellow-700/40'
    : owned ? 'border-indigo-700/30' : 'border-white/10';
  const bgClass = variant === 'legendary'
    ? 'bg-yellow-900/15'
    : owned ? 'bg-indigo-900/10' : 'bg-white/[0.03]';

  return (
    <div className={`rounded-lg p-4 border transition-all ${bgClass} ${borderClass}`}>
      {/* Header */}
      <div className="flex items-start justify-between gap-2 mb-3">
        <div className="flex items-center gap-2">
          {fish.sprite
            ? <img src={fish.sprite} alt={fish.name} className="w-8 h-8 object-contain drop-shadow-md" />
            : <span className="text-2xl">{fish.emoji}</span>}
          <div>
            <div className={`font-bold text-sm ${variant === 'legendary' ? 'text-yellow-200' : owned ? 'text-indigo-200' : 'text-gray-200'}`}>
              {fish.name}
            </div>
            {owned ? (
              <div className="text-xs text-blue-300/80">{formatNumber(currentIncome)} Mana/s</div>
            ) : (
              <div className="text-xs text-gray-500 leading-tight line-clamp-2">{fish.desc}</div>
            )}
          </div>
        </div>
        {owned && (
          <span className={`shrink-0 text-[10px] px-2 py-0.5 rounded border font-bold ${
            isMaxLevel
              ? 'bg-yellow-900/50 text-yellow-300 border-yellow-700/50'
              : 'bg-indigo-900/50 text-indigo-300 border-indigo-700/50'
          }`}>
            {isMaxLevel ? 'MAX' : `Niv. ${instance.level}`}
          </span>
        )}
      </div>

      {/* Milestone bar */}
      <div className="flex gap-1 mb-1">
        {MILESTONE_LEVELS.map(lvl => {
          const milestone = fish.milestones.find(m => m.level === lvl);
          const effectiveLevel = Math.max(1, lvl - bonuses.milestoneLevelReduction);
          const reached = owned && instance.level >= effectiveLevel;
          const isNext = nextMilestone?.level === lvl;
          return (
            <div
              key={lvl}
              title={milestone
                ? `Niv. ${effectiveLevel} — ${milestone.label} (${milestoneBonus(milestone.selfMultiplier, milestone.globalBonus)})`
                : `Niv. ${effectiveLevel}`}
              className={`flex-1 text-center text-[9px] font-bold py-0.5 rounded border transition-all ${
                reached
                  ? 'bg-yellow-600/30 text-yellow-300 border-yellow-600/40'
                  : isNext
                    ? 'bg-indigo-800/30 text-indigo-400 border-indigo-600/30 animate-pulse'
                    : 'bg-gray-800/30 text-gray-600 border-gray-700/20'
              }`}
            >
              {effectiveLevel}
            </div>
          );
        })}
      </div>

      {/* Next milestone description */}
      {nextMilestone && (
        <div className="text-[10px] text-gray-500 mb-3 leading-tight">
          {owned
            ? <>Prochain palier : <span className="text-indigo-400">Niv. {Math.max(1, nextMilestone.level - bonuses.milestoneLevelReduction)}</span> — {nextMilestone.label} <span className="text-yellow-400">({milestoneBonus(nextMilestone.selfMultiplier, nextMilestone.globalBonus)})</span></>
            : <><span className="text-gray-600">1er palier :</span> Niv. {Math.max(1, nextMilestone.level - bonuses.milestoneLevelReduction)} — {nextMilestone.label} <span className="text-gray-600">({milestoneBonus(nextMilestone.selfMultiplier, nextMilestone.globalBonus)})</span></>
          }
        </div>
      )}

      {/* Action */}
      {!owned ? (
        <button
          onClick={onBuy}
          disabled={!canAffordBuy}
          className={`w-full py-2 rounded-lg font-bold text-sm transition-all ${
            canAffordBuy
              ? variant === 'legendary'
                ? 'bg-gradient-to-r from-yellow-600 to-amber-600 hover:from-yellow-500 hover:to-amber-500 text-white'
                : 'bg-blue-600 hover:bg-blue-500 text-white'
              : 'bg-gray-800 text-gray-500 cursor-not-allowed opacity-70'
          }`}
        >
          Acheter — {formatNumber(buyCost)} Mana
        </button>
      ) : isMaxLevel ? (
        <div className="text-center text-xs text-yellow-500/70 py-2 bg-yellow-900/10 rounded-lg border border-yellow-800/20">
          ✦ Niveau maximum atteint
        </div>
      ) : (
        <>
          <div className="flex gap-1 mb-2">
            {UPGRADE_QTYS.map(q => (
              <button
                key={q}
                onClick={() => setUpgradeQty(q)}
                className={`flex-1 py-1 text-[10px] font-bold rounded border transition-all ${
                  upgradeQty === q
                    ? 'bg-indigo-600 border-indigo-500 text-white'
                    : 'bg-gray-800/60 border-gray-700/40 text-gray-400 hover:bg-gray-700/60 hover:text-gray-200'
                }`}
              >
                {q === 'max' ? 'Max' : `×${q}`}
              </button>
            ))}
          </div>
          <button
            onClick={() => onUpgrade(upgradeQty)}
            disabled={!canAffordUpgrade}
            className={`w-full py-2 rounded-lg font-bold text-sm transition-all ${
              canAffordUpgrade
                ? 'bg-indigo-600 hover:bg-indigo-500 text-white'
                : 'bg-gray-800 text-gray-500 cursor-not-allowed opacity-70'
            }`}
          >
            {canAffordUpgrade
              ? <>Améliorer {upgradeQty === 'max' ? `×${maxN}` : upgradeCount > 0 ? `×${upgradeCount}` : ''} — {formatNumber(upgradeCostTotal)} Mana</>
              : maxN === 0 ? 'Mana insuffisante' : `Améliorer — ${formatNumber(upgradeCostTotal)} Mana`
            }
          </button>
        </>
      )}
    </div>
  );
}
