import Decimal from 'break_infinity.js';
import type { PoissonInstance } from '../store/useGameStore';
import type { ComputedBonuses } from './bonuses';
import { getSelfMilestoneMultiplier, getGlobalMultiplier, FISH_TYPES } from '../data/fishTypes';
import { ZONE_MASTERY_BONUS_PERCENT } from '../data/zones';

const DEEP_FISH_TYPES = new Set(
  FISH_TYPES.filter(f => f.requiredDepth >= 4).map(f => f.type)
);

const FISH_DEPTH = new Map(FISH_TYPES.map(f => [f.type, f.requiredDepth]));

export function computeIncomePerSec(
  poissons: PoissonInstance[],
  bonuses: ComputedBonuses,
  boostActiveUntil: number,
  masteredZones: number[] = []
): Decimal {
  if (poissons.length === 0) return new Decimal(0);

  let baseIncomePerSec = new Decimal(0);
  for (const fish of poissons) {
    const levelMult     = new Decimal(1.5).pow(fish.level - 1);
    const milestoneMult = getSelfMilestoneMultiplier(fish, bonuses.milestoneLevelReduction);
    const deepMult      = DEEP_FISH_TYPES.has(fish.type) ? bonuses.deepFishIncomeMult : 1;
    const masteryMult   = masteredZones.includes(FISH_DEPTH.get(fish.type) ?? -1)
      ? 1 + ZONE_MASTERY_BONUS_PERCENT / 100
      : 1;
    baseIncomePerSec = baseIncomePerSec.add(
      new Decimal(fish.baseIncome).mul(levelMult).mul(milestoneMult).mul(deepMult).mul(masteryMult)
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
