import { useGameStore } from '../store/useGameStore';
import Decimal from 'break_infinity.js';

export class GameLoopManager {
  private static instance: GameLoopManager;
  private intervalId: number | null = null;
  private readonly TICK_RATE_MS = 100;

  private constructor() {}

  public static getInstance(): GameLoopManager {
    if (!GameLoopManager.instance) {
      GameLoopManager.instance = new GameLoopManager();
    }
    return GameLoopManager.instance;
  }

  public start() {
    if (this.intervalId !== null) return;

    let lastTick = Date.now();

    this.intervalId = window.setInterval(() => {
      const now = Date.now();
      const deltaMs = now - lastTick;
      lastTick = now;

      this.tick(deltaMs);
    }, this.TICK_RATE_MS);
  }

  public stop() {
    if (this.intervalId !== null) {
      window.clearInterval(this.intervalId);
      this.intervalId = null;
    }
  }

  private tick(deltaMs: number) {
    const state = useGameStore.getState();
    const poissons = state.poissons;

    let baseIncomePerSec = new Decimal(0);
    if (poissons.length > 0) {
      // Calculate total base income per second
      for (const fish of poissons) {
        // Income formula: baseIncome * 1.5 ^ (level - 1)
        const multiplier = new Decimal(1.5).pow(fish.level - 1);
        const fishIncome = new Decimal(fish.baseIncome).mul(multiplier);
        baseIncomePerSec = baseIncomePerSec.add(fishIncome);
      }

      // Apply boost if active
      let finalIncomePerSec = baseIncomePerSec;
      if (state.boostActiveUntil > Date.now()) {
        finalIncomePerSec = finalIncomePerSec.mul(2);
      }

      // Convert income per second to income per tick (deltaMs)
      const incomeThisTick = finalIncomePerSec.mul(deltaMs / 1000);

      if (incomeThisTick.gt(0)) {
        state.addMana(incomeThisTick);
      }
    }

    // Periodically update lastSaveTime
    state.updateLastSaveTime();
  }
}
