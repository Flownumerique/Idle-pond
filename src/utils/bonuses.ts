import { RESEARCH } from '../data/research';
import { PEARL_UPGRADES } from '../data/pearlUpgrades';
import { PRESTIGE_UPGRADES } from '../data/prestigeUpgrades';

export interface ComputedBonuses {
  globalIncomeMult: number;
  pondCostMult: number;
  boostDurationMs: number;
  boostCost: number;
  boostMultiplier: number;       // x2 de base, peut monter à x3
  fishCostMult: number;
  offlineMult: number;
  milestoneLevelReduction: number;
  gemmeRewardMult: number;       // multiplicateur sur récompenses de succès
  passiveGemmesPerMin: number;   // gemmes générées passivement
  deepFishIncomeMult: number;    // multiplicateur pour poissons profondeur 4+
  // Effets prestige
  prestigePearlMult: number;     // multiplicateur sur les perles gagnées
  celestialCostMult: number;     // réduction du coût du Poisson Céleste
  prestigeStartingMana: number;  // Mana de départ après prestige
  prestigeStartingDepth: number; // Profondeur de départ après prestige
  prestigeStartingLevel: number; // Niveau de départ des poissons achetés
  prestigeKeepFishPercent: number; // % de poissons conservés après prestige
  prestigeGlobalIncomePercent: number; // bonus global permanent (prest_global_mult)
}

const BASE_BOOST_DURATION_MS = 5 * 60 * 1000;
const BASE_BOOST_COST = 10;

export const computeBonuses = (
  researchUnlocked: string[],
  pearlUpgradesUnlocked: string[],
  prestigeUpgradesUnlocked: string[] = []
): ComputedBonuses => {
  let globalIncomePercent = 0;
  let pondCostReductionPercent = 0;
  let boostDurationBonusMs = 0;
  let boostCostReduction = 0;
  let boostMultiplierBonus = 0;
  let fishCostReductionPercent = 0;
  let offlineMultPercent = 0;
  let milestoneLevelReduction = 0;
  let gemmeRewardMultPercent = 0;
  let passiveGemmesPerMin = 0;
  let deepFishIncomePercent = 0;

  for (const id of researchUnlocked) {
    const r = RESEARCH.find(x => x.id === id);
    if (!r) continue;
    if (r.effect.globalIncomePercent)       globalIncomePercent       += r.effect.globalIncomePercent;
    if (r.effect.pondCostReductionPercent)  pondCostReductionPercent  += r.effect.pondCostReductionPercent;
    if (r.effect.boostDurationBonusMs)      boostDurationBonusMs      += r.effect.boostDurationBonusMs;
    if (r.effect.boostCostReduction)        boostCostReduction        += r.effect.boostCostReduction;
    if (r.effect.boostMultiplierBonus)      boostMultiplierBonus      += r.effect.boostMultiplierBonus;
    if (r.effect.milestoneLevelReduction)   milestoneLevelReduction   += r.effect.milestoneLevelReduction;
    if (r.effect.gemmeRewardMultPercent)    gemmeRewardMultPercent    += r.effect.gemmeRewardMultPercent;
    if (r.effect.passiveGemmesPerMin)       passiveGemmesPerMin       += r.effect.passiveGemmesPerMin;
    if (r.effect.fishCostReductionPercent)  fishCostReductionPercent  += r.effect.fishCostReductionPercent;
    if (r.effect.deepFishIncomePercent)     deepFishIncomePercent     += r.effect.deepFishIncomePercent;
  }

  for (const id of pearlUpgradesUnlocked) {
    const p = PEARL_UPGRADES.find(x => x.id === id);
    if (!p) continue;
    if (p.effect.globalIncomePercent)      globalIncomePercent      += p.effect.globalIncomePercent;
    if (p.effect.fishCostReductionPercent) fishCostReductionPercent += p.effect.fishCostReductionPercent;
    if (p.effect.offlineMultPercent)       offlineMultPercent       += p.effect.offlineMultPercent;
  }

  // Améliorations de prestige
  let prestigePearlMultPercent = 0;
  let celestialCostMult = 1;
  let prestigeStartingMana = 10;
  let prestigeStartingDepth = 0;
  let prestigeStartingLevel = 1;
  let prestigeKeepFishPercent = 0;
  let prestigeGlobalIncomePercent = 0;

  for (const id of prestigeUpgradesUnlocked) {
    const p = PRESTIGE_UPGRADES.find(x => x.id === id);
    if (!p) continue;
    switch (id) {
      case 'prest_mana':         prestigeStartingMana = Math.max(prestigeStartingMana, 5_000); break;
      case 'prest_mana_max':     prestigeStartingMana = Math.max(prestigeStartingMana, 1_000_000); break;
      case 'prest_creusage':     /* handled inline in upgradePond */ break;
      case 'prest_level':        prestigeStartingLevel = Math.max(prestigeStartingLevel, 3); break;
      case 'prest_level_10':     prestigeStartingLevel = Math.max(prestigeStartingLevel, 10); break;
      case 'prest_pearl_bonus':  prestigePearlMultPercent += 30; break;
      case 'prest_pearl_bonus_2':prestigePearlMultPercent += 50; break;
      case 'prest_keep_fish':    prestigeKeepFishPercent += 10; break;
      case 'prest_depth_1':      prestigeStartingDepth = Math.max(prestigeStartingDepth, 1); break;
      case 'prest_depth_2':      prestigeStartingDepth = Math.max(prestigeStartingDepth, 2); break;
      case 'prest_celestial_cost': celestialCostMult = 0.4; break;
      case 'prest_global_mult':  prestigeGlobalIncomePercent += 100; break;
    }
  }

  globalIncomePercent += prestigeGlobalIncomePercent;

  return {
    globalIncomeMult: 1 + globalIncomePercent / 100,
    pondCostMult: Math.max(0.05, 1 - pondCostReductionPercent / 100),
    boostDurationMs: BASE_BOOST_DURATION_MS + boostDurationBonusMs,
    boostCost: Math.max(1, BASE_BOOST_COST - boostCostReduction),
    boostMultiplier: 2 + boostMultiplierBonus,
    fishCostMult: Math.max(0.05, 1 - fishCostReductionPercent / 100),
    offlineMult: 1 + offlineMultPercent / 100,
    milestoneLevelReduction,
    gemmeRewardMult: 1 + gemmeRewardMultPercent / 100,
    passiveGemmesPerMin,
    deepFishIncomeMult: 1 + deepFishIncomePercent / 100,
    prestigePearlMult: 1 + prestigePearlMultPercent / 100,
    celestialCostMult,
    prestigeStartingMana,
    prestigeStartingDepth,
    prestigeStartingLevel,
    prestigeKeepFishPercent,
    prestigeGlobalIncomePercent,
  };
};
