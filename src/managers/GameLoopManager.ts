import { useGameStore } from '../store/useGameStore';
import { getSelfMilestoneMultiplier, getGlobalMultiplier } from '../data/fishTypes';
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
    const { poissons } = state;

    if (poissons.length > 0) {
      let baseIncomePerSec = new Decimal(0);

      for (const fish of poissons) {
        const levelMult = new Decimal(1.5).pow(fish.level - 1);
        const milestoneMult = getSelfMilestoneMultiplier(fish);
        const fishIncome = new Decimal(fish.baseIncome).mul(levelMult).mul(milestoneMult);
        baseIncomePerSec = baseIncomePerSec.add(fishIncome);
      }

      // Multiplicateur global des jalons
      const globalMult = getGlobalMultiplier(poissons);
      let finalIncomePerSec = baseIncomePerSec.mul(globalMult);

      // Boost temporaire x2
      if (state.boostActiveUntil > Date.now()) {
        finalIncomePerSec = finalIncomePerSec.mul(2);
      }

      const incomeThisTick = finalIncomePerSec.mul(deltaMs / 1000);
      if (incomeThisTick.gt(0)) state.addMana(incomeThisTick);
    }

    state.checkAchievements();
    state.updateLastSaveTime();
  }
}
