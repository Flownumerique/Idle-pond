import { useGameStore } from '../store/useGameStore';
import { getSelfMilestoneMultiplier, getGlobalMultiplier } from '../data/fishTypes';
import { computeBonuses } from '../utils/bonuses';
import Decimal from 'break_infinity.js';

export class OfflineManager {
  private static instance: OfflineManager;
  private readonly MAX_OFFLINE_MS = 24 * 60 * 60 * 1000;

  private constructor() {}

  public static getInstance(): OfflineManager {
    if (!OfflineManager.instance) {
      OfflineManager.instance = new OfflineManager();
    }
    return OfflineManager.instance;
  }

  public calculateOfflineGain() {
    const state = useGameStore.getState();
    const { poissons, lastSaveTime, researchUnlocked, pearlUpgradesUnlocked } = state;

    if (poissons.length === 0 || !lastSaveTime) return;

    const now = Date.now();
    const offlineDurationMs = Math.min(now - lastSaveTime, this.MAX_OFFLINE_MS);
    if (offlineDurationMs < 60 * 1000) return;

    const bonuses = computeBonuses(researchUnlocked, pearlUpgradesUnlocked);

    let baseIncomePerSec = new Decimal(0);
    for (const fish of poissons) {
      const levelMult = new Decimal(1.5).pow(fish.level - 1);
      const milestoneMult = getSelfMilestoneMultiplier(fish, bonuses.milestoneLevelReduction);
      baseIncomePerSec = baseIncomePerSec.add(
        new Decimal(fish.baseIncome).mul(levelMult).mul(milestoneMult)
      );
    }

    const milestoneGlobalMult = getGlobalMultiplier(poissons, bonuses.milestoneLevelReduction);
    baseIncomePerSec = baseIncomePerSec
      .mul(milestoneGlobalMult)
      .mul(bonuses.globalIncomeMult)
      .mul(bonuses.offlineMult);

    const boostEndTime = state.boostActiveUntil;
    let offlineMana = new Decimal(0);

    if (boostEndTime > lastSaveTime) {
      const boostedMs = Math.min(boostEndTime, now) - lastSaveTime;
      offlineMana = offlineMana.add(baseIncomePerSec.mul(2).mul(boostedMs / 1000));
      const unboostedMs = offlineDurationMs - boostedMs;
      if (unboostedMs > 0) {
        offlineMana = offlineMana.add(baseIncomePerSec.mul(unboostedMs / 1000));
      }
    } else {
      offlineMana = baseIncomePerSec.mul(offlineDurationMs / 1000);
    }

    if (offlineMana.gt(0)) {
      state.addMana(offlineMana);
      alert(`Bienvenue de retour ! Absent ${Math.round(offlineDurationMs / 60000)} min → +${offlineMana.toFixed(0)} Mana.`);
    }

    state.updateLastSaveTime();
  }
}
