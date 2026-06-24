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
    <div className="lg-shop">
      {/* Poissons légendaires */}
      {legendaryFish.map(fish => {
        const unlocked = prestiges >= (fish.requiredPrestiges ?? 0) && pondDepth >= fish.requiredDepth;
        if (!unlocked) return null;
        const instance = poissons.find(f => f.type === fish.type) ?? null;
        const atMax = instance !== null && fish.maxOwned !== undefined;
        const buyCost = new Decimal(fish.baseCost).mul(bonuses.celestialCostMult);
        const canAffordBuy = !atMax && mana.gte(buyCost);

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
            variant="legendary"
          />
        );
      })}

      {/* Poissons normaux */}
      <div className="lg-shop-section">
        {normalFish.map(fish => {
          const unlocked = pondDepth >= fish.requiredDepth;
          const instance = poissons.find(f => f.type === fish.type) ?? null;
          const buyCost = new Decimal(fish.baseCost).mul(bonuses.fishCostMult);
          const canAffordBuy = instance === null && mana.gte(buyCost);

          if (!unlocked) {
            return (
              <div key={fish.type} className="lg-fish lg-fish--locked">
                <div className="lg-fish-head">
                  <div className="lg-fish-sprite">
                    {fish.sprite
                      ? <img src={fish.sprite} alt={fish.name} style={{ filter: 'grayscale(1)' }} />
                      : <span style={{ fontSize: 28, filter: 'grayscale(1)' }}>{fish.emoji}</span>}
                  </div>
                  <div className="lg-fish-info">
                    <div className="lg-fish-name">{fish.name} 🔒</div>
                    <div className="lg-fish-locked-note">
                      Profondeur Niv. {fish.requiredDepth} requise — {DEPTH_NAMES[fish.requiredDepth]}
                    </div>
                  </div>
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

  const cardClass = variant === 'legendary'
    ? 'lg-fish lg-fish--legend'
    : owned ? 'lg-fish lg-fish--owned' : 'lg-fish';

  return (
    <div className={cardClass}>
      {variant === 'legendary' && (
        <div className="lg-fish-legend-eyebrow">★ POISSON LÉGENDAIRE</div>
      )}

      <div className="lg-fish-head">
        <div className="lg-fish-sprite">
          {fish.sprite
            ? <img src={fish.sprite} alt={fish.name} />
            : <span style={{ fontSize: 28 }}>{fish.emoji}</span>}
        </div>
        <div className="lg-fish-info">
          <div className="lg-fish-row">
            <span className="lg-fish-name">{fish.name}</span>
            {owned && (
              <span className="lg-fish-badge">{isMaxLevel ? 'MAX' : `NIV. ${instance.level}`}</span>
            )}
          </div>
          {owned
            ? <div className="lg-fish-income">{formatNumber(currentIncome)} Mana/s</div>
            : <div className="lg-fish-desc">{fish.desc}</div>}
        </div>
      </div>

      {owned && (
        <div className="lg-fish-miles">
          {MILESTONE_LEVELS.map(lvl => {
            const milestone = fish.milestones.find(m => m.level === lvl);
            const effectiveLevel = Math.max(1, lvl - bonuses.milestoneLevelReduction);
            const reached = instance.level >= effectiveLevel;
            const isNext = nextMilestone?.level === lvl;
            return (
              <div
                key={lvl}
                className={`lg-fish-chip${reached ? ' hit' : isNext ? ' next' : ''}`}
                title={milestone
                  ? `Niv. ${effectiveLevel} — ${milestone.label} (${milestoneBonus(milestone.selfMultiplier, milestone.globalBonus)})`
                  : `Niv. ${effectiveLevel}`}
              >{effectiveLevel}</div>
            );
          })}
        </div>
      )}

      {!owned ? (
        <button className="cbtn cbtn--coral" onClick={onBuy} disabled={!canAffordBuy}>
          Acheter — {formatNumber(buyCost)} Mana
        </button>
      ) : isMaxLevel ? (
        <div className="lg-fish-max">✦ Niveau maximum atteint</div>
      ) : (
        <>
          <div className="lg-fish-qty">
            {UPGRADE_QTYS.map(q => (
              <button
                key={q}
                className={upgradeQty === q ? 'active' : ''}
                onClick={() => setUpgradeQty(q)}
              >{q === 'max' ? 'Max' : `×${q}`}</button>
            ))}
          </div>
          <button
            className={`cbtn ${variant === 'legendary' ? 'cbtn--gold' : 'cbtn--violet'}`}
            onClick={() => onUpgrade(upgradeQty)}
            disabled={!canAffordUpgrade}
          >
            {canAffordUpgrade
              ? <>Améliorer {upgradeQty === 'max' ? `×${maxN}` : upgradeCount > 0 ? `×${upgradeCount}` : ''} — {formatNumber(upgradeCostTotal)} Mana</>
              : maxN === 0 ? 'Mana insuffisante' : `Améliorer — ${formatNumber(upgradeCostTotal)} Mana`}
          </button>
        </>
      )}
    </div>
  );
}
