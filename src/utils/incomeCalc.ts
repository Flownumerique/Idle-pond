import Decimal from 'break_infinity.js';
import type { PoissonInstance } from '../store/useGameStore';
import type { ComputedBonuses } from './bonuses';
import { getSelfMilestoneMultiplier, getGlobalMultiplier, FISH_TYPES } from '../data/fishTypes';

const DEEP_FISH_TYPES = new Set(
  FISH_TYPES.filter(f => f.requiredDepth >= 4).map(f => f.type)
);

export function computeIncomePerSec(
  poissons: PoissonInstance[],
  bonuses: ComputedBonuses,
  boostActiveUntil: number
): Decimal {
  if (poissons.length === 0) return new Decimal(0);

  let baseIncomePerSec = new Decimal(0);
  for (const fish of poissons) {
    const levelMult     = new Decimal(1.5).pow(fish.level - 1);
    const milestoneMult = getSelfMilestoneMultiplier(fish, bonuses.milestoneLevelReduction);
    const deepMult      = DEEP_FISH_TYPES.has(fish.type) ? bonuses.deepFishIncomeMult : 1;
    baseIncomePerSec = baseIncomePerSec.add(
      new Decimal(fish.baseIncome).mul(levelMult).mul(milestoneMult).mul(deepMult)
    );
  }

  const milestoneGlobalMult = getGlobalMultiplier(poissons, bonuses.milestoneLevelReduction);
  let finalIncomePerSec = baseIncomePerSec
    .mul(milestoneGlobalMult)
    .mul(bonuses.globalIncomeMult);

  if (boostActiveUntil > Date.now()) {
    finalIncomePerSec = finalIncomePerSec.mul(bonuses.boostMultiplier);
  }

  return finalIncomePerSec;
}
