import { useGameStore } from '../store/useGameStore';
import Decimal from 'break_infinity.js';

export class OfflineManager {
  private static instance: OfflineManager;
  private readonly MAX_OFFLINE_MS = 24 * 60 * 60 * 1000; // 24 hours max offline gain

  private constructor() {}

  public static getInstance(): OfflineManager {
    if (!OfflineManager.instance) {
      OfflineManager.instance = new OfflineManager();
    }
    return OfflineManager.instance;
  }

  public calculateOfflineGain() {
    const state = useGameStore.getState();
    const poissons = state.poissons;
    const lastSaveTime = state.lastSaveTime;

    if (poissons.length === 0 || !lastSaveTime) return;

    const now = Date.now();
    const offlineDurationMs = Math.min(now - lastSaveTime, this.MAX_OFFLINE_MS);

    if (offlineDurationMs < 60 * 1000) return; // Less than 1 minute, don't calculate

    // Base income calculation
    let baseIncomePerSec = new Decimal(0);
    for (const fish of poissons) {
      const multiplier = new Decimal(1.5).pow(fish.level - 1);
      const fishIncome = new Decimal(fish.baseIncome).mul(multiplier);
      baseIncomePerSec = baseIncomePerSec.add(fishIncome);
    }

    const boostEndTime = state.boostActiveUntil;
    let offlineMana = new Decimal(0);

    if (boostEndTime > lastSaveTime) {
      // Boost was active during some or all of the offline time
      const boostedTimeMs = Math.min(boostEndTime, now) - lastSaveTime;
      const boostedTimeSec = boostedTimeMs / 1000;
      const boostedIncome = baseIncomePerSec.mul(2).mul(boostedTimeSec);

      const unboostedTimeMs = offlineDurationMs - boostedTimeMs;
      if (unboostedTimeMs > 0) {
        const unboostedTimeSec = unboostedTimeMs / 1000;
        const unboostedIncome = baseIncomePerSec.mul(unboostedTimeSec);
        offlineMana = boostedIncome.add(unboostedIncome);
      } else {
        offlineMana = boostedIncome;
      }
    } else {
      // No boost active offline
      const offlineSec = offlineDurationMs / 1000;
      offlineMana = baseIncomePerSec.mul(offlineSec);
    }

    if (offlineMana.gt(0)) {
      state.addMana(offlineMana);
      alert(`Bienvenue de retour ! Vous étiez absent pendant ${Math.round(offlineDurationMs / 60000)} minutes et avez récolté ${offlineMana.toString()} Mana.`);
    }

    state.updateLastSaveTime();
  }
}
