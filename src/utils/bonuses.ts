import { RESEARCH } from '../data/research';
import { PEARL_UPGRADES } from '../data/pearlUpgrades';

export interface ComputedBonuses {
  globalIncomeMult: number;       // ex: 1.15 = +15%
  pondCostMult: number;           // ex: 0.85 = -15%
  boostDurationMs: number;        // durée totale du boost
  boostCost: number;              // coût en gemmes
  fishCostMult: number;           // ex: 0.80 = -20%
  offlineMult: number;            // ex: 2.0 = ×2 hors-ligne
  milestoneLevelReduction: number;// niveaux en avance sur les jalons
}

const BASE_BOOST_DURATION_MS = 5 * 60 * 1000;
const BASE_BOOST_COST = 10;

export const computeBonuses = (
  researchUnlocked: string[],
  pearlUpgradesUnlocked: string[]
): ComputedBonuses => {
  let globalIncomePercent = 0;
  let pondCostReductionPercent = 0;
  let boostDurationBonusMs = 0;
  let boostCostReduction = 0;
  let fishCostReductionPercent = 0;
  let offlineMultPercent = 0;
  let milestoneLevelReduction = 0;

  for (const id of researchUnlocked) {
    const r = RESEARCH.find(x => x.id === id);
    if (!r) continue;
    if (r.effect.globalIncomePercent) globalIncomePercent += r.effect.globalIncomePercent;
    if (r.effect.pondCostReductionPercent) pondCostReductionPercent += r.effect.pondCostReductionPercent;
    if (r.effect.boostDurationBonusMs) boostDurationBonusMs += r.effect.boostDurationBonusMs;
    if (r.effect.boostCostReduction) boostCostReduction += r.effect.boostCostReduction;
    if (r.effect.milestoneLevelReduction) milestoneLevelReduction += r.effect.milestoneLevelReduction;
  }

  for (const id of pearlUpgradesUnlocked) {
    const p = PEARL_UPGRADES.find(x => x.id === id);
    if (!p) continue;
    if (p.effect.globalIncomePercent) globalIncomePercent += p.effect.globalIncomePercent;
    if (p.effect.fishCostReductionPercent) fishCostReductionPercent += p.effect.fishCostReductionPercent;
    if (p.effect.offlineMultPercent) offlineMultPercent += p.effect.offlineMultPercent;
  }

  return {
    globalIncomeMult: 1 + globalIncomePercent / 100,
    pondCostMult: Math.max(0.05, 1 - pondCostReductionPercent / 100),
    boostDurationMs: BASE_BOOST_DURATION_MS + boostDurationBonusMs,
    boostCost: Math.max(1, BASE_BOOST_COST - boostCostReduction),
    fishCostMult: Math.max(0.05, 1 - fishCostReductionPercent / 100),
    offlineMult: 1 + offlineMultPercent / 100,
    milestoneLevelReduction,
  };
};
